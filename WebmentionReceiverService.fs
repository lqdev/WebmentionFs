namespace WebmentionFs.Services

open System
open System.Net
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open WebmentionFs
open WebmentionFs.Services

// Webmention Receiver Interface
type IWebmentionReceiver<'a> =
    abstract member ValidateAsync : req:HttpRequest -> Task<ValidationResult<'a>>

// Implementaiton of Webmention Receiver
type WebmentionReceiverService (
    requestValidationService : RequestValidationService,
    webmentionValidationService : WebmentionValidationService) = 

    // Concrete implementation of webmention receiver
    interface IWebmentionReceiver<Webmention> with
        member x.ValidateAsync (req:HttpRequest) = 
            task {
                let! requestValidationResult = 
                    x.RequestValidationService.ValidateAsync req

                match requestValidationResult with
                | RequestSuccess r -> 
                    let! webmentionValidationResult = 
                        x.WembentionValidationService.ValidateAsync r.Source r.Target

                    let (result:ValidationResult<Webmention>) = 
                        match webmentionValidationResult with
                        | AnnotatedMention m -> 
                            ValidationSuccess {Urls = r; Mentions = m}
                        | UnannotatedMention -> 
                            ValidationSuccess 
                                {
                                    Urls = r
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

    member x.RequestValidationService = requestValidationService
    member x.WembentionValidationService = webmentionValidationService



    