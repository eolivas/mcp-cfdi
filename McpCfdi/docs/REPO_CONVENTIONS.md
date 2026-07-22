# Repository Conventions

## Commit Messages

This project follows [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).

### Format

```
<type>(<scope>): <short description>

[optional body]

[optional footer(s)]
```

### Types

| Type | Description |
|------|-------------|
| `feat` | A new feature |
| `fix` | A bug fix |
| `docs` | Documentation-only changes |
| `style` | Code style changes (formatting, whitespace) that do not affect logic |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `perf` | Performance improvement |
| `test` | Adding or updating tests |
| `build` | Changes to build system or dependencies |
| `ci` | Changes to CI/CD configuration |
| `chore` | Maintenance tasks (tooling, config, no production code) |
| `revert` | Reverts a previous commit |

### Scopes

Use the project layer or module as scope:

- `domain` — Changes in McpCfdi.Domain
- `application` — Changes in McpCfdi.Application
- `infrastructure` — Changes in McpCfdi.Infrastructure
- `api` — Changes in McpCfdi.Api
- `tests` — Changes across test projects
- `docs` — Documentation changes
- `docker` — Dockerfile or container configuration
- `deps` — Dependency updates

### Examples

```
feat(domain): add Comprobante aggregate with factory method
fix(application): correct decimal rounding in tax calculations
docs: update README with project summary
refactor(infrastructure): extract outbox logic to dedicated service
test(domain): add property-based tests for MontoMoneda value object
build(deps): bump MediatR to 12.4.1
ci: add GitHub Actions build workflow
```

## Branch Strategy

### Branch Naming

```
<type>/<short-description>
```

Examples:

- `feat/generar-cfdi-command`
- `fix/rfc-validation-regex`
- `docs/add-adr-records`
- `refactor/extract-catalog-service`

### Rules

- `main` is the stable branch. Never push directly to `main`.
- All changes go through pull requests.
- Branches are deleted after merging.

## Pull Requests

### Title

Follow the same Conventional Commits format as commit messages:

```
feat(domain): add Emisor and Receptor entities
```

### Description Template

```markdown
## Summary
Brief description of what this PR does.

## Changes
- Change 1
- Change 2

## Testing
How was this tested? (unit tests, manual, integration)

## Notes
Any additional context, breaking changes, or follow-up work.
```

### Rules

- Keep PRs focused on a single concern.
- Link related issues using `Closes #123` or `Relates to #456`.
- Ensure all checks pass before requesting review.
- Squash-merge to keep `main` history clean.

## Code Style

### C# Conventions

- Follow Microsoft's [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- Use `nullable` reference types (enabled globally via `Directory.Build.props`).
- Treat all warnings as errors (`TreatWarningsAsErrors` enabled).
- Use file-scoped namespaces.
- Use `sealed` on classes that are not designed for inheritance.
- Prefer records for DTOs and value objects where appropriate.

### Naming

| Element | Convention | Example |
|---------|-----------|---------|
| Namespace | PascalCase matching folder path | `McpCfdi.Domain.Entities` |
| Class / Record | PascalCase | `GenerarCfdiCommandHandler` |
| Interface | `I` prefix + PascalCase | `ICatalogoSatService` |
| Method | PascalCase | `CalcularTotales` |
| Property | PascalCase | `LugarExpedicion` |
| Private field | `_camelCase` | `_catalogoService` |
| Local variable | camelCase | `decimalesMoneda` |
| Constant | PascalCase | `RfcMoralPattern` |

### Project Organization

- One class per file (exceptions: nested private types, closely related records).
- File name matches the primary type name.
- Follow Clean Architecture dependency rule: inner layers never reference outer layers.

## Issues

### Labels

| Label | Purpose |
|-------|---------|
| `bug` | Something isn't working |
| `feature` | New feature request |
| `enhancement` | Improvement to existing functionality |
| `documentation` | Documentation updates |
| `question` | Needs discussion or clarification |
| `good first issue` | Simple tasks for new contributors |

### Issue Title

Use a clear, concise title that describes the problem or feature:

- Good: "RFC validation rejects valid persona moral format"
- Bad: "Fix bug"

## Versioning

This project uses [Semantic Versioning](https://semver.org/):

- **MAJOR** — Breaking changes to the MCP tool contract or public API.
- **MINOR** — New features (e.g., new CFDI types, new MCP tools) that are backward-compatible.
- **PATCH** — Bug fixes and minor improvements.
