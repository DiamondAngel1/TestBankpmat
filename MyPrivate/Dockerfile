# Етап збірки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копіюємо весь репозиторій
COPY ../ ./

# Публікуємо тільки MyPrivate, але з урахуванням залежностей
RUN dotnet publish ./MyPrivate/MyPrivate.csproj -c Release -o /app/out

# Етап запуску
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 5000
ENTRYPOINT ["dotnet", "MyPrivate.dll"]