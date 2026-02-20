# Docker Configuration Files

Este diretório contém arquivos de configuração para os serviços Docker.

## 📁 Estrutura

```
docker/
└── rabbitmq/
    ├── definitions.json    # Definições de exchanges, queues e bindings
    └── rabbitmq.conf      # Configuração do RabbitMQ
```

## 🐰 RabbitMQ

### definitions.json

Este arquivo define a estrutura inicial do RabbitMQ quando o container é iniciado:

- **Queue**: `database.events` (durável)
- **Exchange**: `database.events.exchange` (tipo: topic, durável)
- **Binding**: Exchange → Queue com routing key `#` (todas as mensagens)

### rabbitmq.conf

Configurações básicas do RabbitMQ:
- Permite acesso remoto do usuário guest
- Define portas padrão (5672 para AMQP, 15672 para Management UI)
- Carrega definições automaticamente na inicialização

## 🔧 Modificando Configurações

Se você precisar alterar as configurações:

1. **Para adicionar novas queues/exchanges**: Edite `definitions.json`
2. **Para alterar configurações do servidor**: Edite `rabbitmq.conf`
3. **Após modificar**: Reinicie o container RabbitMQ

```bash
docker-compose restart rabbitmq
```

## 📖 Referência

- [RabbitMQ Configuration](https://www.rabbitmq.com/configure.html)
- [RabbitMQ Definitions](https://www.rabbitmq.com/definitions.html)
