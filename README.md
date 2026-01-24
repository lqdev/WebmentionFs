# WebmentionFs

[![NuGet Version](https://img.shields.io/nuget/v/lqdev.WebmentionFs.svg)](https://www.nuget.org/packages/lqdev.WebmentionFs/)
[![Build Status](https://github.com/lqdev/WebmentionFs/workflows/CI/badge.svg)](https://github.com/lqdev/WebmentionFs/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive F# library for processing [webmentions](https://www.w3.org/TR/webmention/) according to the W3C specification. WebmentionFs provides a complete toolkit for both sending and receiving webmentions, with built-in validation and URL discovery capabilities.

Built with modern F# best practices, leveraging discriminated unions, immutability, and type-safe error handling.

## ✨ Features

- **🔍 URL Discovery**: Automatic webmention endpoint discovery from HTML pages
- **📨 Send Webmentions**: Send webmentions to discovered endpoints
- **📥 Receive Webmentions**: Process incoming webmention requests
- **✅ Request Validation**: Validate webmention request format and content
- **🏷️ Mention Classification**: Identify different types of mentions (likes, replies, reposts, bookmarks)
- **🔒 Security**: Built-in validation to prevent spam and malicious requests
- **🌐 Standards Compliant**: Full compliance with W3C Webmention specification
- **🎯 Modern F#**: Idiomatic F# with discriminated unions, immutability, and explicit error handling
- **📚 Well Documented**: Comprehensive XML documentation and architectural guides

## 🎯 Target Framework

- **.NET Standard 2.1**: Compatible with .NET Core 3.0+, .NET 5+, .NET 6+, and later
- **Modern F#**: Uses latest F# language features (task CE, string interpolation, enhanced pattern matching) while maintaining .NET Standard 2.1 compatibility

## 📦 Installation

### Package Manager Console
```powershell
Install-Package lqdev.WebmentionFs
```

### .NET CLI
```bash
dotnet add package lqdev.WebmentionFs
```

### PackageReference
```xml
<PackageReference Include="lqdev.WebmentionFs" Version="0.0.7" />
```

## 🚀 Quick Start

### Basic Usage

```f#
open System
open WebmentionFs
open WebmentionFs.Services

// Create service instances
let discoveryService = new UrlDiscoveryService()
let senderService = new WebmentionSenderService(discoveryService)

// Define source and target URLs
let webmentionData = {
    Source = new Uri("https://your-blog.com/post-mentioning-target")
    Target = new Uri("https://target-site.com/post-being-mentioned")
}

// Send a webmention
let result = 
    senderService.SendAsync(webmentionData) 
    |> Async.AwaitTask 
    |> Async.RunSynchronously

match result with
| ValidationSuccess endpoint -> 
    printfn "Webmention sent successfully to: %s" endpoint.Endpoint.OriginalString
| ValidationError error -> 
    printfn "Failed to send webmention: %s" error
```

## 📖 Detailed Usage

### 1. Sending Webmentions

WebmentionFs can automatically discover webmention endpoints and send mentions:

```f#
open WebmentionFs.Services

let discoveryService = new UrlDiscoveryService()
let senderService = new WebmentionSenderService(discoveryService)

let mentionData = {
    Source = new Uri("https://myblog.com/awesome-post")
    Target = new Uri("https://example.com/original-post")
}

async {
    let! result = senderService.SendAsync(mentionData) |> Async.AwaitTask
    
    match result with
    | ValidationSuccess data ->
        printfn "✅ Webmention sent to endpoint: %s" data.Endpoint.OriginalString
        printfn "   Source: %s" data.RequestBody.Source.OriginalString
        printfn "   Target: %s" data.RequestBody.Target.OriginalString
    | ValidationError error ->
        printfn "❌ Error: %s" error
} |> Async.RunSynchronously
```

### 2. Receiving and Validating Webmentions

For processing incoming webmentions in an ASP.NET Core application:

```f#
open Microsoft.AspNetCore.Http
open WebmentionFs.Services

// Setup validation services
let hostList = [| "yourdomain.com"; "www.yourdomain.com" |]
let requestValidator = new RequestValidationService(hostList)
let webmentionValidator = new WebmentionValidationService()
let receiverService = new WebmentionReceiverService(requestValidator, webmentionValidator)

// In your controller/handler
let processWebmention (httpRequest: HttpRequest) = async {
    let! result = receiverService.ReceiveAsync(httpRequest) |> Async.AwaitTask
    
    match result with
    | ValidationSuccess webmention ->
        printfn "✅ Valid webmention received!"
        printfn "   Source: %s" webmention.RequestBody.Source.OriginalString
        printfn "   Target: %s" webmention.RequestBody.Target.OriginalString
        
        // Check mention types
        if webmention.Mentions.IsLike then printfn "   Type: Like ❤️"
        elif webmention.Mentions.IsReply then printfn "   Type: Reply 💬"
        elif webmention.Mentions.IsRepost then printfn "   Type: Repost 🔄"
        elif webmention.Mentions.IsBookmark then printfn "   Type: Bookmark 🔖"
        else printfn "   Type: Generic mention"
        
        // Process the webmention (save to database, send notifications, etc.)
        
    | ValidationError error ->
        printfn "❌ Invalid webmention: %s" error
}
```

### 3. URL Discovery

Discover webmention endpoints from a target URL:

```f#
let discoveryService = new UrlDiscoveryService()

let urlData = {
    Source = new Uri("https://myblog.com/post")
    Target = new Uri("https://target.com/post")
}

async {
    let! result = discoveryService.DiscoverEndpointAsync(urlData) |> Async.AwaitTask
    
    match result with
    | DiscoverySuccess endpoint ->
        printfn "Found webmention endpoint: %s" endpoint.Endpoint.OriginalString
    | DiscoveryError error ->
        printfn "Could not discover endpoint: %s" error
} |> Async.RunSynchronously
```

### 4. Manual Request Validation

Validate webmention requests without processing them:

```f#
let hostList = [| "mydomain.com" |]
let validator = new RequestValidationService(hostList)

async {
    let! result = validator.ValidateAsync(webmentionData) |> Async.AwaitTask
    
    match result with
    | RequestSuccess data ->
        printfn "✅ Request is valid"
        printfn "   Source: %s" data.Source.OriginalString
        printfn "   Target: %s" data.Target.OriginalString
    | RequestError error ->
        printfn "❌ Request validation failed: %s" error
} |> Async.RunSynchronously
```

## 🏗️ Architecture

### Core Types

WebmentionFs uses a functional approach with discriminated unions for result handling:

```f#
// Basic URL data
type UrlData = {
    Source: Uri
    Target: Uri
}

// Mention classification
type MentionTypes = {
    IsBookmark: bool
    IsLike: bool
    IsReply: bool
    IsRepost: bool
}

// Result types
type ValidationResult<'a> = 
    | ValidationSuccess of 'a
    | ValidationError of string

type DiscoveryResult = 
    | DiscoverySuccess of EndpointUrlData
    | DiscoveryError of string
```

### Service Overview

- **`UrlDiscoveryService`**: Discovers webmention endpoints using multiple methods (HTTP headers, `<link>` tags, `<a>` tags)
- **`WebmentionSenderService`**: Combines discovery and sending functionality
- **`RequestValidationService`**: Validates incoming webmention requests
- **`WebmentionValidationService`**: Analyzes source documents for mention types
- **`WebmentionReceiverService`**: Complete pipeline for processing incoming webmentions

## 🔧 Configuration

### Request Validation

Configure which domains you own for validation:

```f#
let myDomains = [| 
    "myblog.com"
    "www.myblog.com" 
    "staging.myblog.com"
|]

let validator = new RequestValidationService(myDomains)
```

### ASP.NET Core Integration

Register services in your DI container:

```f#
// In Startup.fs or Program.fs
services.AddSingleton<UrlDiscoveryService>() |> ignore
services.AddSingleton<WebmentionValidationService>() |> ignore
services.AddSingleton<RequestValidationService>(fun _ -> 
    new RequestValidationService([| "yourdomain.com" |])) |> ignore
services.AddTransient<WebmentionSenderService>() |> ignore
services.AddTransient<WebmentionReceiverService>() |> ignore
```

## 🔍 Endpoint Discovery

WebmentionFs discovers webmention endpoints using the standard methods defined in the specification:

1. **HTTP Link Header**: `Link: <https://example.com/webmention>; rel="webmention"`
2. **HTML Link Element**: `<link rel="webmention" href="https://example.com/webmention">`
3. **HTML Anchor Element**: `<a rel="webmention" href="https://example.com/webmention">webmention</a>`

The library tries all methods and uses the first successful discovery.

## 🏷️ Mention Types

WebmentionFs can automatically classify mentions based on microformat annotations:

- **Likes**: Elements with class `u-like-of`
- **Replies**: Elements with class `u-in-reply-to`
- **Reposts**: Elements with class `u-repost-of`
- **Bookmarks**: Elements with class `u-bookmark-of`
- **Generic**: Any other link to the target URL

## 🛡️ Security Features

- **Protocol validation**: Only HTTP/HTTPS URLs are accepted
- **Domain ownership verification**: Ensures targets belong to configured domains
- **URL validation**: Verifies target URLs are accessible
- **Content type checking**: Validates HTML content types
- **Same URL prevention**: Prevents self-referential webmentions

## 🐛 Error Handling

All operations return discriminated unions for explicit error handling:

```f#
match result with
| ValidationSuccess data -> 
    // Handle success case
    processWebmention data
| ValidationError error ->
    // Handle error case
    logError error
    respondWithError 400 error
```

Common error scenarios:
- Invalid URL formats
- Target URL not accessible
- No webmention endpoint found
- Network connectivity issues
- Invalid request format

## 🏗️ Architecture & Design

WebmentionFs is built using modern F# idioms and best practices:

- **Discriminated Unions**: All result types use discriminated unions for explicit, type-safe error handling
- **Immutability**: All domain types are immutable records
- **Function Composition**: Validation pipelines use function composition for clarity
- **Explicit Errors**: No hidden exceptions - all failures are represented in return types
- **Modern Async**: Uses `task {}` computation expressions for async operations
- **Type Safety**: Leverages F#'s type system to prevent invalid states

For detailed architectural information, see [ARCHITECTURE.md](ARCHITECTURE.md).

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Documentation for Contributors

- **[CONTRIBUTING.md](CONTRIBUTING.md)**: F# coding conventions, development workflow, testing guidelines
- **[ARCHITECTURE.md](ARCHITECTURE.md)**: Design decisions, module relationships, extension points
- **[AI_ASSISTANT_GUIDE.md](AI_ASSISTANT_GUIDE.md)**: Guide for AI coding assistants working with this codebase

### Project Structure

The repository follows standard F# project organization:

```
WebmentionFs/
├── src/
│   └── WebmentionFs/          # Main library source code
│       ├── Domain.fs           # Core types and domain models
│       ├── Constants.fs        # Constants and configuration
│       ├── Utils.fs            # Utility functions
│       ├── UrlDiscoveryService.fs
│       ├── RequestValidationService.fs
│       ├── WebmentionValidationService.fs
│       ├── WebmentionReceiverService.fs
│       ├── WebmentionSenderService.fs
│       └── WebmentionFs.fsproj
├── tests/
│   └── WebmentionFs.Tests/    # XUnit test project
│       ├── Tests.fs            # Unit tests
│       └── WebmentionFs.Tests.fsproj
├── WebmentionFs.slnx          # Solution file (XML format)
└── README.md
```

### Development Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/lqdev/WebmentionFs.git
   cd WebmentionFs
   ```

2. Build the entire solution:
   ```bash
   dotnet build WebmentionFs.slnx
   ```

3. Run the tests:
   ```bash
   dotnet test WebmentionFs.slnx
   ```

   Or run tests directly from the test project:
   ```bash
   dotnet test tests/WebmentionFs.Tests/WebmentionFs.Tests.fsproj
   ```

4. Build only the main library:
   ```bash
   dotnet build src/WebmentionFs/WebmentionFs.fsproj
   ```

### Running Tests

The project uses [XUnit](https://xunit.net/) as the testing framework. Tests are located in the `tests/WebmentionFs.Tests/` directory.

To run all tests:
```bash
dotnet test
```

To run tests with detailed output:
```bash
dotnet test --verbosity normal
```

To run tests for a specific project:
```bash
dotnet test tests/WebmentionFs.Tests/WebmentionFs.Tests.fsproj
```

### F# Conventions

This project follows modern F# best practices:

- **camelCase** for values and functions
- **PascalCase** for types and modules
- Discriminated unions for state representation
- Explicit error handling with Result types
- Pure functions and immutable data
- `task {}` for async operations

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [W3C Webmention Specification](https://www.w3.org/TR/webmention/)
- [Webmention.rocks](https://webmention.rocks/) for testing endpoints
- The IndieWeb community for webmention advocacy

## 📚 Additional Resources

- **Project Documentation**
  - [Architecture Guide](ARCHITECTURE.md)
  - [Contributing Guide](CONTRIBUTING.md)
  - [AI Assistant Guide](AI_ASSISTANT_GUIDE.md)
- **Webmention Resources**
  - [Webmention Specification](https://www.w3.org/TR/webmention/)
  - [IndieWeb Webmention Guide](https://indieweb.org/Webmention)
  - [Microformats2](http://microformats.org/wiki/microformats2)
- **F# Resources**
  - [F# Language Guide](https://learn.microsoft.com/en-us/dotnet/fsharp/)
  - [F# Style Guide](https://learn.microsoft.com/en-us/dotnet/fsharp/style-guide/)
  - [FSharp.Data Documentation](https://fsprojects.github.io/FSharp.Data/)
- **Development Tools**
  - [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

---

**Author**: Luis Quintanilla  
**Repository**: https://github.com/lqdev/WebmentionFs  
**NuGet Package**: https://www.nuget.org/packages/lqdev.WebmentionFs/