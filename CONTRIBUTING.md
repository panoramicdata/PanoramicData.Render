# Contributing to PanoramicData.Render

We welcome contributions! Please follow these guidelines to keep things smooth.

## Getting Started

1. Fork the repository
2. Create a feature branch from `main`
3. Make your changes
4. Ensure all tests pass (`dotnet test`)
5. Open a pull request

## Development Requirements

- .NET 10.0 SDK
- An IDE that supports .editorconfig (e.g. Visual Studio, Rider, VS Code with C# Dev Kit)
- Access to font files (`.ttf`, `.otf`) for test documents

## Code Standards

- Follow the `.editorconfig` rules strictly
- Use file-scoped namespaces
- Add XML documentation comments to all public members
- Prefer `System.Text.Json` over `Newtonsoft.Json`
- Use `Microsoft.Extensions.Logging.Abstractions` for logging — no `Console.Write` or `Trace`
- Keep `TreatWarningsAsErrors` enabled — fix warnings, don't suppress them
- Never suppress warnings without justification in a comment
- Use `StringBuilder` or XML APIs for SVG generation — never string concatenation
- Use central package management — never inline version numbers in `.csproj`

## Architecture Principles

- Internal unit is **twips** (1/1440 inch)
- Line breaking uses the **Knuth-Plass** algorithm (not greedy)
- Style resolution must follow the **full OOXML cascade** including toggle properties
- Layout and rendering are separated via `IRenderTarget`
- All I/O-bound operations support `CancellationToken`

## Pull Request Checklist

- [ ] Code compiles with zero warnings
- [ ] All existing tests pass
- [ ] New features have corresponding tests
- [ ] XML documentation on public API
- [ ] No Newtonsoft.Json references
- [ ] No suppressed warnings without justification
- [ ] No inline package version numbers

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
