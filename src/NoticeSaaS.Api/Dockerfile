# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY NoticeSaaS.sln ./
COPY src/NoticeSaaS.Domain/NoticeSaaS.Domain.csproj src/NoticeSaaS.Domain/
COPY src/NoticeSaaS.Application/NoticeSaaS.Application.csproj src/NoticeSaaS.Application/
COPY src/NoticeSaaS.Infrastructure/NoticeSaaS.Infrastructure.csproj src/NoticeSaaS.Infrastructure/
COPY src/NoticeSaaS.Api/NoticeSaaS.Api.csproj src/NoticeSaaS.Api/
COPY src/NoticeSaaS.Workers/NoticeSaaS.Workers.csproj src/NoticeSaaS.Workers/
COPY tests/NoticeSaaS.UnitTests/NoticeSaaS.UnitTests.csproj tests/NoticeSaaS.UnitTests/
RUN dotnet restore src/NoticeSaaS.Api/NoticeSaaS.Api.csproj
COPY src/ src/
RUN dotnet publish src/NoticeSaaS.Api/NoticeSaaS.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NoticeSaaS.Api.dll"]
