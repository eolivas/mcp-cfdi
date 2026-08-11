# Architecture Selection: pac-timbrado

## Recommended Architecture: Use-Case Oriented (Command per Operation)

### Rationale
Candidate A has the lowest god object score (11%) and best evolvability — adding a new PAC requires exactly one new class with zero modification to existing code (OCP). It aligns with the existing CQRS/MediatR pattern already established in the project, making the PAC integration feel like a natural extension rather than a separate subsystem. The trade-off is slightly more files than a monolithic gateway, but in a project already structured around commands and handlers, this is consistency, not overhead.

### Components
| Component | Owned State | Responsibility |
|-----------|-------------|----------------|
| TimbrarCfdiCommand + Handler | None (stateless) | Validates XML contains sello/certificado, delegates to IPacService.TimbrarAsync, returns structured result |
| CancelarCfdiCommand + Handler | None | Validates motivo/UUID/sustitucion rules, delegates to IPacService.CancelarAsync |
| ConsultarEstatusCfdiQuery + Handler | None | Validates RFC/UUID/Total inputs, delegates to IPacService.ConsultarEstatusAsync |
| IPacService (interface, Application) | None | Port abstracting PAC operations: Timbrar, Cancelar, ConsultarEstatus |
| PacServiceFactory (Infrastructure) | ActiveProvider config reference | Resolves correct IPacService implementation from DI based on configuration |
| MultifacturasPacAdapter (Infrastructure) | HttpClient instance, PAC credentials | Translates IPacService contract to Multifacturas REST JSON API calls |
| PacResilienceDecorator (Infrastructure) | CircuitBreaker state | Wraps IPacService with retry (3x backoff) + circuit breaker (5 fails / 30s) |
| MCP Tools: timbrar_cfdi, cancelar_cfdi, consultar_estatus_cfdi (Api) | None | MCP tool entry points dispatching to MediatR |
| PacOptions (Infrastructure) | Configuration values | Strongly-typed options for PAC provider selection and per-provider settings |

### Information Flow
| From \ To | MCP Tools | Commands/Queries | IPacService | PacFactory | Adapter | ResilienceDecorator | Config |
|-----------|:---------:|:----------------:|:-----------:|:----------:|:-------:|:-------------------:|:------:|
| MCP Tools | - | → | - | - | - | - | - |
| Commands/Queries | - | - | → | - | - | - | - |
| IPacService | - | - | - | - | - | - | - |
| PacFactory | - | - | - | - | → | - | ← |
| Adapter | - | - | - | - | - | - | ← |
| ResilienceDecorator | - | - | - | - | → | - | - |
| Config | - | - | - | - | - | - | - |

### Requirement Allocation
| Requirement | Component(s) |
|-------------|--------------|
| REQ-1 | TimbrarCfdiCommand + Handler, IPacService, MultifacturasPacAdapter |
| REQ-2 | CancelarCfdiCommand + Handler, IPacService, MultifacturasPacAdapter |
| REQ-3 | ConsultarEstatusCfdiQuery + Handler, IPacService, MultifacturasPacAdapter |
| REQ-4 | IPacService, PacServiceFactory, PacOptions |
| REQ-5 | MultifacturasPacAdapter |
| REQ-6 | CancelarCfdiCommand + Handler (passes creds, never persists) |
| REQ-7 | MCP Tools |
| REQ-8 | PacResilienceDecorator |
| REQ-9 | PacResilienceDecorator (metrics), MultifacturasPacAdapter (structured logging) |
| REQ-10 | PacOptions (secure config), MultifacturasPacAdapter (HTTPS, log sanitization) |
| REQ-11 | PacOptions, PacServiceFactory |

### Key Design-Induced Invariants
| Invariant | Arises From |
|-----------|-------------|
| IPacService implementations are stateless regarding business data — all state is transient per-call | Strategy pattern + DI scoping |
| PacResilienceDecorator MUST be transparent to handlers — same interface, added behaviour | Decorator pattern contract |
| PacServiceFactory selects implementation at startup (singleton scope) — changing ActiveProvider requires restart | DI container registration strategy |
| Adapters translate PAC-specific error codes to domain exceptions — handlers never see HTTP status codes | Adapter pattern boundary |
| FluentValidation pipeline runs BEFORE handler executes — invalid requests never reach PAC | MediatR pipeline ordering |

### Alternatives Considered
| Candidate | Strength | Weakness | Why Not Selected |
|-----------|----------|----------|-----------------|
| B: Gateway Service | Fewer files, simpler mental model, low ceremony | God object (60% of logic in one class), violates SRP, adding operations requires modifying Gateway (violates OCP) | God object risk and OCP violation contradict project's SOLID principles |
| C: Event-Driven with Persistence | Full audit trail, retry from persistence, enterprise-grade reliability | Over-engineered for 1-10 facturas/mes, adds persistence boundary + outbox + entities for what is essentially 3 HTTP calls | Complexity disproportionate to the volume and requirements |

### Metrics Summary
| Metric | Selected (A) | Alt B (Gateway) | Alt C (Event-Driven) |
|--------|:------------:|:---------------:|:--------------------:|
| Cross-cutting reqs % | 27% | 36% | 36% |
| Cross-cutting invariants % | 20% | 10% | 20% |
| Flow density | 0.17 | 0.10 | 0.14 |
| God object score | 11% | 60% ⚠️ | 35% |
| Sync cycles | 0 | 0 | 0 |
| Max fan-in | 3 | 4 | 3 |
| Max fan-out | 2 | 3 | 4 |
| Evolvability cost | 1.5 | 1.0 | 2.5 |
