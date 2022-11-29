# ASP.NET Core Minimal APIs sample

This is a sample code for the presentation "Minimal APIs in ASP.NET Core 6+".

## Presentations

- [November 2022, Advanced Technology Days, Zagreb](2022-11-atd-minimal-apis.pptx)
- [September 2022, Road to Init, Banja Luka](2022-09-road-to-init-minimal-apis.pptx)
- [September 2022, KulenDayz, Osijek](2022-09-kulendayz-minimal-apis.pptx)

## Solution organization

There are three sample projects in a solution and one test project.

1. MvcSample demonstrates a regular MVC application using controllers
2. MinimalSample is the same application but written with new ASP.NET Minimal APIs
3. MinimalSample.Refactored is the same sample, but refactored to feature folder organization using [Carter](https://github.com/CarterCommunity/Carter/)

The test project is demonstrating how to write integration tests that target Minimal APIs

## Requirements

The projects are created using .NET 6 and .NET 7 Preview 7. You need to have [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) installed locally.

Before running the samples, you need to make sure that the SQLite database are created and pre-seeded with the data. To ensure that, run the following commands:

```shell
dotnet tool restore
dotnet ef database update --project .\src\MinimalApis.MvcSample\MinimalApis.MvcSample.csproj
dotnet ef database update --project .\src\MinimalApis.MinimalSample\MinimalApis.MinimalSample.csproj
dotnet ef database update --project .\src\MinimalApis.MinimalSample.Refactored\MinimalApis.MinimalSample.Refactored.csproj
```

## Running the samples

You can run each sample one by one and check the Swagger UI for a list of available endpoints. Each sample uses a different port and a database, so they can all be run at the same time.

```shell
dotnet run --project .\src\MinimalApis.MvcSample\MinimalApis.MvcSample.csproj
dotnet run --project .\src\MinimalApis.MinimalSample\MinimalApis.MinimalSample.csproj --launch-profile https
dotnet run --project .\src\MinimalApis.MinimalSample.Refactored\MinimalApis.MinimalSample.Refactored.csproj --launch-profile https
```

Accessing the Swagger UI:

1. [MVC Sample](https://localhost:7147/swagger/)
2. [Minimal Sample](https://localhost:7181/swagger/)
3. [Minimal Sample Refactored](https://localhost:7286/swagger/)

## Running tests

Run the following from the root of the repository:

```shell
dotnet test .\tests\MinimalApis.MinimalSample.Tests\MinimalApis.MinimalSample.Tests.csproj
```

## License

See [LICENSE](LICENSE) file.
