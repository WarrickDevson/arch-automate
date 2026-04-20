FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ArchAutomate.sln ./
COPY ArchAutomate.Api/ArchAutomate.Api.csproj ArchAutomate.Api/
COPY ArchAutomate.BIM/ArchAutomate.BIM.csproj ArchAutomate.BIM/
COPY ArchAutomate.CAD/ArchAutomate.CAD.csproj ArchAutomate.CAD/
COPY ArchAutomate.Data/ArchAutomate.Data.csproj ArchAutomate.Data/
COPY ArchAutomate.AI/ArchAutomate.AI.csproj ArchAutomate.AI/

RUN dotnet restore ArchAutomate.Api/ArchAutomate.Api.csproj

COPY . .
RUN dotnet publish ArchAutomate.Api/ArchAutomate.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "ArchAutomate.Api.dll"]
