# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем CSPROJ и восстанавливаем зависимости
COPY Authorization.csproj ./
RUN dotnet restore "Authorization.csproj"

# Копируем всё остальное и собираем
COPY . .
RUN dotnet build "Authorization.csproj" -c Release -o /app/build

# Публикуем
RUN dotnet publish "Authorization.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Authorization.dll"]
