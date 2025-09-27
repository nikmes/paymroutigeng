# Payment Routing Rules Specification

## Overview
This document defines the rule-driven payment routing capability required to determine the correct correspondent bank (CorrBankBIC) for a payment instruction. Routing decisions are made by evaluating a configurable catalog of rules against incoming payment metadata. When rules match, the engine aggregates routes into explicit GREEN and RED lists so downstream systems can inspect all possible corridors and manually choose among the GREEN options when multiple are available.

## Goals
- Provide a structured contract for defining routing rules.
- Describe the evaluation semantics for resolving a correspondent bank.
- Define the request/response contract for a routing decision service.
- Capture validation rules and edge-case handling to ensure consistent behavior.

## Clarifications
### Session 1 – 2025-09-26
- **Rule priority model**: Use an explicit numeric weight per rule to determine evaluation order (higher weight = higher priority). Ties resolve alphabetically by `RuleCodeName`.
- **Matching semantics**: Phase 1 supports exact (normalized) comparisons only; no pattern or range matching.
- **Customer overrides**: Out of scope for Phase 1.
- **Rule storage**: Phase 1 rules are loaded from a JSON file (no external persistence service).
- **API surface**: Phase 1 exposes a class library only; no HTTP endpoint yet.
- **Logging**: Utilize Serilog with a console sink for Phase 1 instrumentation.
- **Benchmark targets**: Measure latency with BenchmarkDotNet; requirement is <10 ms per evaluation on reference hardware.
- **Packaging**: Deliver the engine as a .NET 9 class library.
- **Testing framework**: Adopt xUnit for automated tests.
- **Configuration**: Provide runtime configuration (e.g., `appsettings.json`) for rule paths, logging, and benchmark parameters.

## Glossary
- **Rule** – A configuration record representing a set of predicates and the target correspondent bank to use when those predicates are met.
- **Condition** – An individual comparison derived from a populated rule field (for example `PR.PaymentDirection = "IN"` or `PR.CustomerAccount = "DE..."`).
- **Outcome Policy** – Defines the effect of a matched rule (`PassOnMatch` issues a route, `FailOnMatch` blocks the payment).
- **Operator** – Logical combinator applied across all conditions inside a rule (`ALL`, `ANY`, `NONE`, `ONE`).
- **CorrBankBIC** – BIC of the correspondent bank to use when a rule passes.

## Rule Data Contract
Each rule is persisted as a flat structure. Fields below are optional unless flagged as required. Empty values are treated as wildcards (they do not generate a condition).

| Field | Type | Required | Allowed Values / Format | Notes |
| --- | --- | --- | --- | --- |
| `RuleCodeName` | string | ✅ | 1–64 ASCII characters | Unique code identifying the rule. |
| `RuleDescription` | string | ✅ | 1–256 UTF-8 characters | Human-readable explanation. For `FailOnMatch`, this must clearly state the reason a route is RED. |
| `OutcomePolicy` | string | ✅ | `PassOnMatch`, `FailOnMatch` | Determines post-match behavior. |
| `Operator` | string | ✅ | `ALL`, `ANY`, `NONE`, `ONE` | Logical combinator for generated conditions. |
| `PR.CPartyBankCountryCode` | string | ❌ | ISO 3166-1 alpha-2 (e.g., `CY`, `GR`) | Case-insensitive comparison. |
| `PR.CPartyBankBIC` | string | ❌ | Valid BIC11 (e.g., `CHASUS33XXX`) | Uppercase alphanumeric. |
| `PR.CPartyAccount` | string | ❌ | Up to 34 characters; IBAN or domestic account | Whitespace ignored for comparison. |
| `PR.CPartyName` | string | ❌ | Up to 140 characters | Trimmed case-insensitive comparison. |
| `PR.CustomerId` | string | ❌ | Up to 64 characters | Exact match. |
| `PR.CustomerIndustry` | string | ❌ | Enumerated codes (e.g., `I001`) | Case-insensitive match. |
| `PR.CustomerType` | string | ❌ | `INDIVIDUAL`, `CORPORATE` | Normalize to uppercase (typo `COORPORATE` is treated as `CORPORATE`). |
| `PR.CustomerAccount` | string | ❌ | Up to 34 characters; IBAN or domestic account | Whitespace ignored for comparison. |
| `PR.PaymentDirection` | string | ❌ | `IN`, `OUT`, `INT`, `OWN` | Uppercase comparison. |
| `PR.PaymentCurrency` | string | ❌ | ISO 4217 alpha-3 (e.g., `EUR`, `GBP`, `USD`) | Uppercase comparison. |
| `CorrBankBIC` | string | ✅ | Valid BIC11 | Route identifier produced by the rule. Required for both `PassOnMatch` and `FailOnMatch` to support GREEN/RED route reporting. |
| `RuleStatus` | string | ✅ | `ON`, `OFF` | Disabled (`OFF`) rules are skipped.

