FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LinkScannerApp.csproj", "./"]
RUN dotnet restore "LinkScannerApp.csproj"

COPY . .
RUN dotnet publish "LinkScannerApp.csproj" -c Release -o /app/publish

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "LinkScannerApp.dll"]