# Phase 1.3 Plan — Corridor Capabilities + Post-Processing Validation + Service

## Workstreams
1) Spec and API contract updates
2) Capabilities storage abstraction and JSON implementation
3) Post-processing pipeline and currency support validator
4) Minimal HTTP service host wiring
5) Tests (unit + scenario snapshots) and benchmarks update

## Milestones
- M1: Spec docs drafted; sample capabilities file added
- M2: ICapabilitiesStore + JsonFileCapabilitiesStore implemented
- M3: CurrencySupportPostProcessor integrated into RoutingEngineHost pipeline
- M4: Minimal API host with DI + configuration
- M5: Tests green (unit, scenario), sample run in README/quickstart

## Risks and Mitigations
- Rule/Capability duplication: Keep capabilities out of rules; validate post-eval
- Performance overhead: Cache snapshot, O(1) BIC lookup (dictionary), measure delta
- Data drift: Version snapshots; optional FileSystemWatcher later

## Deliverables
- docs: spec.md, plan.md, tasks.md, quickstart.md, contracts/openapi.yaml (delta)
- code: capabilities store interfaces and JSON impl; post-processor; host wiring
- tests: unit + scenario
- samples: capabilities.sample.json

