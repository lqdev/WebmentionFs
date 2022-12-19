namespace WebmentionFs.Services

open System
open Microsoft.AspNetCore.Http
open FSharp.Data
open WebmentionFs
open Utils

type UrlDiscoveryService () = 

    let getEndpointFromHref (urlData:UrlData) (cssSelector:string) = 
        task {
            let! docResponse = getDocumentContentAsync urlData.Source

            let! docContent = 
                match docResponse.IsSuccessStatusCode with
                | true -> 
                    task {
                        let! content = docResponse.Content.ReadAsStringAsync()
                        return HtmlDocument.Parse(content)
                    }
                | false -> 
                    task { return HtmlDocument.Parse("<html></html>") }

            let webmentionUrl = 
                getUrlFromSourceDocument docContent cssSelector urlData.Target
                |> List.head

            return webmentionUrl
        }

    let discoverUrlInHeaderAsync (data:UrlData) = 
        task {
            let! sourceDocResponse = getDocumentHeadersAsync data.Source
            
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
                    |> Seq.filter(fun (k,_) -> k = "link")
                    |> Seq.map(fun (_,v) -> v |> Seq.filter(fun header -> header.Contains("webmention")))
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
                | ex -> return DiscoveryError "${ex}"                 
        }          

    let discoverUrlInLinkTagAsync (data:UrlData) = 
        try
            task {
                let! webmentionUrl = getEndpointFromHref data "link[rel='webmention']"

                return DiscoverySuccess { Endpoint = new Uri(webmentionUrl); RequestBody = data }
            }
        with
            | ex -> task { return DiscoveryError $"{ex}" }         
        
    let discoverUrlInAnchorTagAsync (data: UrlData) = 
        try
            task {
                let! webmentionUrl = getEndpointFromHref data "a[rel='webmention']"
                return DiscoverySuccess { Endpoint = new Uri(webmentionUrl); RequestBody = data }
            }
        with
            | ex -> task { return DiscoveryError $"{ex}" }        

    let constructUrl (data: EndpointUrlData) = 
        
        let authority = data.RequestBody.Target.GetLeftPart(UriPartial.Authority)

        let constructedUrl = 
            match data.Endpoint.Scheme.Contains("http") with
            | true -> data.Endpoint
            | false -> 
                let noQueryUrl = 
                    data.Endpoint.OriginalString.Split("?")
                    |> Array.head

                new Uri($"{authority}{noQueryUrl}")

        { data with Endpoint = constructedUrl }

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
                | true -> DiscoveryError "No webmention endpoint available"
                | false -> 
                    discoveryResults 
                    |> List.head 
                    |> constructUrl 
                    |> DiscoverySuccess

            return result
        }

    let discoverUrlFromFormAsync (result:FormParseResult) = 
        match result with
        | ParseSuccess s -> discoverUrlAsync s
        | ParseError e -> task { return DiscoveryError e }

    member x.DiscoverEndpointAsync (req: HttpRequest) = 
        task {
            let formParseResult = getSourceAndTargetUrlsFromFormBody req

            let! discoveryResult = discoverUrlFromFormAsync formParseResult

            return discoveryResult
        }

    member x.DiscoverEndpointAsync (data: UrlData) = 
        task {
            let! discoveryResult = discoverUrlAsync data
            return discoveryResult
        }