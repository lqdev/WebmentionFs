namespace WebmentionFs.Services

open System
open System.Net
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open WebmentionFs
open WebmentionFs.Services

/// <summary>
/// Interface for receiving and processing incoming webmentions.
/// </summary>
/// <typeparam name="'a">The type of data returned on successful receipt.</typeparam>
type IWebmentionReceiver<'a> =
    /// <summary>Receives and validates a webmention from HTTP request form data.</summary>
    abstract member ReceiveAsync : req:HttpRequest -> Task<ValidationResult<'a>>
    /// <summary>Receives and validates a webmention from structured URL data.</summary>
    abstract member ReceiveAsync : data:UrlData -> Task<ValidationResult<'a>>

/// <summary>
/// Service for receiving and validating incoming webmentions.
/// Combines request validation (protocol, ownership, accessibility) with
/// content validation (verifying the source actually mentions the target).
/// </summary>
/// <param name="requestValidationService">Service for validating webmention request format and ownership.</param>
/// <param name="webmentionValidationService">Service for validating that source contains mention of target.</param>
type WebmentionReceiverService (
    requestValidationService : RequestValidationService,
    webmentionValidationService : WebmentionValidationService) = 

    /// <summary>
    /// Implementation of IWebmentionReceiver interface.
    /// </summary>
    interface IWebmentionReceiver<Webmention> with
        member x.ReceiveAsync (req:HttpRequest) = 
            task {
                let! requestValidationResult = 
                    x.RequestValidationService.ValidateAsync req

                match requestValidationResult with
                | RequestSuccess r -> 
                    let! webmentionValidationResult = 
                        x.WebmentionValidationService.ValidateAsync r.Source r.Target

                    let (result:ValidationResult<Webmention>) = 
                        match webmentionValidationResult with
                        | AnnotatedMention m -> 
                            ValidationSuccess {RequestBody = r; Mentions = m}
                        | UnannotatedMention -> 
                            ValidationSuccess 
                                {
                                    RequestBody = r
                                    Mentions = 
                                        {
                                            IsBookmark = false
                                            IsLike = false
                                            IsReply = false
                                            IsRepost = false        
                                        }
                                }
                        | MentionError e -> ValidationError e
                    return result
                | RequestError e -> return ValidationError e
            }
        member x.ReceiveAsync (data:UrlData) = 
            task {
                let! requestValidationResult = 
                    x.RequestValidationService.ValidateAsync data

                match requestValidationResult with
                | RequestSuccess r -> 
                    let! webmentionValidationResult = 
                        x.WebmentionValidationService.ValidateAsync r.Source r.Target

                    let (result:ValidationResult<Webmention>) = 
                        match webmentionValidationResult with
                        | AnnotatedMention m -> 
                            ValidationSuccess {RequestBody = r; Mentions = m}
                        | UnannotatedMention -> 
                            ValidationSuccess 
                                {
                                    RequestBody = r
                                    Mentions = 
                                        {
                                            IsBookmark = false
                                            IsLike = false
                                            IsReply = false
                                            IsRepost = false        
                                        }
                                }
                        | MentionError e -> ValidationError e
                    return result
                | RequestError e -> return ValidationError e
            }            

    /// <summary>
    /// Gets the request validation service.
    /// </summary>
    member x.RequestValidationService = requestValidationService
    
    /// <summary>
    /// Gets the webmention validation service.
    /// </summary>
    member x.WebmentionValidationService = webmentionValidationService



    