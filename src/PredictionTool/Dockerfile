FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/PredictionTool/PredictionTool.csproj", "src/PredictionTool/"]
RUN dotnet restore "src/PredictionTool/PredictionTool.csproj"
COPY . .
WORKDIR "/src/src/PredictionTool"
RUN dotnet build "PredictionTool.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PredictionTool.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PredictionTool.dll"]
