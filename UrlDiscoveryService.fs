namespace WebmentionFs.Services

open System
open Microsoft.AspNetCore.Http
open FSharp.Data
open WebmentionFs
open WebmentionFs.Constants
open Utils

/// <summary>
/// Service for discovering webmention endpoints from target URLs.
/// Implements the W3C Webmention specification's endpoint discovery methods:
/// HTTP Link header, HTML link elements, and HTML anchor elements.
/// </summary>
type UrlDiscoveryService () = 

    /// <summary>
    /// Extracts webmention endpoint URL from HTML using a CSS selector.
    /// </summary>
    /// <param name="urlData">The source and target URL data.</param>
    /// <param name="cssSelector">CSS selector to find the endpoint element.</param>
    /// <returns>A task with DiscoverySuccess containing endpoint, or DiscoveryError if not found.</returns>
    let getEndpointFromHref (urlData:UrlData) (cssSelector:string) = 
        task {
            let! docResponse = getDocumentContentAsync urlData.Target

            let! body = docResponse.Content.ReadAsStringAsync()
            
            let docContent = HtmlDocument.Parse(body)

            let urls = getUrlFromSourceDocument docContent cssSelector urlData.Target

            return 
                match urls.IsEmpty with
                | true -> DiscoveryError $"Could not find endpoint in {cssSelector}"
                | false -> 
                    let webmentionUrl = urls |> List.head
                    DiscoverySuccess { Endpoint = new Uri(webmentionUrl); RequestBody = urlData }             
        }

    /// <summary>
    /// Discovers webmention endpoint from HTTP Link header.
    /// Parses the Link header for rel="webmention" and extracts the URL.
    /// </summary>
    /// <param name="data">The source and target URL data.</param>
    /// <returns>A task with DiscoverySuccess if Link header found, or DiscoveryError.</returns>
    let discoverUrlInHeaderAsync (data:UrlData) = 
        task {
            let! sourceDocResponse = getDocumentHeadersAsync data.Target
            
            // Get request headers
            let responseHeaders = 
                [
                    for header in sourceDocResponse.Headers do
                        header.Key.ToLower(), header.Value
                ]

            // Look for webmention header
            try
                // Find "link" header that contains "webmention"
                let webmentionHeader =
                    responseHeaders
                    |> Seq.filter(fun (k,_) -> k = Http.linkHeader)
                    |> Seq.map(fun (_,v) -> v |> Seq.filter(fun header -> header.Contains(Http.webmentionRel)))
                    |> Seq.head
                    |> List.ofSeq
                    |> List.head

                // Get first part of "link" header
                let webmentionUrl = 
                    webmentionHeader.Split(';')
                    |> Array.head

                // Remove angle brackets from URL
                let sanitizedWebmentionUrl = 
                    webmentionUrl
                        .Replace("<","")
                        .Replace(">","")
                        .Trim()

                return DiscoverySuccess { Endpoint = new Uri(sanitizedWebmentionUrl) ; RequestBody = data }
            with
                | ex -> return DiscoveryError $"{ex}"                 
        }

    /// <summary>
    /// Discovers webmention endpoint from HTML link element with rel="webmention".
    /// </summary>
    /// <param name="data">The source and target URL data.</param>
    /// <returns>A task with DiscoverySuccess if link element found, or DiscoveryError.</returns>
    let discoverUrlInLinkTagAsync (data:UrlData) = 
        try
            task {
                return! getEndpointFromHref data EndpointSelectors.linkElement
            }
        with
            | ex -> task { return DiscoveryError $"{ex}" }         

    /// <summary>
    /// Discovers webmention endpoint from HTML anchor element with rel="webmention".
    /// </summary>
    /// <param name="data">The source and target URL data.</param>
    /// <returns>A task with DiscoverySuccess if anchor element found, or DiscoveryError.</returns>
    let discoverUrlInAnchorTagAsync (data: UrlData) = 
        try
            task {
                return! getEndpointFromHref data EndpointSelectors.anchorElement
            }
        with
            | ex -> task { return DiscoveryError $"{ex}" }

    /// <summary>
    /// Constructs a complete URL for the webmention endpoint.
    /// Handles relative URLs by combining with the target's authority.
    /// </summary>
    /// <param name="data">The endpoint URL data to construct.</param>
    /// <returns>EndpointUrlData with a properly constructed absolute URL.</returns>
    let constructUrl (data: EndpointUrlData) = 

        let scheme = data.Endpoint.Scheme
        let authority = data.RequestBody.Target.GetLeftPart(UriPartial.Authority)

        let constructedUrl = 
            match scheme.Contains(Http.httpScheme) with
            | true -> data.Endpoint
            | false -> 
                let noQueryUrl = 
                    data.Endpoint.OriginalString.Split("?")
                    |> Array.head

                new Uri($"{authority}{noQueryUrl}")

        { data with Endpoint = constructedUrl }

    /// <summary>
    /// Discovers webmention endpoint using all available methods.
    /// Tries HTTP headers, link elements, and anchor elements sequentially.
    /// Returns the first successfully discovered endpoint.
    /// </summary>
    /// <param name="data">The source and target URL data.</param>
    /// <returns>A task with DiscoverySuccess if any method succeeds, or DiscoveryError if all fail.</returns>
    let discoverUrlAsync (data: UrlData) = 
        task {
            let! headerResult = discoverUrlInHeaderAsync data
            let! linkResult = discoverUrlInLinkTagAsync data
            let! anchorResult = discoverUrlInAnchorTagAsync data

            let discoveryResults = 
                [headerResult; linkResult; anchorResult]                
                |> List.choose(fun r -> 
                    match r with
                    | DiscoverySuccess d -> Some d 
                    | DiscoveryError _ -> None)

            let result = 
                match discoveryResults.IsEmpty with
                | true -> DiscoveryError ErrorMessages.noEndpoint
                | false -> 
                    discoveryResults 
                    |> List.head 
                    |> constructUrl 
                    |> DiscoverySuccess

            return result
        }

    /// <summary>
    /// Helper to discover endpoint from form parse result.
    /// </summary>
    /// <param name="result">The form parse result.</param>
    /// <returns>A task with discovery result.</returns>
    let discoverUrlFromFormAsync (result:FormParseResult) = 
        match result with
        | ParseSuccess s -> discoverUrlAsync s
        | ParseError e -> task { return DiscoveryError e }

    /// <summary>
    /// Discovers webmention endpoint from HTTP request form data.
    /// </summary>
    /// <param name="req">HTTP request containing source and target in form body.</param>
    /// <returns>A task with DiscoverySuccess containing endpoint data, or DiscoveryError.</returns>
    member x.DiscoverEndpointAsync (req: HttpRequest) = 
        task {
            let formParseResult = getSourceAndTargetUrlsFromFormBody req

            let! discoveryResult = discoverUrlFromFormAsync formParseResult

            return discoveryResult
        }

    /// <summary>
    /// Discovers webmention endpoint from structured URL data.
    /// </summary>
    /// <param name="data">The UrlData containing source and target URLs.</param>
    /// <returns>A task with DiscoverySuccess containing endpoint data, or DiscoveryError.</returns>
    member x.DiscoverEndpointAsync (data: UrlData) = 
        task {
            let! discoveryResult = discoverUrlAsync data
            return discoveryResult
        }