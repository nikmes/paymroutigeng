# Tasks: Payment Routing Rules Engine (Phase 1)

**Input**: Design documents from `/specs/002-payment-routing/`
**Prerequisites**: `plan.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

## Phase 3.1: Setup
- [x] T001 Scaffold solution structure (`corrroute.sln`) with `src/`, `tests/`, and `benchmarks/` folders as outlined in `plan.md`.
- [x] T002 Create class library project `src/RoutingEngine/RoutingEngine.csproj` targeting `net9.0` and add package references for `Serilog`, `Serilog.Sinks.Console`, and `System.Text.Json`.
- [x] T003 Create test project `tests/RoutingEngine.Tests/RoutingEngine.Tests.csproj` targeting `net9.0` with dependencies on `xunit`, `xunit.runner.visualstudio`, `FsCheck.Xunit`, and `Verify.Xunit`; reference the main library.
- [x] T004 Create benchmark project `benchmarks/RoutingEngine.Benchmarks/RoutingEngine.Benchmarks.csproj` targeting `net9.0` with `BenchmarkDotNet` and reference to the library project.

## Phase 3.2: Tests First (TDD)
- [x] T005 [P] Add failing rule operator unit tests in `tests/RoutingEngine.Tests/RuleEvaluationSpec.cs` covering `ALL`, `ANY`, `NONE`, `ONE`, priority weights, and CorrBankBIC suppression.
- [x] T006 [P] Add failing decision/result aggregation tests in `tests/RoutingEngine.Tests/RouteDecisionSpec.cs` verifying `CAN_ROUTE` vs `CAN_NOT_ROUTE` outcomes and audit trail content.
- [x] T007 [P] Add FsCheck property-based tests in `tests/RoutingEngine.Tests/RuleEvaluationPropertyTests.cs` asserting determinism, corridor exclusivity, and normalization invariants.
- [x] T008 [P] Add Verify snapshot tests in `tests/RoutingEngine.Tests/ScenarioSnapshotTests.cs` using sample catalogs/requests to capture expected GREEN/RED lists.
- [x] T009 [P] Add contract tests in `tests/RoutingEngine.Tests/LibraryContractTests.cs` ensuring `RoutingEvaluationResult` and related records serialize according to `contracts/library.md`.
- [x] T010 [P] Add contract tests in `tests/RoutingEngine.Tests/OpenApiParityTests.cs` validating `RoutingEvaluationResponse` DTOs align with `contracts/openapi.yaml` schemas.

## Phase 3.3: Core Implementation
- [x] T011 Implement domain records and enums (`RoutingRule`, `RouteOutcome`, `RoutingDecision`, context records) in `src/RoutingEngine/Domain` to satisfy `data-model.md`.
- [x] T012 Implement rule catalog loader `JsonRuleCatalogLoader` in `src/RoutingEngine/Configuration` to parse JSON into domain models and raise `RoutingValidationException` on schema issues.
- [x] T013 Implement normalization utilities and predicate builders in `src/RoutingEngine/Evaluation/RuleConditionFactory.cs` handling trimming, casing, and null wildcards.
- [x] T014 Implement core evaluator `RoutingEngine` in `src/RoutingEngine/Evaluation/RoutingEngine.cs` applying priority weights, operator logic, and CorrBankBIC suppression.
- [x] T015 Implement decision aggregator and audit trail writer in `src/RoutingEngine/Evaluation/RoutingEngine.cs` returning `RoutingEvaluationResult` per contract.
- [x] T016 Implement exception hierarchy (`RoutingValidationException`, `RoutingEvaluationException`) and guard clauses in `src/RoutingEngine/Exceptions`.

## Phase 3.4: Integration & Instrumentation
- [x] T017 Configure Serilog console logging and evaluation event enrichment in `src/RoutingEngine/Logging/LoggingExtensions.cs` per contract guidance.
- [x] T018 Implement `RoutingEngineOptions` binding and configuration helpers in `src/RoutingEngine/Configuration/RoutingEngineOptionsBinder.cs`, including `RulesFilePath` resolution and optional cache settings.
- [x] T019 Add sample configuration (`appsettings.Development.json`) and sample rule catalog `config/rules.sample.json`; document overrides via environment variables.
- [x] T020 Build BenchmarkDotNet harness in `benchmarks/RoutingEngine.Benchmarks/BaselineBenchmarks.cs` exercising catalogs of 10, 100, and 1,000 rules with `[MemoryDiagnoser]` enabled.

## Phase 3.5: Validation & Polish
- [x] T021 [P] Implement regression fixtures in `tests/RoutingEngine.Tests/ScenarioSnapshotTests.cs` using Verify to capture GREEN/RED outputs for real-world cases (update snapshots post-implementation).
- [x] T022 Execute `dotnet test --configuration Release` ensuring all tests now pass and update Verify snapshots as needed.
- [x] T023 Run benchmarks via `dotnet run --project benchmarks/RoutingEngine.Benchmarks --configuration Release` and record latency results (<10 ms) in `specs/002-payment-routing/quickstart.md`.
- [x] T024 [P] Update documentation (`quickstart.md`, new `README` section if needed) with configuration instructions, Serilog usage, and benchmark outcomes.

## Phase 1.2: Rule Storage Abstraction
- [x] T101 Define rule snapshot and store interfaces (`RuleCatalogSnapshot`, `IRuleStore`, `IMutableRuleStore`).
- [x] T102 Implement `InMemoryRuleStore` with thread-safe add/update/remove and versioning.
- [x] T103 Implement `JsonFileRuleStore` leveraging `JsonRuleCatalogLoader` and version bump on file change.
- [x] T104 Add `RoutingEngineHost` that caches engine per snapshot version and rehydrates on changes.
- [x] T105 Unit tests: store semantics (replace, add/update, remove) and host cache invalidation.
- [x] T106 Docs: update `spec.md` and `quickstart.md` with store usage patterns and examples.

## Dependencies
- T002 depends on T001; T003 depends on T002; T004 depends on T002.
- T005–T010 depend on setup (T001–T003) but can run in parallel with each other.
- Implementation tasks (T011–T016) must begin only after tests (T005–T010) are in place and failing.
- T014 depends on T011–T013; T015 depends on T014; T016 depends on T012–T014.
- Integration tasks (T017–T020) depend on core implementation (T011–T016).
- Validation/polish tasks (T021–T024) depend on prior phases; T021 can run in parallel with doc updates (T024) after implementation.

## Parallel Execution Example
```
# After completing T003, launch parallel test authoring:
task run T005
task run T006
task run T007
task run T008
task run T009
task run T010

# After core implementation (T011–T016), run polishing tasks in parallel:
task run T021
task run T024
```

## Notes
- Keep tests failing until corresponding implementation tasks are executed.
- Use `dotnet format` (optional) before committing each task completion.
- Record benchmark results and configuration paths in `quickstart.md` as part of T023/T024.
- Commit after each task with conventional messages (e.g., `feat(routing): add rule evaluation tests`).
