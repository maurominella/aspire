# Microsoft Aspire — Quickstart & End‑to‑End Walkthrough

A detailed, chapter‑by‑chapter guide that takes you from an empty machine to a
multi‑language distributed application built with **Microsoft Aspire**, running
locally and deployed to the cloud.

The solution we build combines **.NET** and **Python** services orchestrated by a
single Aspire *AppHost*, and we take it all the way through two deployment
targets: a **local Docker Compose** environment and **Azure Container Apps (ACA)**.

> This documentation was written up from hands‑on notes taken while completing the
> full journey end to end. Every step shown here was actually executed, and the
> screenshots come from those real runs.

---

## What you will build

By the end of this guide you will have a working Aspire solution made of:

- **A Blazor web frontend** (`webfrontend`) — the only externally exposed service.
- **Two ASP.NET Core Web APIs** (`apiservice`, `apiservice02`) — internal backends.
- **A Python FastAPI service** (`pyapi01`) — packaged as a container and orchestrated
  by Aspire exactly like the .NET services.

All four services are wired together with Aspire's **service discovery**, **health
checks**, **resilience** and **observability**, then published to Docker and to Azure.

Before diving into the steps, jump straight to the **[Final Result](00-final-result.md)**
to see where we are heading.

---

## The journey (A → F)

This is the exact path the guide follows:

| Step | What we do | Chapter |
|------|------------|---------|
| **A** | Create the Aspire solution from a template that already includes **Frontend + Backend + Test** | [Create a new Aspire application](03-create-a-new-aspire-application.md) |
| **B** | Add a second **ASP.NET Web API** to the solution | [Add an ASP.NET Web API](04-add-an-aspnet-web-api.md) |
| **C** | Create a **Python FastAPI** app, test it locally, then run it (standalone, for testing) in a **Docker container** | [Python FastAPI](05-python-fastapi.md) |
| **D** | Add the **Python app to the Aspire** solution | [Integrate the Python app into Aspire](06-integrate-python-into-aspire.md) |
| **E** | Deploy Aspire to a **local Docker** environment | [Deploy your Aspire application](07-deploy.md#deploy-to-a-local-docker-environment) |
| **F** | Deploy Aspire to **Azure Container Apps (ACA)** | [Deploy your Aspire application](07-deploy.md#deploy-to-azure-container-apps) |

---

## Table of contents

1. [Final Result](00-final-result.md) — *where we are heading*
2. [Prerequisites](01-prerequisites.md)
3. [Install the Aspire CLI](02-install-aspire-cli.md)
4. [Create a new Aspire application](03-create-a-new-aspire-application.md) *(Step A)*
5. [Add an ASP.NET Web API](04-add-an-aspnet-web-api.md) *(Step B)*
6. [Python FastAPI](05-python-fastapi.md) *(Step C)*
7. [Integrate the Python app into Aspire](06-integrate-python-into-aspire.md) *(Step D)*
8. [Deploy your Aspire application](07-deploy.md) *(Steps E & F)*

---

## What is Aspire?

**Aspire** (Microsoft Aspire) is a way to build applications that are composed of
multiple services that collaborate with each other. It helps you **organize**,
**run** and **observe** those services without wrestling with manual
configuration. In short:

- **Aspire** — a framework to orchestrate the multiple components of an app.
- **AppHost** — the project that starts everything.
- **Resources** — databases, queues, storage, etc. that the app uses.
- **Service** — one piece of your app (API, worker, frontend).
- **Dashboard** — the UI to see logs, dependencies, and service state.

This quickstart uses the **starter template**, which generates a C# AppHost. You
create the solution, review the generated AppHost, run it locally, then grow it by
adding more .NET and Python services, and finally deploy it.

The starter template uses modern C#:

- [Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis) for lightweight HTTP APIs.
- [Blazor](https://learn.microsoft.com/aspnet/core/blazor) for interactive web UIs using C#.
- [Service defaults](https://aspire.dev/get-started/csharp-service-defaults/) for shared configuration of observability and resilience.

> **Prefer VS Code?** Install the
> [Aspire VS Code extension](https://aspire.dev/get-started/aspire-vscode-extension/)
> for the same path inside the editor: create or open the app, run
> **Aspire: Configure launch.json file**, then press <kbd>F5</kbd> to start the
> AppHost and open the dashboard.

---

## How this repository is organized

```
.
├── README.md                              ← you are here (introduction + index)
├── 00-final-result.md                     ← the end state we are building toward
├── 01-prerequisites.md
├── 02-install-aspire-cli.md
├── 03-create-a-new-aspire-application.md   (Step A)
├── 04-add-an-aspnet-web-api.md             (Step B)
├── 05-python-fastapi.md                    (Step C)
├── 06-integrate-python-into-aspire.md      (Step D)
├── 07-deploy.md                            (Steps E & F)
└── _IMAGES/                               ← all screenshots referenced in the docs
```

Every chapter ends with links to the previous and next chapter so you can read the
whole thing as a continuous tutorial.

---

## Reference links

- [Aspire — Get started](https://aspire.dev/get-started/)
- [Install the Aspire CLI](https://aspire.dev/get-started/install-cli/)
- [Aspire AppHost](https://aspire.dev/get-started/app-host/)
- [Service discovery scheme resolution](https://aka.ms/dotnet/sdschemes)
- [FastAPI](https://fastapi.tiangolo.com/) · [Uvicorn](https://www.uvicorn.org/)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)

---

*Environment used throughout this guide: Ubuntu 24.04 on WSL, .NET 10 SDK, Aspire
CLI 13.4.6, Docker, and the `uv` Python package manager.*
