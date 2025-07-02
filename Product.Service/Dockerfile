FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore
COPY Product.Service.csproj ./
RUN dotnet restore Product.Service.csproj

# Copy everything else
COPY . ./

# Build and publish
RUN dotnet publish Product.Service.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Product.Service.dll"]