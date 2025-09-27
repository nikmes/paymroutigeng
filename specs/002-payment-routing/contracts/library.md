# Library Contract – Routing Engine Phase 1

## Public Surface
```csharp
namespace CorrRoute.Routing;

public interface IRoutingEngine
{
    RoutingEvaluationResult Evaluate(RoutingContext context, CancellationToken cancellationToken = default);
}
```

### RoutingContext
```csharp
public sealed record RoutingContext(
    PaymentContext Payment,
    CounterpartyContext Counterparty,
    CustomerContext Customer);
```

| Type | Members | Notes |
| --- | --- | --- |
| `PaymentContext` | `string Direction`, `string Currency` | Direction ∈ {IN, OUT, INT, OWN}; Currency is ISO-4217 Alpha3. |
| `CounterpartyContext` | `string? BankCountryCode`, `string? BankBic`, `string? Account`, `string? Name` | All optional for Phase 1. |
| `CustomerContext` | `string? Id`, `string? Industry`, `string? Type` | Type normalized to uppercase, optional fields treated as null. |

### RoutingEvaluationResult
```csharp
public sealed record RoutingEvaluationResult(
    RoutingDecision Decision,
    IReadOnlyList<RouteOutcome> GreenRoutes,
    IReadOnlyList<RouteOutcome> RedRoutes,
    IReadOnlyList<RuleAuditEntry> EvaluationTrail);
```

| Type | Description |
| --- | --- |
| `RoutingDecision` | Enum { `CanRoute`, `CanNotRoute` }. |
| `RouteOutcome` | Record containing `string RuleCode`, `string CorrBankBic`, `string Description`. |
| `RuleAuditEntry` | Record containing `string RuleCode`, `bool Match`, `OutcomePolicy Outcome`, `TimeSpan EvaluationDuration`. |

### Configuration Contract
```csharp
public sealed record RoutingEngineOptions
{
    public required string RulesFilePath { get; init; }
    public int EvaluationCacheSize { get; init; }
    public LogEventLevel MinimumLogLevel { get; init; } = LogEventLevel.Warning;
}
```

- `RulesFilePath` is required and points to the JSON catalog described in `data-model.md`.
- `EvaluationCacheSize` defaults to 0 (disabled); future phases may enable compiled predicate caching.
- Options loaded via `IOptions<RoutingEngineOptions>` / `IConfiguration` binding in host applications.

## Exceptions
- `RoutingValidationException` thrown when the catalog file fails schema validation.
- `RoutingEvaluationException` thrown when evaluation encounters an unrecoverable error (e.g., corrupted cache entry).

## Logging Contract
- All evaluation runs emit a Serilog event with:
  - Message template: `"Routing evaluation completed in {Elapsed} with decision {Decision}"`
  - Properties: `RuleCode`, `OutcomePolicy`, `MatchResult`, `PriorityWeight`, `CorrBankBic` for each evaluated rule (written as structured property array).
- Log level Warning for failures (`CanNotRoute` OR validation issues), Information for success when the hosting application raises minimum level below Warning.

## Benchmark Harness Expectations
- Benchmark project references the library and exercises `IRoutingEngine` against synthetic catalogs (10, 100, 1,000 rules).
- Benchmarks run with `[MemoryDiagnoser]` enabled; regression gate fails if mean > 10 ms for any scenario.
