# Quickstart – Payment Routing Rules Engine (Phase 1)

## Prerequisites
- .NET 9 SDK preview (latest build supporting `net9.0`)
- PowerShell 7+ or Bash for CLI commands
- Local configuration files included in the repo (`appsettings.Development.json`, `config/rules.sample.json`)

## Setup
1. Restore dependencies and build the solution:
   ```powershell
   dotnet restore
   dotnet build --configuration Release
   ```
2. Verify the sample rule catalog is present at `config/rules.sample.json` (referenced by `appsettings.Development.json`).

## Configuration
- The routing library reads settings from the `RoutingEngine` section. The provided `appsettings.Development.json` configures:
  - `RulesFilePath`: relative path to the routing catalog (`config/rules.sample.json`).
  - `EvaluationCacheSize`: optional compiled-evaluator cache capacity (set to `128`).
  - `MinimumLogLevel`: Serilog level the engine will emit (`Information`).
- Override critical values without editing the file:
  - `ROUTING_RULES_PATH` — absolute or relative path to an alternative catalog file.
  - `ROUTING_LOG_LEVEL` — Serilog minimum level (`Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`).
- When hosting in another application, reuse these sections or bind them with `RoutingEngineOptionsBinder.Bind(configuration)`.

## Running Tests
- Execute all unit and property-based tests:
  ```powershell
  dotnet test --configuration Release --logger "console;verbosity=normal"
  ```
- To run only property-based invariants (FsCheck traits):
  ```powershell
  dotnet test --filter Category=PropertyBased
  ```
- Golden/verify tests can be re-approved after intentional changes:
  ```powershell
  dotnet test --filter Category=Golden -- TestRunParameters.Parameter(name="Verify.AutoVerify", value=true)
  ```

## Running Benchmarks
- Launch the BenchmarkDotNet harness (Release build required):
  ```powershell
  dotnet run --project benchmarks/RoutingEngine.Benchmarks --configuration Release
  ```
- Benchmark output is written to `BenchmarkDotNet.Artifacts/results/`. The Phase 1 acceptance gate is **<10 ms** mean latency per evaluation for catalogs up to 1,000 rules.

## Logging & Diagnostics
- Serilog writes structured logs to the console. Adjust the minimum level via `RoutingEngine:MinimumLogLevel` or the `ROUTING_LOG_LEVEL` environment variable.
- Use `ROUTING_RULES_PATH` to point at bespoke catalogs per developer or CI environment.

## Next Steps
1. After validating tests/benchmarks locally, push changes and ensure CI executes the same commands.
2. When ready for API exposure (Phase 2+), extend the plan/spec with Minimal API contracts per the constitution.
