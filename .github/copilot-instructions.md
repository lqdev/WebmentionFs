# Copilot Instructions for WebmentionFs

## Repository Overview

**WebmentionFs** is an F# library implementing the W3C Webmention specification for sending and receiving webmentions. It's a **small, focused library** targeting .NET Standard 2.1 with modern F# idioms.

- **Size**: ~8 F# source files (~35KB total code)
- **Language**: F# (functional-first)
- **Target**: .NET Standard 2.1 (compatible with .NET Core 3.0+, .NET 5+, .NET 6+, .NET 8+)
- **Dependencies**: FSharp.Data 5.0.2, Microsoft.AspNetCore.Http.Abstractions 2.2.0
- **Package**: Published to NuGet as `lqdev.WebmentionFs`

## Build & Validation

### Prerequisites
- .NET SDK 6.0+ (tested with .NET 8.0 and 10.0)
- F# compiler (included with .NET SDK)

### Build Commands (VALIDATED - All Working)

**Build from solution (recommended):**
```bash
# 1. Restore all projects
dotnet restore WebmentionFs.slnx

# 2. Build all projects (library + tests)
dotnet build WebmentionFs.slnx

# 3. Build Release
dotnet build WebmentionFs.slnx --configuration Release

# 4. Create NuGet package
dotnet pack src/WebmentionFs/WebmentionFs.fsproj --configuration Release
```

**Build library only:**
```bash
cd src/WebmentionFs
dotnet clean
dotnet restore
dotnet build
```

**Build Output**: `src/WebmentionFs/bin/Debug/netstandard2.1/WebmentionFs.dll`

### Testing

**XUnit tests** (proper test suite):
```bash
# Run all tests
dotnet test WebmentionFs.slnx

# Or from tests directory
cd tests/WebmentionFs.Tests
dotnet test
```

**Interactive test script** (`test.fsx` - for manual exploration):
```bash
dotnet fsi test.fsx
```

**Note**: `test.fsx` references `./src/WebmentionFs/bin/Debug/netstandard2.1/WebmentionFs.dll` - build the library first.

### Validation Checklist
- ✓ `dotnet build WebmentionFs.slnx` must succeed with 0 warnings, 0 errors
- ✓ `dotnet test WebmentionFs.slnx` must pass all XUnit tests
- ✓ XML documentation is generated (`GenerateDocumentationFile=true`)
- ✓ No F# specific formatter (fantomas) configured - rely on .editorconfig

### NO CI/CD Yet
The README mentions CI, but **no .github/workflows directory exists**. There are no automated checks to replicate. When making changes:
1. Run `dotnet build WebmentionFs.slnx` - must succeed with 0 warnings
2. Run `dotnet test WebmentionFs.slnx` - all tests must pass
3. Verify XML docs are present on public APIs
4. Follow F# conventions in CONTRIBUTING.md

## Project Structure

### Directory Layout

```
WebmentionFs/
├── .github/                    # GitHub configuration
├── src/
│   └── WebmentionFs/          # Main library project
│       ├── Domain.fs          # Core types (no dependencies)
│       ├── Constants.fs       # String constants, CSS selectors
│       ├── Utils.fs           # Pure utility functions (module)
│       ├── UrlDiscoveryService.fs
│       ├── RequestValidationService.fs
│       ├── WebmentionValidationService.fs
│       ├── WebmentionReceiverService.fs
│       ├── WebmentionSenderService.fs
│       └── WebmentionFs.fsproj
├── tests/
│   └── WebmentionFs.Tests/   # XUnit test project (.NET 8.0)
│       ├── Tests.fs
│       └── WebmentionFs.Tests.fsproj
├── WebmentionFs.slnx         # Solution file
├── test.fsx                  # Interactive test script
└── [docs, config files...]
```

### File Organization (CRITICAL - Order Matters)

F# projects require **dependency order** in .fsproj. Files in `src/WebmentionFs/` are compiled top-to-bottom:

```
Domain.fs → Constants.fs → Utils.fs → UrlDiscoveryService.fs 
→ RequestValidationService.fs → WebmentionValidationService.fs 
→ WebmentionReceiverService.fs → WebmentionSenderService.fs
```

**Never reorder files in WebmentionFs.fsproj** - it will break compilation.

### Key Directories
- `src/WebmentionFs/` - Main library source files (.NET Standard 2.1)
- `tests/WebmentionFs.Tests/` - XUnit test project (.NET 8.0)
- `src/WebmentionFs/bin/` - Build output (gitignored)
- `src/WebmentionFs/obj/` - Build artifacts (gitignored)
- `.github/` - GitHub configuration

