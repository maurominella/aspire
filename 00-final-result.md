# Final Result — where this guide is heading

> This page previews the end state we reach at the end of the walkthrough. Read it
> first to keep the big picture in mind; every chapter that follows builds one piece
> of it.

At the end of the journey we have **one Aspire solution** that orchestrates four
services written in two languages, running identically in three environments:
locally with `aspire run`, on **local Docker Compose**, and on **Azure Container
Apps (ACA)**.

---

## The final architecture

The AppHost orchestrates:

- **2 .NET projects** — an API service and a Blazor web frontend.
- **1 additional ASP.NET Web API** — a second backend.
- **1 Python project** — a FastAPI/uvicorn service.

The applications communicate with each other through Aspire's service discovery.

![Architecture: a Blazor frontend calling two ASP.NET APIs and one Python FastAPI service, all orchestrated by the Aspire AppHost](_IMAGES/21-architecture-three-services.png)

*The Blazor `webfrontend` is the only externally exposed service. It calls the two
ASP.NET APIs (`apiservice`, `apiservice02`) and the Python API (`pyapi01`)
internally, each through its own typed client.*

| Service | Language / stack | Role | Exposed publicly? |
|---------|------------------|------|-------------------|
| `webfrontend` | .NET / Blazor | Web UI | **Yes** |
| `apiservice` | .NET / ASP.NET Core Minimal API | Backend API | No (internal) |
| `apiservice02` | .NET / ASP.NET Core Web API | Second backend API | No (internal) |
| `pyapi01` | Python / FastAPI + uvicorn | Backend API (containerized) | No (internal) |

---

## Running locally (`aspire run`)

When the solution runs locally, all four services appear in the Aspire dashboard —
each with its endpoints, health checks, logs, container info, dependencies and
environment variables — and the frontend's **Weather** page shows data pulled from
every backend, including the Python one.

![The Aspire dashboard listing all four resources as Running/Healthy](_IMAGES/25-deploy-start-dashboard.png)

![The Weather page rendering forecasts returned by every backend, including the Python FastAPI service](_IMAGES/26-deploy-start-weather.png)

---

## Deployed to local Docker Compose (Step E)

`aspire deploy` generates a `docker-compose.yaml`, builds one image per service and
starts the containers locally. The frontend becomes reachable on a mapped port
(for example `http://localhost:32769`), and a standalone Aspire dashboard container
serves the telemetry.

![The Weather page served from the containerized frontend on local Docker](_IMAGES/31-docker-webfrontend-weather.png)

---

## Deployed to Azure Container Apps (Step F)

`aspire deploy` — this time with the Azure Container Apps environment configured —
provisions a resource group containing a Container Apps Environment with **4
Container Apps** (1 frontend + 3 APIs) and an **Azure Container Registry** holding
one repository per service.

![The Azure resource group created by the deployment](_IMAGES/41-azure-resource-group.png)

![The Container Apps Environment with its four Container Apps](_IMAGES/42-azure-container-apps-environment.png)

![The Azure Container Registry with one repository per service](_IMAGES/43-azure-container-registry.png)

The public frontend ends up on an ACA URL such as:

```
https://webfrontend.ambitiousforest-cd46575a.westeurope.azurecontainerapps.io
```

and the Aspire dashboard on:

```
https://aspire-dashboard.ext.ambitiousforest-cd46575a.westeurope.azurecontainerapps.io
```

---

## The key idea

The same Aspire model — the code in `AppHost.cs` that declares the services and how
they connect — is *translated* by Aspire into whatever the target needs: nothing
for local dev, a `docker-compose.yaml` for Docker, Bicep infrastructure for ACA.
You describe the system once and deploy it many ways.

---

➡️ Ready to build it? Start with the **[Prerequisites](01-prerequisites.md)**.

[⬅ Back to index](README.md)
