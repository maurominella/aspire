using Aspire.Hosting.Foundry;

var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireApp01_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

var apiService02 = builder.AddProject<Projects.AspireApp01_ApiService02>("apiservice02")
    .WithHttpHealthCheck("/health");

var pyApi = builder.AddUvicornApp("pyapi01", "../AspireApp01.PyApi01", "main:app")
    .WithUv()
    .WithHttpEndpoint(port: 8000, env: "PORT")
    .WithEnvironment("ENABLE_DEBUGPY", "1")
    .WithEnvironment("PYDEVD_DISABLE_FILE_VALIDATION", "1")
    .WithArgs(context =>
    {
        // Remove uvicorn's "--reload": the reloader respawns the worker process,
        // which drops the debugpy attach and un-binds breakpoints. A single stable
        // process makes step-through debugging in main.py reliable.
        for (var i = context.Args.Count - 1; i >= 0; i--)
        {
            if (context.Args[i] is "--reload")
            {
                context.Args.RemoveAt(i);
            }
        }
    })    
    .WithReference(apiService)   // 👈 injects services__apiservice__http__0 / __https__0
    .WaitFor(apiService)         // 👈 starts pyapi01 only when apiservice is ready
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

/*
// Docker Compose environment: it enables `aspire publish`/`aspire deploy`
// to generate docker-compose.yaml, build the images, and run the containers.
var compose = builder.AddDockerComposeEnvironment("compose");

// Add the following line to configure the Azure Container App environment
builder.AddAzureContainerAppEnvironment("aca");
*/ 

/*
var foundry = builder.AddFoundry("ai");
var project = foundry.AddProject("project");

var chat = foundry.AddDeployment("chat", FoundryModel.OpenAI.Gpt41);

var webSearch = project.AddWebSearchTool("websearch");

var researchAgent = project.AddPromptAgent("researcher", chat,
    instructions: """
        Answer product questions. Use web search when current information is needed.
        """)
    .WithTool(webSearch);

*/