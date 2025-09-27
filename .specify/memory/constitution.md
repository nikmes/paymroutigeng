<!--
Sync Impact Report
- Version change: 0.0.0 → 1.0.0
- Modified principles: N/A (initial adoption)
- Added sections: Core Principles (5), Additional Constraints, Development Workflow & Quality Gates
- Removed sections: None
- Templates requiring updates:
	- .specify/templates/plan-template.md → ✅ updated (footer path/version)
	- .specify/templates/tasks-template.md → ✅ updated (footer path/version)
	- .specify/templates/spec-template.md → ✅ no change needed
	- specs/001-corresponding-bank-routing/plan.md → ✅ updated (footer path/version)
- Follow-up TODOs: None
-->

# corrroute Project Constitution

## Core Principles

### I. Test- and Contract-First (NON-NEGOTIABLE)
All externally visible behavior MUST be defined by executable tests and contracts before
implementation. Write contract tests from OpenAPI first; ensure they fail; then implement
to pass. Follow Red-Green-Refactor with small commits.

Rationale: Contracts and tests anchor behavior, reduce regressions, and accelerate safe change.

### II. Clean Endpoints & Separation of Concerns
Expose HTTP endpoints via Minimal APIs with each endpoint in its own file. `Program.cs`
MUST only contain composition/wiring. Handlers delegate to application services; no data
access or business rules in endpoint files. Domain, Application, and Infrastructure layers
are separated and unit-testable.

Rationale: Keeps I/O thin, promotes maintainability, and enables focused testing.

### III. Simplicity First & Progressive Observability
Prefer the simplest solution that satisfies current requirements (YAGNI). For Phase 1,
logging is errors-only. More observability (structured logs, tracing, metrics) MAY be added
in later phases behind clear acceptance criteria.

Rationale: Avoids premature complexity and noise while preserving room to evolve.

### IV. Data Integrity & Time-Windowed Rules
Time-bounded entries use UTC and are effective when `now ∈ [EffectiveFrom, EffectiveTo]`.
Evaluations MUST be deterministic and audited. Status precedence MUST be enforced as
defined by the feature spec (e.g., UNAVAILABLE > BLOCKED > OPEN).

Rationale: Ensures correctness, auditability, and reproducible decisions.

### V. Traceability & Documentation
Every feature includes a spec → plan → tasks chain. Decisions and clarifications are
recorded close to the feature. Acceptance scenarios map to tests. Documentation is updated
as a first-class deliverable.

Rationale: Maintains transparency and reduces onboarding and maintenance costs.

## Additional Constraints

- Backend stack: .NET 9 (C#), ASP.NET Core Minimal APIs, PostgreSQL, EF Core.
- Logging: Serilog with errors-only baseline in Phase 1.
- API documentation: Swagger (Swashbuckle).
- Caching: FusionCache for read-heavy endpoints where justified.
- Testing: xUnit; contract tests for OpenAPI endpoints; unit tests for rules.
- Security (Phase 1): Any authenticated internal user may manage/query the lists and routes.
	Future RBAC will refine scopes.
- Performance: Targets to be defined per feature; add BenchmarkDotNet harnesses for hot paths.
- Code quality: `nullable enable`; warnings as errors where practical.

## Development Workflow & Quality Gates

1) Gates and Phases
- Spec → Clarify → Plan (Phase 0 research, Phase 1 design/contracts) → Tasks → Implementation → Validation.

2) Quality Gates (MUST PASS in CI)
- Build and lint/typecheck pass.
- Contract tests exist and pass for public endpoints.
- Unit tests for core rules and precedence pass.
- Constitution compliance acknowledged in PR description.

3) Pull Requests
- Small, focused PRs with clear scope and tests.
- Conventional commit messages preferred.
- Reviewers verify alignment with this constitution and the feature spec.

4) Versioning
- Public contracts and this constitution follow SemVer. 
	- MAJOR: Backward-incompatible governance or contract changes
	- MINOR: New principles/sections or materially expanded guidance
	- PATCH: Clarifications and non-semantic refinements

## Governance

This constitution supersedes prior ad-hoc practices. Amendments require a PR that:
- Explains the change and its impact
- Includes a migration/communication plan when needed
- Bumps the constitution version per SemVer and updates dependent templates/docs
- Is approved by maintainers

Compliance is reviewed during PRs. Exceptions MUST be explicitly justified and time-boxed.

**Version**: 1.0.0 | **Ratified**: 2025-09-24 | **Last Amended**: 2025-09-24