# Contributing to WebmentionFs

Thank you for your interest in contributing to WebmentionFs! This document provides guidelines for contributing to this F# library.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [F# Coding Conventions](#f-coding-conventions)
- [Development Workflow](#development-workflow)
- [Testing](#testing)
- [Documentation](#documentation)
- [Submitting Changes](#submitting-changes)

## Code of Conduct

This project follows a standard code of conduct. Please be respectful and constructive in all interactions.

## Getting Started

### Prerequisites

- [.NET SDK 6.0+](https://dotnet.microsoft.com/download) (supports .NET Standard 2.1)
- F# compiler (included with .NET SDK)
- Editor with F# support (VS Code with Ionide, Visual Studio, or Rider)

### Building the Project

```bash
git clone https://github.com/lqdev/WebmentionFs.git
cd WebmentionFs
dotnet build
```

### Running Tests

```bash
# Interactive testing
dotnet fsi test.fsx
```

## F# Coding Conventions

WebmentionFs follows modern F# best practices. Please adhere to these conventions:

### Naming Conventions

- **Values and Functions**: Use `camelCase`
  ```fsharp
  let validateRequest data = ...
  let isValidUrl url = ...
  ```

- **Types**: Use `PascalCase`
  ```fsharp
  type UrlData = { Source: Uri; Target: Uri }
  type ValidationResult<'a> = ...
  ```

- **Modules**: Use `PascalCase`
  ```fsharp
  module Utils = ...
  module Constants = ...
  ```

- **Type Parameters**: Use descriptive names with single quote prefix
  ```fsharp
  type ValidationResult<'a> = ...  // Generic
  type Result<'TSuccess, 'TError> = ...  // More descriptive
  ```

### Code Style

- **Indentation**: 4 spaces (configured in .editorconfig)
- **Line Length**: Maximum 120 characters
- **Spacing**: Space after commas, around operators
  ```fsharp
  let list = [1; 2; 3]  // Space after semicolon
  let sum = a + b       // Spaces around operator
  ```

### F# Idioms to Follow

#### 1. Prefer Immutability

Use immutable records and let bindings:

```fsharp
// Good
type UrlData = { Source: Uri; Target: Uri }

// Avoid (unless required by dependencies)
type UrlData() =
    member val Source = Uri("") with get, set
```

#### 2. Use Discriminated Unions for State

Model states explicitly:

```fsharp
// Good
type ValidationResult<'a> = 
    | ValidationSuccess of 'a
    | ValidationError of string

// Avoid
type ValidationResult<'a> = 
    { Success: bool; Value: 'a option; Error: string option }
```

#### 3. Pattern Matching over Conditionals

```fsharp
// Good
match result with
| ValidationSuccess data -> processData data
| ValidationError error -> logError error

// Avoid
if result.Success then
    processData result.Value.Value
else
    logError result.Error.Value
```

#### 4. Function Composition

Use `>>` for composing functions:

```fsharp
// Good
let validateAsync = 
    isProtocolValid >> isSameUrl >> isTargetUrlValidAsync

// Less idiomatic
let validateAsync data =
    data
    |> isProtocolValid
    |> isSameUrl
    |> isTargetUrlValidAsync
```

#### 5. Pipeline Operator

Use `|>` for data flow:

```fsharp
// Good
data
|> List.filter isValid
|> List.map transform
|> List.head

// Less clear
List.head (List.map transform (List.filter isValid data))
```

#### 6. Modules for Utilities

Use modules with functions instead of static classes:

```fsharp
// Good
module Utils =
    let parseForm req = ...
    let fetchDocument uri = ...

// Avoid (in F# projects)
type Utils =
    static member ParseForm(req) = ...
    static member FetchDocument(uri) = ...
```

#### 7. Type Inference

Let F# infer types when clear:

```fsharp
// Good
let isValid url = url.IsAbsoluteUri

// Unnecessary annotation (unless for documentation)
let isValid (url: Uri) : bool = url.IsAbsoluteUri
```

### Async Patterns

Use modern async patterns:

```fsharp
// Good - Use task CE
let fetchDataAsync url =
    task {
        let! response = client.GetAsync(url)
        let! content = response.Content.ReadAsStringAsync()
        return content
    }

// Avoid - Legacy blocking
let fetchData url =
    let response = client.GetAsync(url).Result  // Blocks!
    response.Content.ReadAsStringAsync().Result
```

### Error Handling

Return Result types, don't throw exceptions:

```fsharp
// Good
let parseData input =
    try
        ParseSuccess (parseLogic input)
    with
    | ex -> ParseError $"Failed to parse: {ex.Message}"

// Avoid throwing in normal flow
let parseData input =
    parseLogic input  // throws exception
```

### Documentation

#### XML Documentation

All public APIs must have XML documentation:

```fsharp
/// <summary>
/// Validates that the source document contains a mention of the target URL.
/// </summary>
/// <param name="source">The source URI to fetch and analyze.</param>
/// <param name="target">The target URI that should be mentioned.</param>
/// <returns>
/// A task with AnnotatedMention, UnannotatedMention, or MentionError result.
/// </returns>
member _.ValidateAsync (source: Uri) (target: Uri) = ...
```

#### Inline Comments

Use comments to explain "why", not "what":

```fsharp
// Good
// Remove angle brackets per W3C spec format: <url>; rel="webmention"
let sanitizedUrl = url.Replace("<", "").Replace(">", "")

// Unnecessary
// Create a new URI
let uri = new Uri(url)
```

### File Organization

Files should be ordered in the project file based on dependencies:

1. Domain types (no dependencies)
2. Constants (depends on Domain)
3. Utilities (depends on Domain, Constants)
4. Services (depends on all above)

## Development Workflow

### Making Changes

1. **Create a Branch**: Use descriptive branch names
   ```bash
   git checkout -b feature/add-validation
   git checkout -b fix/endpoint-discovery
   ```

2. **Make Small Commits**: Commit logical units of work
   ```bash
   git commit -m "Add validation for duplicate URLs"
   git commit -m "Extract error messages to Constants"
   ```

3. **Follow Conventions**: Use the style guide above

4. **Test Your Changes**: Ensure all tests pass

### Code Review Checklist

Before submitting, verify:

- [ ] Code follows F# idioms and conventions
- [ ] All public APIs have XML documentation
- [ ] No magic strings (use Constants module)
- [ ] Explicit error handling (Result types)
- [ ] Uses modern async patterns (task CE)
- [ ] Tests added for new functionality
- [ ] Build succeeds with no warnings
- [ ] Changes are minimal and focused

## Testing

### Unit Tests

Test pure functions and validation logic:

```fsharp
// Example test structure
let testValidation () =
    let result = validateUrl validUrl
    match result with
    | ValidationSuccess _ -> printfn "✓ Test passed"
    | ValidationError e -> failwith $"Test failed: {e}"
```

### Integration Tests

Test end-to-end workflows:

```fsharp
// Test actual HTTP calls and HTML parsing
let testEndpointDiscovery () =
    let service = UrlDiscoveryService()
    let data = { Source = uri1; Target = uri2 }
    let result = service.DiscoverEndpointAsync(data) |> Async.AwaitTask |> Async.RunSynchronously
    // Assert result
```

### Test Script (test.fsx)

Use for manual verification:

```bash
dotnet fsi test.fsx
```

## Documentation

### When to Update Documentation

- Adding new public APIs
- Changing existing behavior
- Adding new features
- Fixing bugs that affect API contracts

### Documentation Files

- **README.md**: User-facing documentation, examples
- **ARCHITECTURE.md**: Design decisions, module relationships
- **CONTRIBUTING.md**: This file
- **AI_ASSISTANT_GUIDE.md**: Guidance for AI assistants
- **XML Comments**: All public APIs

## Submitting Changes

### Pull Request Process

1. **Update Documentation**: Ensure all docs reflect changes
2. **Add Tests**: Cover new functionality
3. **Run All Tests**: Verify nothing breaks
4. **Write Clear PR Description**: Explain what and why
5. **Reference Issues**: Link related issues

### PR Description Template

```markdown
## Description
Brief description of changes

## Motivation
Why is this change needed?

## Changes
- Added X
- Fixed Y
- Updated Z

## Testing
How was this tested?

## Checklist
- [ ] Follows F# conventions
- [ ] XML docs added
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] Build passes
```

### Review Process

1. Maintainers will review your PR
2. Address feedback and questions
3. Once approved, changes will be merged
4. Your contribution will be included in next release

## Questions?

If you have questions:

- Open an issue for discussion
- Check existing issues and PRs
- Review documentation files

## Resources

- [F# Style Guide](https://learn.microsoft.com/en-us/dotnet/fsharp/style-guide/)
- [F# Coding Conventions](https://github.com/demystifyfp/FSharp.styleguide)
- [Effective F#](https://fsharpforfunandprofit.com/)
- [W3C Webmention Spec](https://www.w3.org/TR/webmention/)

Thank you for contributing to WebmentionFs!
