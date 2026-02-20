.PHONY: help dev-up dev-down dev-logs dev-build prod-up prod-down prod-logs build push clean test

# Variables
DOCKER_COMPOSE_DEV = docker-compose -f docker-compose.dev.yml
DOCKER_COMPOSE_PROD = docker-compose
PROJECT_NAME = Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker

# Default target
help:
	@echo "🚀 Smart Mechanical Workshop - Audit Log - Makefile Commands"
	@echo ""
	@echo "Development:"
	@echo "  make dev-up          - Start all services in development mode"
	@echo "  make dev-down        - Stop all development services"
	@echo "  make dev-down-v      - Stop and remove volumes (clean database)"
	@echo "  make dev-logs        - Show logs for all development services"
	@echo "  make dev-logs-worker - Show logs only for worker"
	@echo "  make dev-build       - Rebuild worker image"
	@echo "  make dev-restart     - Restart worker service"
	@echo ""
	@echo "Production:"
	@echo "  make prod-up         - Start all services in production mode"
	@echo "  make prod-down       - Stop all production services"
	@echo "  make prod-logs       - Show logs for all production services"
	@echo ""
	@echo "Build & Deploy:"
	@echo "  make build           - Build Docker image"
	@echo "  make push            - Push image to Docker Hub"
	@echo "  make tag             - Tag image with version"
	@echo ""
	@echo "Development (Local):"
	@echo "  make restore         - Restore .NET dependencies"
	@echo "  make compile         - Compile the project"
	@echo "  make run             - Run the worker locally"
	@echo "  make test            - Run tests"
	@echo "  make clean           - Clean build artifacts"
	@echo ""
	@echo "Database:"
	@echo "  make mongo-shell     - Open MongoDB shell"
	@echo "  make mongo-backup    - Backup MongoDB data"
	@echo ""
	@echo "Other:"
	@echo "  make ps              - Show running containers"
	@echo "  make health          - Check services health"

# Development targets
dev-up:
	@echo "🚀 Starting development environment..."
	$(DOCKER_COMPOSE_DEV) up -d
	@echo "✅ Development environment started!"
	@echo "RabbitMQ Management: http://localhost:15672"
	@echo "MongoDB: mongodb://localhost:27017"

dev-down:
	@echo "🛑 Stopping development environment..."
	$(DOCKER_COMPOSE_DEV) down
	@echo "✅ Development environment stopped!"

dev-down-v:
	@echo "🛑 Stopping development environment and removing volumes..."
	$(DOCKER_COMPOSE_DEV) down -v
	@echo "✅ Development environment cleaned!"

dev-logs:
	$(DOCKER_COMPOSE_DEV) logs -f

dev-logs-worker:
	$(DOCKER_COMPOSE_DEV) logs -f audit-log-worker

dev-build:
	@echo "🔨 Building worker image..."
	$(DOCKER_COMPOSE_DEV) build audit-log-worker
	@echo "✅ Worker image built!"

dev-restart:
	@echo "🔄 Restarting worker..."
	$(DOCKER_COMPOSE_DEV) restart audit-log-worker
	@echo "✅ Worker restarted!"

# Production targets
prod-up:
	@echo "🚀 Starting production environment..."
	$(DOCKER_COMPOSE_PROD) up -d
	@echo "✅ Production environment started!"

prod-down:
	@echo "🛑 Stopping production environment..."
	$(DOCKER_COMPOSE_PROD) down
	@echo "✅ Production environment stopped!"

prod-logs:
	$(DOCKER_COMPOSE_PROD) logs -f

# Build and push targets
build:
	@echo "🔨 Building Docker image..."
	@if [ -z "$(VERSION)" ]; then \
		echo "❌ Error: VERSION not specified. Use: make build VERSION=v1.0.0"; \
		exit 1; \
	fi
	@if [ -z "$(DOCKERHUB_USERNAME)" ]; then \
		echo "❌ Error: DOCKERHUB_USERNAME not set in .env"; \
		exit 1; \
	fi
	docker build -t $(DOCKERHUB_USERNAME)/smart-mechanical-workshop-audit-log-worker:$(VERSION) .
	docker tag $(DOCKERHUB_USERNAME)/smart-mechanical-workshop-audit-log-worker:$(VERSION) \
	           $(DOCKERHUB_USERNAME)/smart-mechanical-workshop-audit-log-worker:latest
	@echo "✅ Image built and tagged!"

push:
	@echo "📤 Pushing image to Docker Hub..."
	@if [ -z "$(VERSION)" ]; then \
		echo "❌ Error: VERSION not specified. Use: make push VERSION=v1.0.0"; \
		exit 1; \
	fi
	@if [ -z "$(DOCKERHUB_USERNAME)" ]; then \
		echo "❌ Error: DOCKERHUB_USERNAME not set in .env"; \
		exit 1; \
	fi
	docker push $(DOCKERHUB_USERNAME)/smart-mechanical-workshop-audit-log-worker:$(VERSION)
	docker push $(DOCKERHUB_USERNAME)/smart-mechanical-workshop-audit-log-worker:latest
	@echo "✅ Image pushed!"

# Local development targets
restore:
	@echo "📦 Restoring dependencies..."
	dotnet restore
	@echo "✅ Dependencies restored!"

compile:
	@echo "🔨 Compiling project..."
	dotnet build
	@echo "✅ Project compiled!"

run:
	@echo "🚀 Running worker locally..."
	dotnet run --project src/$(PROJECT_NAME)/$(PROJECT_NAME).csproj

test:
	@echo "🧪 Running tests..."
	dotnet test
	@echo "✅ Tests completed!"

clean:
	@echo "🧹 Cleaning build artifacts..."
	dotnet clean
	rm -rf src/$(PROJECT_NAME)/bin
	rm -rf src/$(PROJECT_NAME)/obj
	@echo "✅ Cleaned!"

# Database targets
mongo-shell:
	@echo "🔌 Connecting to MongoDB..."
	docker exec -it smart-mechanical-workshop-mongodb-dev mongosh -u root -p example --authenticationDatabase admin

mongo-backup:
	@echo "💾 Creating MongoDB backup..."
	@mkdir -p backups
	docker exec smart-mechanical-workshop-mongodb-dev mongodump \
		--username=root \
		--password=example \
		--authenticationDatabase=admin \
		--db=AuditLog \
		--out=/tmp/backup
	docker cp smart-mechanical-workshop-mongodb-dev:/tmp/backup/AuditLog ./backups/AuditLog-$(shell date +%Y%m%d-%H%M%S)
	@echo "✅ Backup created in ./backups/"

# Utility targets
ps:
	@echo "📊 Running containers:"
	$(DOCKER_COMPOSE_DEV) ps

health:
	@echo "🏥 Checking services health..."
	@echo ""
	@echo "Worker:"
	@docker inspect --format='{{.State.Health.Status}}' smart-mechanical-workshop-audit-log-worker-dev 2>/dev/null || echo "Not running"
	@echo ""
	@echo "MongoDB:"
	@docker inspect --format='{{.State.Health.Status}}' smart-mechanical-workshop-mongodb-dev 2>/dev/null || echo "Not running"
	@echo ""
	@echo "RabbitMQ:"
	@docker inspect --format='{{.State.Health.Status}}' smart-mechanical-workshop-rabbitmq-dev 2>/dev/null || echo "Not running"
