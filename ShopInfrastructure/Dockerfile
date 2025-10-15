FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ShopInfrastructure.csproj", "."]
RUN dotnet restore "./ShopInfrastructure.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ShopInfrastructure.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShopInfrastructure.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopInfrastructure.dll"]
