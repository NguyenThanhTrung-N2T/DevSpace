# Architecture Decision Records (ADR) Backlog

We use ADRs to capture important architectural choices and their trade-offs.

## Writing Agreement
- **Evidence-First**: We write ADRs **after** the corresponding decision is proven or implemented in the codebase, not before.
- **Location**: Each decision is stored in a separate markdown file under `docs/04-adr/ADR-0xx-short-title.md`.

---

## ADR Backlog Checklist

- [ ] ADR-001 — Why Next.js?
- [ ] ADR-002 — Why ASP.NET for Auth?
- [ ] ADR-003 — Why Spring Boot for Core?
- [ ] ADR-004 — Why Express for Collaboration?
- [ ] ADR-005 — Database Ownership (schema-per-service)
- [ ] ADR-006 — JWT Strategy (RS256)
- [ ] ADR-007 — RabbitMQ Introduction
- [ ] ADR-008 — Redis Usage
- [ ] ADR-009 — Event Payload Design
- [ ] ADR-010 — Why Docker Compose (not Kubernetes)
- [ ] ADR-011 — Why PostgreSQL
- [ ] ADR-012 — Why (or why not) API Gateway
- [ ] ADR-013 — Why MinIO (when implemented)
- [ ] ADR-014 — Modular Core Internal Structure
- [ ] ADR-015 — Evolution Strategy Retrospective

---

## ADR Markdown Template

Use the following template for creating new ADRs:

```markdown
# ADR-0xx: [Decision Title]

- **Status**: [Proposed | Accepted | Superseded]
- **Date**: YYYY-MM-DD
- **Author**: [Name]

## Context / Problem
Describe the context, the problem we are trying to solve, and the constraints. What real-world issue led to this decision?

## Decision
State the chosen path clearly. What did we decide to do?

## Alternatives Considered
- **Alternative A**: Description and why it was rejected.
- **Alternative B**: Description and why it was rejected.

## Why (Trade-offs)
Explain the justification for the selected decision. Detail the trade-offs (pros and cons).

## Consequences & Future Evolution
- What are the immediate consequences of this decision?
- What must be done differently?
- Under what conditions will we reconsider or change this decision?
```
