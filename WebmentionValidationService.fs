namespace WebmentionFs.Services

open System
open System.Net.Http
open System.Net.Http.Headers
open FSharp.Data
open WebmentionFs

type WebmentionValidationService () = 

    let getSourceDocumentAsync (source:Uri) = 
        task {
            // Prepare HTTP GET request
            use client = new HttpClient()
            let reqMessage = new HttpRequestMessage(new HttpMethod(HttpMethod.Get), source)
            reqMessage.Headers.Accept.Clear()

            // Only accept text/html content
            reqMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"))

            //Send HTTP request
            return! client.SendAsync(reqMessage)
        }

    // Use CSS selectors to find target link in source document
    let findTargetUrlInSourceDocument (doc:HtmlDocument) (selector:string) (target:Uri) = 
        doc.CssSelect(selector)
        |> List.map(fun x -> x.AttributeValue("href"))
        |> List.filter(fun x -> x = target.OriginalString)

    // Identify webmentions in source document using microformat annotations
    let findMentionsInSourceDocument (doc:HtmlDocument) (target:Uri) = 

        // Get mentions annotated as bookmarks
        let bookmarks = 
            findTargetUrlInSourceDocument doc ".u-bookmark-of" target

        // Get mentions annotated as replies
        let replies = 
            findTargetUrlInSourceDocument doc ".u-in-reply-to" target

        // Get mentions annotated as likes 
        let likes = 
            findTargetUrlInSourceDocument doc ".u-like-of" target

        // Get mentions annotated as reposts
        let reposts = 
            findTargetUrlInSourceDocument doc ".u-repost-of" target

        // Group all annotated webmentions
        let annotatedMentions = 
            [bookmarks;likes;replies;reposts]

        // Group all unannotated webmentions
        let unannotatedMentions = 
            findTargetUrlInSourceDocument doc "a" target

        annotatedMentions,unannotatedMentions

    // Check whether list of mentions is empty
    let hasMention (mentions: string list) = 
        mentions |> List.isEmpty |> not

    let validate (annotatedMentions:string list list, unannotatedMentions:string list) = 
        match annotatedMentions.IsEmpty,unannotatedMentions.IsEmpty with
        | true, true -> MentionError "Target not mentioned"
        | true, false | false, false -> 
            let isBookmark = hasMention annotatedMentions[0]
            let isLike = hasMention annotatedMentions[1]
            let isReply = hasMention annotatedMentions[2]
            let isRepost = hasMention annotatedMentions[3]

            AnnotatedMention
                {
                    IsBookmark = isBookmark
                    IsLike = isLike
                    IsReply = isReply
                    IsRepost = isRepost
                }
        | false, true -> UnannotatedMention

    member _.ValidateAsync (source:Uri) (target:Uri) = 
        task {
            let! sourceDocResponse = source |> getSourceDocumentAsync

            match sourceDocResponse.IsSuccessStatusCode with
            | true -> 
                let! sourceDocument = sourceDocResponse.Content.ReadAsStringAsync()

                let html = HtmlDocument.Parse(sourceDocument)

                return
                    target
                    |> findMentionsInSourceDocument html
                    |> validate
            | false -> return MentionError "Could not get source document"
        }