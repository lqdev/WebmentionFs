# AI Assistant Guide for WebmentionFs

This guide helps AI coding assistants understand and effectively work with the WebmentionFs codebase.

## Project Overview

**Purpose**: F# library implementing W3C Webmention specification for sending/receiving webmentions  
**Language**: F# (functional-first)  
**Target**: .NET Standard 2.1  
**Style**: Modern F# idioms with discriminated unions, immutability, and explicit error handling

## Quick Reference

### File Organization & Dependencies

Files are ordered by dependency (top to bottom, no circular refs):

1. **Domain.fs** - Core types, no dependencies
2. **Constants.fs** - String constants, CSS selectors, error messages
3. **Utils.fs** - Pure utility functions (HTTP, parsing, HTML)
4. **Services** - Classes for DI compatibility
   - UrlDiscoveryService.fs
   - RequestValidationService.fs
   - WebmentionValidationService.fs
   - WebmentionReceiverService.fs
   - WebmentionSenderService.fs

### Key Types to Understand

```fsharp
// Core data type
type UrlData = { Source: Uri; Target: Uri }

// Result types (discriminated unions)
type ValidationResult<'a> = 
    | ValidationSuccess of 'a
    | ValidationError of string

type DiscoveryResult = 
    | DiscoverySuccess of EndpointUrlData
    | DiscoveryError of string

// Mention classification
type MentionTypes = {
    IsBookmark: bool
    IsLike: bool
    IsReply: bool
    IsRepost: bool
}
```

### Always Use Constants

**Never hardcode strings**. Use Constants module:

```fsharp
open WebmentionFs.Constants

// Good
let selector = EndpointSelectors.linkElement
let error = ErrorMessages.invalidTarget

// Bad
let selector = "link[rel='webmention']"
let error = "Target is not a valid resource"
```

### Error Handling Pattern

All operations return Result types. **Never throw exceptions** in normal flow:

```fsharp
// Correct pattern
let validateSomething input =
    try
        // logic
        ValidationSuccess result
    with
    | ex -> ValidationError $"Description: {ex.Message}"

// Wrong - don't throw
let validateSomething input =
    if not valid then
        failwith "Invalid!"  // ❌
```

### Async Pattern

Use `task {}` computation expression:

```fsharp
// Correct
let fetchDataAsync uri =
    task {
        let! response = httpClient.GetAsync(uri)
        return! response.Content.ReadAsStringAsync()
    }

// Wrong - avoid blocking
let fetchData uri =
    httpClient.GetAsync(uri).Result  // ❌ Blocks!
```

## Common Tasks for AI Assistants

### Adding a New Validation Rule

**Where**: RequestValidationService.fs

**How**: Add function to validation pipeline:

```fsharp
// 1. Add validation function
let myNewValidation (result: RequestValidationResult) =
    match result with
    | RequestSuccess r ->
        if (* check something *) then
            RequestSuccess r
        else
            RequestError ErrorMessages.myNewError  // Add to Constants first!
    | RequestError e -> RequestError e

// 2. Compose into pipeline
let validateAsync = 
    isProtocolValid 
    >> isSameUrl 
    >> myNewValidation  // Add here
    >> isTargetUrlValidAsync
```

**Remember**: 
- Add error message to Constants.ErrorMessages first
- Maintain pure function signature: `RequestValidationResult -> RequestValidationResult` or `-> Task<RequestValidationResult>`

### Adding a New CSS Selector

**Where**: Constants.fs

**How**:

```fsharp
// 1. Add to appropriate module in Constants.fs
module EndpointSelectors =
    let myNewSelector = "div[data-webmention]"

// 2. Use in service
open WebmentionFs.Constants

let findInDocument doc =
    getUrlFromSourceDocument doc EndpointSelectors.myNewSelector target
```

### Adding a New Mention Type

**Where**: Multiple files need updates

**Steps**:

