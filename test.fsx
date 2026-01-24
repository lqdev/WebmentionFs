// WebmentionFs Interactive Test Script
// This script demonstrates the library's capabilities and can be used for manual testing

#r "./src/WebmentionFs/bin/Debug/netstandard2.1/WebmentionFs.dll"
#r "nuget:Microsoft.AspNetCore.Http.Abstractions"
#r "nuget:FSharp.Data"

open System
open WebmentionFs
open WebmentionFs.Services

printfn "╔════════════════════════════════════════════════════════════╗"
printfn "║         WebmentionFs Interactive Test Suite               ║"
printfn "╚════════════════════════════════════════════════════════════╝\n"

// ============================================================================
// Test 1: URL Discovery and Sending
// ============================================================================
printfn "Test 1: Discovering webmention endpoint and sending mention"
printfn "────────────────────────────────────────────────────────────\n"

let discoveryService = new UrlDiscoveryService()
let senderService = new WebmentionSenderService(discoveryService)

let testData = 
    {   
        Source = new Uri("http://lqdev.me/feed/webmentionfs-send-test")  
        Target = new Uri("https://webmention.rocks/test/1")
    }

printfn "Source: %s" testData.Source.OriginalString
printfn "Target: %s\n" testData.Target.OriginalString

let sendResult = 
    senderService.SendAsync(testData) 
    |> Async.AwaitTask 
    |> Async.RunSynchronously

match sendResult with
| ValidationSuccess data -> 
    printfn "✅ SUCCESS: Webmention sent!"
    printfn "   Endpoint: %s" data.Endpoint.OriginalString
    printfn "   Source: %s" data.RequestBody.Source.OriginalString
    printfn "   Target: %s\n" data.RequestBody.Target.OriginalString
| ValidationError error -> 
    printfn "❌ ERROR: Failed to send webmention"
    printfn "   Error: %s\n" error

// ============================================================================
// Test 2: Endpoint Discovery Only
// ============================================================================
printfn "\nTest 2: Testing endpoint discovery"
printfn "────────────────────────────────────────────────────────────\n"

let discoveryTestData = 
    {
        Source = new Uri("https://example.com/source")
        Target = new Uri("https://webmention.rocks/test/1")
    }

printfn "Discovering endpoint for: %s\n" discoveryTestData.Target.OriginalString

let discoveryResult = 
    discoveryService.DiscoverEndpointAsync(discoveryTestData)
    |> Async.AwaitTask
    |> Async.RunSynchronously

match discoveryResult with
| DiscoverySuccess data ->
    printfn "✅ SUCCESS: Endpoint discovered!"
    printfn "   Endpoint: %s\n" data.Endpoint.OriginalString
| DiscoveryError error ->
    printfn "❌ ERROR: Could not discover endpoint"
    printfn "   Error: %s\n" error

// ============================================================================
// Test 3: Request Validation
// ============================================================================
printfn "\nTest 3: Testing request validation"
printfn "────────────────────────────────────────────────────────────\n"

// Configure with your domain(s)
let myDomains = [| "example.com"; "www.example.com" |]
let requestValidator = new RequestValidationService(myDomains)

let validationTestData = 
    {
        Source = new Uri("https://other-site.com/post")
        Target = new Uri("https://example.com/my-post")
    }

printfn "Validating request:"
printfn "  Source: %s" validationTestData.Source.OriginalString
printfn "  Target: %s" validationTestData.Target.OriginalString
printfn "  Allowed domains: %s\n" (String.Join(", ", myDomains))

let validationResult = 
    requestValidator.ValidateAsync(validationTestData)
    |> Async.AwaitTask
    |> Async.RunSynchronously

match validationResult with
| RequestSuccess data ->
    printfn "✅ SUCCESS: Request is valid!"
    printfn "   Source: %s" data.Source.OriginalString
    printfn "   Target: %s\n" data.Target.OriginalString
| RequestError error ->
    printfn "❌ ERROR: Request validation failed"
    printfn "   Error: %s\n" error

// ============================================================================
// Summary
// ============================================================================
printfn "\n╔════════════════════════════════════════════════════════════╗"
printfn "║                  Test Suite Complete                      ║"
printfn "╚════════════════════════════════════════════════════════════╝"
printfn "\nNote: Some tests may fail if:"
printfn "  - Target URLs are not accessible"
printfn "  - Network connectivity issues"
printfn "  - Remote endpoints are unavailable"
printfn "\nFor more information, see:"
printfn "  - README.md: User guide and examples"
printfn "  - ARCHITECTURE.md: Design and implementation details"
printfn "  - CONTRIBUTING.md: F# conventions and development guide\n"