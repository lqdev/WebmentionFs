module WebmentionFs.Tests

open System
open Xunit
open WebmentionFs

/// <summary>
/// Tests for Domain types and basic functionality
/// </summary>
[<Fact>]
let ``UrlData can be created with valid URIs`` () =
    let source = new Uri("https://example.com/source")
    let target = new Uri("https://example.com/target")
    let urlData = { Source = source; Target = target }
    
    Assert.Equal(source, urlData.Source)
    Assert.Equal(target, urlData.Target)

[<Fact>]
let ``EndpointUrlData can be created with endpoint and request body`` () =
    let endpoint = new Uri("https://example.com/webmention")
    let source = new Uri("https://example.com/source")
    let target = new Uri("https://example.com/target")
    let requestBody = { Source = source; Target = target }
    let endpointData = { Endpoint = endpoint; RequestBody = requestBody }
    
    Assert.Equal(endpoint, endpointData.Endpoint)
    Assert.Equal(requestBody, endpointData.RequestBody)

[<Fact>]
let ``MentionTypes can represent different mention types`` () =
    let mentionTypes = { 
        IsBookmark = true
        IsLike = false
        IsReply = false
        IsRepost = false
    }
    
    Assert.True(mentionTypes.IsBookmark)
    Assert.False(mentionTypes.IsLike)
    Assert.False(mentionTypes.IsReply)
    Assert.False(mentionTypes.IsRepost)

[<Fact>]
let ``Webmention can be created with request body and mentions`` () =
    let source = new Uri("https://example.com/source")
    let target = new Uri("https://example.com/target")
    let requestBody = { Source = source; Target = target }
    let mentions = { 
        IsBookmark = false
        IsLike = true
        IsReply = false
        IsRepost = false
    }
    let webmention = { RequestBody = requestBody; Mentions = mentions }
    
    Assert.Equal(requestBody, webmention.RequestBody)
    Assert.True(webmention.Mentions.IsLike)
