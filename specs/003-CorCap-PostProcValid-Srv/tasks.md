# Phase 1.3 Tasks — Corridor Capabilities + Post-Processing Validation + Service

- [ ] D01: Draft spec (spec.md) and align glossary/semantics
- [ ] D02: Update OpenAPI (contracts/openapi.yaml) to include greenRoutes[].nostroIban and greenRoutes[].chargeBearer; clarify post-processing demotions
- [ ] D03: Add sample capabilities file config/capabilities.sample.json (with supportedCharges per currency)
- [ ] C01: Define ICapabilitiesStore and CorridorCapabilitiesSnapshot
- [ ] C02: Implement JsonFileCapabilitiesStore (cache + version)
- [ ] C03: Add IRoutePostProcessor and CapabilityPostProcessor (currency + charge-bearer)
- [ ] C04: Integrate post-processors into RoutingEngineHost
- [ ] T01: Unit tests for currency support (enrich + demote)
- [ ] T01b: Unit tests for charge-bearer support (normalize OWN→OUR; demote when unsupported)
- [ ] T02: Scenario snapshot including mixed supported/unsupported routes
- [ ] T03: Benchmarks update to include post-processor overhead
- [ ] Q01: README/quickstart updates with capabilities and service wiring
- [ ] R01: Optional hot-reload (FileSystemWatcher) with debounce
- [ ] R02: Optional content-hash versioning for capabilities

