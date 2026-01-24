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

**ALWAYS run commands in this exact order:**

```bash
# 1. Clean build (takes ~0.7s)
dotnet clean

# 2. Restore packages (takes ~2-3s on first run, instantaneous after)
dotnet restore

# 3. Build Debug (takes ~3-4s after restore, ~10s from clean)
dotnet build

# 4. Build Release (takes ~3-4s)
dotnet build --configuration Release

# 5. Create NuGet package (only if needed)
dotnet pack --configuration Release
```

**Build Output**: `bin/Debug/netstandard2.1/WebmentionFs.dll` or `bin/Release/netstandard2.1/WebmentionFs.dll`

### Testing

**Interactive test script** (`test.fsx`):
```bash
dotnet fsi test.fsx
```

**KNOWN ISSUE**: The test script has a version compatibility issue with FSharp.Data when running via `dotnet fsi`. The library builds successfully, but the test script may fail with `TypeLoadException` related to `FSharp.Data.HttpMethod`. This does NOT affect the compiled library - only the interactive test script. You can safely ignore test.fsx failures if the build succeeds.

### Validation Checklist
- ✓ `dotnet build` must succeed with 0 warnings, 0 errors
- ✓ XML documentation is generated (`GenerateDocumentationFile=true`)
- ✓ No F# specific formatter (fantomas) configured - rely on .editorconfig
- ✓ No formal unit tests - validation done via test.fsx (interactive)

### NO CI/CD Yet
The README mentions CI, but **no .github/workflows directory exists**. There are no automated checks to replicate. When making changes:
1. Run `dotnet build` - must succeed with 0 warnings
2. Verify XML docs are present on public APIs
3. Follow F# conventions in CONTRIBUTING.md

## Project Structure

### File Organization (CRITICAL - Order Matters)

F# projects require **dependency order** in .fsproj. Files are compiled top-to-bottom:

```
WebmentionFs.fsproj (defines compilation order)
├── Domain.fs                      # Core types (no dependencies)
├── Constants.fs                   # String constants, CSS selectors
├── Utils.fs                       # Pure utility functions (module)
├── UrlDiscoveryService.fs         # Endpoint discovery service
├── RequestValidationService.fs    # Request validation service
├── WebmentionValidationService.fs # Content validation service
├── WebmentionReceiverService.fs   # Complete receive pipeline
└── WebmentionSenderService.fs     # Complete send pipeline
```

**Never reorder files in .fsproj** - it will break compilation.

### Key Directories
- `/` - Source files at root (no `src/` directory)
- `bin/` - Build output (gitignored)
- `obj/` - Build artifacts (gitignored)
- `.github/` - GitHub configs (you may be creating this)
- No tests/ or samples/ directories exist

### Configuration Files
- `.editorconfig` - F# formatting rules (4 spaces, 120 char lines, LF endings)
- `.gitignore` - Standard Visual Studio/Rider/VS Code ignores
- `.devcontainer.json` - VS Code devcontainer with Ionide F# extension
- `WebmentionFs.fsproj` - Project file (XML, 2-space indent)

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
| New domain type | Domain.fs | Add discriminated union or record |
| New constant/selector | Constants.fs | Add to appropriate module |
| New utility function | Utils.fs | Add pure function to module |
| New validation rule | RequestValidationService.fs | Add to validation pipeline |
| New service | New .fs file | Create class, add to .fsproj AFTER all dependencies |

### Before Committing
1. Run `dotnet build` - verify 0 warnings, 0 errors (takes ~3-4s)
2. Check XML docs on any new public APIs
3. Verify no magic strings - use Constants module
4. Ensure discriminated unions for all results
5. Confirm async uses `task {}` not blocking calls

### Common Gotchas
- **File order matters** - dependencies must come before dependents in .fsproj
- **No .github directory** - you may need to create it for workflows
- **test.fsx has known issue** - ignore FSharp.Data TypeLoadException in test script
- **Build from clean is slower** - first build after clean takes ~10s, incremental builds ~3-4s
- **No separate test project** - testing is via test.fsx interactive script

## Key Files Quick Reference

**Domain.fs** (99 lines) - All core types: UrlData, ValidationResult, DiscoveryResult, WebmentionValidationResult, MentionTypes  
**Constants.fs** (138 lines) - EndpointSelectors, MentionSelectors, Http constants, FormFields, ErrorMessages  
**Utils.fs** (84 lines) - getSourceAndTargetUrlsFromFormBody, getDocumentHeadersAsync, getDocumentContentAsync, getUrlFromSourceDocument  
**Services** (5 files, ~4-8KB each) - Complete webmention send/receive implementation

## Trust These Instructions

**These instructions are based on actual testing** of all build commands, exploration of all source files, and review of all documentation. Only search the codebase if:
1. You need to understand implementation details of a specific function
2. These instructions are incomplete for your specific task
3. You find information here that contradicts actual behavior

For general understanding of F# patterns, error handling, async, and architecture - **trust CONTRIBUTING.md and ARCHITECTURE.md** - they are comprehensive and accurate.
