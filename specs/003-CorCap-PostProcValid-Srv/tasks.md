# Phase 1.3 Tasks — Corridor Capabilities + Post-Processing Validation + Service

- [x] D01: Draft spec (spec.md) and align glossary/semantics
- [x] D02: Update OpenAPI (contracts/openapi.yaml) to include greenRoutes[].nostroIban and greenRoutes[].chargeBearer; clarify post-processing demotions
- [x] D03: Add sample capabilities file config/capabilities.sample.json (with supportedCharges per currency)
- [x] C01: Define ICapabilitiesStore and CorridorCapabilitiesSnapshot
- [x] C02: Implement JsonFileCapabilitiesStore (cache + version)
- [x] C03: Add IRoutePostProcessor and CapabilityPostProcessor (currency + charge-bearer)
- [x] C04: Integrate post-processors into RoutingEngineHost
- [x] T01: Unit tests for currency support (enrich + demote)
- [x] T01b: Unit tests for charge-bearer support (normalize OWN; demote when unsupported)
- [ ] T02: Scenario snapshot including mixed supported/unsupported routes
- [ ] T03: Benchmarks update to include post-processor overhead
- [ ] Q01: README/quickstart updates with capabilities and service wiring
- [ ] R01: Optional hot-reload (FileSystemWatcher) with debounce
- [ ] R02: Optional content-hash versioning for capabilities
 - [ ] S01 (deferred): Minimal API service scaffolding — create ASP.NET Core minimal API, wire DI (IRuleStore, ICapabilitiesStore, IRoutePostProcessor, RoutingEngineHost), add endpoints (/payment-routing/resolve, /payment-routing/eligibility), configure appsettings paths, and add a small "Try it" section.

