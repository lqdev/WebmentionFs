namespace WebmentionFs

open System

type UrlData =
    {
        Source: Uri
        Target: Uri
    }

type EndpointUrlData = 
    {
        Endpoint: Uri
        RequestBody: UrlData
    }

type MentionTypes = 
    {
        IsBookmark: bool
        IsLike: bool
        IsReply: bool
        IsRepost: bool        
    }

type Webmention = 
    {
        RequestBody: UrlData
        Mentions: MentionTypes
    }

type FormParseResult = 
    | ParseSuccess of UrlData
    | ParseError of string

type DiscoveryResult = 
    | DiscoverySuccess of EndpointUrlData
    | DiscoveryError of string

type RequestValidationResult = 
    | RequestSuccess of UrlData
    | RequestError of string

type WebmentionValidationResult = 
    | AnnotatedMention of MentionTypes
    | UnannotatedMention
    | MentionError of string

type ValidationResult<'a> = 
    | ValidationSuccess of 'a
    | ValidationError of string