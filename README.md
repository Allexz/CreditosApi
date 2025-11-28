# CreditosApi

## Tecnologias Utilizadas

Este projeto utiliza as seguintes tecnologias:

- **.NET 10.0**: Framework principal para desenvolvimento da aplicação.
- **ASP.NET Core**: Framework para construção de APIs web.
- **Entity Framework Core**: ORM para acesso a dados, com PostgreSQL como banco de dados.
- **Confluent Kafka**: Biblioteca para integração com Apache Kafka.
- **Quartz**: Biblioteca para agendamento de tarefas em background.
- **Serilog**: Biblioteca para logging estruturado.
- **Swashbuckle (Swagger)**: Para geração de documentação da API.
- **Docker**: Para containerização e orquestração dos serviços.
- **PostgreSQL**: Sistema de gerenciamento de banco de dados relacional.
- **Apache Kafka**: Plataforma de streaming de eventos.
- **Kafka-UI**: Interface web para monitoramento do Kafka.

## Pontos de Ajuste  
1 . **Configurações de Conexão**: Ajuste as strings de conexão para o banco de dados PostgreSQL e o servidor Kafka no arquivo `appsettings.json` ou nas variáveis de ambiente conforme necessário.  
2 . **Portas de Serviço**: Verifique se as portas utilizadas (8080 para a API, 8081 para Kafka-UI, 5432 para PostgreSQL) estão disponíveis no seu ambiente local ou ajuste conforme necessário.  
3.  **Regras de Negócio**: A propriedade Data da Constituição (DataConstituicao) precisa estar no futuro.  

## Caminhos para Acesso aos Recursos

### API
- **Local (desenvolvimento)**: http://localhost:5105
- **Docker**: http://localhost:8080

### Kafka-UI
- **Acesso**: http://localhost:8081

### Banco de Dados (PostgreSQL)
- **Host**: localhost
- **Porta**: 5432
- **Banco de Dados**: CreditosDb
- **Usuário**: postgres
- **Senha**: senha123

Para executar o projeto, utilize o Docker Compose:

```bash
docker-compose up --build
```

A documentação da API está disponível via Swagger em `/swagger` quando a aplicação estiver rodando.
