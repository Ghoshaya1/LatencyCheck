# --------- Build Stage ---------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app


# Copy csproj and restore
COPY LatencyChecker.csproj ./
RUN dotnet restore

# Copy source and build
COPY . ./
RUN dotnet publish -c Release -o out

# --------- Runtime Stage ---------
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Copy published app from build
COPY --from=build /app/out .

# Default command
ENTRYPOINT ["dotnet", "LatencyChecker.dll"]