1. **Constants.fs**: Add CSS selector
   ```fsharp
   module MentionSelectors =
       let myMention = ".u-my-mention-of"
   ```

2. **Domain.fs**: Add field to MentionTypes
   ```fsharp
   type MentionTypes = {
       IsBookmark: bool
       IsLike: bool
       IsReply: bool
       IsRepost: bool
       IsMyMention: bool  // Add here
   }
   ```

3. **WebmentionValidationService.fs**: Update discovery
   ```fsharp
   let findMentionsInSourceDocument (doc: HtmlDocument) (target: Uri) =
       // Add extraction
       let myMentions = 
           (getUrlFromSourceDocument doc MentionSelectors.myMention target)
           |> findTargetUrlInSourceDocument target.OriginalString
       
       // Add to list
       let annotatedMentions = 
           [bookmarks; likes; replies; reposts; myMentions]
       // ...
   
   // Update validation
   let validate (annotatedMentions: string list list, ...) =
       // ...
       let isMyMention = hasMention annotatedMentions[4]  // Index depends on order
       AnnotatedMention {
           IsBookmark = isBookmark
           IsLike = isLike
           IsReply = isReply
           IsRepost = isRepost
           IsMyMention = isMyMention
       }
   ```

4. **Update all default instances**: Search for `IsBookmark = false` patterns and add new field

### Adding XML Documentation

**Required for**: All public types, functions, members

**Template**:

```fsharp
/// <summary>
/// Brief description of what this does.
/// </summary>
/// <param name="paramName">Description of parameter.</param>
/// <returns>Description of return value.</returns>
/// <remarks>
/// Additional details, usage notes, edge cases.
/// </remarks>
member _.MethodName (paramName: Type) =
    // implementation
```

### Modifying Async Operations

**Pattern to maintain**:

```fsharp
// Service methods return Task<Result>
member _.DoSomethingAsync (data: InputType) =
    task {
        try
            let! result = someAsyncOp data
            return ValidationSuccess result
        with
        | ex -> return ValidationError $"Context: {ex.Message}"
    }
```

**Never**:
- Use `.Result` or `.Wait()` (blocks threads)
- Use `Async.RunSynchronously` in library code (only in tests/scripts)
- Mix `async {}` and `task {}` unnecessarily

## Important Patterns

### Function Composition

Services use `>>` for declarative pipelines:

```fsharp
let processData = 
    parseInput 
    >> validateInput 
    >> transformData 
    >> formatOutput
```

### Pattern Matching

Always use pattern matching for discriminated unions:

```fsharp
// Exhaustive - compiler checks all cases
match result with
| ValidationSuccess data -> processData data
| ValidationError error -> logError error

// ❌ Wrong - bypasses type safety
if result.IsSuccess then ...
```

### Modules vs Classes

- **Use Modules** for: Utilities, pure functions, constants
- **Use Classes** for: Services (DI compatibility), stateful objects

## Common Pitfalls to Avoid

### ❌ Don't: Throw exceptions in normal flow
```fsharp
if invalid then failwith "Error"  // Wrong
```

### ✓ Do: Return Result types
```fsharp
if invalid then ValidationError "Error"  // Correct
```

### ❌ Don't: Use mutable variables
```fsharp
let mutable result = None  // Avoid
```

### ✓ Do: Use immutable bindings and recursion
```fsharp
let rec processItems items acc = ...  // Correct
```

### ❌ Don't: Hardcode strings
```fsharp
let selector = "a[rel='webmention']"  // Wrong
```

### ✓ Do: Use Constants
```fsharp
let selector = EndpointSelectors.anchorElement  // Correct
```

### ❌ Don't: Block async operations
```fsharp
let result = asyncOp.Result  // Wrong - blocks
```

### ✓ Do: Use async throughout
```fsharp
let! result = asyncOp  // Correct - async
```

## Testing

### test.fsx Structure

