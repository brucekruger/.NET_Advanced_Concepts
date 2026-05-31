# Self-Check Questions

Conceptual answers for the containerization module. Where useful, answers reference the stack in this project (the `docker-compose.yml` with Catalog/Cart services + SQL Server, Redis, RabbitMQ, Keycloak).

## 1. What is orchestration?

**Container orchestration** is the automated management of the full lifecycle of containers across one or many hosts. Instead of manually starting, wiring, and watching containers, an orchestrator does it for you.

Typical responsibilities:

- **Scheduling/placement** – deciding which host/node a container runs on.
- **Service discovery & networking** – letting containers find each other by name.
- **Scaling** – running N replicas, scaling up/down on demand.
- **Self-healing** – restarting crashed containers, rescheduling them if a node dies.
- **Rolling updates & rollbacks** – deploying new versions without downtime.
- **Load balancing**, **secret/config management**, **health checks**.

Examples: **Kubernetes** (the de-facto standard), Docker Swarm, Nomad, AWS ECS.

> In this project, **Docker Compose** is a lightweight, single-host form of orchestration — it brings up the containers, sets up the `microservices-network`, defines `depends_on` ordering, health checks, and restart policies. Kubernetes would be the multi-host, production-grade equivalent.

## 2. What is containerization (pros and cons)?

**Containerization** packages an application together with its dependencies, libraries, and runtime configuration into a single, portable unit (an **image**) that runs as an isolated process (a **container**) on a shared OS kernel. "Build once, run anywhere."

**Pros:**

- **Consistency** – eliminates "works on my machine"; the same image runs in dev, CI, and prod.
- **Lightweight & fast** – shares the host kernel, so containers start in seconds and use far less RAM/disk than VMs.
- **Isolation** – each service has its own filesystem, network namespace, and process space.
- **Portability** – runs on any host with a container runtime.
- **Scalability & density** – many containers per host; easy to replicate.
- **Reproducible builds** – the Dockerfile is infrastructure-as-code.

**Cons:**

- **Weaker isolation than VMs** – containers share the host kernel, so the security boundary is thinner (a kernel exploit affects all).
- **OS coupling** – Linux containers need a Linux kernel (on Windows/Mac they run via a lightweight VM).
- **Statefulness is harder** – databases/persistence need volumes and careful handling (e.g., the named volumes for SQL Server, Redis, RabbitMQ in this project).
- **Operational complexity** – orchestration, networking, monitoring, image/registry management add overhead.
- **Image sprawl & security** – large or outdated base images introduce vulnerabilities; needs scanning and maintenance.

## 3. Containerization vs virtualization

| Aspect | Virtualization (VMs) | Containerization |
|--------|----------------------|------------------|
| **Isolation unit** | Full virtual machine | Process (namespaces + cgroups) |
| **Includes a guest OS?** | Yes — each VM has its own OS kernel | No — shares the host OS kernel |
| **Hypervisor / engine** | Hypervisor (ESXi, Hyper-V, KVM) | Container runtime (Docker/containerd) |
| **Size** | GBs | MBs |
| **Startup time** | Minutes | Seconds (or less) |
| **Resource overhead** | High (duplicate OS per VM) | Low |
| **Isolation strength** | Strong (hardware-level) | Lighter (kernel-level) |
| **Density per host** | Few | Many |

**Key idea:** a VM virtualizes the **hardware** (so you can run different OSes), while a container virtualizes the **operating system** (sharing one kernel). They're complementary — containers often run *inside* VMs in the cloud for an extra isolation layer.

## 4. Usage flow of Docker & Kubernetes

**Docker (build → ship → run):**

