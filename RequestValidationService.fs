namespace WebmentionFs.Services

open System
open System.Net.Http
open Microsoft.AspNetCore.Http
open WebmentionFs
open WebmentionFs.Utils

/// <summary>
/// Service for validating incoming webmention requests.
/// Ensures that webmention requests meet W3C specification requirements including
/// protocol validation, target URL ownership verification, and preventing self-mentions.
/// </summary>
/// <param name="hostList">Array of domain names owned by the service to validate target URLs against.</param>
type RequestValidationService (hostList: string array) =

    /// <summary>
    /// Checks whether a URL belongs to one of the configured domains.
    /// </summary>
    /// <param name="uri">The URI to check.</param>
    /// <param name="hostList">Array of valid host names.</param>
    /// <returns>True if the URI's host is in the list, false otherwise.</returns>
    let isUrlMine (uri:Uri) (hostList:string array)= 
        hostList |> Array.contains uri.Host

    /// <summary>
    /// Validates that both source and target URLs use HTTP or HTTPS protocols.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <returns>RequestSuccess if both protocols are valid, RequestError with descriptive message otherwise.</returns>
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

    /// <summary>
    /// Validates that source and target URLs are different to prevent self-referential webmentions.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <returns>RequestSuccess if URLs differ, RequestError if they are the same.</returns>
    let isSameUrl (result:RequestValidationResult) =
        match result with 
        | RequestSuccess r -> 
            match r.Source.Equals(r.Target) with
            | true -> RequestError "Source and target urls are the same"
            | false -> RequestSuccess r
        | RequestError e -> RequestError e

    /// <summary>
    /// Validates that the target URL is owned by this service and is accessible.
    /// Checks domain ownership and that the resource returns a successful HTTP status code.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <returns>A task with RequestSuccess if target is valid and accessible, RequestError otherwise.</returns>
    let isTargetUrlValidAsync (result:RequestValidationResult) =
        match result with 
        | RequestSuccess r -> 
            task {
                let targetIsMine = isUrlMine r.Target hostList
                let! targetDocResponse = getDocumentHeadersAsync r.Target

                let isTargetValid = 
                    targetIsMine && targetDocResponse.IsSuccessStatusCode

                return 
                    match isTargetValid with
                    | true -> RequestSuccess r
                    | false -> RequestError "Target is not a valid resource"
            }
        | RequestError e -> task { return RequestError e}

    /// <summary>
    /// Composes the validation pipeline using function composition.
    /// Validates protocol, checks for self-mention, and verifies target URL.
    /// </summary>
    let validateAsync = 
        isProtocolValid >> isSameUrl >> isTargetUrlValidAsync

    /// <summary>
    /// Validates a webmention request from HTTP form data.
    /// </summary>
    /// <param name="req">The HTTP request containing form data with source and target URLs.</param>
    /// <returns>A task with RequestSuccess containing validated URL data, or RequestError with error message.</returns>
    member _.ValidateAsync (req:HttpRequest) =
        
        let parseResults = getSourceAndTargetUrlsFromFormBody req

        match parseResults with
        | ParseSuccess s -> 
            task { 
                let! validationResult = RequestSuccess s |> validateAsync
                return validationResult
            } 
        | ParseError e -> task { return RequestError e }

    /// <summary>
    /// Validates a webmention request from structured URL data.
    /// </summary>
    /// <param name="data">The UrlData containing source and target URLs.</param>
    /// <returns>A task with RequestSuccess containing validated URL data, or RequestError with error message.</returns>
    member _.ValidateAsync (data:UrlData) =
        
        task { 
            let! validationResult = RequestSuccess data |> validateAsync
            return validationResult
        } 
