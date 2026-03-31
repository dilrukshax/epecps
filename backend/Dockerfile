FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY epecps.sln ./
COPY backend/Epecps.Api/Epecps.Api.csproj backend/Epecps.Api/
COPY backend/Epecps.Application/Epecps.Application.csproj backend/Epecps.Application/
COPY backend/Epecps.Domain/Epecps.Domain.csproj backend/Epecps.Domain/
COPY backend/Epecps.Infrastructure/Epecps.Infrastructure.csproj backend/Epecps.Infrastructure/

RUN dotnet restore backend/Epecps.Api/Epecps.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/Epecps.Api/Epecps.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Epecps.Api.dll"]
