# Kocluk API

Multi-tenant, microservice-based backend built on **.NET 9** (minimal APIs, Clean
Architecture per service, MassTransit + RabbitMQ messaging, EF Core + PostgreSQL).
A **YARP** API gateway sits in front and is the single public entrypoint.

---

## Architecture

```
                         ┌─────────────────────────┐
        browser / app ──▶│   API Gateway (YARP)     │  :8080
                         └───────────┬─────────────┘
              routes /api/**         │
        ┌───────────────────┬────────┴───────────┐
        ▼                   ▼                     ▼
┌───────────────┐  ┌───────────────┐   ┌────────────────────┐
│ Security.API  │  │  Tenant.API   │   │ Notification.API   │
│ auth, users,  │  │  tenants      │   │ (internal worker:  │
│ roles, perms, │  │               │   │  email dispatch,   │
│ sessions, MFA │  │               │   │  no public routes) │
└──────┬────────┘  └──────┬────────┘   └─────────┬──────────┘
       │                  │                       │
       ▼                  ▼                       ▼
   PostgreSQL         PostgreSQL              PostgreSQL
   + Redis          (RabbitMQ bus shared by all three services)
```

| Service            | Responsibility                                             | Depends on                     | Public via gateway |
|--------------------|------------------------------------------------------------|--------------------------------|--------------------|
| **API Gateway**    | Reverse proxy, routing, CORS, single entrypoint            | Security, Tenant               | —                  |
| **Security.API**   | Auth (JWT), users, memberships, roles, permissions, sessions, MFA | PostgreSQL, Redis, RabbitMQ | ✅ `/api/auth`, `/api/users`, `/api/memberships`, `/api/roles`, `/api/permissions`, `/api/sessions`, `/api/mfa` |
| **Tenant.API**     | Tenant lifecycle (create/rename/activate/deactivate/delete)| PostgreSQL, RabbitMQ           | ✅ `/api/tenants`  |
| **Notification.API**| Consumes integration events, sends emails (Resend)        | PostgreSQL, RabbitMQ           | ❌ internal only   |

Users log in with **email + password only** — there is no tenant header. A user is
**globally unique by email** and can belong to many tenants through **memberships**
(each with its own tenant roles and status; removed memberships are kept for history).
Self-registration enrolls the user into the **public tenant** (the seeded default tenant
id). The access token carries a `memberships` claim (per-tenant roles + status) and a
`system_admin` claim; a system administrator (a platform-scoped role) can access and
manage every tenant. The Tenant service owns the tenant lifecycle and publishes/consumes
integration events so each service keeps its own view of tenants consistent.

---

## Repository layout

```
src/
  Gateway/ApiGateway/              # YARP reverse proxy (public entrypoint)
  Services/
    Security/                      # Security.{API,Application,Domain,Infrastructure}
    Tenant/                        # Tenant.{API,Application,Domain,Infrastructure}
    Notification/                  # Notification.{API,Application,Domain,Infrastructure}
  BuildingBlocks/                  # shared Domain + Messaging (RabbitMQ/MassTransit)
build/
  docker/                          # one Dockerfile per service + the gateway
deploy/
  docker-compose.yml               # full local stack (infra + services + gateway)
  .env.example                     # copy to .env
  postgres/init-databases.sh       # creates the per-service databases
  k8s/                             # Kubernetes reference manifests (kustomize)
.github/workflows/                 # CI (build/test) + Docker image build
frontend_guide.md                  # every gateway route with request/response contracts
```

