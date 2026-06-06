FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["LinkScanner.sln", "./"]

COPY ["src/LinkScanner.Domain/LinkScanner.Domain.csproj", "src/LinkScanner.Domain/"]
COPY ["src/LinkScanner.Application/LinkScanner.Application.csproj", "src/LinkScanner.Application/"]
COPY ["src/LinkScanner.Infrastructure/LinkScanner.Infrastructure.csproj", "src/LinkScanner.Infrastructure/"]
COPY ["src/LinkScannerApp/LinkScannerApp.csproj", "src/LinkScannerApp/"]

RUN dotnet restore "src/LinkScannerApp/LinkScannerApp.csproj"

COPY . .

RUN dotnet publish "src/LinkScannerApp/LinkScannerApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "LinkScannerApp.dll"]