
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ShopInfrastructure/ShopInfrastructure.csproj ShopInfrastructure/
COPY ShopDomain/ShopDomain.csproj ShopDomain/
COPY ShopMVC/ShopMVC.csproj ShopMVC/

RUN dotnet restore ShopMVC/ShopMVC.csproj

COPY . .
RUN dotnet publish ShopMVC/ShopMVC.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShopMVC.dll"]
