# Roteiro de evidencias de execucao

Use este arquivo como checklist para gerar os prints ou um video curto da API rodando.

## Prints recomendados

1. API executando no terminal com `dotnet run`.
2. Swagger aberto em `/swagger`.
3. `POST /api/devices` criando um gadget BrightSpot.
4. `GET /api/devices` listando o gadget criado.
5. `PATCH /api/devices/{id}/configuration` ajustando thresholds.
6. `POST /api/devices/{id}/sensors` cadastrando pelo menos dois sensores.
7. `POST /api/devices/{deviceId}/readings` registrando uma leitura.
8. `GET /api/devices/{deviceId}/risk-assessment` mostrando a avaliacao de risco.
9. `POST /api/devices/{deviceId}/alerts` cadastrando um alerta manual.
10. `GET /api/devices/{deviceId}/alerts` listando alertas.
11. `POST /api/devices/{deviceId}/sync` demonstrando sincronizacao.
12. `DELETE /api/devices/{deviceId}/alerts/{alertId}` ou `DELETE /api/devices/{id}` demonstrando remocao.

## Payloads de exemplo

### Criar gadget

```json
{
  "name": "BrightSpot Ares-01",
  "serialNumber": "BS-ARES-001",
  "environmentContext": "Mars habitat simulation tunnel",
  "batteryLevel": 92,
  "connectionStatus": 1,
  "operationMode": 1,
  "collectionIntervalSeconds": 60
}
```

### Adicionar sensor

```json
{
  "name": "External temperature probe",
  "sensorType": 1
}
```

### Registrar leitura

```json
{
  "sensorId": 1,
  "value": 38.5,
  "recordedAtUtc": "2026-06-07T18:30:00Z",
  "connectionStatusAtCollection": 1
}
```

### Cadastrar alerta manual

```json
{
  "severity": 3,
  "message": "Preventive retreat recommended after unstable environmental readings.",
  "triggeredAtUtc": "2026-06-07T18:35:00Z",
  "sensorReadingId": null
}
```

## Observacoes para a apresentacao

- Mostre que a API continua aberta mesmo se a migration inicial nao conseguir aplicar por indisponibilidade do Oracle.
- Explique que a senha Oracle deve ser configurada localmente e nao publicada no GitHub.
- Mostre no Swagger os endpoints de POST, GET, PATCH e DELETE para evidenciar o CRUD.
