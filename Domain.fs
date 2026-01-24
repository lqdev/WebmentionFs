namespace WebmentionFs

open System

/// <summary>
/// Represents the source and target URLs for a webmention request.
/// </summary>
type UrlData =
    {
        /// <summary>The URL of the document that mentions the target.</summary>
        Source: Uri
        /// <summary>The URL of the document being mentioned.</summary>
        Target: Uri
    }

/// <summary>
/// Represents a discovered webmention endpoint along with the request data.
/// </summary>
type EndpointUrlData = 
    {
        /// <summary>The discovered webmention endpoint URL.</summary>
        Endpoint: Uri
        /// <summary>The source and target URL data for the webmention.</summary>
        RequestBody: UrlData
    }

/// <summary>
/// Represents the different types of webmentions based on microformat annotations.
/// </summary>
type MentionTypes = 
    {
        /// <summary>Indicates if the mention is a bookmark (u-bookmark-of).</summary>
        IsBookmark: bool
        /// <summary>Indicates if the mention is a like (u-like-of).</summary>
        IsLike: bool
        /// <summary>Indicates if the mention is a reply (u-in-reply-to).</summary>
        IsReply: bool
        /// <summary>Indicates if the mention is a repost (u-repost-of).</summary>
        IsRepost: bool        
    }

/// <summary>
/// Represents a validated webmention with its request data and classified mention types.
/// </summary>
type Webmention = 
    {
        /// <summary>The source and target URL data for the webmention.</summary>
        RequestBody: UrlData
        /// <summary>The classification of the mention type(s) found in the source.</summary>
        Mentions: MentionTypes
    }

/// <summary>
/// Result type for parsing form data into URL pairs.
/// </summary>
type FormParseResult = 
    /// <summary>Successfully parsed source and target URLs from form data.</summary>
    | ParseSuccess of UrlData
    /// <summary>Failed to parse form data with an error message.</summary>
    | ParseError of string

/// <summary>
/// Result type for webmention endpoint discovery operations.
/// </summary>
type DiscoveryResult = 
    /// <summary>Successfully discovered a webmention endpoint.</summary>
    | DiscoverySuccess of EndpointUrlData
    /// <summary>Failed to discover an endpoint with an error message.</summary>
    | DiscoveryError of string

/// <summary>
/// Result type for webmention request validation operations.
/// </summary>
type RequestValidationResult = 
    /// <summary>Request passed all validation checks.</summary>
    | RequestSuccess of UrlData
    /// <summary>Request failed validation with an error message.</summary>
    | RequestError of string

/// <summary>
/// Result type for webmention content validation operations.
/// </summary>
type WebmentionValidationResult = 
    /// <summary>Found an annotated mention with classified types.</summary>
    | AnnotatedMention of MentionTypes
    /// <summary>Found an unannotated mention (generic link).</summary>
    | UnannotatedMention
    /// <summary>Failed to find a valid mention with an error message.</summary>
    | MentionError of string

/// <summary>
/// Generic result type for validation operations that can succeed or fail.
/// </summary>
/// <typeparam name="'a">The type of the success value.</typeparam>
type ValidationResult<'a> = 
    /// <summary>Validation succeeded with a result value.</summary>
    | ValidationSuccess of 'a
    /// <summary>Validation failed with an error message.</summary>
    | ValidationError of string