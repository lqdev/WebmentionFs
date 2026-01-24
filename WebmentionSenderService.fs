namespace WebmentionFs.Services

open System
open System.Net
open System.Net.Http
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open WebmentionFs
open WebmentionFs.Services

/// <summary>
/// Interface for sending webmentions to discovered endpoints.
/// </summary>
/// <typeparam name="'a">The type of data returned on successful send.</typeparam>
type IWebmentionSender<'a> =
    /// <summary>Sends a webmention from HTTP request form data.</summary>
    abstract member SendAsync : req:HttpRequest -> Task<ValidationResult<'a>>
    /// <summary>Sends a webmention from structured URL data.</summary>
    abstract member SendAsync : data:UrlData -> Task<ValidationResult<'a>>

/// <summary>
/// Service for sending webmentions to discovered endpoints.
/// Combines endpoint discovery with HTTP POST delivery of webmention notifications.
/// </summary>
/// <param name="discoveryService">The UrlDiscoveryService used to find webmention endpoints.</param>
type WebmentionSenderService (discoveryService: UrlDiscoveryService) = 
    
    /// <summary>
    /// Sends a webmention POST request to the discovered endpoint.
    /// </summary>
    /// <param name="data">The endpoint URL and request body data.</param>
    /// <returns>A task with the HTTP response from the endpoint.</returns>
    let sendMentionAsync (data: EndpointUrlData) = 
        task {
            use client = new HttpClient()

            // Prepare webmention request data
            let reqData = 
                dict [
                    ("source", data.RequestBody.Source.OriginalString)
                    ("target", data.RequestBody.Target.OriginalString)
                ]

            let content = new FormUrlEncodedContent(reqData)
            
            let! response = client.PostAsync(data.Endpoint.OriginalString, content)
            return response
        }

    /// <summary>
    /// Processes discovery results and sends the webmention if endpoint was found.
    /// </summary>
    /// <param name="result">The discovery result.</param>
    /// <returns>A task with ValidationSuccess if sent successfully, ValidationError otherwise.</returns>
    let processDiscoveryResults (result:DiscoveryResult) = 
        match result with
        | DiscoverySuccess s -> 
            task {
                let! response =  sendMentionAsync s
                match response.IsSuccessStatusCode with
                | true -> return ValidationSuccess s
                | false -> 
                    let! errorMessage = response.Content.ReadAsStringAsync()
                    return ValidationError $"{errorMessage}"
            }
        | DiscoveryError e -> task { return ValidationError e }

    /// <summary>
    /// Implementation of IWebmentionSender interface.
    /// </summary>
    interface IWebmentionSender<EndpointUrlData> with
        member x.SendAsync (req:HttpRequest) = 
            task {
                let! discoveryResult = x.DiscoveryService.DiscoverEndpointAsync req
                let! results = processDiscoveryResults discoveryResult
                return results
            }
        member x.SendAsync (data:UrlData) = 
            task {
                let! discoveryResult = x.DiscoveryService.DiscoverEndpointAsync data
                let! results = processDiscoveryResults discoveryResult
                return results
            }

    /// <summary>
    /// Gets the underlying discovery service.
    /// </summary>
    member x.DiscoveryService = discoveryService

    /// <summary>
    /// Sends a webmention from HTTP request form data.
    /// Discovers the endpoint and posts the webmention notification.
    /// </summary>
    /// <param name="req">HTTP request containing source and target in form body.</param>
    /// <returns>A task with ValidationSuccess containing endpoint data if successful, ValidationError otherwise.</returns>
    member x.SendAsync (req:HttpRequest) = (x :> IWebmentionSender<EndpointUrlData>).SendAsync(req)
    
    /// <summary>
    /// Sends a webmention from structured URL data.
    /// Discovers the endpoint and posts the webmention notification.
    /// </summary>
    /// <param name="data">The UrlData containing source and target URLs.</param>
    /// <returns>A task with ValidationSuccess containing endpoint data if successful, ValidationError otherwise.</returns>
    member x.SendAsync (data:UrlData) = (x :> IWebmentionSender<EndpointUrlData>).SendAsync(data)