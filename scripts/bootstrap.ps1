param(
  [string]$SolutionName = "ConduitR"
)

dotnet new sln -n $SolutionName
dotnet sln add ./src/ConduitR.Abstractions/ConduitR.Abstractions.csproj
dotnet sln add ./src/ConduitR/ConduitR.csproj
dotnet sln add ./src/ConduitR.DependencyInjection/ConduitR.DependencyInjection.csproj
dotnet sln add ./samples/Samples.Console/Samples.Console.csproj
dotnet sln add ./samples/Samples.WebApi/Samples.WebApi.csproj
dotnet sln add ./tests/ConduitR.Tests/ConduitR.Tests.csproj
