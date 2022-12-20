#r "./bin/Debug/netstandard2.1/WebmentionFs.dll"
#r "nuget:Microsoft.AspNetCore.Http.Abstractions"
#r "nuget:FSharp.Data"

open System
open WebmentionFs
open WebmentionFs.Services

let ds = new UrlDiscoveryService()

let ws = new WebmentionSenderService(ds)

let data = 
    {   
        Source = new Uri("https://twitter.com/ljquintanilla/status/1603602055435894784")  
        Target = new Uri("https://www.luisquintanilla.me/feed/mastodon-hashtag-rss-boffosocko")
    }

ws.SendAsync(data) |> Async.AwaitTask |> Async.RunSynchronously