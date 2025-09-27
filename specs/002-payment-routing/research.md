# Phase 0 Research – Payment Routing Rules Engine

## Rule Prioritisation Strategy
- **Decision**: Each rule carries an integer `priorityWeight`; higher values evaluate first, ties broken alphabetically by `RuleCodeName`.
- **Rationale**: Numeric weights allow external catalog ordering without re-sorting the JSON file and align with future UI sliders.
- **Alternatives Considered**: List ordering (fragile when files are edited manually); date-based priority (adds clock drift risk).

## Matching Semantics
- **Decision**: Phase 1 supports exact, case-insensitive string comparisons after normalization; no pattern or range operators.
- **Rationale**: Keeps evaluation deterministic and fast, minimising scope for the validation-only release.
- **Alternatives Considered**: Prefix and regex matching (deferred to later phases pending business rules).

## Rule Storage
- **Decision**: Load rule catalog and metadata from a JSON file configured via `appsettings.json` or environment variables.
- **Rationale**: Simplifies Phase 1 deployment and mirrors eventual configuration service without external dependencies.
- **Alternatives Considered**: Database-backed storage (requires schema/versioning); remote configuration service (adds network complexity).

## Logging & Observability
- **Decision**: Use Serilog with console sink, logging at Warning+ by default and enriching with evaluation metrics for benchmarking runs.
- **Rationale**: Meets constitutional baseline while enabling structured output for CI.
- **Alternatives Considered**: File or Seq sinks (unnecessary for validation phase).

## Benchmarking Targets
- **Decision**: Integrate BenchmarkDotNet harness covering single-rule and multi-rule catalogs (10, 100, 1,000 entries) with goal <10 ms per evaluation.
- **Rationale**: Provides reproducible latency metrics and aligns with spec quality gates.
- **Alternatives Considered**: Custom Stopwatch-based harness (less accurate, harder to compare across runs).

## Testing Stack
- **Decision**: Use xUnit for unit/integration tests, FsCheck for property-based invariants, Verify for golden JSON snapshots.
- **Rationale**: xUnit is standard for .NET, FsCheck integrates smoothly, Verify simplifies regression coverage.
- **Alternatives Considered**: NUnit/MSTest (no clear benefit), bespoke assertion frameworks.

## Packaging & Phase Sequencing
- **Decision**: Deliver a single .NET 9 class library with accompanying `tests/` project; no HTTP API or external dependencies in Phase 1.
- **Rationale**: Aligns with clarifications and keeps scope focused on rule validation prior to exposing endpoints in later phases.
- **Alternatives Considered**: Early Minimal API (violates phase boundary and constitution instructions).
