FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["AnalyseApp.csproj", "./"]
RUN dotnet restore "AnalyseApp.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "AnalyseApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AnalyseApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AnalyseApp.dll"]
