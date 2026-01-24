namespace WebmentionFs.Services

open System
open System.Net.Http
open FSharp.Data
open WebmentionFs
open WebmentionFs.Utils
open WebmentionFs.Constants

/// <summary>
/// Service for validating that a source document contains a valid mention of the target URL.
/// Analyzes HTML content for microformat-annotated mentions (likes, replies, reposts, bookmarks)
/// as well as generic unannotated links.
/// </summary>
type WebmentionValidationService () = 

    /// <summary>
    /// Filters a list of links to find those matching the target URL.
    /// </summary>
    /// <param name="targetUrl">The target URL string to match.</param>
    /// <param name="links">List of link href values to filter.</param>
    /// <returns>List of links that match the target URL.</returns>
    let findTargetUrlInSourceDocument (targetUrl:string) (links:string list) = 
        links
        |> List.filter(fun link -> link = targetUrl)

    /// <summary>
    /// Identifies and classifies webmentions in a source document using microformat annotations.
    /// Searches for specific CSS classes that indicate mention types according to IndieWeb standards.
    /// </summary>
    /// <param name="doc">The parsed HTML document to analyze.</param>
    /// <param name="target">The target URI being mentioned.</param>
    /// <returns>A tuple of (annotated mentions list, unannotated mentions list).</returns>
    /// <remarks>
    /// Annotated mention types:
    /// - u-bookmark-of: Bookmarked content
    /// - u-in-reply-to: Reply to content
    /// - u-like-of: Liked content
    /// - u-repost-of: Reposted/shared content
    /// </remarks>
    let findMentionsInSourceDocument (doc:HtmlDocument) (target:Uri) = 

        // Get mentions annotated as bookmarks
        let bookmarks = 
            (getUrlFromSourceDocument doc MentionSelectors.bookmark target)
            |> findTargetUrlInSourceDocument target.OriginalString

        // Get mentions annotated as replies
        let replies = 
            (getUrlFromSourceDocument doc MentionSelectors.reply target)
            |> findTargetUrlInSourceDocument target.OriginalString

        // Get mentions annotated as likes 
        let likes = 
            (getUrlFromSourceDocument doc MentionSelectors.like target)
            |> findTargetUrlInSourceDocument target.OriginalString


        // Get mentions annotated as reposts
        let reposts = 
            (getUrlFromSourceDocument doc MentionSelectors.repost target)
            |> findTargetUrlInSourceDocument target.OriginalString

        // Group all annotated webmentions
        let annotatedMentions = 
            [bookmarks;likes;replies;reposts]

        // Group all unannotated webmentions
        let unannotatedMentions = 
            (getUrlFromSourceDocument doc MentionSelectors.anchor target)
            |> findTargetUrlInSourceDocument target.OriginalString

        annotatedMentions,unannotatedMentions

    /// <summary>
    /// Checks whether a list of mentions is non-empty.
    /// </summary>
    /// <param name="mentions">List of mention strings to check.</param>
    /// <returns>True if the list contains mentions, false if empty.</returns>
    let hasMention (mentions: string list) = 
        mentions |> List.isEmpty |> not

    /// <summary>
    /// Validates and classifies mentions found in the source document.
    /// </summary>
    /// <param name="annotatedMentions">List of lists containing annotated mentions by type.</param>
    /// <param name="unannotatedMentions">List of generic unannotated mentions.</param>
    /// <returns>AnnotatedMention with type classification, UnannotatedMention, or MentionError if no mentions found.</returns>
    let validate (annotatedMentions:string list list, unannotatedMentions:string list) = 
        match annotatedMentions.IsEmpty,unannotatedMentions.IsEmpty with
        | true, true -> MentionError ErrorMessages.targetNotMentioned
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

    /// <summary>
    /// Validates that a source document contains a valid mention of the target URL.
    /// Fetches the source document, parses it, and classifies any mentions found.
    /// </summary>
    /// <param name="source">The source URI to fetch and analyze.</param>
    /// <param name="target">The target URI that should be mentioned in the source.</param>
    /// <returns>A task with AnnotatedMention, UnannotatedMention, or MentionError result.</returns>
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
            | false -> return MentionError ErrorMessages.cannotGetSource
        }