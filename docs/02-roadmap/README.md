# DevSpace Evolutionary Roadmap

This roadmap tracks the development of DevSpace. Because we practice **Evolutionary Architecture**, each phase builds on top of the previous one by responding to specific engineering needs.

---

## Phase Checklist

### [ ] Bootstrap (Current Phase)
- **Goal**: Setup monorepo structure, minimal Docker Compose, build pipelines (CI), and root skeletons.
- **Exit Criteria**:
  - `docker compose up` runs successfully with Next.js web skeleton, ASP.NET auth stub, and PostgreSQL.
  - CI (GitHub Actions) passes lint/build checks on Pull Request.
  - Initial codebase is committed and tagged `v0.0-bootstrap`.

### [ ] Phase 1 — ASP.NET Auth Service
- **Goal**: Complete Auth Service in ASP.NET Core: Register, Login, JWT verification, Refresh Tokens, Google OAuth, Forgot Password.
- **Exit Criteria**:
  - Auth Service is fully functional and deployed on VPS.
  - Secure JWT validation (RS256 asymmetric keys via JWKS endpoint).
  - Production-grade tests are added.

### [ ] Phase 2 — Core Service
- **Goal**: Introduce Spring Boot Core Service to manage workspaces, projects, tasks, wikis, and comments.
- **Exit Criteria**:
  - Auth & Core services run alongside Next.js under Nginx.
  - Core Service validates requests using JWKS public keys.
  - API documentation generated and verified.

### [ ] Phase 3 — Next.js UX/UI
- **Goal**: Build full dashboard, project Kanban boards, Wiki reader, and rich workspaces on the frontend.
- **Exit Criteria**:
  - Frontend connects smoothly to backend endpoints through Nginx routing.
  - Application deployed to staging/production VPS.

### [ ] Phase 4 — Caching & Session (Redis)
- **Goal**: Add Redis caching to decrease database read latency and optimize JWT/refresh token verification.
- **Exit Criteria**:
  - Measurable latency reduction on high-traffic endpoints.
  - Cache invalidation strategies documented and verified.

### [ ] Phase 5 — Collaboration (Express & WebSockets)
- **Goal**: Build an Express.js real-time service for notifications, online presence, and collaboration.
- **Exit Criteria**:
  - Real-time updates pushed to frontend clients via WebSockets (Socket.io).
  - Clear service separation: Express doesn't own or directly write to core PostgreSQL tables.

### [ ] Phase 6 — Event-Driven Architecture (RabbitMQ)
- **Goal**: Decouple communication between Spring Boot Core and Express Collaboration using RabbitMQ.
- **Exit Criteria**:
  - Event payloads follow the Event-Carried State Transfer pattern.
  - Dead-letter exchanges (DLX) and message retries configured.

### [ ] Phase 7 — Centralized Monitoring & Telemetry
- **Goal**: Add Prometheus and Grafana dashboards to monitor resource usage, error rates, and API throughput.
- **Exit Criteria**:
  - Live dashboards reflecting CPU, Memory, HTTP rates, and errors.
