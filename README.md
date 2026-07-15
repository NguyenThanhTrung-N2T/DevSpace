# DevSpace — Developer Workspace for Modern Software Engineering

DevSpace is a **Living Portfolio** project designed as a case study to learn and implement real-world software engineering practices. It utilizes three backend ecosystems (.NET, Spring Boot, Node.js) and a modern Next.js frontend, progressing through an evolutionary architecture.

## 4 Core Principles

1. **Evidence First**: Technologies and services are added only when there is a concrete, real-world engineering problem to solve. No premature "best practice" introduction.
2. **Business Simple, Engineering Deep**: Keep business logic straightforward while focusing heavily on architecture quality, clean code, testing, security, and DevOps.
3. **Evolutionary Architecture**: The system evolves incrementally. We start with a single Monorepo, Docker Compose, and Nginx path-routing, and introduce API Gateways, message queues (RabbitMQ), caches (Redis), and centralized logging/monitoring only when necessary.
4. **Production First**: Every development phase must end with a fully buildable, deployable, and demoable release on a live environment (VPS).

---

## Monorepo Directory Structure

```text
devspace/
├── apps/
│   ├── web/                    # Next.js Frontend (Bootstrap / Phase 3)
│   ├── auth-service/           # ASP.NET Core Auth Service (Bootstrap / Phase 1)
│   ├── core-service/           # Spring Boot Core Business (Phase 2)
│   └── collaboration-service/  # Express.js Realtime Service (Phase 5)
├── packages/
│   ├── contracts/              # Shared API/Event schemas and DTOs
│   └── shared/                 # Shared configs, helpers, and types
├── infrastructure/
│   ├── docker/                 # docker-compose and DB initialization scripts
│   └── nginx/                  # nginx.conf for path-routing
├── docs/                       # Project Documentation & ADRs
│   ├── 01-vision/              # Project Vision and Core Ideas
│   ├── 02-roadmap/             # Evolutionary Development Roadmap
│   ├── 03-architecture/        # Architectural Diagrams
│   ├── 04-adr/                 # Architecture Decision Records (ADRs)
│   └── ...
└── .github/
    └── workflows/              # GitHub Actions CI/CD workflows
```

---

## Quick Start (Local Development)

### Prerequisites
- [Docker & Docker Compose](https://www.docker.com/)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/)
- [Node.js v22+](https://nodejs.org/)

### Spin up the environment
1. Copy the environment template:
   ```bash
   cp infrastructure/docker/.env.example infrastructure/docker/.env
   ```
2. Open `infrastructure/docker/.env` and specify a secure database password.
3. Run docker compose:
   ```bash
   cd infrastructure/docker
   docker compose up --build -d
   ```
4. Access:
   - Next.js Web: [http://localhost](http://localhost)
   - Auth Service Health Check: [http://localhost/api/auth/health](http://localhost/api/auth/health)

## ⚠️ Security Notes

- **Never commit `.env` files** — they contain sensitive credentials and are ignored by `.gitignore`.
- **Use strong keys & credentials** in production environments.
- **Enable HTTPS/SSL** at the Nginx edge or proxy layer before public deployment.
- **Rotate JWT Keys** regularly to prevent compromise.

---

## Documentation Index
- [Project Roadmap](file:///d:/Personal%20Project/DevSpace/docs/02-roadmap/README.md)
- [Architecture Decision Records (ADRs)](file:///d:/Personal%20Project/DevSpace/docs/04-adr/README.md)
