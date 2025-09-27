# Phase 1.3 — Corridor Capabilities, Post-Processing Validation, and Service

## Overview
This phase introduces a post-evaluation validation step to ensure returned correspondent routes (CorrBankBIC) actually support the requested payment currency, and to enrich GREEN routes with the appropriate Nostro IBAN. It also scaffolds the first HTTP service around the existing routing engine pipeline.

Pipeline after this phase:
1) Enrichment (e.g., CounterpartyType)
2) Rule Evaluation → provisional GREEN/RED
3) Post-Processing Validation → capability checks per CorrBankBIC:
  - Currency support (required)
  - Charge-bearer support (required): BEN, SHA, OWN 
  - Enrich supported routes with Nostro IBAN for the requested currency
  - Demote any unsupported routes to RED with a clear reason
4) Response shaping

## Goals
- Keep rules focused on matching logic; move bank capability checks out of rules.
- Validate currency and charge-bearer support per CorrBankBIC after evaluation and enrich supported routes with Nostro IBAN.
- Provide a simple, hostable HTTP service endpoint around the engine and post-processors.

## Corridor Capabilities Model
We externalize correspondent capabilities as bank master data, not rules.

- Entity: CorrespondentCapabilities
  - bic: string (BIC11)
  - currencies: array of objects:
    - code: string (ISO 4217 alpha-3)
    - nostroIban: string (IBAN)
    - supportedCharges?: array of strings, subset of ["BEN","SHA","OWN"] 
    - priority?: integer (optional, future-proofing)

- Representation: JSON file for Phase 1.3, pluggable store later.
- Versioning: Snapshot with monotonically increasing version (or content-hash in future).

Example (config/capabilities.sample.json):
{
  "correspondents": [
    { "bic": "DEUTDEFFXXX", "currencies": [
      { "code": "EUR", "nostroIban": "DE12123456789012345678", "supportedCharges": ["BEN","SHA","OWN"] },
      { "code": "USD", "nostroIban": "DE34987654321098765432", "supportedCharges": ["SHA","OWN"] }
    ]},
    { "bic": "CHASUS33XXX", "currencies": [
      { "code": "USD", "nostroIban": "GB12CHAS12345678901234", "supportedCharges": ["BEN","SHA"] }
    ]}
  ]
}

## Post-Processing Validation
- For each GREEN route from evaluation:
  1) Currency support
     - If capabilities contain the route.corrBankBic AND a currency entry matching request.payment.currency → proceed
     - Else → move to RED with reason: `CAPABILITY:CURRENCY_UNSUPPORTED`
  2) Charge-bearer support
    - Normalize requested `payment.chargeBearer` (OUR→OWN) and compare against `supportedCharges`
     - If supported → annotate GREEN route with `nostroIban` and echo `chargeBearer`
     - Else → move to RED with reason: `CAPABILITY:CHARGE_BEARER_UNSUPPORTED`
- Keep existing RED routes intact. If a corresponding RED already exists, either coalesce descriptions or append a separate RED entry.

### Output Contract Changes
- greenRoutes[].nostroIban: string | omitted when not applicable
- greenRoutes[].currency: string (echo of request payment currency for clarity)
- greenRoutes[].chargeBearer: string (echo of normalized requested charge bearer)
- redRoutes[] may include entries created by the post-processor with a synthetic ruleCode (e.g., "CAPABILITY:CURRENCY_UNSUPPORTED").
  - Additional reason: "CAPABILITY:CHARGE_BEARER_UNSUPPORTED"

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
 - Modeling of fee amounts (only support presence/absence of charge-bearer types)

## Edge Cases
- Unknown BIC in capabilities → treat as unsupported for the requested currency (demote to RED)
- Multiple entries for the same BIC×currency → pick highest priority or first entry deterministically
- Mixed case currency codes → normalize to uppercase
 - "OWN" input is accepted and normalized to "OUR" for comparison and output

## Telemetry
- Metrics: routes_demoted_unsupported_currency, routes_demoted_unsupported_charge_bearer, routes_enriched_with_nostro, capabilities_snapshot_version
- Logs: per-route decisions and demotions with reasons

## Security
- Same as prior phases; service protected via environment (future mTLS/OAuth)

## Compatibility
- Rule catalog unchanged; capabilities are orthogonal.
- Response shape is backward compatible (nostroIban is additive; GREEN list only gets stricter).

## Service Boundaries
This phase formalizes three logical services (pipeline stages). They can run in-process or be split later:

- PaymentRequestEnrichment
  - Input: raw ruleContext
  - Output: enriched ruleContext (+ enrichmentVersion)
  - Responsibilities: normalization, derived fields (e.g., counterparty.type), low-cost lookups

- RuleEvaluation
  - Input: enriched ruleContext
  - Output: provisional decision (GREEN/RED lists) (+ rulesVersion)
  - Responsibilities: pure evaluation over a versioned rule snapshot; deterministic ordering

- PostProcessingValidation
  - Input: request + provisional decision
  - Output: final decision (+ capabilitiesVersion)
  - Responsibilities: currency and charge-bearer capability checks per CorrBankBIC; enrich nostroIban; demotions with reasons

Notes:
- Charge-bearer normalization: inputs may include "OUR"; the system normalizes to "OWN" for comparison and output. Capability data MAY use OWN/OUR but will be normalized internally to OWN.
- All responses SHOULD include versions for auditability: enrichmentVersion, rulesVersion, capabilitiesVersion.

---

### Appendix A — Split Deployment Option

When operational needs require independent scaling or ownership boundaries, the three logical services may be deployed separately.

- EnrichmentService
  - POST /enrich
  - Body: { ruleContext: { …raw… } }
  - Returns: { ruleContext: { …enriched… }, enrichmentVersion, correlationId }

- EvaluationService
  - POST /evaluate
  - Body: { ruleContext: { …enriched… }, correlationId }
  - Returns: { status, decision, greenRoutes[], redRoutes[], rulesVersion, correlationId }

- ValidationService
  - POST /validate
  - Body: { request: { ruleContext }, decision: { …from evaluate… }, correlationId }
  - Returns: { …final decision… (nostroIban, demotions) …, capabilitiesVersion, correlationId }

Considerations:
- Latency and reliability impact due to network hops; add retries and circuit breakers.
- Version drift across services; include explicit version fields in responses and logs.
- Shared correlationId passed end-to-end for tracing.