1. **Write a `Dockerfile`** – define base image, copy code, build, set entrypoint (e.g., this project's multi-stage `CatalogService.Api/Dockerfile`: SDK build stage → publish → slim `aspnet` runtime stage).
2. **Build the image** – `docker build` produces an immutable image layered for cache efficiency.
3. **Ship** – `docker push` to a registry (Docker Hub, ACR, ECR).
4. **Run** – `docker run` (single container) or `docker compose up` (multi-container app, as here).

**Kubernetes (declare desired state → reconcile):**

1. **Containerize** with Docker (same images).
2. **Write manifests** (YAML): `Deployment` (pods + replicas), `Service` (stable networking/load balancing), `ConfigMap`/`Secret` (config), `Ingress` (external routing), `PersistentVolumeClaim` (storage), etc.
3. **Apply** – `kubectl apply -f`. You declare the *desired state*.
4. **Reconcile** – the control plane (API server + scheduler + controllers) continuously schedules pods onto nodes and keeps actual state == desired state.
5. **Operate** – self-healing, `kubectl scale`, rolling updates (`kubectl rollout`), autoscaling (HPA), monitoring.

**The mental model:** Docker is **imperative and single-host** ("run this container now"); Kubernetes is **declarative and multi-host** ("always keep 3 healthy replicas of this, behind this service"). Docker builds the artifact; Kubernetes runs it at scale in production.

## 5. Best practices for containerization

**Image/Dockerfile:**

- **Use multi-stage builds** to keep runtime images small (build with SDK, ship with runtime only — as done in both Dockerfiles here).
- **Use small, official, pinned base images** (e.g., `redis:7-alpine`, `postgres:16-alpine`) — avoid `latest` in production for reproducibility.
- **Order layers for cache efficiency** – copy project files and `restore` *before* copying all source (both Dockerfiles do this).
- **Use `.dockerignore`** to keep the build context small and avoid leaking secrets.
- **Run as a non-root user** where possible.
- **One main process per container** (single responsibility).

**Configuration & secrets:**

- **Externalize config via environment variables** (this project injects `ConnectionStrings__*`, `RabbitMq__*`, `Authentication__*` through Compose), not baked into images.
- **Never hardcode secrets** in images — use secret managers / orchestrator secrets. (For real deployments, the demo passwords here should move to a secret store.)

**Runtime & operations:**

- **Make containers stateless**; persist data in **volumes** (as with SQL Server/Redis/RabbitMQ/LiteDB volumes here).
- **Add health checks** (every service here has one) and proper startup ordering / retries (e.g., Catalog's DB-connect retry loop).
- **Set resource limits** (CPU/memory — defined per service in the compose file).
- **Log to stdout/stderr** so the platform can collect logs.
- **Scan images for vulnerabilities** and rebuild regularly to pick up patches.
- **Make images immutable & versioned** (tag releases).

## 6. How is "Docker CI" different from a classic CI pipeline?

A **classic CI pipeline** runs build/test steps directly on the CI agent (or a shared build server) using whatever tools that machine has installed (specific .NET SDK, Node version, etc.). The artifact is typically a binary/zip/package.

A **Docker-based CI pipeline** uses containers as the unit of build, test, and delivery:

| | Classic CI | Docker CI |
|--|-----------|-----------|
| **Build environment** | Tools installed on the agent | Defined in a `Dockerfile` / runs inside containers |
| **Consistency** | Varies by agent ("works on the build server") | Identical everywhere — the image *is* the environment |
| **Artifact** | Binaries / packages | A versioned **container image** pushed to a registry |
| **Dependencies for tests** | Installed/mocked on the agent | Spun up as containers (e.g., a real SQL Server/Redis via Compose/Testcontainers) |
| **Deploy step** | Copy/install binaries onto servers | Pull and run the same image that was tested |
| **Isolation/cleanup** | Shared agent state can leak between builds | Each build is clean and isolated |

Key differences/benefits:

- **Environment parity** – the exact image you tested is the exact image you deploy ("the artifact is the environment"). No drift between CI and prod.
- **Reproducibility & isolation** – builds don't depend on what's pre-installed on the agent; no cross-build contamination.
- **Realistic integration tests** – you can boot real dependencies as containers (this project's `CatalogService.IntegrationTests` + SQL Server is a natural fit, e.g., via Testcontainers).
- **Self-contained agents** – the CI runner only needs Docker, not a dozen language toolchains.

Trade-offs: image build/pull adds time (mitigated by layer caching and registries), and you take on image security/registry management.

> In this repo, a Docker CI flow would be: build the `catalog-service`/`cart-service` images → run unit + integration tests (with dependency containers) → push the tagged images to a registry → deploy that same image to the target environment (Compose or Kubernetes).
