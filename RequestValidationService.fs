namespace WebmentionFs.Services

open System
open System.Net.Http
open Microsoft.AspNetCore.Http
open WebmentionFs

type RequestValidationService (hostList: string array) = 

    // Process HttpRequest and extract source and target urls from form body
    let getSourceAndTargetUrlsFromFormBody (req:HttpRequest) = 
        try
            let source = req.Form["source"].ToString() |> Uri
            let target = req.Form["target"].ToString() |> Uri
            RequestSuccess { Source=source; Target=target }
        with
            | ex -> RequestError $"{ex}"

    // Send HTTP HEAD request to HTML document 
    let getDocumentAsync (uri:Uri) = 
        task {
            use client = new HttpClient()
            let reqMessage = new HttpRequestMessage(HttpMethod.Head, uri)
            let! response = client.SendAsync(reqMessage)
            return response
        }

    // Check whether a URL is one of the domains I own
    let isUrlMine (uri:Uri) (hostList:string array)= 
        hostList |> Array.contains uri.Host

    // Check that the source and target URL protocols are HTTP or HTTPS
    let isProtocolValid (result:RequestValidationResult) = 
        match result with
        | RequestSuccess r -> 
            let sourceProtocol = r.Source.Scheme.Equals("http") || r.Source.Scheme.Equals("https")
            let targetProtocol = r.Target.Scheme.Equals("http") || r.Target.Scheme.Equals("https")

            let protocolResult = 
                match sourceProtocol,targetProtocol with
                | true, true -> RequestSuccess r
                | true, false -> RequestError "Invalid target Protocol"
                | false, true -> RequestError "Invalid source protocol"
                | false,false -> RequestError "Invalid source and target protocol"

            protocolResult
        | RequestError e -> RequestError e

    // Compare source and target URLs to check whether they're the same
    let isSameUrl (result:RequestValidationResult) = 
        match result with 
        | RequestSuccess r -> 
            match r.Source.Equals(r.Target) with
            | true -> RequestError "Sourrce and target urls are the same"
            | false -> RequestSuccess r
        | RequestError e -> RequestError e

    // Check whether target URL is valid. 
    // Valid in this case means, I own the domain and the document doesn't return a 400 or 500 HTML status code
    let isTargetUrlValidAsync (result:RequestValidationResult) = 
        match result with 
        | RequestSuccess r -> 
            task {
                let targetIsMine = isUrlMine r.Target hostList
                let! targetDocResponse = getDocumentAsync r.Target

                let isTargetValid = 
                    targetIsMine && targetDocResponse.IsSuccessStatusCode

                return 
                    match isTargetValid with
                    | true -> RequestSuccess r
                    | false -> RequestError "Target is not a valid resource"
            }
        | RequestError e -> task { return RequestError e}

    // Compose validation pipeline 
    let validateAsync = 
        isProtocolValid >> isSameUrl >> isTargetUrlValidAsync

    member _.ValidateAsync (req:HttpRequest) = 
        task {
            return! 
                req 
                |> getSourceAndTargetUrlsFromFormBody
                |> validateAsync
        }
