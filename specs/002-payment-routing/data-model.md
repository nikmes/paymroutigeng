# Payment Routing Rule Data Model

## Entity: RoutingRule
Represents a configurable rule evaluated against payment context data. Fields mirror the spreadsheet provided by the business team.

| Field | Type | Cardinality | Description |
| --- | --- | --- | --- |
| `RuleCodeName` | string(64) | Mandatory | Unique identifier for the rule (e.g., `RULE-OUT-GR-EUR`). |
| `RuleDescription` | string(256) | Mandatory | Human-readable summary of the rule purpose. For `FailOnMatch`, must explain the reason for the RED route. |
| `OutcomePolicy` | enum | Mandatory | `PassOnMatch` (return `CorrBankBIC`) or `FailOnMatch` (block routing). |
| `Operator` | enum | Mandatory | Logical combinator for predicates: `ALL`, `ANY`, `NONE`, `ONE`. |
| `PR.CPartyBankCountryCode` | string(2) | Optional | ISO 3166-1 alpha-2 country code of the counterparty bank. |
| `PR.CPartyBankBIC` | string(11) | Optional | BIC of the counterparty bank. |
| `PR.CPartyAccount` | string(34) | Optional | Counterparty account reference (IBAN or domestic). |
| `PR.CPartyName` | string(140) | Optional | Counterparty display name. |
| `PR.CustomerId` | string(64) | Optional | Internal identifier of the initiating customer. |
| `PR.CustomerIndustry` | string | Optional | Industry classification code (`I001`, `I002`, etc.). |
| `PR.CustomerType` | enum | Optional | `INDIVIDUAL` or `CORPORATE`. Input variants (e.g., `COORPORATE`) are normalized. |
| `PR.PaymentDirection` | enum | Optional | `IN`, `OUT`, `INT`, `OWN`. |
| `PR.PaymentCurrency` | string(3) | Optional | ISO 4217 alpha-3 currency code. |
| `CorrBankBIC` | string(11) | Mandatory | BIC returned for the route regardless of outcome policy. Enables both GREEN and RED route reporting. |
| `PriorityWeight` | int | Mandatory | Higher number = higher priority during evaluation. |
| `RuleStatus` | enum | Mandatory | `ON` for active, `OFF` to disable the rule without deleting it. |

### Constraints & Notes
- Optional fields left blank act as wildcards (no predicate generated).
- All string comparisons are case-insensitive after trimming whitespace.
- `PriorityWeight` determines ordering; ties resolved alphabetically by `RuleCodeName`.
- `CorrBankBIC` is always required; duplicate values are allowed across rules but should be accompanied by distinct conditions.
- `FailOnMatch` rules must provide a descriptive `RuleDescription` used in RED route messaging.
- Rules are stored in a JSON catalog (`RoutingRule[]`) loaded at startup; future persistence layers must preserve these fields.

## Sample Rules
```json
{
  "RuleCodeName": "RULE-OUT-GR-EUR",
  "RuleDescription": "Send outbound EUR payments to Greece via Deutsche Bank",
  "OutcomePolicy": "PassOnMatch",
  "Operator": "ALL",
  "PR.CPartyBankCountryCode": "GR",
  "PR.PaymentDirection": "OUT",
  "PR.PaymentCurrency": "EUR",
  "CorrBankBIC": "DEUTDEFFXXX",
  "PriorityWeight": 100,
  "RuleStatus": "ON"
}
```

```json
{
  "RuleCodeName": "RULE-INTL-BLOCK",
  "RuleDescription": "Block international wires for sanctioned counterparties",
  "OutcomePolicy": "FailOnMatch",
  "Operator": "ANY",
  "PR.CPartyBankBIC": "DEUTDEFFXXX",
  "PR.CPartyName": "SANCTIONED ENTITY",
  "CorrBankBIC": "DEUTDEFFXXX",
  "PriorityWeight": 500,
  "RuleStatus": "ON"
}
```

```json
{
  "RuleCodeName": "RULE-GR-ALL-BLOCK",
  "RuleDescription": "Block every payment when the counterparty bank is in Greece",
  "OutcomePolicy": "FailOnMatch",
  "Operator": "ALL",
  "PR.CPartyBankCountryCode": "GR",
  "CorrBankBIC": "DEUTDEFFXXX",
  "PriorityWeight": 400,
  "RuleStatus": "ON"
}
```

## Entity: RoutingEvaluationResponse
Represents the payload returned by the routing service for each evaluation request.

| Field | Type | Cardinality | Description |
| --- | --- | --- | --- |
| `status` | enum | Mandatory | `EVALUATED` when rules were processed, `NO_MATCH` when neither GREEN nor RED routes were produced. |
| `decision` | enum | Mandatory | `CAN_ROUTE` if `greenRoutes` is non-empty, otherwise `CAN_NOT_ROUTE`. |
| `greenRoutes` | array<RouteOutcome> | Mandatory | GREEN route entries that remain eligible after conflict resolution. Empty array when no GREEN corridors survive or `status = NO_MATCH`. |
| `redRoutes` | array<RouteOutcome> | Mandatory | RED route entries describing blocked corridors. Empty array when no blocking rules matched. |

### Entity: RouteOutcome
| Field | Type | Cardinality | Description |
| --- | --- | --- | --- |
| `ruleCode` | string(64) | Mandatory | Identifier of the rule producing this outcome. |
| `corrBankBic` | string(11) | Mandatory | Route corridor represented by the outcome. |
| `description` | string(256) | Mandatory | Message explaining the GREEN/RED determination. For RED, must describe the blocking rationale. |

### Derived Constraints
- If a `RouteOutcome` with a given `corrBankBic` appears in `redRoutes`, the same `corrBankBic` cannot appear in `greenRoutes` for that response.
- The `decision` field is computed from `greenRoutes` and cannot be directly supplied by clients.
- Audit trail should capture ordered evaluation results with timing metadata per request.

```json

## Entity: RoutingEngineConfiguration
Represents runtime configuration values loaded from `appsettings.json` or environment variables.

| Field | Type | Cardinality | Description |
| --- | --- | --- | --- |
| `RulesFilePath` | string | Mandatory | Absolute or relative path to the JSON catalog of routing rules. |
| `EvaluationCacheSize` | int | Optional | Maximum number of compiled condition sets cached in memory (default 0 = disabled). |
| `BenchmarkSettings` | object | Optional | Overrides for BenchmarkDotNet job configuration (e.g., iterations). |
| `LoggingLevelSwitch` | string | Optional | Minimum Serilog level (`Warning` default). |

Configuration may grow in later phases; Phase 1 only requires `RulesFilePath` and Serilog console sink settings.
