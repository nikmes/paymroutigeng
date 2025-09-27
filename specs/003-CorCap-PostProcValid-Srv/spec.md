# Phase 1.3 — Corridor Capabilities, Post-Processing Validation, and Service

## Overview
This phase introduces a post-evaluation validation step to ensure returned correspondent routes (CorrBankBIC) actually support the requested payment currency, and to enrich GREEN routes with the appropriate Nostro IBAN. It also scaffolds the first HTTP service around the existing routing engine pipeline.

Pipeline after this phase:
1) Enrichment (e.g., CounterpartyType)
2) Rule Evaluation → provisional GREEN/RED
3) Post-Processing Validation → currency support check per CorrBankBIC; enrich with Nostro IBAN; demote unsupported to RED
4) Response shaping

## Goals
- Keep rules focused on matching logic; move bank capability checks out of rules.
- Validate currency support per CorrBankBIC after evaluation and enrich supported routes with Nostro IBAN.
- Provide a simple, hostable HTTP service endpoint around the engine and post-processors.

## Corridor Capabilities Model
We externalize correspondent capabilities as bank master data, not rules.

- Entity: CorrespondentCapabilities
  - bic: string (BIC11)
  - currencies: array of objects:
    - code: string (ISO 4217 alpha-3)
    - nostroIban: string (IBAN)
    - priority?: integer (optional, future-proofing)

- Representation: JSON file for Phase 1.3, pluggable store later.
- Versioning: Snapshot with monotonically increasing version (or content-hash in future).

Example (config/capabilities.sample.json):
{
  "correspondents": [
    { "bic": "DEUTDEFFXXX", "currencies": [
      { "code": "EUR", "nostroIban": "DE12123456789012345678" },
      { "code": "USD", "nostroIban": "DE34987654321098765432" }
    ]},
    { "bic": "CHASUS33XXX", "currencies": [
      { "code": "USD", "nostroIban": "GB12CHAS12345678901234" }
    ]}
  ]
}

## Post-Processing Validation
- For each GREEN route from evaluation:
  - If capabilities contain the route.corrBankBic AND a currency entry matching request.payment.currency:
    - annotate route with nostroIban
  - Else:
    - move route to RED with reason: `CAPABILITY:CURRENCY_UNSUPPORTED`
- Keep existing RED routes intact. If a corresponding RED already exists, either coalesce descriptions or append a separate RED entry.

### Output Contract Changes
- greenRoutes[].nostroIban: string | omitted when not applicable
- greenRoutes[].currency: string (echo of request payment currency for clarity)
- redRoutes[] may include entries created by the post-processor with a synthetic ruleCode (e.g., "CAPABILITY:CURRENCY_UNSUPPORTED").

## Service Scope
- Minimal HTTP service hosting the pipeline with dependency injection:
  - Enrichers (existing)
  - Rule store + RoutingEngineHost (existing)
  - Capabilities store (new)
  - Post-processors (new: CurrencySupportPostProcessor)
- Endpoint: POST /payment-routing/resolve (same shape; response now includes nostroIban when applicable and only true GREEN routes after validation)

## Non-Goals (Phase 1.3)
- UI for managing capabilities
- External persistence or admin APIs for capabilities (file-backed only for now)
- Multi-currency quoting or FX logic

## Edge Cases
- Unknown BIC in capabilities → treat as unsupported for the requested currency (demote to RED)
- Multiple entries for the same BIC×currency → pick highest priority or first entry deterministically
- Mixed case currency codes → normalize to uppercase

## Telemetry
- Metrics: routes_demoted_unsupported_currency, routes_enriched_with_nostro, capabilities_snapshot_version
- Logs: per-route decisions and demotions with reasons

## Security
- Same as prior phases; service protected via environment (future mTLS/OAuth)

## Compatibility
- Rule catalog unchanged; capabilities are orthogonal.
- Response shape is backward compatible (nostroIban is additive; GREEN list only gets stricter).

