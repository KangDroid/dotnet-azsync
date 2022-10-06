FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["dotnet-azsync/dotnet-azsync.csproj", "dotnet-azsync/"]
RUN dotnet restore "dotnet-azsync/dotnet-azsync.csproj"
COPY . .
WORKDIR "/src/dotnet-azsync"
RUN dotnet build "dotnet-azsync.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "dotnet-azsync.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dotnet-azsync.dll"]
