# Message-Based Architecture - RabbitMQ Setup

This project uses **RabbitMQ** as the message broker for asynchronous communication between microservices (CartService and CatalogService). It also includes **PostgreSQL** as the database for **Keycloak** identity server.

> **Looking to run everything in containers?** Jump to **[Containerization (Docker Compose)](#containerization-docker-compose)**. That section is the single source of truth for building, running, and testing the full stack (both services + all external dependencies) in Docker. The manual "Keycloak Setup Guide" further below is **only** needed for the legacy local-dev workflow (`dotnet run` on the host) — the containerized stack imports the realm automatically.

---

## Containerization (Docker Compose)

The entire system runs as containers via a single `docker-compose.yml`. Both microservices and **all** their external dependencies are containerized using pre-built images, so no local databases, brokers, or identity servers are required.

### What runs in the stack

| Container | Image | Host URL / Port | Role |
|-----------|-------|-----------------|------|
| `catalog-service` | built from `CatalogService.Api/Dockerfile` | `http://localhost:8080` | Catalog REST API (ASP.NET Core) |
| `cart-service` | built from `CartService.Api/Dockerfile` | `http://localhost:8081` | Cart REST API (ASP.NET Core) |
| `catalog-db` | `mcr.microsoft.com/mssql/server:2022-latest` | `localhost:1433` | SQL Server — Catalog relational store |
| `cart-cache` | `redis:7-alpine` | `localhost:6379` | Redis — Cart cache (NoSQL) |
| `rabbitmq` | `rabbitmq:3.12-management-alpine` | `localhost:5672`, UI `localhost:15672` | Message broker (Catalog → Cart events) |
| `keycloak` | `quay.io/keycloak/keycloak:24.0` | `http://localhost:8090` | Identity provider (OIDC/JWT) |
| `keycloak-db` | `postgres:16-alpine` | internal only | PostgreSQL backing store for Keycloak |

**External dependencies that were containerized:** SQL Server (relational DB), Redis (NoSQL cache), RabbitMQ (message broker), and Keycloak + PostgreSQL (auth). The Cart service additionally keeps an embedded **LiteDB** file persisted in a Docker volume.

> Note: the Cart service uses Redis as a read cache in front of its embedded LiteDB store; both are persisted to volumes.

### Prerequisites

- **Docker** and **Docker Compose v2** (Docker Desktop). Nothing else is required to run the stack — the .NET SDK is only needed for local (non-container) development.

### Build, run, and test

From the project root (`07.Containerization`):

```bash
# Build the API images and start the whole stack
docker compose up -d --build

# Watch until every service reports healthy (first run takes a few minutes:
# SQL Server and Keycloak are slow to initialize)
docker compose ps
```

On first start the stack is self-initializing:

- **Catalog** waits for SQL Server and auto-creates the `CatalogDB` schema on startup (with a retry loop), so no manual migration step is needed.
- **Keycloak** imports the `microservices-realm` from `keycloak/realm-export.json` — clients, roles, and test users are created automatically.
- **Cart** logs a benign `products.changed` queue warning until the Catalog publisher declares the queue; it retries automatically.

### Verify the services

```bash
# Health endpoints (return HTTP 200 "Healthy")
curl http://localhost:8080/health      # Catalog
curl http://localhost:8081/health      # Cart
```

| What | URL |
|------|-----|
| Catalog Swagger UI | `http://localhost:8080/swagger` |
| Catalog health | `http://localhost:8080/health` |
| Cart Swagger UI | `http://localhost:8081/swagger` |
| Cart health | `http://localhost:8081/health` |
| Keycloak admin console | `http://localhost:8090/admin/` |
| RabbitMQ management UI | `http://localhost:15672` |

### Credentials

| Service | Username | Password |
|---------|----------|----------|
| Keycloak admin console | `admin` | `admin123` |
| RabbitMQ management UI | `guest` | `guest` |
| SQL Server (`sa`) | `sa` | `YourStrongPassword123!` |

**Pre-imported Keycloak realm** (`microservices-realm`):

- Clients: `catalog-service` (secret `catalog-service-secret`), `cart-service` (secret `cart-service-secret`)
- Roles: `Manager`, `StoreCustomer`
- Test users: `manager@example.com` / `managers_password` (Manager), `customer@example.com` / `customers_password` (StoreCustomer)

### Authentication notes (important)

The API endpoints are protected with JWT bearer auth. Keycloak's public **issuer is pinned to `http://localhost:8090`**, while the API services fetch signing keys internally from `http://keycloak:8080` (configured via `Authentication__MetadataAddress`). This keeps a single consistent issuer for both browsers/Postman on the host **and** the in-network services.

Get a token from the host and call a protected endpoint:

```bash
# Request an access token (password grant)
curl -X POST "http://localhost:8090/realms/microservices-realm/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=catalog-service&client_secret=catalog-service-secret&grant_type=password&username=manager@example.com&password=managers_password&scope=openid"

# Call the API with the returned access_token
curl http://localhost:8080/api/products -H "Authorization: Bearer <access_token>"
```

Calling a protected endpoint without a token returns `401`; with a valid token it returns `200`. Manager-only operations (POST/PUT/DELETE) require a token for the `manager@example.com` user.

### Configuration (environment variables)

Container configuration is supplied through `docker-compose.yml` environment variables (overriding `appsettings.json`):

- `ConnectionStrings__MSSQL` → `Server=catalog-db;Database=CatalogDB;...`
- `ConnectionStrings__Redis` → `cart-cache:6379`
- `RabbitMq__HostName` → `rabbitmq`
- `Authentication__Authority` / `Authentication__ValidIssuer` → `http://localhost:8090/realms/microservices-realm`
- `Authentication__MetadataAddress` → `http://keycloak:8080/realms/microservices-realm/.well-known/openid-configuration`

### Data persistence (volumes)

`catalog-db-data`, `catalog-db-logs`, `cart-cache-data`, `cart-data` (LiteDB), `rabbitmq-data`, `keycloak-db-data`. Data survives restarts; use `docker compose down -v` to wipe it.

### Common commands

```bash
docker compose up -d --build      # build images + start
docker compose ps                 # status / health
docker compose logs -f cart-service
docker compose restart catalog-service
docker compose down               # stop (keep data)
docker compose down -v            # stop + remove volumes
```

### Troubleshooting

- **`catalog-db` shows `unhealthy`** — cosmetic. It is only the SQL Server container's own health-check command; the database itself works (confirmed by `/api/products` returning 200).
- **Cart logs `no queue 'products.changed'`** — benign startup ordering; the consumer retries until the Catalog publisher declares the queue.
- **`/swagger` returns 404 or a port is refused** — make sure you rebuilt with `--build`; a plain restart keeps old images.
- **Keycloak admin console hangs / empty response** — use `http://localhost:8090/admin/` (not the bare root) and an incognito tab to avoid cached redirects.
- **`401` on API calls** — tokens minted before the issuer was set to `localhost:8090` are rejected; request a fresh token.

---

## Prerequisites

- **Docker** installed on your machine
- **Docker Compose** (usually included with Docker Desktop)
- **.NET 10 SDK** (for running the applications)
- **Postman** (for testing API endpoints with authentication)

## Running Services with Docker Compose

### 1. Start Services

Navigate to the project root directory and run:

```bash
docker-compose up -d
```

This command:
- Starts all containers (RabbitMQ, PostgreSQL, Keycloak) in the background (`-d` flag)
- Creates named volumes for data persistence
- Sets up the microservices network
- Initializes the containers with health checks

### 2. Verify Services are Running

Check if the containers are running:

```bash
docker-compose ps
```

You should see output indicating all containers are "Up" and "healthy".

### 3. Access RabbitMQ Management UI

Open your browser and navigate to:

**URL:** `http://localhost:15672` (use Incognito mode in browser!)

**Credentials:**
- Username: `guest`
- Password: `guest`

### 4. Access Keycloak Admin Console

Open your browser and navigate to:

**URL:** `http://localhost:8080/admin` (use Incognito mode in browser!)

**Credentials:**
- Username: `admin`
- Password: `admin123`

### 5. Verify Message Broker Connection

The applications are pre-configured to connect to RabbitMQ. When you run CartService.Api or CatalogService.Api:

- Connection: `localhost:5672`
- Username: `guest`
- Password: `guest`
- Virtual Host: `/`

## Service Configuration

| Component | Port | Purpose |
|-----------|------|---------|
| RabbitMQ AMQP | `5672` | Message broker communication |
| RabbitMQ Management UI | `15672` | Web-based queue and exchange management |
| Keycloak | `8080` | Identity and Access Management |
| PostgreSQL | `5432` | Relational Database Service |

---

## 🔐 Keycloak Setup Guide

### Step 1: Create Realm

1. Navigate to **Keycloak Admin Console**: `http://localhost:8080/admin`
2. Login with credentials: `admin` / `admin123`
3. Click the **realm dropdown** (top-left, currently says "Master")
4. Click **"Create Realm"**
5. **Realm name:** `microservices-realm`
6. Click **"Create"**

### Step 2: Create Realm Roles

1. Go to **Realm roles** (left sidebar under "Manage")
2. Click **"Create role"**
3. Create first role:
   - **Name:** `Manager`
   - Click **"Save"**
4. Create second role:
   - **Name:** `StoreCustomer`
   - Click **"Save"**

### Step 3: Create Clients

#### CatalogService Client

1. Go to **Clients** (left sidebar)
2. Click **"Create client"**
3. **Client ID:** `catalog-service`
4. **Client type:** OpenID Connect
5. Click **"Next"**
6. **Capability config:**
   - Enable: **Client authentication** ✅
   - Enable: **Service accounts roles** ✅
7. Click **"Save"**
8. Go to **Settings** tab:
   - **Valid Redirect URIs:** `http://localhost:5063/*`
   - **Web Origins:** `http://localhost:5063`
   - Click **"Save"**
9. Go to **Credentials** tab:
   - **Copy the Client Secret** (you'll need this for Postman)

#### CartService Client

Repeat the same process with:
- **Client ID:** `cart-service`
- **Valid Redirect URIs:** `http://localhost:5064/*`
- **Web Origins:** `http://localhost:5064`
- Copy the Client Secret

### Step 4: Create Test Users

#### Manager User

1. Go to **Users** (left sidebar)
2. Click **"Add user"**
3. **Username:** `manager@example.com`
4. **Email:** `manager@example.com`
5. **First Name:** Manager
6. **Last Name:** User
7. **Email Verified:** ON ✅
8. **Enabled:** ON ✅
9. Click **"Create"**
10. Go to **Credentials** tab:
    - Click **"Set password"**
    - **Password:** `managers_password`
    - **Temporary:** OFF
    - Click **"Set Password"**
11. Go to **Role Mapping** tab:
    - Click **"Assign role"**
    - Select **"Manager"**
    - Click **"Assign"**

#### Customer User

Repeat with:
- **Username:** `customer@example.com`
- **Email:** `customer@example.com`
- **Password:** `customers_password`
- **Role:** `StoreCustomer`

### Step 5: Configure Advanced Client Settings

For each client (catalog-service and cart-service):

1. Go to **Clients** → **{client-name}**
2. **Advanced** tab
3. Find **Authentication flow overrides**
4. Enable **"Direct access grants"** ✅
5. Click **"Save"**

---

## 📮 Using Postman Collection

### Step 1: Import Postman Collection

1. Open **Postman**
2. Click **"File"** → **"Import"**
3. Select the file: `Catalog Service.postman_collection.json`
4. Click **"Import"**

### Step 2: Configure Environment Variables

The collection uses variables that need to be set up:

1. In Postman, create a **New Environment**
2. Name it: `Keycloak Auth`
3. Add these variables:

| Variable | Initial Value | Example Value |
|----------|---|---|
| `client_id_catalog_service` | (empty) | `catalog-service` |
| `client_secret_catalog_service` | (empty) | `{copy from Keycloak Credentials tab}` |
| `username_catalog_service` | (empty) | `manager@example.com` |
| `password_catalog_service` | (empty) | `Manager@123` |
| `client_id_cart_service` | (empty) | `cart-service` |
| `client_secret_cart_service` | (empty) | `{copy from Keycloak Credentials tab}` |
| `username_cart_service` | (empty) | `manager@example.com` |
| `password_cart_service` | (empty) | `Manager@123` |
| `access_token_catalog_service` | (empty) | (auto-filled after login) |
| `refresh_token_catalog_service` | (empty) | (auto-filled after login) |
| `access_token_cart_service` | (empty) | (auto-filled after login) |
| `refresh_token_cart_service` | (empty) | (auto-filled after login) |

4. Click **"Save"** and select this environment as active

### Step 3: Get Access Tokens

Before making API calls, you must get access tokens from Keycloak.

#### For CatalogService:

1. In the collection, find **"Get Token CatalogService API"** request
2. Click **"Send"**
3. **Expected Response:** `200 OK` with `access_token` and `refresh_token`
   - The token is **automatically saved** to your environment variables
   - You'll see in the response console:
     ```
     Access Token saved: eyJhbGciOiJSUzI1NiIsInR5cCI...
     Refresh Token saved: eyJhbGciOiJIUzI1NiIsInR5cCI...
     Token expires in: 300 seconds
     ```

#### For CartService:

1. In the collection, find **"Get Token CartService API"** request
2. Click **"Send"**
3. Same process as above

### Step 4: Test API Endpoints

Now you can test the API endpoints with the saved tokens.

#### Example: Get Categories (CatalogService)

1. Find the request: **"Get Categories"**
2. Click **"Send"**
3. **Expected Response:** `200 OK` with list of categories
   - The request automatically includes the `access_token_catalog_service` in the Authorization header

#### Example: Delete Category (Manager Only)

1. Find the request: **"Delete Category"**
2. Click **"Send"**
3. **Expected Responses:**
   - If using **Manager token** → `204 No Content` ✅
   - If using **Customer token** → `403 Forbidden` ❌

---

## 🧪 Complete Testing Workflow

### Scenario 1: Manager User (Full Access)

**Setup:**
1. Set `username_catalog_service` = `manager@example.com`
2. Set `password_catalog_service` = `Manager@123`
3. Run **"Get Token CatalogService API"**

**Tests:**
```
✅ GET /api/products                    → 200 OK
✅ GET /api/products/{id}               → 200 OK
✅ POST /api/products                   → 201 Created
✅ PUT /api/products/{id}               → 200 OK
✅ DELETE /api/products/{id}            → 204 No Content

✅ GET /api/categories                  → 200 OK
✅ GET /api/categories/{id}             → 200 OK
✅ POST /api/categories                 → 201 Created
✅ PUT /api/categories/{id}             → 200 OK
✅ DELETE /api/categories/{id}          → 204 No Content
```

### Scenario 2: Customer User (Read-Only)

**Setup:**
1. Set `username_catalog_service` = `customer@example.com`
2. Set `password_catalog_service` = `Customer@123`
3. Run **"Get Token CatalogService API"**

**Tests:**
```
✅ GET /api/products                    → 200 OK
✅ GET /api/products/{id}               → 200 OK
❌ POST /api/products                   → 403 Forbidden
❌ PUT /api/products/{id}               → 403 Forbidden
❌ DELETE /api/products/{id}            → 403 Forbidden

✅ GET /api/categories                  → 200 OK
✅ GET /api/categories/{id}             → 200 OK
❌ POST /api/categories                 → 403 Forbidden
❌ PUT /api/categories/{id}             → 403 Forbidden
❌ DELETE /api/categories/{id}          → 403 Forbidden
```

### Scenario 3: Refresh Token

When access token expires (5 minutes):

1. Find request: **"Refresh CatalogService API"** or **"Refresh Token CartService API"**
2. Click **"Send"**
3. New tokens are automatically saved to environment
4. Continue testing with fresh tokens

---

## 🔑 Token Information

### Access Token
- **Lifetime:** 300 seconds (5 minutes)
- **Contains:** User claims, roles, permissions
- **Usage:** Include in `Authorization: Bearer {token}` header for API calls

### Refresh Token
- **Lifetime:** 1800 seconds (30 minutes)
- **Purpose:** Obtain a new access token when it expires
- **Usage:** Use in refresh token grant flow

### JWT Token Inspector
To view token details:
1. Copy the `access_token` value
2. Go to: `https://jwt.io`
3. Paste the token
4. View the decoded claims including:
   - User ID (`sub`)
   - Username (`preferred_username`)
   - Roles (`realm_access.roles`)
   - Expiration (`exp`)

---

## 🛠️ Code Quality & Style Setup

This project enforces consistent code style and quality standards across all team members using industry-leading tools.

### EditorConfig

The `.editorconfig` file at the solution root defines formatting rules applied automatically by Visual Studio:

- **Indentation:** 4 spaces (no tabs)
- **Line endings:** CRLF
- **Charset:** UTF-8
- **Sorted using statements:** System directives first
- **Naming conventions:** PascalCase, camelCase, _camelCase patterns
- **Code style preferences:** Nullable reference types, pattern matching, etc.

**These rules apply automatically** when you open the solution in Visual Studio.

### dotnet-format Tool

Validate and fix code formatting from the command line:

#### Installation

# Install globally (one-time setup)
dotnet tool install -g dotnet-format

#### Verify Formatting (without changes)

# Check if code matches formatting rules
dotnet format --verify-no-changes --verbosity diagnostic

#### Apply Formatting Fixes

# Automatically fix formatting issues
dotnet format

#### Usage in CI/CD

# Fail build if formatting violations exist
dotnet format --verify-no-changes

### Pre-Push Git Hook (Optional)

A Git pre-push hook can automatically validate code style before pushing.

#### Setup

1. Create `.githooks/pre-push` file at solution root
2. Configure Git to use the hooks directory:

git config core.hooksPath .githooks

#### What It Does

When you run `git push`, the hook:
- Runs `dotnet format --verify-no-changes`
- **Prevents push** if formatting violations exist
- Provides clear error messages

#### Override Hook (if needed)

# Push without running the pre-push hook (not recommended)
git push --no-verify

### Analyzer Rules

The `.editorconfig` enables **3+ additional analyzer checks** beyond defaults:

| Rule ID | Check | Severity |
|---------|-------|----------|
| `dotnet_sort_system_directives_first` | Sort using statements (System first) | Built-in |
| **CA2249** | Use `String.Contains` instead of `String.IndexOf` | Suggestion |
| **CA1849** | Call async methods when in async method | Suggestion |
| **CA1826** | Use property instead of Linq Enumerable method | Suggestion |

These appear as **warnings/suggestions** in Visual Studio's Error List during build.

---

## 📊 SonarQube Code Quality Analysis

SonarQube provides comprehensive code quality metrics, security vulnerabilities, and code smells detection.

### Prerequisites

- **Docker** installed on your machine
- **SonarQube Community Edition** (or higher)

### Installation & Setup

#### Step 1: Start SonarQube with Docker

# Pull and run SonarQube Community Edition
docker run -d --name sonarqube -p 9000:9000 sonarqube:latest

# Wait for startup (1-2 minutes)
docker logs -f sonarqube

#### Step 2: Access SonarQube Dashboard

Open browser and navigate to:

http://localhost:9000

**Default Credentials:**
- Username: `admin`
- Password: `admin`

**Change password on first login** for security.

#### Step 3: Create Project

1. Click **Projects** → **Create Project**
2. **Project Key:** `dotnet-advanced-concepts`
3. **Display Name:** `.NET Advanced Concepts`
4. Click **Create**

#### Step 4: Generate Authentication Token

1. Click your **profile icon** (top-right) → **My Account** → **Security**
2. Click **Generate Tokens**
3. **Name:** `dotnet-analysis`
4. Click **Generate** and **copy the token** (you'll need it for analysis)

#### Step 5: Install SonarScanner

# Install globally
dotnet tool install -g dotnet-sonarscanner

### Running Code Analysis

#### Initial Analysis

Navigate to your solution root and run:

# Begin analysis session
dotnet sonarscanner begin \
  /k:"dotnet-advanced-concepts" \
  /d:sonar.host.url="http://localhost:9000" \
  /d:sonar.login="YOUR_SONARQUBE_TOKEN"

# Build your solution
dotnet build

# End analysis and publish results
dotnet sonarscanner end \
  /d:sonar.login="YOUR_SONARQUBE_TOKEN"

**Windows PowerShell:**
$token = "YOUR_SONARQUBE_TOKEN"

dotnet sonarscanner begin `
  /k:"dotnet-advanced-concepts" `
  /d:sonar.host.url="http://localhost:9000" `
  /d:sonar.login="$token"

dotnet build

dotnet sonarscanner end /d:sonar.login="$token"

#### Re-run Analysis After Fixes

After fixing code issues, run the same commands to get updated metrics:

dotnet sonarscanner begin /k:"dotnet-advanced-concepts" /d:sonar.host.url="http://localhost:9000" /d:sonar.login="YOUR_TOKEN"
dotnet build
dotnet sonarscanner end /d:sonar.login="YOUR_TOKEN"

### Viewing Results

#### Dashboard

After analysis completes, view results at:

http://localhost:9000/dashboard?id=dotnet-advanced-concepts

Shows:
- ✅ **Quality Gate** - Pass/Fail status
- 📊 **Metrics** - Complexity, duplications, code coverage
- 🔒 **Security** - Vulnerabilities and security hotspots
- 🐛 **Reliability** - Bugs and blockers
- 🧹 **Maintainability** - Code smells and technical debt

#### Issues

Review code issues:

1. Go to **Issues** section
2. **Filter by Severity:**
   - **Blocker** - Must fix immediately
   - **Critical** - Should fix soon
   - **Major** - Fix before release
   - **Minor** - Nice to fix
   - **Info** - FYI

3. Click any issue to see:
   - **Location** - File and line number
   - **Rule** - What the rule checks
   - **Explanation** - Why it's an issue
   - **Solution** - How to fix it

### Example Fixes

#### Issue: Hardcoded Credentials

**Before:**
```csharp
var connection = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "guest",
    Password = "guest"  // ❌ Hardcoded credential
};
```

**After:**
```csharp
var settings = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>();
var connection = new ConnectionFactory
{
    HostName = settings.HostName,
    UserName = settings.UserName,
    Password = settings.Password
};
```

#### Issue: Missing Null Check

**Before:**
```csharp
public void ProcessOrder(Order order)
{
    var total = order.Items.Sum(x => x.Price);  // ❌ NullReferenceException if order is null
}
```

**After:**
```csharp
public void ProcessOrder(Order? order)
{
    if (order is null)
        throw new ArgumentNullException(nameof(order));
        
    var total = order.Items.Sum(x => x.Price);
}
```

### Continuous Integration

#### GitHub Actions Workflow

Create `.github/workflows/sonarqube-analysis.yml`:

name: SonarQube Analysis

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  sonarqube:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Set up .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Restore
        run: dotnet restore

      - name: Install SonarScanner
        run: dotnet tool install -g dotnet-sonarscanner

      - name: Begin SonarQube Analysis
        run: |
          dotnet sonarscanner begin \
            /k:"dotnet-advanced-concepts" \
            /d:sonar.host.url="${{ secrets.SONARQUBE_HOST }}" \
            /d:sonar.login="${{ secrets.SONARQUBE_TOKEN }}"

      - name: Build
        run: dotnet build --no-restore

      - name: End SonarQube Analysis
        run: dotnet sonarscanner end /d:sonar.login="${{ secrets.SONARQUBE_TOKEN }}"

Add GitHub Secrets:
- `SONARQUBE_HOST`: `http://localhost:9000` (or your SonarQube URL)
- `SONARQUBE_TOKEN`: Your generated token

### Ignoring SonarQube Files

Add to `.gitignore`:
# SonarQube analysis
.sonarqube/
.sonartmp/
.sonar/

### References

- [SonarQube Documentation](https://docs.sonarqube.org/)
- [SonarScanner for .NET](https://docs.sonarqube.org/latest/analysis/scan/sonarscanner-for-dotnet/)
- [EditorConfig Documentation](https://editorconfig.org/)
- [dotnet-format GitHub](https://github.com/dotnet/format)

---

## Common Docker Compose Commands

### Stop Services
```bash
docker-compose down
```

### Stop and Remove All Data
```bash
docker-compose down -v
```

### View Live Logs
```bash
docker-compose logs -f rabbitmq
docker-compose logs -f keycloak
```

### Restart Services
```bash
docker-compose restart
```

### Update to Latest Image
```bash
docker-compose pull
docker-compose up -d
```

### Check Container Status
```bash
docker-compose ps
```

## Troubleshooting

### RabbitMQ Container Won't Start

**Check logs:**
```bash
docker-compose logs rabbitmq
```

**Force clean restart:**
```bash
docker-compose down -v
docker-compose up -d
```

### Keycloak Container Won't Start

**Check logs:**
```bash
docker-compose logs keycloak
```

**Ensure PostgreSQL is running first:**
```bash
docker-compose ps postgres
```

**Wait for startup:** Keycloak can take 40+ seconds to start. Check logs regularly.

### Port Already in Use

If port `5672` or `15672` is already in use, modify the `docker-compose.yml`:

```yaml
ports:
  - "5673:5672"     # Use 5673 instead of 5672
  - "15673:15672"   # Use 15673 instead of 15672
```

If port `8080` is already in use:

```yaml
ports:
  - "8081:8080"     # Use 8081 instead of 8080
```

Then update your `appsettings.json` settings accordingly.

### Can't Connect from Application

Ensure:
1. RabbitMQ container is running: `docker-compose ps`
2. Keycloak container is running and healthy: `docker-compose ps`
3. `appsettings.json` has correct connection settings:
   ```json
   "RabbitMq": {
     "HostName": "localhost",
     "Port": 5672,
     "UserName": "guest",
     "Password": "guest",
     "VirtualHost": "/"
   },
   "Authentication": {
     "Authority": "http://localhost:8080/realms/microservices-realm",
     "Audience": "catalog-service",
     "ValidIssuer": "http://localhost:8080/realms/microservices-realm"
   }
   ```
4. No firewall blocking ports `5672`, `8080`, or `15672`

### Getting 403 Forbidden on Protected Endpoints

If you get 403 when trying to delete/update endpoints as a Manager:

1. **Verify token contains roles:**
   - Copy `access_token` from Postman response
   - Paste at `https://jwt.io`
   - Look for `realm_access.roles` containing `Manager`

2. **Verify user has Manager role:**
   - Keycloak Admin → Users → manager@example.com
   - Role Mapping tab
   - Verify `Manager` role is assigned

3. **Verify Direct Access Grants enabled:**
   - Keycloak Admin → Clients → {client-name}
   - Advanced tab
   - `Direct access grants` must be **enabled**

4. **Check application logs:**
   - Look for messages like: `Added role from realm_access: Manager`
   - This confirms roles are being extracted

---

## Running the Applications

Once RabbitMQ and Keycloak are running, you can start the microservices:

### CatalogService.Api
```bash
cd CatalogService.Api
dotnet run
```

Access Swagger UI: `http://localhost:5063`

### CartService.Api
```bash
cd CartService.Api
dotnet run
```

Access Swagger UI: `http://localhost:5064`

Both services will automatically:
- Connect to RabbitMQ
- Create necessary exchanges and queues
- Start consuming messages
- Validate JWT tokens from Keycloak

## Messaging Architecture

### Exchanges and Queues

| Component | Name | Type | Purpose |
|-----------|------|------|---------|
| Exchange | `catalog.products` | Direct | Product events from CatalogService |
| Exchange | `catalog.products.dlx` | Direct | Dead Letter Exchange (DLQ) |
| Queue | `products.changed` | - | Receives product change events |
| Queue | `products.changed.dlq` | - | Handles failed message delivery |

### Message Flow

1. **CatalogService** publishes `ProductChangedEvent` to `catalog.products` exchange
2. Message is routed to `products.changed` queue
3. **CartService** consumes the message and updates cart items
4. On failure, message is sent to `products.changed.dlq` (Dead Letter Queue)

## Health Checks

Container health checks:
- **RabbitMQ:** Runs every 30 seconds, timeout 10 seconds, max 5 retries
- **Keycloak:** Runs every 10 seconds, timeout 5 seconds, max 5 retries
- **PostgreSQL:** Runs every 10 seconds, timeout 5 seconds, max 5 retries

Startup grace periods:
- **RabbitMQ:** 40 seconds before first check
- **Keycloak:** 40 seconds before first check
- **PostgreSQL:** 5 seconds before first check

## Data Persistence

Data is persisted in Docker volumes:
- `rabbitmq_data`: Message queue data
- `rabbitmq_logs`: Service logs
- `postgres_data`: PostgreSQL database and Keycloak data
- `keycloak_data`: Keycloak configuration and themes

Data survives container restarts unless you use `docker-compose down -v`.

## Next Steps

1. Start services: `docker-compose up -d`
2. Verify all services are running: `docker-compose ps`
3. Complete **Keycloak Setup Guide** (see above)
4. Configure **Postman Collection** (see above)
5. Run CartService.Api and CatalogService.Api
6. Test using Postman collection
7. Monitor messages in RabbitMQ: `http://localhost:15672`
8. View application logs for debugging

---

## Additional Resources

- **RabbitMQ Documentation:** https://www.rabbitmq.com/
- **Keycloak Documentation:** https://www.keycloak.org/
- **OAuth2/OIDC Standards:** https://oauth.net/2/
- **JWT Tokens:** https://jwt.io/
- **Postman Documentation:** https://learning.postman.com/