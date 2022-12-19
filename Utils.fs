namespace WebmentionFs

module Utils = 

    open System
    open System.Net.Http
    open System.Net.Http.Headers
    open Microsoft.AspNetCore.Http
    open FSharp.Data

    // Process HttpRequest and extract source and target urls from form body
    let getSourceAndTargetUrlsFromFormBody (req:HttpRequest) = 
        try
            let source = req.Form["source"].ToString() |> Uri
            let target = req.Form["target"].ToString() |> Uri
            ParseSuccess { Source=source; Target=target }
        with
            | ex -> ParseError $"{ex}" 

    // Send HTTP HEAD request to HTML document 
    let getDocumentHeadersAsync (uri:Uri) = 
        task {
            use client = new HttpClient()
            let reqMessage = new HttpRequestMessage(new HttpMethod(HttpMethod.Head), uri)
            let! response = client.SendAsync(reqMessage)
            return response
        }

    // Send HTTP GET request to HTML document
    let getDocumentContentAsync (uri:Uri) = 
        task {
            // Prepare HTTP GET request
            use client = new HttpClient()
            let reqMessage = new HttpRequestMessage(new HttpMethod(HttpMethod.Get), uri)
            reqMessage.Headers.Accept.Clear()

            // Only accept text/html content
            reqMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"))

            //Send HTTP request
            return! client.SendAsync(reqMessage)
        }

    // Use CSS selectors to find target link in source document
    let getUrlFromSourceDocument (doc:HtmlDocument) (selector:string) (target:Uri) = 
        doc.CssSelect(selector)
        |> List.map(fun x -> x.AttributeValue("href"))                