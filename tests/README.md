# RoutingEngine Test Suite

This directory contains the high-level specification tests that exercise the payment routing engine end-to-end. Every test spins up the real rules engine through the infrastructure harness and validates the public contract outputs that downstream callers rely on.

## Running the suite

Run the full test suite from the repository root:

```
dotnet test tests/RoutingEngine.Tests/RoutingEngine.Tests.csproj
```

The Verify snapshot tests (`ScenarioSnapshotTests`) store their baselines under `RoutingEngine.Tests/Snapshots`. If test expectations change intentionally, delete the corresponding `.verified.txt` file and rerun the test to regenerate it.

## Test inventory

### `RuleEvaluationSpec`

These facts validate the rule-matching semantics for targeted scenarios.

- **Evaluation_returns_all_green_routes_that_match** – Builds a catalog with two matching corridors and confirms both remain in the green list.
- **Evaluation_excludes_green_routes_that_fail_optional_predicates** – Ensures optional predicates remove non-compliant green options while leaving a single valid route.
- **Evaluation_collects_all_failed_rules_in_red_list** – Confirms rules that fail with `FailOnMatch` appear in `RedRoutes` while the surviving corridor remains green.
- **Rule_status_off_disables_rule_without_affecting_priority** – Verifies disabled rules (`RuleStatus = "OFF"`) are ignored even when they have higher priority weights.
- **Rules_without_priority_weight_are_pushed_to_bottom** – Demonstrates that routes lacking a weight are evaluated last, keeping weighted routes ahead.
- **Evaluation_returns_can_not_route_when_everything_fails** – Shows that the engine returns `CAN_NOT_ROUTE` when only blocking rules match.
- **All_green_corridors_are_returned_when_every_rule_passes** – Builds five pass rules with unique corridors and confirms every eligible corridor is surfaced in `greenRoutes`.

### `RouteDecisionSpec`

Decision-level tests focusing on the overall evaluation outcome and audit details.

- **Decision_is_can_route_when_any_green_route_survives** – Confirms the engine delivers `CAN_ROUTE` when at least one pass rule matches.
- **Decision_is_can_not_route_when_green_routes_are_suppressed** – Validates that a higher-priority fail rule suppresses pass candidates, producing `CAN_NOT_ROUTE`.
- **Decision_is_can_not_route_when_catalog_has_no_rules** – Shows that with an empty rule catalog the engine returns `CAN_NOT_ROUTE`, `NO_MATCH`, and no audit entries.
- **Audit_trail_records_all_rules_in_priority_order_with_match_flags** – Checks that the audit trail preserves priority ordering, match flags, and associated policies for each evaluated rule.

### `RuleEvaluationPropertyTests`

FsCheck properties that probe the engine with randomized catalogs and requests (75 cases per property).

- **Evaluation_is_deterministic** – The same input seed always yields identical results, asserting purity of the evaluation pipeline.
- **Decision_flag_aligns_with_green_routes** – Guards against mismatches where `Decision` claims success with no green routes, or vice versa.
- **Corridors_do_not_exist_in_both_lists** – Ensures the same corridor cannot appear simultaneously in the green and red route outputs.

### `ScenarioSnapshotTests`

Verify-based snapshot scenarios that validate serialized responses.

- **Mixed_green_and_red_routes_are_verified** – Captures a mixed outcome containing both pass and fail corridors along with the full audit.
- **No_matching_rules_are_verified** – Documents the engine response when nothing matches; the empty result stays consistent over time.
- **Empty_catalog_response_is_verified** – Locks in the serialized shape when the catalog has zero rules, ensuring the engine emits the bare minimum response (`CAN_NOT_ROUTE` / `NO_MATCH`).
- **Customer_specific_block_snapshot_is_verified** – Covers a customer-specific block that suppresses `PASSBIC001` while leaving an alternate corridor (`TFIMCY2NXXX`) green, producing a mixed response snapshot.

### `LibraryContractTests`

Contract-level verification of the serialized response.

- **Evaluation_result_serializes_to_expected_contract_shape** – Serializes the evaluation result to JSON and confirms the presence of required contract fields.

### `OpenApiParityTests`

Parity test between runtime responses and the OpenAPI contract.

- **Engine_response_contains_all_fields_defined_in_openapi_contract** – Parses `specs/002-payment-routing/contracts/openapi.yaml` and confirms the engine output exposes each declared property.

## Supporting infrastructure

The helper types under `Infrastructure/` provide repeatable fixtures shared across tests:

- `RuleCatalogBuilder` – Assembles JSON catalogs on the fly.
- `RoutingRequestFactory` – Creates realistic requests with optional overrides for targeted assertions.
- `RoutingEngineTestHarness` – Executes the engine and normalizes outputs (including audit trail projection and equality comparison for property tests).
