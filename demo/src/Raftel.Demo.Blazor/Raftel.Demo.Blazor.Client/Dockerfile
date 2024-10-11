FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["demo/src/Raftel.Demo.Blazor/Raftel.Demo.Blazor.Client/Raftel.Demo.Blazor.Client.csproj", "demo/src/Raftel.Demo.Blazor/Raftel.Demo.Blazor.Client/"]
RUN dotnet restore "demo/src/Raftel.Demo.Blazor/Raftel.Demo.Blazor.Client/Raftel.Demo.Blazor.Client.csproj"
COPY . .
WORKDIR "/src/demo/src/Raftel.Demo.Blazor/Raftel.Demo.Blazor.Client"
RUN dotnet build "Raftel.Demo.Blazor.Client.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Raftel.Demo.Blazor.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Raftel.Demo.Blazor.Client.dll"]
