# BrightSpot API

API em .NET 8 para monitoramento ambiental contextual em ambientes extremos e de dificil acesso. O projeto modela um gadget chamado BrightSpot, capaz de registrar sensores, coletar leituras ambientais, gerar alertas, calcular risco operacional e manter rastreabilidade de sincronizacao offline.

## Integrantes

> Preencher antes da entrega:

- Nome completo - RM000000
- Nome completo - RM000000
- Nome completo - RM000000

## Motivacao e conexao com o tema

Missoes espaciais, simulacoes de colonizacao lunar/marciana e operacoes em areas isoladas dependem de telemetria confiavel para reduzir risco humano. O BrightSpot representa uma API para apoiar esse tipo de operacao: um dispositivo remoto coleta dados de temperatura, umidade, luminosidade, qualidade do ar e vibracao, permitindo que equipes avaliem se um ambiente e seguro para exploracao.

A proposta tambem se conecta aos ODS por apoiar tomada de decisao em ambientes de risco, monitoramento de areas isoladas e prevencao de incidentes em operacoes remotas.

## Tecnologias

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- Oracle Entity Framework Core
- Oracle Database FIAP
- Swagger / Swashbuckle
- Git e GitHub

## Principais recursos

- Cadastro e consulta de gadgets BrightSpot
- Configuracao de modo de operacao e thresholds ambientais
- Cadastro de sensores por dispositivo
- Registro de leituras ambientais com data/hora UTC
- Geracao automatica de alertas por threshold
- Cadastro manual e exclusao de alertas operacionais
- Avaliacao de risco ambiental
- Sincronizacao de leituras coletadas offline
- Logs de sincronizacao
- Exclusao de dispositivos com historico vinculado

## Estrutura do projeto

```text
GlobalSolution.SenseSpot.API/
|-- Controllers/
|   |-- DevicesController.cs
|   `-- ReadingsController.cs
|-- Data/
|   |-- AppDbContext.cs
|   `-- AppDbContextFactory.cs
|-- Middleware/
|   `-- ExceptionHandlingMiddleware.cs
|-- Models/
|   |-- Device.cs
|   |-- Sensor.cs
|   |-- SensorReading.cs
|   |-- Alert.cs
|   |-- RiskAssessment.cs
|   `-- SyncLog.cs
|-- Services/
|   |-- IAlertService.cs
|   |-- AlertService.cs
|   |-- IRiskAssessmentService.cs
|   `-- RiskAssessmentService.cs
|-- Migrations/
|-- docs/
`-- Program.cs
```

## Como executar

1. Configure a connection string do Oracle.

   Opcao recomendada via variavel de ambiente:

   ```powershell
   $env:ConnectionStrings__OracleConnection="User Id=SEU_RM;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL"
   ```

   Tambem e possivel editar localmente o `appsettings.Development.json`, mas nao publique senhas reais no GitHub.

2. Restaure e compile o projeto:

   ```powershell
   dotnet restore
   dotnet build
   ```

3. Execute a API:

   ```powershell
   dotnet run
   ```

4. Abra o Swagger:

   ```text
   https://localhost:7107/swagger
   ```

   A porta pode variar conforme o `Properties/launchSettings.json`.

## Endpoints principais

| Metodo | Rota | Finalidade |
| --- | --- | --- |
| POST | `/api/devices` | Cadastra um gadget |
| GET | `/api/devices` | Lista gadgets |
| GET | `/api/devices/{id}` | Detalha um gadget |
| PATCH | `/api/devices/{id}/status` | Atualiza status operacional |
| PATCH | `/api/devices/{id}/configuration` | Atualiza thresholds e modo |
| POST | `/api/devices/{id}/sensors` | Adiciona sensor |
| GET | `/api/devices/{id}/sensors` | Lista sensores |
| DELETE | `/api/devices/{id}` | Remove gadget e historico vinculado |
| POST | `/api/devices/{deviceId}/readings` | Registra leitura ambiental |
| GET | `/api/devices/{deviceId}/readings` | Lista leituras |
| POST | `/api/devices/{deviceId}/alerts` | Cadastra alerta manual |
| GET | `/api/devices/{deviceId}/alerts` | Lista alertas |
| DELETE | `/api/devices/{deviceId}/alerts/{alertId}` | Remove alerta |
| GET | `/api/devices/{deviceId}/risk-assessment` | Consulta risco mais recente |
| POST | `/api/devices/{deviceId}/sync` | Sincroniza leituras offline |
| GET | `/api/devices/{deviceId}/sync-logs` | Lista logs de sincronizacao |

## POO, abstracao e regras de dominio

- `BaseEntity` concentra propriedades comuns de auditoria.
- `Sensor` e uma classe abstrata para os sensores do gadget.
- `TemperatureSensor`, `HumiditySensor`, `LuminositySensor`, `AirQualitySensor` e `VibrationSensor` especializam sensores com tipo e unidade.
- `IAlertService` e `IRiskAssessmentService` separam as regras de alerta e risco dos controllers.
- `Device.RegisterReading`, `Device.UpdateStatus` e `SensorReading.MarkAsSynced` encapsulam comportamentos de dominio.
- `DateTime.UtcNow`, `RecordedAtUtc`, `ReceivedAtUtc`, `TriggeredAtUtc`, `AssessedAtUtc` e `OccurredAtUtc` registram historico temporal.

## Tratamento de excecoes

A API possui `ExceptionHandlingMiddleware` para responder de forma controlada a:

- `DbUpdateException`: falhas de persistencia no banco
- `FormatException`: dados em formato invalido
- `InvalidOperationException`: operacoes invalidas conhecidas
- `Exception`: falhas inesperadas com log informativo

## Diagramas e evidencias

- Diagrama do projeto: [docs/architecture.md](docs/architecture.md)
- Roteiro de evidencias: [docs/evidencias.md](docs/evidencias.md)

## Pendencias antes da entrega

- Preencher os integrantes com nome completo e RM.
- Confirmar a URL publica do repositorio GitHub.
- Gerar prints ou video curto com Swagger/endpoints funcionando.
- Conferir se a connection string Oracle esta configurada localmente antes da demonstracao.
