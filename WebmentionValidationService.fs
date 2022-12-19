namespace WebmentionFs.Services

open System
open System.Net.Http
open FSharp.Data
open WebmentionFs
open WebmentionFs.Utils

type WebmentionValidationService () = 

    let findTargetUrlInSourceDocument (targetUrl:string) (links:string list) = 
        links
        |> List.filter(fun link -> link = targetUrl)

    // Identify webmentions in source document using microformat annotations
    let findMentionsInSourceDocument (doc:HtmlDocument) (target:Uri) = 

        // Get mentions annotated as bookmarks
        let bookmarks = 
            (getUrlFromSourceDocument doc ".u-bookmark-of" target)
            |> findTargetUrlInSourceDocument target.OriginalString

        // Get mentions annotated as replies
        let replies = 
            (getUrlFromSourceDocument doc ".u-in-reply-to" target)
            |> findTargetUrlInSourceDocument target.OriginalString

        // Get mentions annotated as likes 
        let likes = 
            (getUrlFromSourceDocument doc ".u-like-of" target)
            |> findTargetUrlInSourceDocument target.OriginalString


        // Get mentions annotated as reposts
        let reposts = 
            (getUrlFromSourceDocument doc ".u-repost-of" target)
            |> findTargetUrlInSourceDocument target.OriginalString

        // Group all annotated webmentions
        let annotatedMentions = 
            [bookmarks;likes;replies;reposts]

        // Group all unannotated webmentions
        let unannotatedMentions = 
            (getUrlFromSourceDocument doc "a" target)
            |> findTargetUrlInSourceDocument target.OriginalString

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
            let! sourceDocResponse = source |> getDocumentContentAsync

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