### Configuration Files
- `.editorconfig` - F# formatting rules (4 spaces, 120 char lines, LF endings)
- `.gitignore` - Standard Visual Studio/Rider/VS Code ignores
- `.devcontainer.json` - VS Code devcontainer with Ionide F# extension
- `WebmentionFs.slnx` - Solution file (XML format)
- `src/WebmentionFs/WebmentionFs.fsproj` - Library project file
- `tests/WebmentionFs.Tests/WebmentionFs.Tests.fsproj` - Test project file

### Documentation Files (Read These First!)
1. **CONTRIBUTING.md** - F# conventions, naming, async patterns, error handling
2. **ARCHITECTURE.md** - Design philosophy, module descriptions, extension points
3. **AI_ASSISTANT_GUIDE.md** - AI-specific guidance, patterns, common tasks
4. **README.md** - User documentation, examples, API overview

## Coding Conventions (ENFORCE STRICTLY)

### Must Follow
1. **camelCase** for values/functions, **PascalCase** for types/modules
2. **Never use magic strings** - add to Constants module
3. **Discriminated unions for results** - never throw exceptions in normal flow
4. **Use `task {}` for async** - never `.Result` or `.Wait()`
5. **XML docs on ALL public APIs** - build generates documentation file
6. **Pure functions in modules** - Utils and Constants are modules, not classes
7. **Services are classes** - for DI compatibility with ASP.NET Core

### Common Patterns
```fsharp
// Good: Discriminated union result
match result with
| ValidationSuccess data -> // handle success
| ValidationError error -> // handle error

// Good: task {} async
let fetchAsync url =
    task {
        let! response = httpClient.GetAsync(url)
        return! response.Content.ReadAsStringAsync()
    }

// Good: Constants usage
open Constants
let selector = EndpointSelectors.linkElement

// Bad: Magic string
let selector = "link[rel='webmention']"  // ❌ NEVER DO THIS

// Bad: Throwing exceptions
if invalid then failwith "Error"  // ❌ Return ValidationError instead
```

## Making Changes

### Where to Add Code
| Feature Type | File | Pattern |
|-------------|------|---------|
| New domain type | src/WebmentionFs/Domain.fs | Add discriminated union or record |
| New constant/selector | src/WebmentionFs/Constants.fs | Add to appropriate module |
| New utility function | src/WebmentionFs/Utils.fs | Add pure function to module |
| New validation rule | src/WebmentionFs/RequestValidationService.fs | Add to validation pipeline |
| New service | src/WebmentionFs/NewService.fs | Create class, add to .fsproj AFTER all dependencies |
| New test | tests/WebmentionFs.Tests/Tests.fs | Add XUnit test with [<Fact>] or [<Theory>] |

### Before Committing
1. Run `dotnet build WebmentionFs.slnx` - verify 0 warnings, 0 errors
2. Run `dotnet test WebmentionFs.slnx` - all tests must pass
3. Check XML docs on any new public APIs
4. Verify no magic strings - use Constants module
5. Ensure discriminated unions for all results
6. Confirm async uses `task {}` not blocking calls

### Common Gotchas
- **File order matters** - dependencies must come before dependents in src/WebmentionFs/WebmentionFs.fsproj
- **Paths changed** - source files now in `src/WebmentionFs/`, not at root
- **Solution file** - use `WebmentionFs.slnx` to build both library and tests
- **Test project** - XUnit tests in `tests/WebmentionFs.Tests/` target .NET 8.0
- **test.fsx path** - references `./src/WebmentionFs/bin/Debug/netstandard2.1/WebmentionFs.dll`

## Key Files Quick Reference

**src/WebmentionFs/Domain.fs** (99 lines) - All core types: UrlData, ValidationResult, DiscoveryResult, WebmentionValidationResult, MentionTypes  
**src/WebmentionFs/Constants.fs** (138 lines) - EndpointSelectors, MentionSelectors, Http constants, FormFields, ErrorMessages  
**src/WebmentionFs/Utils.fs** (84 lines) - getSourceAndTargetUrlsFromFormBody, getDocumentHeadersAsync, getDocumentContentAsync, getUrlFromSourceDocument  
**src/WebmentionFs/Services** (5 files, ~4-8KB each) - Complete webmention send/receive implementation  
**tests/WebmentionFs.Tests/Tests.fs** - XUnit tests for core domain types and services  
**WebmentionFs.slnx** - Solution file organizing library and test projects  
**test.fsx** - Interactive test script (references built DLL from src/WebmentionFs/bin/)

## Trust These Instructions

**These instructions are based on actual testing** of all build commands, exploration of all source files, and review of all documentation. Only search the codebase if:
1. You need to understand implementation details of a specific function
2. These instructions are incomplete for your specific task
3. You find information here that contradicts actual behavior

For general understanding of F# patterns, error handling, async, and architecture - **trust CONTRIBUTING.md and ARCHITECTURE.md** - they are comprehensive and accurate.