---

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) + Docker Compose v2 — for the containerised stack
- [.NET SDK 9.0](https://dotnet.microsoft.com/download) — only if you want to run services directly
- (optional) `kubectl` + a local cluster (kind / minikube / Docker Desktop) for the k8s manifests

---

## Quick start — Docker Compose (recommended)

From the **repository root**:

```bash
# 1. Create your env file (defaults are fine for local dev)
cp deploy/.env.example deploy/.env

# 2. Build images and start the whole stack
docker compose -f deploy/docker-compose.yml up -d --build

# 3. Watch it come up (services wait for Postgres/RabbitMQ/Redis to be healthy)
docker compose -f deploy/docker-compose.yml ps
```

Everything is reachable **through the gateway**:

| Endpoint                                   | URL                              |
|--------------------------------------------|----------------------------------|
| API Gateway (use this from the frontend)   | http://localhost:8080            |
| Gateway health                             | http://localhost:8080/health/ready |
| RabbitMQ management UI                      | http://localhost:15672 (kocluk / kocluk) |

Direct per-service ports are also published for debugging: Security `:8081`,
Tenant `:8082`, Notification `:8083`. In normal use you only need `:8080`.

Databases are auto-migrated on startup (the services run in the `Development`
environment inside Compose) and Security seeds a default admin — see
[Seeded admin](#seeded-admin-development).

### Managing the stack

```bash
docker compose -f deploy/docker-compose.yml logs -f api-gateway   # tail one service
docker compose -f deploy/docker-compose.yml logs -f security-api
docker compose -f deploy/docker-compose.yml restart security-api  # restart one
docker compose -f deploy/docker-compose.yml down                  # stop (keep data)
docker compose -f deploy/docker-compose.yml down -v               # stop + wipe volumes
docker compose -f deploy/docker-compose.yml up -d --build security-api  # rebuild one
```

---

## Running locally without Docker

Start the infrastructure in containers, then run the services with `dotnet`:

```bash
# Infra only
docker compose -f deploy/docker-compose.yml up -d postgres rabbitmq redis
```

The per-service `appsettings.Development.json` files point at `localhost` for
Postgres/Redis/RabbitMQ, so once the infra is up you can run each project:

```bash
dotnet run --project src/Services/Security/Security.API      # http://localhost:5188
dotnet run --project src/Services/Tenant/Tenant.API          # http://localhost:5200
dotnet run --project src/Services/Notification/Notification.API  # http://localhost:5100
dotnet run --project src/Gateway/ApiGateway                  # http://localhost:5000
```

> The dev connection strings expect a Postgres role `kemal` / `kemal1234`. Either
> create that role, or override with environment variables (see below). The Compose
> databases use the `kocluk` role, so when mixing the two, pass a matching
> `ConnectionStrings__Postgres`.

Swagger UI (development only) is available per service, e.g.
`http://localhost:5188/swagger`.

Build / test the whole solution:

```bash
dotnet build kocluk-api.sln
dotnet test  kocluk-api.sln
```

---

## Configuration

Configuration is layered: `appsettings.json` → `appsettings.{Environment}.json` →
environment variables (highest priority). Nested keys use `__` in env vars.

Key settings (all overridable via env):

| Setting                         | Env var                                   | Used by        |
|---------------------------------|-------------------------------------------|----------------|
| Postgres connection             | `ConnectionStrings__Postgres`             | all services   |
| Redis connection                | `ConnectionStrings__Redis`                | Security       |
| RabbitMQ host / user / pass     | `RabbitMq__Host` / `__Username` / `__Password` | all services |
| JWT signing key (≥ 32 chars)    | `Jwt__SigningKey`                         | Security, Tenant |
| JWT issuer / audience           | `Jwt__Issuer` / `Jwt__Audience`           | Security, Tenant |
| Seeded admin                    | `IdentitySeed__AdminEmail` / `__AdminPassword` | Security  |
| Public tenant id                | `Tenancy__PublicTenantId`                 | Security, Tenant |
| Resend email API key            | `Resend__ApiKey`                          | Notification   |
| Gateway → service addresses     | `ReverseProxy__Clusters__<cluster>__Destinations__primary__Address` | Gateway |

The Compose defaults live in `deploy/.env`; edit that file to change credentials or
host port mappings.

### Environment behaviour

- **Development** — migrations run automatically; Security seeds the admin + baseline
  roles/permissions; Swagger enabled. (This is what Compose uses.)
- **Staging** — migrations run automatically; no dev seeding.
- **Production** — no automatic migration/seed; run migrations as a deliberate step.

---

## Database migrations

Migrations are applied automatically in Development/Staging on startup. To manage
them manually with the EF Core tools:

```bash
dotnet tool install --global dotnet-ef      # once

# Add a migration (example: Security)
dotnet ef migrations add <Name> \
  --project src/Services/Security/Security.Infrastructure \
  --startup-project src/Services/Security/Security.API

# Apply migrations against a running database
dotnet ef database update \
  --project src/Services/Security/Security.Infrastructure \
  --startup-project src/Services/Security/Security.API
```

Each service owns its schema and migration history table (`__ef_migrations_history`
in schema `security` / `tenant` / `notification`).

---

## Messaging (RabbitMQ)

All three services share one RabbitMQ broker via MassTransit with the transactional
EF Core outbox. Tenant publishes tenant lifecycle events; Security consumes them to
keep a local tenant read model; Notification consumes email-request events (password
reset, email verification, etc.) and sends mail. Inspect queues/exchanges in the
management UI at http://localhost:15672.

---

## Health checks

Every service (and the gateway) exposes:

- `GET /health/live` — liveness (process is up)
- `GET /health/ready` — readiness (dependencies — DB/Redis — reachable)

These back the Docker `HEALTHCHECK`, Compose `depends_on: service_healthy`, and the
Kubernetes liveness/readiness probes.

---

## CI/CD

GitHub Actions under `.github/workflows/`:

- **`ci.yml`** — restore, build and test the solution on every push/PR.
- **`docker-build.yml`** — builds all four container images (matrix) to verify the
  Dockerfiles. **No registry push and no deploy step yet** — a target platform has
  not been chosen. When you pick one, add registry login + `push: true` (and a deploy
  job) to this workflow; the images are already produced and ready to tag.

---

## Kubernetes (reference)

`deploy/k8s/` contains a complete kustomize base (namespace, config/secret, Postgres,
RabbitMQ, Redis, the three services, and the gateway with an Ingress). It is a
starting point — replace the demo secret and wire an ingress controller for real use.

```bash
# Build images and load them into your local cluster first, e.g. for kind:
docker compose -f deploy/docker-compose.yml build
kind load docker-image kocluk/security-api:latest kocluk/tenant-api:latest \
  kocluk/notification-api:latest kocluk/api-gateway:latest

kubectl apply -k deploy/k8s
kubectl -n kocluk get pods
# Reach the gateway (if no ingress): port-forward
kubectl -n kocluk port-forward svc/api-gateway 8080:80
```

---

## Seeded admin (development)

When Security runs in Development it seeds an admin account and the baseline
roles/permissions:

- **Email:** `admin@local`
- **Password:** `DummyPw123!`

Override via `IdentitySeed__AdminEmail` / `IdentitySeed__AdminPassword`. Never enable
this seeding outside development.

---

## Troubleshooting

- **A service restarts / is unhealthy on first boot** — it is waiting for Postgres or
  RabbitMQ. Compose orders startup on health, but the very first migration can race a
  slow DB; it self-heals on retry. Check `docker compose ... logs <service>`.
- **`Jwt signing key must be at least 32 characters`** — set a longer `JWT_SIGNING_KEY`
  in `deploy/.env`.
- **RabbitMQ auth failures** — the broker rejects the built-in `guest` user over the
  network, so a dedicated `kocluk` user is created; make sure `RabbitMq__Username/Password`
  match `deploy/.env`.
- **429 responses** — Security enforces per-endpoint rate limits (see
  `frontend_guide.md`); back off and retry.

---

## Frontend integration

See **[frontend_guide.md](frontend_guide.md)** for every gateway route with its
request/response contract, required authentication, and required permissions.
