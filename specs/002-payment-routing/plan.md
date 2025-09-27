
# Implementation Plan: Payment Routing Rules Engine (Phase 1)

**Branch**: `002-payment-routing` | **Date**: 2025-09-26 | **Spec**: [`spec.md`](./spec.md)
**Input**: Feature specification from `/specs/002-payment-routing/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code or `AGENTS.md` for opencode).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Implement the first delivery phase of the payment routing rules engine as a .NET 9 class library that evaluates routing rules loaded from JSON, producing GREEN and RED route lists plus a `CAN_ROUTE` / `CAN_NOT_ROUTE` decision. Phase 1 focuses on validating the engine’s rule evaluation logic, logging via Serilog (console sink), comprehensive test coverage with xUnit (including property-based checks), and benchmarking hot paths with BenchmarkDotNet to confirm <10 ms per evaluation.

## Technical Context
**Language/Version**: C# / .NET 9 (class library)  
**Primary Dependencies**: Serilog (console sink), BenchmarkDotNet, System.Text.Json  
**Storage**: JSON rule catalog file (Phase 1 in-memory load)  
**Testing**: xUnit (+ FsCheck for property-based tests), Verify for golden snapshots  
**Target Platform**: Cross-platform .NET runtime (Windows/Linux build agents)  
**Project Type**: Single back-end engine library (Option 1 structure)  
**Performance Goals**: <10 ms latency per rule evaluation in BenchmarkDotNet harness  
**Constraints**: Deterministic evaluation, numeric priority weights, logging errors/warnings only via Serilog console sink  
**Scale/Scope**: Initial validation with catalogs up to 1,000 rules; no HTTP/API surface yet

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Principle I – Test & Contract First**: Plan emphasizes xUnit and property-based tests preceding implementation; golden fixtures derived from spec satisfy the mandate. ✅
- **Principle II – Clean Endpoints**: No HTTP endpoints in Phase 1 (library only), so separation of concerns is maintained. ✅
- **Principle III – Simplicity & Observability**: Serilog console sink with minimal logging matches the “errors-only baseline”. ✅
- **Principle IV – Data Integrity**: Numeric priority weights, deterministic evaluation, and auditing outputs align with deterministic requirements. ✅
- **Principle V – Traceability**: Spec includes Clarifications; plan + forthcoming research/tasks maintain traceability chain. ✅

**Result (Initial Check)**: No constitutional violations identified.

### Post-Design Constitution Check
- New artifacts (research, data model, library contract, quickstart) maintain the same constraints; still no violations. ✅

## Project Structure

### Documentation (this feature)
```
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
# Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure]
```

**Structure Decision**: Option 1 (single project) – `src/` for engine library, `tests/` for unit/property/benchmark harnesses.

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:
   ```
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved (Phase 1 clarifications already captured; document supporting details)

## Phase 1: Design & Contracts
*Prerequisites: research.md complete*

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:
   - For each user action → endpoint
   - Use standard REST/GraphQL patterns
   - Output OpenAPI/GraphQL schema to `/contracts/`

3. **Generate contract tests** from contracts:
   - One test file per endpoint
   - Assert request/response schemas
   - Tests must fail (no implementation yet)

4. **Extract test scenarios** from user stories:
   - Each story → integration test scenario
   - Quickstart test = story validation steps

5. **Update agent file incrementally** (O(1) operation):
    - Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`
       **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
    - Record adoption of .NET 9, Serilog console sink, BenchmarkDotNet, FsCheck, JSON rule catalogs
    - Preserve manual additions between markers, keep last 3 recent changes
    - Keep file under 150 lines for token efficiency
    - Output to repository root

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, agent-specific file

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `.specify/templates/tasks-template.md` as base
- Derive tasks from Phase 1 artifacts (research.md, data-model.md, quickstart.md)
- Each contract/interface → contract test & validation task [P]
- Each entity/config → model mapping & parser task [P]
- Benchmark harness → performance test task
- Logging & configuration requirements → infrastructure wiring tasks
- Implementation tasks allocated after tests (TDD compliance)

**Ordering Strategy**:
- TDD sequencing: contract/property tests → rule evaluation engine → benchmarking harness → logging/config wiring
- Dependency order: Domain models & parser before evaluator, evaluator before bench/test harness, instrumentation last
- Mark independent tasks (e.g., benchmark harness vs. config loader) with [P] for parallel execution

**Estimated Output**: ~20-24 ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |


## Progress Tracking
*This checklist is updated during execution flow*

- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [x] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved
- [ ] Complexity deviations documented

*Based on Constitution v1.0.0 - See `.specify/memory/constitution.md`*
