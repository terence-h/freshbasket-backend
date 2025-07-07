FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore
COPY Order.Service.csproj ./
RUN dotnet restore Order.Service.csproj

# Copy everything else
COPY . ./

# Build and publish
RUN dotnet publish Order.Service.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Order.Service.dll"]