### Derived Conditions
A condition is generated for every non-empty predicate field. For example, when `PR.PaymentCurrency = "EUR"`, the condition is `payment.currency == "EUR"`.

String comparisons default to exact match after normalization (trim, uppercase) unless otherwise noted. Future enhancements (like pattern matching) must extend this document.

## Rule Evaluation Semantics
1. **Candidate Set** – Collect all rules where `RuleStatus = ON`.
2. **Ordering** – Evaluate candidates in descending priority order. Priority is externally managed (e.g., via rule list position or metadata). Ties default to alphabetical by `RuleCodeName`.
3. **Condition Generation** – For each rule, build a set of conditions from populated predicate fields.
4. **Operator Execution**:
   - `ALL`: every condition must evaluate to true.
   - `ANY`: at least one condition evaluates to true.
   - `NONE`: no condition evaluates to true (treat empty condition set as false).
   - `ONE`: exactly one condition evaluates to true.
5. **Match Recording**:
  - `PassOnMatch`: when the operator evaluates to true, add the rule and its `CorrBankBIC` to the provisional GREEN route list.
  - `FailOnMatch`: when the operator evaluates to true, add the rule and its `CorrBankBIC` to the provisional RED route list. Evaluation continues so auditing captures all matches.
6. **Outcome Resolution**:
  - Collate GREEN routes, removing duplicates by keeping the highest-priority rule when multiple produce the same `CorrBankBIC`.
  - Collate RED routes in priority order (ties resolved alphabetically). Each RED entry must surface a human-readable description (use `RuleDescription` or a dedicated override field).
  - Remove any GREEN route whose `CorrBankBIC` is also present in the RED list; the corridor is considered blocked and must only appear as RED.
  - Publish the two lists independently in the response payload so clients can use GREEN routes for downstream routing decisions and review RED routes for governance.
  - Derive an automatic `decision` flag: `CAN_ROUTE` when at least one GREEN route survives, `CAN_NOT_ROUTE` when the GREEN list is empty. This flag is advisory and can be overridden by client workflows.
7. **Fall-through** – If neither list contains entries, return `NO_MATCH` with no correspondent bank.

### Route Color Semantics
- **GREEN** – A `PassOnMatch` rule that evaluated to true. GREEN routes are returned so operators can choose among them when multiple corridors are viable. If the same `CorrBankBIC` is blocked by a RED rule, it is removed from the GREEN list. Presence of at least one GREEN route sets `decision = "CAN_ROUTE"`.
- **RED** – A `FailOnMatch` rule that evaluated to true. RED routes include a description explaining why the payment is blocked or discouraged.
- Future colours (e.g., `AMBER` for advisory routes) must extend both this section and the response contract.

### Conflict Handling
- All matching `PassOnMatch` rules contribute potential routes. When multiple rules yield the same `CorrBankBIC`, retain the highest-priority rule (or alphabetically earliest `RuleCodeName` on ties) in the returned list.
- A matching `FailOnMatch` rule produces a RED route. RED routes alone do not remove `CAN_ROUTE`; only when no GREEN routes remain does the decision become `CAN_NOT_ROUTE`. Any GREEN route with the same `CorrBankBIC` is suppressed to avoid conflicting signals.
- When multiple RED routes match, list them all in priority order while logging complete evaluation details for audit.

### Auditing
For every evaluated request, record:
- Ordered list of evaluated rules with boolean outcome.
- The final GREEN and RED route lists (each item: colour, `CorrBankBIC`, description) after GREEN routes overlapping with RED corridors have been removed. Derive `decision = "CAN_ROUTE"` if the GREEN list is non-empty; otherwise `"CAN_NOT_ROUTE"`.
- Timestamp and principal (system user) executing the evaluation.

## Routing Decision Service Contract
The routing engine is exposed over HTTP.

### Request
`POST /payment-routing/resolve`

```json
{
  "ruleContext": {
    "payment": {
      "direction": "OUT",
      "currency": "EUR"
    },
    "counterparty": {
      "bankCountryCode": "GR",
      "bankBic": "DEUTDEFFXXX",
      "account": "DE12345678901234567890",
      "name": "ACME SUPPLIER"
    },
    "customer": {
      "id": "CUST-001",
      "industry": "I001",
  "type": "CORPORATE",
  "account": "DE44500105175407324931"
    }
  }
}
```

