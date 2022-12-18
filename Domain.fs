namespace WebmentionFs

open System

type UrlData =
    {
        Source: Uri
        Target: Uri
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
        Urls: UrlData
        Mentions: MentionTypes
    }

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