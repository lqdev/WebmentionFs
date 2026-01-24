# WebmentionFs Architecture

## Overview

WebmentionFs is a functional-first F# library implementing the [W3C Webmention specification](https://www.w3.org/TR/webmention/). The library provides a complete solution for both sending and receiving webmentions, with a focus on idiomatic F# patterns, type safety, and composability.

## Design Philosophy

### Functional-First Approach

WebmentionFs embraces F# functional programming principles:

- **Immutability**: All domain types are immutable records and discriminated unions
- **Pure Functions**: Core logic is implemented as pure, composable functions
- **Explicit Error Handling**: Uses discriminated unions (`Result` types) for explicit error states
- **Type Safety**: Leverages F#'s type system to prevent invalid states at compile time
- **Function Composition**: Validation pipelines use function composition (`>>`) for clarity

### Architecture Layers

```
┌─────────────────────────────────────────────────────┐
│          Service Layer (Classes)                    │
│  WebmentionSenderService, WebmentionReceiverService │
│  UrlDiscoveryService, ValidationServices            │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│          Utility Layer (Module)                     │
│  HTTP operations, form parsing, HTML processing     │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│          Domain Layer (Types)                       │
│  UrlData, ValidationResults, Webmention types       │
└─────────────────────────────────────────────────────┘
```

## Target Framework

- **Target**: .NET Standard 2.1
- **Rationale**: Maximizes compatibility across .NET Core 3.0+, .NET 5+, and modern .NET while enabling use of latest F# language features
- **F# Language**: Uses modern F# features (task CE, string interpolation, pattern matching enhancements) that compile to .NET Standard 2.1

## Core Modules

### Domain.fs

Defines the core domain types using F# discriminated unions and records:

- **UrlData**: Immutable record containing source and target URLs
- **Result Types**: Discriminated unions for explicit success/failure states
  - `ValidationResult<'a>`: Generic success/error result
  - `DiscoveryResult`: Endpoint discovery result
  - `RequestValidationResult`: Request validation result
  - `WebmentionValidationResult`: Content validation result
- **MentionTypes**: Record classifying mention types (like, reply, repost, bookmark)

**Design Rationale**: Using discriminated unions makes invalid states unrepresentable and forces consumers to handle all cases explicitly via pattern matching.

### Constants.fs

Centralizes magic strings, CSS selectors, and error messages:

- **EndpointSelectors**: CSS selectors for discovering webmention endpoints
- **MentionSelectors**: Microformat CSS selectors for mention classification
- **Http**: HTTP-related constants (schemes, headers, content types)
- **FormFields**: Standard form field names
- **ErrorMessages**: Consistent, descriptive error messages

**Design Rationale**: Eliminates magic strings, improves maintainability, and enables consistent error messages throughout the library.

### Utils.fs (Module)

Provides pure utility functions for low-level operations:

- `getSourceAndTargetUrlsFromFormBody`: Parses HTTP form data
- `getDocumentHeadersAsync`: Fetches HTTP headers without body
- `getDocumentContentAsync`: Fetches HTML document content
- `getUrlFromSourceDocument`: Extracts URLs using CSS selectors

**Design Rationale**: Implemented as a module with pure functions rather than a class, following F# conventions. These functions are the building blocks for higher-level services.

### Service Layer

Services are implemented as classes for compatibility with dependency injection frameworks and imperative hosting environments (ASP.NET Core, console apps):

#### UrlDiscoveryService

Discovers webmention endpoints using three methods per W3C spec:
1. HTTP Link headers
2. HTML `<link>` elements
3. HTML `<a>` elements

**Key Pattern**: Tries all methods in parallel, returns first successful result.

#### RequestValidationService

Validates incoming webmention requests through a composed validation pipeline:

```fsharp
let validateAsync = 
    isProtocolValid >> isSameUrl >> isTargetUrlValidAsync
```

**Key Pattern**: Function composition creates a clear, declarative validation pipeline.

#### WebmentionValidationService

Analyzes source documents for mentions of target URLs, classifying them using IndieWeb microformat annotations (u-like-of, u-in-reply-to, etc.).

**Key Pattern**: Searches for multiple mention types, returning rich classification data.

#### WebmentionSenderService

Combines discovery and sending:
1. Discovers endpoint
2. POSTs webmention data
3. Returns result

**Key Pattern**: Orchestrates multiple async operations, using discriminated unions for clear result handling.

#### WebmentionReceiverService

Complete pipeline for receiving webmentions:
1. Validates request (protocol, ownership, accessibility)
2. Validates content (source mentions target)
3. Classifies mention type
4. Returns rich webmention data

**Key Pattern**: Composes validation services, handles multiple error conditions gracefully.

