# api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --retries=3 CMD wget -qO- http://127.0.0.1:8080/healthz || exit 1

# Install wget into the runtime image so container healthchecks using wget succeed
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget ca-certificates && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./prepup.sln ./
COPY ./Api/Api.csproj ./Api/
RUN dotnet restore ./Api/Api.csproj
COPY ./Api ./Api
WORKDIR /src/Api
RUN dotnet publish -c Release -o /out

FROM base
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet","Api.dll"]
