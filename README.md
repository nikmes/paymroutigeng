# payroutigeng – Payment Routing Rules Engine (Phase 1)

This repository contains the Phase 1 implementation of the corrroute payment routing rules engine. The goal is to evaluate configurable routing rules, surface deterministic GREEN/RED corridors, and provide the verification, benchmarking, and documentation artifacts needed for future phases.

## Project layout
- `src/RoutingEngine` – .NET 9 class library implementing the routing domain, evaluation pipeline, configuration binder, and Serilog logging hooks.
- `tests/RoutingEngine.Tests` – xUnit + FsCheck + Verify test project covering rule semantics, decisions, snapshots, contracts, and properties.
- `benchmarks/RoutingEngine.Benchmarks` – BenchmarkDotNet harness exercising catalogs with 10, 100, and 1,000 rules.
- `specs/002-payment-routing` – Design documents, data models, OpenAPI/contract definitions, quickstart guide, and task tracker.
- `config/rules.sample.json` – Sample rule catalog referenced by the development configuration.

## Detailed implementation overview

The engine follows a rule-based design: a JSON catalog describing routing rules is loaded into domain models, transformed into compiled predicates, and evaluated against an incoming payment context.

1. **Rule ingestion**
  - `JsonRuleCatalogLoader` parses the external catalog into `RoutingRule` records. Each rule includes metadata (code, description, outcome policy) plus optional match predicates (direction, currency, counterparty/customer attributes).
  - `RuleConditionFactory` converts those optional fields into executable delegates. Missing values act as wildcards, enabling partial matches.

2. **Evaluation pipeline**
  - `RoutingEngine` takes the hydrated rule set, orders active rules by `PriorityWeight`, and iterates through them for a given `RoutingContext` (payment + counterparty + customer payload).
  - Each rule evaluation produces a `RuleEvaluationAuditRecord` capturing match status, outcome policy, and priority so the full audit trail can be returned.
  - Successful `PassOnMatch` rules become GREEN candidates; `FailOnMatch` rules become RED candidates. Corridors that appear in RED automatically suppress the same corridor in GREEN, ensuring exclusivity.
  - The final `RoutingEvaluationResult` contains decision (`CAN_ROUTE` / `CAN_NOT_ROUTE`), status (`EVALUATED` / `NO_MATCH`), ordered route lists, and the audit trail. Serilog logging hooks (`LoggingExtensions`) emit summary events with duration metrics when enabled.

3. **Testing & verification**
  - `RuleEvaluationSpec` and `RouteDecisionSpec` assert deterministic rule behavior and decision logic.
  - `RuleEvaluationPropertyTests` exercises randomized catalogs via FsCheck to guarantee purity and corridor exclusivity.
  - `ScenarioSnapshotTests` capture end-to-end serialized responses with Verify, providing regression coverage for key scenarios (mixed outcomes, empty catalogs, customer-specific blocks, etc.).
  - Contract parity tests ensure serialized DTOs align with the published OpenAPI definition.

4. **Tooling & observability**
  - BenchmarkDotNet harness (`BaselineBenchmarks`) measures performance for catalogs up to 1,000 rules (<10 ms target).
  - Configuration binder (`RoutingEngineOptionsBinder`) wires the library into host applications, supporting environment overrides for rule paths and logging levels.

This structure keeps the rule engine composable: hosts can feed different catalogs, enrich the `RoutingContext`, or extend predicate generation without altering the core evaluation flow.

### Enrichment (Phase 1.1)
- The engine now supports an optional derived attribute `counterparty.type` (enum PERSON | BUSINESS | UNKNOWN) exposed to rules via `PR.CPartyType`.
- Recommended usage: run your classifier (e.g., name/entity model) in your host application and populate `CounterpartyContext.Type` before calling `RoutingEngine.Evaluate(...)`.
- Why: keeps the evaluator deterministic and fast, and makes rules explicit about targeting enriched data.
- Contract updates: OpenAPI request includes `counterparty.type`; rules may specify `PR.CPartyType`. Missing values behave as wildcards (no condition generated).

## Prerequisites
- .NET 9 SDK (build 9.0.305 or later).
- PowerShell 7+ (or Bash) to execute the documented commands.
- Optional: High-performance power plan when running benchmarks (handled automatically by BenchmarkDotNet on Windows).

## Build & test
```powershell
# Restore and build the full solution (Release configuration recommended)
dotnet restore
dotnet build --configuration Release

# Execute the entire test suite (includes FsCheck and Verify snapshots)
dotnet test --configuration Release --logger "console;verbosity=normal"
```

Snapshot tests live under `tests/RoutingEngine.Tests/Snapshots`. If behavior changes intentionally, delete the affected `.verified.txt` files and rerun the tests to regenerate baselines.

## Benchmarks
```powershell
# Run the BenchmarkDotNet harness
 dotnet run --project benchmarks/RoutingEngine.Benchmarks --configuration Release
```

Latest benchmark run (2025-09-27 on a Lenovo Windows 11 laptop, .NET SDK 9.0.305):

| Rules | Mean latency | StdDev | Allocated |
| ----- | ------------ | ------ | --------- |
| 10    | 1.07 µs      | 0.02 µs | 4.27 KB   |
| 100   | 9.71 µs      | 0.07 µs | 31.16 KB  |
| 1,000 | 101.85 µs    | 0.44 µs | 297.54 KB |

The Phase 1 acceptance bar (<10 ms per evaluation up to 1,000 rules) is comfortably met.

Benchmark artifacts, including CSV and GitHub-formatted reports, are produced under `BenchmarkDotNet.Artifacts/results/` for archival or CI uploads.

## Configuration & logging
- Default settings live in `appsettings.Development.json` under the `RoutingEngine` section.
- `RoutingEngineOptionsBinder` can be used to bind an `IConfiguration` instance to `RoutingEngineOptions`.
- Environment overrides:
  - `ROUTING_RULES_PATH` – alternate catalog path.
  - `ROUTING_LOG_LEVEL` – Serilog minimum level (`Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`).
- Serilog console logging is enabled via `LoggingExtensions` and can be adjusted or enriched by host applications.

## Documentation
For deeper details covering the data model, OpenAPI contract, task breakdown, and quickstart instructions, see `specs/002-payment-routing/`. The `quickstart.md` file is the canonical reference for setup, testing, benchmarking, and configuration guidance.

## Next steps
Phase 2 will expose the engine via an API surface and broaden integration scenarios. Continue tracking work items in `specs/002-payment-routing/tasks.md` and extend test/benchmark coverage as new features land.
