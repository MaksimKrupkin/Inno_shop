# UserService/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Копируем всё решение
WORKDIR /src
COPY . .

# Восстанавливаем зависимости для всего решения
RUN dotnet restore "Inno_Shop.sln"

# Собираем конкретный проект
RUN dotnet build "UserService/src/UserService.API/UserService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserService/src/UserService.API/UserService.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.API.dll"]