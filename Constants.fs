namespace WebmentionFs

/// <summary>
/// Constants used throughout the WebmentionFs library.
/// Centralizes magic strings, CSS selectors, and microformat class names
/// to improve maintainability and consistency.
/// </summary>
module Constants =

    /// <summary>
    /// CSS selectors for discovering webmention endpoints.
    /// Based on W3C Webmention specification discovery methods.
    /// </summary>
    module EndpointSelectors =
        /// <summary>Selector for HTML link element with rel="webmention".</summary>
        let linkElement = "link[rel='webmention']"
        
        /// <summary>Selector for HTML anchor element with rel="webmention".</summary>
        let anchorElement = "a[rel='webmention']"

    /// <summary>
    /// CSS selectors for finding microformat-annotated mentions.
    /// Based on IndieWeb microformats2 specification.
    /// </summary>
    module MentionSelectors =
        /// <summary>Selector for bookmark mentions (u-bookmark-of).</summary>
        let bookmark = ".u-bookmark-of"
        
        /// <summary>Selector for reply mentions (u-in-reply-to).</summary>
        let reply = ".u-in-reply-to"
        
        /// <summary>Selector for like mentions (u-like-of).</summary>
        let like = ".u-like-of"
        
        /// <summary>Selector for repost mentions (u-repost-of).</summary>
        let repost = ".u-repost-of"
        
        /// <summary>Selector for all anchor elements (generic mentions).</summary>
        let anchor = "a"

    /// <summary>
    /// HTTP-related constants.
    /// </summary>
    module Http =
        /// <summary>HTTP protocol scheme.</summary>
        let httpScheme = "http"
        
        /// <summary>HTTPS protocol scheme.</summary>
        let httpsScheme = "https"
        
        /// <summary>HTTP Link header name (lowercase for comparison).</summary>
        let linkHeader = "link"
        
        /// <summary>Webmention relationship value.</summary>
        let webmentionRel = "webmention"
        
        /// <summary>Content type for HTML documents.</summary>
        let htmlContentType = "text/html"

    /// <summary>
    /// Form field names for webmention requests.
    /// </summary>
    module FormFields =
        /// <summary>Form field name for source URL.</summary>
        let source = "source"
        
        /// <summary>Form field name for target URL.</summary>
        let target = "target"

    /// <summary>
    /// Error messages for common validation failures.
    /// Provides consistent, descriptive error messages throughout the library.
    /// </summary>
    module ErrorMessages =
        /// <summary>Error when source and target URLs are identical.</summary>
        let sameUrl = "Source and target URLs are the same"
        
        /// <summary>Error when target URL is not valid or accessible.</summary>
        let invalidTarget = "Target is not a valid resource"
        
        /// <summary>Error when source protocol is invalid.</summary>
        let invalidSourceProtocol = "Invalid source protocol"
        
        /// <summary>Error when target protocol is invalid.</summary>
        let invalidTargetProtocol = "Invalid target protocol"
        
        /// <summary>Error when both protocols are invalid.</summary>
        let invalidBothProtocols = "Invalid source and target protocols"
        
        /// <summary>Error when no webmention endpoint can be discovered.</summary>
        let noEndpoint = "No webmention endpoint available"
        
        /// <summary>Error when target URL is not mentioned in source document.</summary>
        let targetNotMentioned = "Target not mentioned"
        
        /// <summary>Error when source document cannot be retrieved.</summary>
        let cannotGetSource = "Could not get source document"
