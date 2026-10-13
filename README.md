# SMS Dispatch

A full-stack SMS dispatch system built with Angular, ASP.NET Core, and Azure Functions.

## Projects

| Project | Tech | Description |
|---|---|---|
| `sms-dispatch-ui` | Angular | Single-page web app — compose messages, view status |
| `SmsDispatch.Api` | ASP.NET Core (.NET 10) | REST API — MessagesController, EF Core, Azure Service Bus |
| `SmsDispatch.Functions` | Azure Functions (isolated worker) | SmsPoller (timer), SmsSender (SB trigger), DeadLetterHandler (DLQ) |

## Architecture

```
Angular SPA → Azure WAF → Azure API Management → ASP.NET Core API
                                                        ↓
                                                  Azure SQL (Pending)
                                                        ↓
                                             SmsPoller (every 60s)
                                                        ↓
                                             Azure Service Bus Topic
                                                        ↓
                                              SmsSender Function
                                                        ↓
                                           3rd-Party SMS API → Sent
```

## Message Status Flow

`Pending` → `Queued` → `Sending` → `Sent`  
`Sending` → `Failed` → `DeadLettered` (after 5 retries)

## Azure Services Used

- **Azure App Service** — hosts Angular SPA and ASP.NET Core API
- **Azure Service Bus** — topic `sms-dispatch` with subscription `send-processor`
- **Azure Functions** — serverless dispatch pipeline (Consumption plan)
- **Azure SQL** — Messages table with EF Core code-first migrations
- **Azure Storage** — required by Azure Functions runtime
- **Azure Application Insights** — centralised telemetry

> ⚠️ Azure WAF and Azure API Management are best-practice components shown in the architecture. They are not active in the free-tier demo environment.

## Local Development Setup

### Prerequisites
- .NET 10 SDK
- Node.js 24+
- Angular CLI (`npm install -g @angular/cli`)
- Azure Functions Core Tools
- Azure Storage Emulator (Azurite) or Azure Storage account

### 1. API (`SmsDispatch.Api`)

Copy the template and fill in your values:

```bash
cp SmsDispatch.Api/appsettings.json SmsDispatch.Api/appsettings.Development.json
```

Edit `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "SmsDb": "<your Azure SQL or localdb connection string>"
  },
  "AzureServiceBus": {
    "ConnectionString": "<your Service Bus connection string>",
    "TopicName": "sms-dispatch"
  }
}
```

```bash
cd SmsDispatch.Api
dotnet ef database update
dotnet run
```

### 2. Functions (`SmsDispatch.Functions`)

Create `local.settings.json` (git-ignored):
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceBusConnection": "<your Service Bus connection string>",
    "SmsDb": "<your connection string>",
    "SmsApiBaseUrl": "<your SMS API URL>",
    "SmsApiKey": "<your SMS API key>",
    "AzureServiceBus:TopicName": "sms-dispatch"
  }
}
```

```bash
cd SmsDispatch.Functions
func start
```

### 3. Angular UI (`sms-dispatch-ui`)

```bash
cd sms-dispatch-ui
npm install
ng serve
```

App runs at `http://localhost:4200`

## Security Notes

- `appsettings.Development.json` and `local.settings.json` are git-ignored — never commit real credentials
- In production, all secrets are injected via Azure App Service / Function App Settings
- See `.gitignore` at the repo root
