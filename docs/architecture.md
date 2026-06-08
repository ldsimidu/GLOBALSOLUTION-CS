# Diagrama do projeto

Este documento apresenta o diagrama de classes da BrightSpot API. A versão principal está exportada em PNG para facilitar a visualização no GitHub e em correções acadêmicas. A fonte em PlantUML também está disponível para manutenção futura.

- Fonte PlantUML: [architecture.puml](architecture.puml)
- Imagem exportada: [architecture.png](architecture.png)

![Diagrama de classes da BrightSpot API](architecture.png)

## Leitura do diagrama

O diagrama foi organizado em duas áreas principais para preservar a legibilidade:

- `Fluxo da API`: mostra os `Controllers`, os serviços de domínio, o acesso a dados com Entity Framework Core, a conexão com Oracle, os DTOs/records, os enums e o middleware de tratamento de exceções.
- `Modelo de domínio e persistência`: mostra as entidades principais, a herança de `BaseEntity`, a abstração `Sensor`, as especializações concretas de sensores e os relacionamentos centrais do agregado `Device`.

## Arquitetura representada

O fluxo principal da aplicação parte dos controllers ASP.NET Core:

- `DevicesController` concentra o CRUD do gadget BrightSpot, configuração operacional, sensores e exclusão do dispositivo.
- `ReadingsController` concentra leituras ambientais, sincronização offline, avaliação de risco e alertas.
- `ExceptionHandlingMiddleware` protege as rotas e converte falhas conhecidas ou inesperadas em respostas controladas.
- `IAlertService` e `IRiskAssessmentService` separam regras de negócio dos controllers.
- `AppDbContext` centraliza os `DbSet` e o mapeamento do Entity Framework Core para persistência Oracle.

## Modelo de domínio

O agregado central é `Device`. A partir dele são organizados:

- `DeviceConfiguration`: configuração de modo de operação, intervalo de coleta e thresholds ambientais.
- `Sensor`: classe abstrata para sensores acoplados ao gadget.
- `TemperatureSensor`, `HumiditySensor`, `LuminositySensor`, `AirQualitySensor` e `VibrationSensor`: especializações concretas de sensores.
- `SensorReading`: leitura ambiental coletada por sensor, com data de coleta, data de recebimento e status de sincronização.
- `Alert`: alerta manual ou automático, opcionalmente associado a uma leitura.
- `RiskAssessment`: avaliação de risco calculada a partir das leituras recentes.
- `SyncLog`: histórico de sincronizações e leituras armazenadas offline.

Algumas relações repetitivas foram simplificadas na imagem para evitar setas sobrepostas. Por exemplo, as entidades persistidas herdam de `BaseEntity`, mas o diagrama mostra essa herança de forma resumida. Da mesma forma, `RiskAssessment` e `SyncLog` pertencem ao `Device` por `DeviceId`, mas aparecem com explicação textual para manter a leitura limpa.

## Legenda

- Linhas contínuas indicam uso, composição ou relacionamento entre classes.
- Linhas tracejadas indicam dependência, herança ou simplificação visual.
- Caixas cinza representam classes, records ou enums.
- DTOs e enums aparecem agrupados como contratos de apoio para evitar cruzamento excessivo de setas.
