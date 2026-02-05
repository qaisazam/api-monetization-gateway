# Build stage (.NET 10 per plan)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/MonetizationGateway/MonetizationGateway.csproj", "MonetizationGateway/"]
RUN dotnet restore "MonetizationGateway/MonetizationGateway.csproj"
COPY src/MonetizationGateway/ MonetizationGateway/
WORKDIR /src/MonetizationGateway
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MonetizationGateway.dll"]