Interactive test script for manual verification:

```fsharp
#r "./bin/Debug/netstandard2.1/WebmentionFs.dll"
#r "nuget:Microsoft.AspNetCore.Http.Abstractions"
#r "nuget:FSharp.Data"

open WebmentionFs
open WebmentionFs.Services

// Create services
let discoveryService = new UrlDiscoveryService()
let senderService = new WebmentionSenderService(discoveryService)

// Test data
let testData = {
    Source = new Uri("https://source.com/post")
    Target = new Uri("https://target.com/post")
}

// Execute and display
let result = 
    senderService.SendAsync(testData) 
    |> Async.AwaitTask 
    |> Async.RunSynchronously

match result with
| ValidationSuccess data -> printfn "✓ Success: %A" data
| ValidationError error -> printfn "✗ Error: %s" error
```

### Building and Testing

```bash
# Build
dotnet build

# Run test script
dotnet fsi test.fsx

# Check for warnings
dotnet build /warnaserror
```

## Extension Points

### Where to Add New Features

| Feature Type | Location | Pattern |
|--------------|----------|---------|
| New domain type | Domain.fs | Discriminated union or record |
| New constant | Constants.fs | Module with let bindings |
| New utility | Utils.fs | Pure function in module |
| New validation | RequestValidationService.fs | Function in pipeline |
| New discovery method | UrlDiscoveryService.fs | Async function, add to parallel discovery |
| New service | New .fs file | Class with interface, add to .fsproj |

### Safe Modification Areas

**Safe to modify** (low coupling):
- Constants.fs (just update usages)
- Adding new functions to Utils
- Adding new validation to RequestValidationService

**Requires care** (high impact):
- Domain.fs types (affects all code)
- Service interfaces (breaks compatibility)
- Result type changes (breaks consumers)

## Build and Compatibility

### Target Framework
- .NET Standard 2.1
- Compatible with .NET Core 3.0+, .NET 5+, .NET 6+

### Language Features Safe to Use
- task {} computation expression ✓
- String interpolation ($"text") ✓
- Pattern matching enhancements ✓
- Inline records ✓
- Anonymous records ✓

### Dependencies
- FSharp.Data (HTML parsing)
- Microsoft.AspNetCore.Http.Abstractions (HTTP types)

## Code Quality Checks

Before submitting changes:

1. **Build**: `dotnet build` - must succeed with 0 warnings
2. **Style**: Check .editorconfig compliance
3. **Documentation**: All public APIs have XML docs
4. **Constants**: No magic strings
5. **Error Handling**: All operations return Result types
6. **Async**: No blocking calls
7. **Tests**: test.fsx runs successfully

## Questions and Edge Cases

### Q: Should I use async {} or task {}?
**A**: Use `task {}` for all async operations. It's more performant and interops better with .NET Task-based APIs.

### Q: When should I add a new discriminated union vs. adding a field to a record?
**A**: Use DU for states that are mutually exclusive. Use record fields for properties that can coexist.

### Q: How do I handle multiple error conditions?
**A**: Chain Result types or use Railway Oriented Programming pattern (like the validation pipeline).

### Q: Should new services be modules or classes?
**A**: Use classes for DI compatibility with ASP.NET Core. Use modules for pure utilities.

### Q: How detailed should error messages be?
**A**: Include enough context to diagnose issues without exposing sensitive data. Add error message to Constants.ErrorMessages.

## Summary for AI Assistants

**Core Principles**:
1. ✓ Functional-first: immutable data, pure functions, explicit errors
2. ✓ Use discriminated unions for states
3. ✓ Use task {} for async
4. ✓ Use Constants module (no magic strings)
5. ✓ XML docs on all public APIs
6. ✗ Don't throw exceptions in normal flow
7. ✗ Don't block async operations
8. ✗ Don't use mutable state

**When in doubt**: Check existing code patterns, favor simplicity, and maintain type safety.
