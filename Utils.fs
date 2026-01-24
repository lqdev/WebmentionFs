namespace WebmentionFs

/// <summary>
/// Utility functions for HTTP operations, form parsing, and HTML document processing.
/// This module provides low-level primitives used by webmention services.
/// </summary>
module Utils = 

    open System
    open System.Net.Http
    open System.Net.Http.Headers
    open Microsoft.AspNetCore.Http
    open FSharp.Data
    open Constants

    /// <summary>
    /// Extracts source and target URLs from an HTTP form body.
    /// </summary>
    /// <param name="req">The HTTP request containing form data.</param>
    /// <returns>ParseSuccess with UrlData if parsing succeeds, ParseError with error message if it fails.</returns>
    /// <remarks>
    /// Expects form fields named "source" and "target" containing valid URI strings.
    /// </remarks>
    let getSourceAndTargetUrlsFromFormBody (req:HttpRequest) = 
        try
            let source = req.Form[FormFields.source].ToString() |> Uri
            let target = req.Form[FormFields.target].ToString() |> Uri
            ParseSuccess { Source=source; Target=target }
        with
            | ex -> ParseError $"{ex}" 

    /// <summary>
    /// Sends an HTTP HEAD request to retrieve document headers without downloading the body.
    /// </summary>
    /// <param name="uri">The URI to send the HEAD request to.</param>
    /// <returns>A task containing the HTTP response message.</returns>
    /// <remarks>
    /// Useful for checking document status and discovering Link headers without transferring the full content.
    /// </remarks>
    let getDocumentHeadersAsync (uri:Uri) = 
        task {
            use client = new HttpClient()
            let reqMessage = new HttpRequestMessage(new HttpMethod(HttpMethod.Head), uri)
            let! response = client.SendAsync(reqMessage)
            return response
        }

    /// <summary>
    /// Sends an HTTP GET request to retrieve HTML document content.
    /// </summary>
    /// <param name="uri">The URI to send the GET request to.</param>
    /// <returns>A task containing the HTTP response message with document content.</returns>
    /// <remarks>
    /// Configures the request to only accept "text/html" content type.
    /// Used for fetching HTML documents to parse for webmention endpoints or mentions.
    /// </remarks>
    let getDocumentContentAsync (uri:Uri) = 
        task {
            // Prepare HTTP GET request
            use client = new HttpClient()
            let reqMessage = new HttpRequestMessage(new HttpMethod(HttpMethod.Get), uri)
            reqMessage.Headers.Accept.Clear()

            // Only accept text/html content
            reqMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Http.htmlContentType))

            //Send HTTP request
            return! client.SendAsync(reqMessage)
        }

    /// <summary>
    /// Uses CSS selectors to find links in an HTML document that match the target URL.
    /// </summary>
    /// <param name="doc">The parsed HTML document to search.</param>
    /// <param name="selector">The CSS selector to use for finding elements.</param>
    /// <param name="target">The target URI to match against (currently unused in implementation).</param>
    /// <returns>A list of href attribute values from matching elements.</returns>
    /// <remarks>
    /// Extracts href attributes from all elements matching the provided CSS selector.
    /// Commonly used to find webmention endpoints and annotated mentions.
    /// </remarks>
    let getUrlFromSourceDocument (doc:HtmlDocument) (selector:string) (target:Uri) = 
        doc.CssSelect(selector)
        |> List.map(fun x -> x.AttributeValue("href"))                