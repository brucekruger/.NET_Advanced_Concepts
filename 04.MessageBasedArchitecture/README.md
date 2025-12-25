# Message-Based Architecture - RabbitMQ Setup

This project uses **RabbitMQ** as the message broker for asynchronous communication between microservices (CartService and CatalogService).

## Prerequisites

- **Docker** installed on your machine
- **Docker Compose** (usually included with Docker Desktop)
- **.NET 9 SDK** (for running the applications)

## Running RabbitMQ with Docker Compose

### 1. Start RabbitMQ Container

Navigate to the project root directory and run:

```bash
docker-compose up -d
```

This command:
- Starts the RabbitMQ container in the background (`-d` flag)
- Creates named volumes for data persistence
- Sets up the microservices network
- Initializes the container with health checks

### 2. Verify RabbitMQ is Running

Check if the container is running:

```bash
docker-compose ps
```

You should see output similar to:

```
NAME                    STATUS
rabbitmq-container      Up (healthy)
```

### 3. Access RabbitMQ Management UI

Open your browser and navigate to:

**URL:** `http://localhost:15672` (use Incognito mode in browser!)

**Credentials:**
- Username: `guest`
- Password: `guest`

### 4. Verify Message Broker Connection

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

## Common Docker Compose Commands

### Stop RabbitMQ
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

### Restart RabbitMQ
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

### Port Already in Use

If port `5672` or `15672` is already in use, modify the `docker-compose.yml`:

```yaml
ports:
  - "5673:5672"     # Use 5673 instead of 5672
  - "15673:15672"   # Use 15673 instead of 15672
```

Then update your `appsettings.json` RabbitMQ settings accordingly.

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

Data survives container restarts unless you use `docker-compose down -v`.

## Next Steps

1. Start RabbitMQ: `docker-compose up -d`
2. Verify it's running: `docker-compose ps`
3. Access Management UI: `http://localhost:15672`
4. Run CartService.Api and CatalogService.Api
5. Monitor messages in the Management UI

---

For more information on RabbitMQ, visit: https://www.rabbitmq.com/
