# === BUILD ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# === RUNTIME ===
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# App Service verwacht dat de app luistert op de poort hieronder
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

# App-bestanden
COPY --from=build /app/publish .

# (optioneel) als je een bestaand SQLite-bestand wilt meeleveren:
# COPY AuthDatabase.sqlite /home/site/wwwroot/AuthDatabase.sqlite

EXPOSE 8080
ENTRYPOINT ["dotnet", "Showcase.dll"]
