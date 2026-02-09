# Message-Based Architecture - RabbitMQ Setup

This project uses **RabbitMQ** as the message broker for asynchronous communication between microservices (CartService and CatalogService). It also includes **PostgreSQL** as the database for **Keycloak** identity server.

## Prerequisites

- **Docker** installed on your machine
- **Docker Compose** (usually included with Docker Desktop)
- **.NET 9 SDK** (for running the applications)

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
2. `appsettings.json` has correct connection settings:
   ```json
   "RabbitMq": {
     "HostName": "localhost",
     "Port": 5672,
     "UserName": "guest",
     "Password": "guest",
     "VirtualHost": "/"
   }
   ```
3. No firewall blocking port `5672`

## Running the Applications

Once RabbitMQ is running, you can start the microservices:

### CartService.Api
```bash
cd CartService.Api
dotnet run
```

### CatalogService.Api
```bash
cd CatalogService.Api
dotnet run
```

Both services will automatically:
- Connect to RabbitMQ
- Create necessary exchanges and queues
- Start consuming messages

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

RabbitMQ container includes health checks that:
- Run every 30 seconds
- Timeout after 10 seconds
- Retry up to 5 times
- Wait 40 seconds before first check (startup grace period)

## Data Persistence

RabbitMQ data is persisted in Docker volumes:
- `rabbitmq_data`: Message queue data
- `rabbitmq_logs`: Service logs
- `postgres_data`: PostgreSQL data
- `keycloak_data`: Keycloak data

Data survives container restarts unless you use `docker-compose down -v`.

## Next Steps

1. Start services: `docker-compose up -d`
2. Verify all services are running: `docker-compose ps`
3. Access RabbitMQ Management UI: `http://localhost:15672`
4. Access Keycloak Admin Console: `http://localhost:8080/admin`
5. Run CartService.Api and CatalogService.Api
6. Monitor messages in the Management UI

---

For more information on RabbitMQ, visit: https://www.rabbitmq.com/

For more information on Keycloak, visit: https://www.keycloak.org/