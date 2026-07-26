var builder = DistributedApplication.CreateBuilder(args);

/*
// Docker Compose environment: it enables `aspire publish`/`aspire deploy`
// to generate docker-compose.yaml, build the images, and run the containers.
var compose = builder.AddDockerComposeEnvironment("compose");
*/

// Add the following line to configure the Azure Container App environment
builder.AddAzureContainerAppEnvironment("aca");

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