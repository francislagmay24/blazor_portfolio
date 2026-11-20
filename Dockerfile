
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["Blazor_Portfolio/Blazor_Portfolio.csproj", "Blazor_Portfolio/"]
RUN dotnet restore "Blazor_Portfolio/Blazor_Portfolio.csproj"

# Copy source code
COPY . .

# Publish application
WORKDIR /src/Blazor_Portfolio
RUN dotnet publish "Blazor_Portfolio.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Render uses port 10000 by default
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Blazor_Portfolio.dll"]
