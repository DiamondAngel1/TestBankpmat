# Етап збірки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копіюємо весь код
COPY . .

# Публікуємо VisualClient
RUN dotnet publish ./VisualClient/VisualClient.csproj -c Release -o /app/out

# Етап запуску
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Копіюємо зібраний код
COPY --from=build /app/out .

# Відкриваємо порт (Render призначить свій)
EXPOSE 10000

# Запускаємо VisualClient
ENTRYPOINT ["dotnet", "VisualClient.dll"]