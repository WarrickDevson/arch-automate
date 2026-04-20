FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/ArchAutomate.sln backend/
COPY backend/ArchAutomate.Api/ArchAutomate.Api.csproj backend/ArchAutomate.Api/
COPY backend/ArchAutomate.BIM/ArchAutomate.BIM.csproj backend/ArchAutomate.BIM/
COPY backend/ArchAutomate.CAD/ArchAutomate.CAD.csproj backend/ArchAutomate.CAD/
COPY backend/ArchAutomate.Data/ArchAutomate.Data.csproj backend/ArchAutomate.Data/
COPY backend/ArchAutomate.AI/ArchAutomate.AI.csproj backend/ArchAutomate.AI/

RUN dotnet restore backend/ArchAutomate.Api/ArchAutomate.Api.csproj

COPY . .
RUN dotnet publish backend/ArchAutomate.Api/ArchAutomate.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "ArchAutomate.Api.dll"]
