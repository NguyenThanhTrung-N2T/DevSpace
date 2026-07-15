# Project Vision & Tenets

## Project Vision
DevSpace is a Developer Workspace for Modern Software Engineering. The project's goal is to serve as a **Living Portfolio** where the depth of modern software engineering practices, patterns, and principles can be demonstrated and learned.

Instead of writing complex business workflows, DevSpace focuses on building simple features (Workspaces, Projects, Tasks, Wikis, Comments) using deep engineering techniques:
- Multiple backend technology stacks to evaluate trade-offs (.NET, Java Spring Boot, Express.js).
- Rigorous security practices (JWT token rotation, RS256 asymmetry, HTTPS/TLS).
- Solid containerization & deployment strategies.
- Scalable asynchronous architecture and service decoupled communication.
- Comprehensive telemetry and instrumentation.

## Core Architectural Tenets

### 1. Evidence First
- Do not introduce a technology just because it is a "best practice".
- We only introduce new components (Redis, RabbitMQ, API Gateway) when there is an active problem or measurement showing we need them.

### 2. Business Simple, Engineering Deep
- Keep features simple to avoid getting bogged down in business logic.
- Put maximum effort into writing production-grade code, setting up testing suites, logging, performance optimization, and clean architecture.

### 3. Evolutionary Architecture
- The system starts as a single monorepo with path routing through Nginx.
- As traffic or decouple requirements grow, we refactor and introduce services sequentially.

### 4. Production First
- Every phase has to end with a fully functional build deployed on a VPS.
- No code only exists on a developer's localhost.
