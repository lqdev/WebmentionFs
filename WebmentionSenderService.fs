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
            let! (response:HttpResponseMessage) = client.PostAsync(data.Endpoint, content)
            return response
        }

    let processDiscoveryResults (result:DiscoveryResult) = 
        match result with
        | DiscoverySuccess s -> 
            task {
                let! response =  sendMentionAsync s
                match response.IsSuccessStatusCode with
                | true -> return ValidationSuccess s
                | false -> return ValidationError "Error sending webmention"
            }
        | DiscoveryError e -> task { return ValidationError e }

    // Concrete implementation of webmention sender
    interface IWebmentionSender<EndpointUrlData> with
        member x.SendAsync (req:HttpRequest) = 
            task {
                let! discoveryResult = x.DiscoveryService.DiscoverEndpointAsync req
                return! processDiscoveryResults discoveryResult
            }
        member x.SendAsync (data:UrlData) = 
            task {
                let! discoveryResult = x.DiscoveryService.DiscoverEndpointAsync data
                return! processDiscoveryResults discoveryResult
            }

    member x.DiscoveryService = discoveryService