### Response – Evaluation (no RED routes)
```json
{
  "status": "EVALUATED",
  "decision": "CAN_ROUTE",
  "greenRoutes": [
    {
      "ruleCode": "RULE-OUT-GR-EUR",
      "corrBankBic": "DEUTDEFFXXX",
      "description": "Send outbound EUR payments to Greece via Deutsche Bank"
    },
    {
      "ruleCode": "RULE-OUT-GR-USD",
      "corrBankBic": "CHASUS33XXX",
      "description": "USD contingency route"
    }
  ],
  "redRoutes": []
}
```

### Response – Evaluation (RED routes only)
```json
{
  "status": "EVALUATED",
  "decision": "CAN_NOT_ROUTE",
  "greenRoutes": [
  ],
  "redRoutes": [
    {
      "ruleCode": "RULE-INTL-BLOCK",
      "corrBankBic": "DEUTDEFFXXX",
      "description": "Block international wires for sanctioned counterparties"
    }
  ]
}
```

### Response – Evaluation (mixed GREEN and RED routes)
```json
{
  "status": "EVALUATED",
  "decision": "CAN_ROUTE",
  "greenRoutes": [
    {
      "ruleCode": "RULE-OUT-GR-USD",
      "corrBankBic": "CHASUS33XXX",
      "description": "USD contingency route"
    }
  ],
  "redRoutes": [
    {
      "ruleCode": "RULE-INTL-BLOCK",
      "corrBankBic": "DEUTDEFFXXX",
      "description": "Block international wires for sanctioned counterparties"
    }
  ]
}
```

### Response – No Match
```json
{
  "status": "NO_MATCH",
  "decision": "CAN_NOT_ROUTE",
  "greenRoutes": [],
  "redRoutes": []
}
```

### Errors
| HTTP Status | Condition | Payload |
| --- | --- | --- |
| 400 | Invalid request (missing required context) | `{ "error": "INVALID_CONTEXT", "details": "payment.currency is required" }` |
| 500 | Internal error | `{ "error": "INTERNAL_ERROR", "details": "Unexpected failure" }` |

## Validation Rules
- Incoming request values must be normalized (trim, uppercase for codes) before comparison.
- `CorrBankBIC` must conform to ISO 9362 (11-character BIC). Reject rule creation if invalid.
- `RuleCodeName` uniqueness is enforced at persistence.
- Every rule (Pass or Fail) must supply a `CorrBankBIC`; duplicate BICs are allowed but should be monitored to avoid ambiguity.
- `FailOnMatch` rules must expose a meaningful human-readable description that can be returned with RED routes.
- Rules with no populated predicate fields are invalid (prevent creation) to avoid catch-all collisions.

## Quality Gates
- **Unit Tests**: Cover rule matching permutations (operators `ALL`, `ANY`, `NONE`, `ONE`), wildcard predicates, normalization, and CorrBankBIC overlap suppression. Assert both `greenRoutes` and `redRoutes` values plus `decision`.
- **Integration Tests**: Provide scenario fixtures (JSON request + expected response) for key corridors, sanctions blocks, missing data errors, and multi-match situations. Run in CI on every change.
- **Property-Based Tests**: Randomly generate rule catalogs and requests to ensure invariants hold (no duplicate CorrBankBIC across lists, decision correctness, determinism of evaluation order).
- **Regression Harness**: Maintain golden files for critical customers to prevent accidental routing drift when rules change.
- **Benchmark Suite**: Execute a dedicated performance test measuring throughput (evaluations/sec) and P99 latency under catalogs of 10, 100, and 1,000 rules. Target <1 ms per evaluation for catalogs ≤100 rules on reference hardware; publish results in release notes.
- **Load/Soak Testing**: Run hourly evaluation bursts (e.g., 10k requests) and monitor memory footprint, ensuring no leaks or priority queue starvation.
- **Observability Validation**: Verify metrics (evaluations processed, match rate, block rate, latency histogram) and structured logs are emitted and consumable by dashboards.

## Operational Considerations
- **Versioning**: Maintain rule catalog revisions with activation timestamps for traceability.
- **Testing**: Provide a sandbox endpoint allowing users to run dry-run evaluations without affecting live routing.
- **Monitoring**: Emit metrics for number of evaluations, match rates, blocks, and fallback occurrences.
- **Security**: Authenticate routing API requests with mTLS or OAuth2 client credentials.

## Open Questions
- How is rule priority maintained? (e.g., numeric weight, UI ordering)?
- Should partial matches support pattern or range comparisons (e.g., account prefixes)?
- Do we need per-customer override rules outside the global catalog?

Answers to these questions should be incorporated into future revisions of this specification.