## Async Patterns

WebmentionFs uses modern F# async patterns:

- **task {}**: Primary computation expression for async operations
- **Task<T>**: Return type for public APIs (ASP.NET Core compatibility)
- **Async interop**: Avoids legacy blocking patterns (`.Result`, `.Wait()`)

**Design Rationale**: `task {}` provides better performance and simpler interop with .NET async APIs while maintaining F# async semantics.

## Error Handling Strategy

### Explicit Error Types

Every operation that can fail returns a discriminated union:

```fsharp
type ValidationResult<'a> = 
    | ValidationSuccess of 'a
    | ValidationError of string
```

**Benefits**:
- Compile-time guarantee that errors are handled
- No hidden exceptions in normal flow
- Self-documenting API (types show failure modes)

### Exception Handling

Exceptions are caught at service boundaries and converted to Result types:

```fsharp
try
    let source = req.Form["source"].ToString() |> Uri
    ParseSuccess { Source=source; Target=target }
with
    | ex -> ParseError $"{ex}"
```

**Rationale**: Allows library consumers to handle errors functionally without try/catch blocks.

## Extension Points

### Adding New Validation Rules

Extend `RequestValidationService` by composing additional validation functions:

```fsharp
let customValidation (result: RequestValidationResult) = 
    // Your validation logic
    result

let validateAsync = 
    isProtocolValid 
    >> isSameUrl 
    >> customValidation  // Add here
    >> isTargetUrlValidAsync
```

### Adding New Mention Types

1. Add CSS selector to `Constants.MentionSelectors`
2. Add field to `MentionTypes` record
3. Update `WebmentionValidationService.findMentionsInSourceDocument`

### Custom Discovery Methods

Implement additional discovery strategies in `UrlDiscoveryService`:

```fsharp
let discoverFromCustomMethod (data: UrlData) = 
    task {
        // Custom discovery logic
        return DiscoverySuccess endpoint
    }
```

Add to parallel discovery in `discoverUrlAsync`.

## Dependencies

- **FSharp.Data**: HTML parsing and CSS selectors
- **Microsoft.AspNetCore.Http.Abstractions**: HTTP request/response types (for ASP.NET Core integration)

**Rationale**: Minimal dependencies, using well-established libraries. FSharp.Data provides powerful HTML parsing with F# type providers.

## Testing Strategy

### Unit Tests

Test individual functions and validation logic:
- Pure functions in `Utils` module
- Validation predicates
- Discovery logic

### Integration Tests

Test complete workflows:
- Endpoint discovery against real HTML
- Full send/receive pipelines
- Error scenarios

### test.fsx

Interactive testing script for manual verification and experimentation.

## Performance Considerations

- **Parallel Discovery**: Endpoint discovery methods run in parallel for faster results
- **Lazy Evaluation**: Uses sequences and lazy operations where appropriate
- **Async Throughout**: All I/O operations are async, avoiding thread pool starvation
- **Immutable Data**: Reduces GC pressure, enables safe concurrent access

## F# Idioms Used

1. **Discriminated Unions for State**: All result types use DUs
2. **Records for Data**: All domain entities are immutable records
3. **Modules for Utilities**: Utils and Constants are modules, not classes
4. **Function Composition**: Validation pipelines use `>>`
5. **Pattern Matching**: All DU results are handled via exhaustive pattern matching
6. **Type Inference**: Minimal type annotations, leveraging F#'s inference
7. **Pipeline Operator**: Uses `|>` for data flow clarity
8. **Computation Expressions**: Uses `task {}` for async operations

## Future Enhancements

Potential areas for expansion:

1. **Rate Limiting**: Add support for respecting rate limits
2. **Retry Policies**: Implement configurable retry for network failures
3. **Caching**: Cache discovered endpoints to reduce network calls
4. **Telemetry**: Add structured logging and metrics
5. **Batch Operations**: Support sending multiple webmentions efficiently
6. **More Result Types**: Consider using Result<'T, 'TError> instead of string errors

## Contributing

When contributing to WebmentionFs, follow these principles:

1. **Maintain Functional Style**: Prefer immutable data and pure functions
2. **Use Type Safety**: Leverage discriminated unions for states
3. **Explicit Errors**: Return Result types, don't throw exceptions
4. **Document Thoroughly**: Add XML docs to all public APIs
5. **Test Coverage**: Add tests for new functionality
6. **Keep It Simple**: Favor clarity over cleverness

## References

- [W3C Webmention Specification](https://www.w3.org/TR/webmention/)
- [F# Language Guide](https://learn.microsoft.com/en-us/dotnet/fsharp/)
- [IndieWeb Microformats](https://indieweb.org/microformats)
- [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
