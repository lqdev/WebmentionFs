namespace WebmentionFs.Services

open System
open System.Net
open System.Net.Http
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open WebmentionFs
open WebmentionFs.Services

// Webmention Sender Interface
type IWebmentionSender<'a> =
    abstract member SendAsync : req:HttpRequest -> Task<ValidationResult<'a>>
    abstract member SendAsync : data:UrlData -> Task<ValidationResult<'a>>

// Concrete implementation of IWebmentionSender
type WebmentionSenderService (discoveryService: UrlDiscoveryService) = 
    
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

    // Concrete implementation of webmention sender
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

    member x.DiscoveryService = discoveryService

    member x.SendAsync (req:HttpRequest) = (x :> IWebmentionSender<EndpointUrlData>).SendAsync(req)
    member x.SendAsync (data:UrlData) = (x :> IWebmentionSender<EndpointUrlData>).SendAsync(data)