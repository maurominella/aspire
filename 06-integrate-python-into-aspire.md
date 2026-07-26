# 6. Integrate the Python app into Aspire  *(Step D)*

Now that the Python FastAPI service works standalone (and inside its own container),
we hand it over to Aspire so it is orchestrated exactly like the .NET services.

## Table of contents

- [0. General information](#0-general-information)
- [1. Containerize the Python app](#1-containerize-the-python-app)
- [2. Add the `Aspire.Hosting.Python` integration](#2-add-the-aspirehostingpython-integration)
- [3. Add the `/health` path to the Python app](#3-add-the-health-path-to-the-python-app)
- [4. Register the Python app in the AppHost](#4-register-the-python-app-in-the-apphost)
- [5. Expose the Python application's API](#5-expose-the-python-applications-api)
- [6. Set up discovery for the Python API](#6-set-up-discovery-for-the-python-api)
- [Key point: Dependency Injection and the typed client](#key-point-dependency-injection-and-the-typed-client)
- [Final result — running locally without Docker](#final-result--running-locally-without-docker)
- [Final considerations](#final-considerations)

---

## 0. General information

**🧩 Why do the .NET projects have no Dockerfile, while Python does?** Because:

- .NET has a **builder integrated** in Aspire → it automatically generates a container.
- Python **has no integrated builder** → it needs a Dockerfile.

Aspire containerizes everything, but for Python it must be told *how* to do it.

**🎯 Objective:** add the Python application **`AspireApp01.PyApi01`** to the AppHost,
then let Aspire:

- build the container from our Dockerfile,
- start FastAPI,
- assign the port,
- manage the health check,
- manage service discovery.

**Do we add the Python project to the AppHost `.csproj`? NO, but…** We cannot add the
Python project name to the Aspire AppHost, because:

- there is no referenceable file (like `.csproj`, `.fsproj`, `.vbproj`),
- Aspire does **not** use `ProjectReference` for non‑.NET projects.

However, in the `.csproj` we must still add the Python integration called
**`Aspire.Hosting.Python`**. This package:

- registers Python projects as "Aspire projects",
- enables `builder.AddUvicornApp(<name>, <path>, <module like "main:app">)`,
- enables the automatic build of the Dockerfile,
- enables local execution inside Aspire.

**Do we add the Python project to the Aspire SOLUTION? NO!** No non‑.NET project
appears in the Aspire *solution*, and that is **normal**. Aspire cannot automatically
find a Python project in the solution because it is not an MSBuild project. Aspire only
sees .NET projects (they have a `.csproj`), the AppHost (a .NET project) and other
MSBuild projects. A Python project has no `.csproj`, is not an MSBuild project, and
therefore cannot appear in the solution. **Aspire manages it only by path, not through
the project system.**

So the two things we do are:

1. **Tell the AppHost to include that project** using the dedicated method.
2. Aspire builds the container, starts it, assigns it a port, and connects it to the
   other services.

---

## 1. Containerize the Python app

This was done in the [previous chapter](05-python-fastapi.md#5-containerize-with-a-dockerfile).
For reference, the Dockerfile is:

```dockerfile
FROM python:3.11-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY main.py monitoring.py favicon.ico ./
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
```

---

## 2. Add the `Aspire.Hosting.Python` integration

First, clean the NuGet package cache for this Aspire project. This prevents the next
`Restore` from taking too long:

```bash
dotnet nuget locals all --clear
```

```text
Clearing NuGet HTTP cache: /home/mauromi/.local/share/NuGet/http-cache
Clearing NuGet global packages folder: /home/mauromi/.nuget/packages/
Clearing NuGet Temp cache: /tmp/NuGetScratchmauromi
Clearing NuGet plugins cache: /home/mauromi/.local/share/NuGet/plugin-cache
Local resources cleared.
```

You can either add the entry to `AspireApp01.AppHost.csproj` directly, or (as done
here) use a command:

```bash
dotnet add AspireApp01.AppHost package Aspire.Hosting.Python --version 13.4.6
```

Installing the package automatically triggers a `Restore` (unless you pass
`--norestore`, but then you'd have to run restore separately, so keep it simple):

```text
info : X.509 certificate chain validation will use the fallback certificate bundle at '/usr/lib/dotnet/sdk/10.0.110/trustedroots/codesignctl.pem'.
info : X.509 certificate chain validation will use the fallback certificate bundle at '/usr/lib/dotnet/sdk/10.0.110/trustedroots/timestampctl.pem'.
info : Adding PackageReference for package 'Aspire.Hosting.Python' into project '.../AspireApp01.AppHost/AspireApp01.AppHost.csproj'.
...
log  : Restored .../AspireApp01.AppHost/AspireApp01.AppHost.csproj (in 12.71 sec).
```

> Note the message *"X.509 certificate chain validation will use the fallback
> certificate bundle"* — it shouldn't strictly happen, but it doesn't matter; at worst
> the automatic restore takes a few seconds longer. What matters is that all operations
> complete correctly.

The result in `AspireApp01.AppHost.csproj` (using a command like this is cleaner and
more "enterprise" than editing by hand; and although the two `<ItemGroup>` blocks could
be merged, we keep them separate for clarity):

```xml
<Project Sdk="Aspire.AppHost.Sdk/13.4.6">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UserSecretsId>dc27b775-2a17-457d-bbda-2bdd30b0304c</UserSecretsId>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\AspireApp01.ApiService\AspireApp01.ApiService.csproj" />
    <ProjectReference Include="..\AspireApp01.Web\AspireApp01.Web.csproj" />
    <ProjectReference Include="..\AspireApp01.ApiService02\AspireApp01.ApiService02.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.Python" Version="13.4.6" />
  </ItemGroup>
</Project>
```

---

## 3. Add the `/health` path to the Python app

Add the `/health` path to `main.py` for the "health" check. In the next step,
`WithHttpHealthCheck("/health")` will have Aspire poll this `/health` endpoint.

How the mechanism works:

- `WithHttpHealthCheck("/health")` is on the **AppHost/Aspire** side: it only configures
  an HTTP probe that periodically calls `GET /health` on the resource. **It does not
  create any endpoint.**
- The endpoint must be implemented in the application (in our case, FastAPI). In ASP.NET
  projects it works because `MapDefaultEndpoints()` in `ServiceDefaults` automatically
  exposes `/health` — but for the Python app that mechanism doesn't exist, so we must
  define it by hand.

```python
# ./AspireApp01.PyApi01/main.py
from fastapi import FastAPI
from monitoring import logger

logger.info(f"Program started.")

app = FastAPI()

@app.get("/")
def read_root():
    logger.info(f"Root endpoint accessed.")
    return {"message": "Hello from FastAPI"}

@app.get("/health")
def health_check():
    return {"status": "healthy"}

@app.get("/WeatherForecast")
def read_weather_forecast():
    logger.info(f"Weather forecast endpoint accessed.")
    return {"message": "Weather forecast data"}
```

---

## 4. Register the Python app in the AppHost

Our Python project uses FastAPI/uvicorn. With `Aspire.Hosting.Python` you do **not** use
`AddProject` (reserved for .NET `.csproj`) but the dedicated method for Python apps. For
the 13.4.6 version we added above, the method to use is **`AddUvicornApp`** (provided by
`Aspire.Hosting.Python`), designed for ASGI applications like FastAPI/Starlette/Quart.

**🎯 Short explanation:**

- `var pyApi = builder.AddUvicornApp("pyapi01", "../AspireApp01.PyApi01", "main:app")` →
  Aspire uses **the path** to find the Python project and its **Dockerfile**, from which
  it builds the container at deploy time. Since our `main.py` has `app = FastAPI()`, the
  ASGI reference is `"main:app"`. Also, the health check points to `/health`, but that
  endpoint didn't originally exist in our `main.py` (we only had `/` and
  `/weatherforecast`), so it must be added — otherwise the resource stays "Unhealthy".
- `.WithUv()` avoids a later deployment error that would otherwise try to run
  `pip install .`.
- `.WithHttpEndpoint(port: 8000)` — FastAPI listens on **8000**, so we declare it to let
  Aspire expose the service.
- `.WithHttpHealthCheck("/health")` — as explained above. It does **not** create the
  endpoint: we implement it in the Python app.
- `.WithReference(pyApi)` — if we want to be able to reach the Python API.
- `.WaitFor(pyApi)` — best practice to wait for dependent services (here, from the web
  frontend).

```csharp
# ./AspireApp01.AppHost/AppHost.cs
var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireApp01_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

var apiService02 = builder.AddProject<Projects.AspireApp01_ApiService02>("apiservice02")
    .WithHttpHealthCheck("/health");

var pyApi = builder.AddUvicornApp("pyapi01", "../AspireApp01.PyApi01", "main:app")
    .WithUv()
    .WithHttpEndpoint(port: 8000, env: "PORT")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AspireApp01_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WithReference(apiService02)
    .WithReference(pyApi)
    .WaitFor(apiService)
    .WaitFor(apiService02)
    .WaitFor(pyApi);

builder.Build().Run();
```

---

## 5. Expose the Python application's API

In addition to the `/health` path implemented above, we set up the paths through which
our Python application can be consumed. The whole file is short, so it is shown in full;
note in particular the `"/weatherforecast"` path, which — remember — is **case
sensitive**. That path returns an array of JSON objects (itself valid JSON) where each
object has three key/value pairs: `date`, `temperature`, and `summary`.

```python
# ./AspireApp01.PyApi01/main.py
from fastapi import FastAPI
from monitoring import logger
from datetime import date, timedelta
import random

logger.info(f"Program started.")

app = FastAPI()

@app.get("/")
def read_root():
    logger.info(f"Root endpoint accessed.")
    return {"message": "Hello from FastAPI"}

@app.get("/health")
def health_check():
    return {"status": "healthy"}

@app.get("/weatherforecast")
def read_weather_forecast():
    logger.info(f"Weather forecast endpoint accessed.")
    summaries = ["Helado", "Refrescante", "Frío", "Fresco", "Templado",
                 "Cálido", "Agradable", "Caliente", "Sofocante", "Abrumador"]
    today = date.today()
    return [
        {
            "date": (today + timedelta(days=i)).isoformat(),
            "temperatureC": random.randint(-20, 55),
            "summary": random.choice(summaries),
        }
        for i in range(1, 6)
    ]
```

---

## 6. Set up discovery for the Python API

We proceed similarly to how we set up discovery for the second Web API.

Remember that the Aspire template gave us "for free" the `WeatherApiClient` class, which
exposes the public method `GetWeatherAsync` returning the `WeatherForecast[]` array of
individual forecasts (`public record WeatherForecast`). This class lives in the
same‑named file `WeatherApiClient.cs` of the Web project, which consumes the API exposed
by the ASP.NET and Python projects integrated in the Aspire solution.

### Key point: Dependency Injection and the typed client

As with a classic Dependency Injection container, this class receives as a parameter the
`httpClient` object that was injected into the container in the Web project's
`Program.cs`:

```csharp
builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
    client.BaseAddress = new("https+http://apiservice");
});
```

This means that when this class is instantiated, it is passed the `httpClient` object
already populated in its `client.BaseAddress` member which — thanks to Aspire replacing
the placeholder `"https+http://apiservice"` — points to the API host name. It can
therefore call the API path directly, here statically written in the code as
`"/weatherforecast"`.

Now, on the one hand, for every service we invoke we use a dedicated class, with a
coherent naming convention:

- class `WeatherApiClient` for the first ASP.NET project `AspireApp01.ApiService`,
- class `WeatherApiClient2` for the second ASP.NET project `AspireApp01.ApiService02`,
- class `WeatherApiClientFromPython` for the Python project `AspireApp01.PyApi01`.

On the other hand, these classes all happen to expose the same `/weatherforecast` path,
so instead of copy‑pasting the same code, we make them all **derive** from
`WeatherApiClient`. In short, for our Python application we wrote a single line —
`public class WeatherApiClientFromPython(HttpClient httpClient) : WeatherApiClient(httpClient);`
— and everything else follows:

```csharp
// ./AspireApp01.Web/WeatherApiClient.cs
namespace AspireApp01.Web;

public class WeatherApiClient(HttpClient httpClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
        {
            if (forecasts?.Count >= maxItems)
            {
                break;
            }
            if (forecast is not null)
            {
                forecasts ??= [];
                forecasts.Add(forecast);
            }
        }

        return forecasts?.ToArray() ?? [];
    }
}

// Second typed client: reuses the same logic as WeatherApiClient,
// but receives an HttpClient configured towards "apiservice02".
public class WeatherApiClient2(HttpClient httpClient) : WeatherApiClient(httpClient);

// Third typed client: reuses the same logic as WeatherApiClient,
// but receives an HttpClient configured towards the Python service.
public class WeatherApiClientFromPython(HttpClient httpClient) : WeatherApiClient(httpClient);

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

### Register the Python client in DI

As explained above, in the Web frontend's DI container we add the HTTP client that points
to the Python endpoint, which will be provided to the `WeatherApiClientFromPython` class.
This way, when that class is instantiated, it automatically receives the HTTP path to
call, to which it only appends the `"/weatherforecast"` endpoint defined in the base
class it derives from. The AppHost is already set (you have `.WithReference(pyApi)` and
`.WaitFor(pyApi)`).

```csharp
// ../AspireApp01.Web/Program.cs
builder.Services.AddHttpClient<WeatherApiClientFromPython>(client =>
{
    // The Python app (uvicorn/FastAPI) is exposed by Aspire in HTTPS:
    // "https+http://" prefers HTTPS with fallback to HTTP.
    client.BaseAddress = new("https+http://pyapi01");
});
```

### Consume it from the Razor page

In `Weather.razor` we inject `WeatherApiClientFromPython` (as `WeatherApiFromPython`), set
`forecastsFromPython` in `OnInitializedAsync`, and render it in a table under
`<h2>pyapi01</h2>`. (Only the parts shown are what we add; the `<h2>` headers of the
`apiService` and `apiService02` menus are omitted for brevity, but the `<h2>` of the
Python menu is included.)

```razor
@* ./AspireApp01.Web/Components/Pages/Weather.razor *@
@page "/weather"
@attribute [StreamRendering(true)]
@attribute [OutputCache(Duration = 5)]

@inject WeatherApiClient WeatherApi
@inject WeatherApiClient2 WeatherApi2
@inject WeatherApiClientFromPython WeatherApiFromPython

<PageTitle>Weather</PageTitle>

<h1>Weather</h1>
<p>This component demonstrates showing data loaded from a backend API service.</p>

...

<h2>pyapi01</h2>
@if (forecastsFromPython == null)
{
    <p><em>Loading...</em></p>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Date</th>
                <th aria-label="Temperature in Celsius">Temp. (C)</th>
                <th aria-label="Temperature in Fahrenheit">Temp. (F)</th>
                <th>Summary</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var forecast in forecastsFromPython)
            {
                <tr>
                    <td>@forecast.Date.ToShortDateString()</td>
                    <td>@forecast.TemperatureC</td>
                    <td>@forecast.TemperatureF</td>
                    <td>@forecast.Summary</td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private WeatherForecast[]? forecasts;
    private WeatherForecast[]? forecasts2;
    private WeatherForecast[]? forecastsFromPython;

    protected override async Task OnInitializedAsync()
    {
        forecasts = await WeatherApi.GetWeatherAsync();
        forecasts2 = await WeatherApi2.GetWeatherAsync();
        forecastsFromPython = await WeatherApiFromPython.GetWeatherAsync();
    }
}
```

---

## Final result — running locally without Docker

An Aspire AppHost with:

- 2 .NET projects (API + Blazor frontend),
- 1 Python project (FastAPI/uvicorn).

The applications communicate with each other.

![The Aspire dashboard with the .NET and Python resources running](_IMAGES/22-python-integrated-dashboard.png)

![The Weather page showing forecasts from the Python service](_IMAGES/23-python-integrated-weather.png)

![Architecture of the three services communicating](_IMAGES/21-architecture-three-services.png)

---

## Final considerations

**📌 Is the line `pyapi01-installer - Finished` correct every time I launch this Aspire
project?** Yes, it is entirely correct and normal.

![The pyapi01-installer resource reaching the Finished state](_IMAGES/24-pyapi01-installer-finished.png)

`pyapi01-installer` is a child resource that Aspire's Python integration creates
automatically to prepare the environment before starting the app. With `.WithUv()` it
runs `uv sync`; without it, it would run `pip install`. A few points to keep in mind:

- It runs on **every** AppHost start: it is a "one‑shot" resource (not a persistent
  service), so the correct final state is exactly `Finished` (not `Running`), while
  `pyapi01` stays `Running`.
- It is fast and idempotent: if the dependencies are already synchronized (as you saw
  with `uv sync` → *"Checked 60 packages"*), it reinstalls nothing and finishes
  immediately.
- It ensures the `.venv` is aligned with `uv.lock` before uvicorn starts. It is how
  Aspire gives you a reproducible environment without preparing it by hand.

So it is neither an error nor a waste: it is the expected behavior. If one day you see
`pyapi01-installer` in the `Failed` state, *then* it would be a problem (dependencies not
installable) — but `Finished` is exactly what should happen.

**So now I can see, and run, any project from the Aspire console? Yes!** After adding the
Python project with `AddUvicornApp("pyapi01", path)` and the `Aspire.Hosting.Python`
package, we can run the whole solution locally from the Aspire console, exactly as we do
with the .NET projects. Aspire doesn't "see" the Python project in the *solution* (it's
not a `.csproj`), **but it sees it perfectly at runtime** because you registered it, you
added `Aspire.Hosting.Python`, Aspire finds the Dockerfile, builds the container, and
starts it alongside the other services.

**🎯 Locally you see and can start:** `apiservice` (.NET), `apiservice02` (.NET),
`webfrontend` (.NET), `pyapi01` (Python → container). They all appear in the Aspire
dashboard, with endpoints, health checks, logs, container info, dependencies and
environment variables.

**🎯 In ACA the same thing happens:** Aspire generates **one container per project**, even
for Python.

---

[⬅ Back to index](README.md) · [⬅ Previous: Python FastAPI](05-python-fastapi.md) · [Next: Deploy your Aspire application ➡](07-deploy.md)
