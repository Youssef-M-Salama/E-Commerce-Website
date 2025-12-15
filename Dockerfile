# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# copy csproj and restore first (layer caching)
COPY *.csproj ./ 
RUN dotnet restore

# copy everything else
COPY . . 

# publish
RUN dotnet publish -c Release -o /app/publish

# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

# Expose port 80 (inside container)
EXPOSE 80

ENTRYPOINT ["dotnet", "E-Commerce-Website.dll"]
