# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["DigitalPlatform.API/DigitalPlatform.API/DigitalPlatform.API.csproj", "DigitalPlatform.API/DigitalPlatform.API/"]
COPY ["DigitalPlatform.API/DigitalPlatform.Application/DigitalPlatform.Application.csproj", "DigitalPlatform.API/DigitalPlatform.Application/"]
COPY ["DigitalPlatform.API/DigitalPlatform.Domain/DigitalPlatform.Domain.csproj", "DigitalPlatform.API/DigitalPlatform.Domain/"]
COPY ["DigitalPlatform.API/DigitalPlatform.Infrastructure/DigitalPlatform.Infrastructure.csproj", "DigitalPlatform.API/DigitalPlatform.Infrastructure/"]
RUN dotnet restore "DigitalPlatform.API/DigitalPlatform.API/DigitalPlatform.API.csproj"

COPY DigitalPlatform.API/. DigitalPlatform.API/
WORKDIR /src/DigitalPlatform.API/DigitalPlatform.API
RUN dotnet publish DigitalPlatform.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DigitalPlatform.API.dll"]
