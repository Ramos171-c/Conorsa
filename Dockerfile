# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install native dependencies for SkiaSharp / ShapeCrawler
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*


# SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first to leverage Docker caching for restore
COPY ["EnterpriseBillingSystem.WebApi/EnterpriseBillingSystem.WebApi.csproj", "EnterpriseBillingSystem.WebApi/"]
COPY ["EnterpriseBillingSystem.Application/EnterpriseBillingSystem.Application.csproj", "EnterpriseBillingSystem.Application/"]
COPY ["EnterpriseBillingSystem.Domain/EnterpriseBillingSystem.Domain.csproj", "EnterpriseBillingSystem.Domain/"]
COPY ["EnterpriseBillingSystem.Infrastructure/EnterpriseBillingSystem.Infrastructure.csproj", "EnterpriseBillingSystem.Infrastructure/"]

# Restore NuGet packages
RUN dotnet restore "EnterpriseBillingSystem.WebApi/EnterpriseBillingSystem.WebApi.csproj"

# Copy the entire source code
COPY . .

# Build the WebApi project
WORKDIR "/src/EnterpriseBillingSystem.WebApi"
RUN dotnet build "EnterpriseBillingSystem.WebApi.csproj" -c Release -o /app/build

# Publish the WebApi
FROM build AS publish
RUN dotnet publish "EnterpriseBillingSystem.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EnterpriseBillingSystem.WebApi.dll"]
