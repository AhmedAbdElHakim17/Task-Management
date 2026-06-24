# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first (layer-cache friendly)
COPY TaskManagement.API/TaskManagement.API.csproj               TaskManagement.API/
COPY TaskManagement.Application/TaskManagement.Application.csproj TaskManagement.Application/
COPY TaskManagement.Domain/TaskManagement.Domain.csproj         TaskManagement.Domain/
COPY TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj TaskManagement.Infrastructure/

# Restore NuGet packages (API project transitively resolves all references)
RUN dotnet restore TaskManagement.API/TaskManagement.API.csproj

# Copy all remaining source
COPY . .

# Publish the API project in Release mode
RUN dotnet publish TaskManagement.API/TaskManagement.API.csproj \
    -c Release \
    -o /app/publish 
    
# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

ENV TZ=Africa/Cairo

RUN apt-get update && \
    apt-get install -y tzdata && \
    ln -fs /usr/share/zoneinfo/$TZ /etc/localtime && \
    dpkg-reconfigure -f noninteractive tzdata && \
    apt-get clean

WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Expose the port ASP.NET Core listens on inside the container
EXPOSE 8080

ENTRYPOINT ["dotnet", "TaskManagement.API.dll"]
