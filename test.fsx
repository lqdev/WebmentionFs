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
        Source = new Uri("http://lqdev.me/feed/webmentionfs-send-test")  
        Target = new Uri("https://webmention.rocks/test/1")
    }

ws.SendAsync(data) |> Async.AwaitTask |> Async.RunSynchronously