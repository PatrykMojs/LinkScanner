# LinkScanner

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20App-5C2D91?style=for-the-badge&logo=dotnet&logoColor=white)
![Razor Pages](https://img.shields.io/badge/Razor%20Pages-Frontend-0A66C2?style=for-the-badge)
![Serilog](https://img.shields.io/badge/Serilog-Logging-1E1E1E?style=for-the-badge)

**LinkScanner** is a web application for analyzing URLs and estimating whether a website is potentially safe or suspicious.  
The project combines a simple user-facing interface with a backend scanning pipeline that validates a submitted URL, follows redirects in a controlled way, fetches page metadata, analyzes HTTP/TLS/security-related information and returns a structured scan result.

The application was created as a portfolio project focused on **web security, clean architecture, backend engineering, observability and production-oriented coding practices**.

---

## Table of contents

- [Project goal](#project-goal)
- [Main features](#main-features)
- [Screenshots](#screenshots)
- [Tech stack](#tech-stack)
- [Architecture](#architecture)
- [Project structure](#project-structure)
- [How the scanning flow works](#how-the-scanning-flow-works)
- [Security and reliability mechanisms](#security-and-reliability-mechanisms)
- [Configuration](#configuration)
- [API usage](#api-usage)
- [Getting started](#getting-started)
- [Running tests](#running-tests)
- [Docker](#docker)
- [Roadmap](#roadmap)
- [Author](#author)

---

## Project goal

The main goal of LinkScanner is to provide a simple tool that helps users check whether a link looks trustworthy before opening it.

From the engineering perspective, the project is designed to show practical backend skills:

- building an ASP.NET Core application in .NET 8,
- separating responsibilities between application, domain, infrastructure and presentation layers,
- working with dependency injection,
- using structured logging with Serilog,
- protecting public endpoints with rate limiting,
- validating and limiting untrusted user input,
- designing a scanning pipeline that can be extended with new analyzers,
- preparing the project for future AI/ML-based URL classification.

---

## Main features

### URL scanning

The application accepts a URL from the user and performs a technical scan. The scan can include:

- URL validation,
- HTTP request execution with timeout control,
- redirect analysis,
- HTML metadata extraction,
- TLS certificate analysis,
- HTTP security headers analysis,
- host/IP resolution,
- risk score calculation,
- final safety decision.

### Web interface

The project contains a Razor Pages frontend that allows the user to scan a link from the browser.

### REST API

The application exposes an API endpoint that can be used by external clients or future integrations.

```http
POST /api/scan
Content-Type: application/json

"https://example.com"
```

### Logging

Serilog is configured for structured logging to the console and rolling log files. This makes the application easier to debug and closer to production-style diagnostics.

### Rate limiting

The scan endpoint is protected with a fixed-window rate limit policy based on the caller IP address. This helps reduce abuse and protects the application from excessive scanning requests.

### Request and scan limits

The application contains configurable limits such as:

- maximum URL length,
- allowed ports,
- HTTP timeout,
- maximum redirects,
- maximum downloaded HTML size,
- maximum request body size,
- maximum number of concurrent scans.

---

## Screenshots

> Screenshots are intentionally separated from the source code description, so the README can later be used as portfolio documentation.

Create a folder like this:

```text
/docs/images/
```

Then add screenshots and replace the placeholders below.

### Home page

![Home page](docs/images/home-page.png)

### Scan result

![Scan result](docs/images/scan-result.png)

### API / logs example

![Logs example](docs/images/logs-example.png)

### Future demo video

A short demo video can be added later, for example:

```markdown
[Watch demo](https://your-demo-link-here)
```

---

## Tech stack

| Area | Technology |
|---|---|
| Backend | .NET 8, ASP.NET Core |
| Frontend | Razor Pages, HTML, CSS, JavaScript |
| API | ASP.NET Core Controllers |
| Architecture | Clean Architecture-inspired layered structure |
| Logging | Serilog |
| Rate limiting | ASP.NET Core Rate Limiting |
| Caching | In-memory cache |
| Tests | xUnit / .NET test project |
| Deployment readiness | Dockerfile included |

---

## Architecture

The solution is divided into separate projects:

```text
LinkScanner
├── src
│   ├── LinkScannerApp
│   ├── LinkScanner.Application
│   ├── LinkScanner.Domain
│   └── LinkScanner.Infrastructure
└── tests
    └── LinkScanner.Tests
```

### `LinkScannerApp`

Presentation layer of the application.

Responsible for:

- application startup,
- HTTP pipeline configuration,
- Razor Pages frontend,
- API controllers,
- middleware registration,
- rate limiting,
- security headers,
- global exception handling,
- request size limiting,
- Serilog request logging.

### `LinkScanner.Application`

Application layer containing use cases and abstractions.

Responsible for:

- application use cases,
- scan command handling,
- interfaces used by infrastructure,
- application options,
- orchestration between validation and scanning logic.

Example use case:

```text
UseCases/ScanUrl
```

### `LinkScanner.Domain`

Domain layer containing core business entities and models.

Responsible for:

- scan result models,
- domain-level data structures,
- keeping core concepts independent from external infrastructure.

### `LinkScanner.Infrastructure`

Infrastructure layer containing technical implementations.

Responsible for:

- URL validation implementation,
- HTTP fetching,
- redirect handling,
- HTML metadata extraction,
- TLS certificate analysis,
- security headers analysis,
- risk score calculation,
- caching,
- concurrency limiting.

---

## Project structure

A simplified view of the most important folders:

```text
src/
├── LinkScannerApp/
│   ├── Controllers/
│   │   └── ScanController.cs
│   ├── Extensions/
│   ├── Middleware/
│   ├── Options/
│   ├── Pages/
│   ├── wwwroot/
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
│
├── LinkScanner.Application/
│   ├── Abstractions/
│   ├── Options/
│   ├── UseCases/
│   │   └── ScanUrl/
│   └── DependencyInjection.cs
│
├── LinkScanner.Domain/
│   └── Entities/
│
└── LinkScanner.Infrastructure/
    ├── Caching/
    ├── Scanning/
    │   ├── Analyzers/
    │   │   ├── HostIpResolver.cs
    │   │   ├── HtmlMetadataExtractor.cs
    │   │   ├── RedirectAnalyzer.cs
    │   │   ├── RiskScoreCalculator.cs
    │   │   ├── SafetyDecisionAnalyzer.cs
    │   │   ├── SecurityHeadersAnalyzer.cs
    │   │   └── TlsCertificateAnalyzer.cs
    │   ├── Http/
    │   │   ├── HttpPageFetcher.cs
    │   │   ├── RedirectHttpClient.cs
    │   │   └── RedirectHttpResult.cs
    │   ├── LinkScannerService.cs
    │   └── SemaphoreScanConcurrencyLimiter.cs
    ├── Validation/
    └── DependencyInjection.cs
```

---

## How the scanning flow works

The scan flow is designed as a pipeline:

1. The user submits a URL through the web UI or API.
2. `ScanController` receives the request.
3. The controller passes the request to the `ScanUrlHandler` use case.
4. The application layer validates and orchestrates the scan.
5. Infrastructure services perform technical analysis of the target URL.
6. Analyzers collect signals such as redirects, metadata, TLS and security headers.
7. A risk score is calculated.
8. A final safety decision is returned to the user.

Simplified flow:

```text
User / Browser
     │
     ▼
Razor Pages / API Controller
     │
     ▼
ScanUrlHandler
     │
     ▼
URL Validator
     │
     ▼
LinkScannerService
     │
     ├── RedirectAnalyzer
     ├── HtmlMetadataExtractor
     ├── SecurityHeadersAnalyzer
     ├── TlsCertificateAnalyzer
     ├── HostIpResolver
     ├── RiskScoreCalculator
     └── SafetyDecisionAnalyzer
     │
     ▼
Scan result
```

---

## Security and reliability mechanisms

LinkScanner scans external URLs, so the project includes several mechanisms that are important when working with untrusted input.

### Input validation

The application validates submitted URLs before scanning them.

### Allowed ports

The configuration limits scanning to selected ports, currently:

```json
"AllowedPorts": [80, 443]
```

### Timeout control

HTTP requests use a configurable timeout:

```json
"HttpTimeoutSeconds": 8
```

### Redirect limits

Redirect processing is limited:

```json
"MaxRedirects": 5
```

### HTML size limit

The amount of downloaded HTML is limited:

```json
"MaxHtmlBytes": 1000000
```

### Request body size limit

The request body size is limited:

```json
"MaxScanRequestBodyBytes": 4096
```

### Rate limiting

The scan endpoint uses rate limiting:

```json
"RateLimiting": {
  "ScanPermitLimit": 10,
  "WindowSeconds": 60,
  "QueueLimit": 0
}
```

### Concurrency limit

The scanner can limit the number of concurrent scans:

```json
"MaxConcurrentScans": 3
```

### Security headers

The web application uses custom security headers middleware.

### Global exception handling

Unexpected exceptions are handled by global middleware, which improves reliability and prevents leaking implementation details to the client.

---

## Configuration

Main configuration is stored in:

```text
src/LinkScannerApp/appsettings.json
```

Example configuration:

```json
{
  "LinkScanner": {
    "HttpTimeoutSeconds": 8,
    "MaxRedirects": 5,
    "MaxHtmlBytes": 1000000,
    "MaxUrlLength": 2048,
    "AllowedPorts": [80, 443],
    "CacheTtlMinutes": 10,
    "MaxConcurrentScans": 3
  },
  "RateLimiting": {
    "ScanPermitLimit": 10,
    "WindowSeconds": 60,
    "QueueLimit": 0
  },
  "RequestLimits": {
    "MaxScanRequestBodyBytes": 4096
  }
}
```

---

## API usage

### Scan URL

```http
POST /api/scan
Content-Type: application/json
```

Request body:

```json
"https://example.com"
```

Example using PowerShell:

```powershell
Invoke-RestMethod `
  -Uri "https://localhost:5001/api/scan" `
  -Method Post `
  -ContentType "application/json" `
  -Body '"https://example.com"'
```

Example using cURL:

```bash
curl -X POST "https://localhost:5001/api/scan" \
  -H "Content-Type: application/json" \
  -d '"https://example.com"'
```

Possible successful response contains a structured scan result with information collected during analysis.

Possible error response:

```json
{
  "error": "Invalid URL."
}
```

When the rate limit is exceeded, the API returns HTTP `429 Too Many Requests`.

---

## Getting started

### Prerequisites

Install:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git
- Visual Studio 2022 or Visual Studio Code

### Clone repository

```bash
git clone https://github.com/PatrykMojs/LinkScanner.git
cd LinkScanner
git checkout develop
```

### Restore dependencies

```bash
dotnet restore
```

### Build solution

```bash
dotnet build
```

### Run application

```bash
dotnet run --project src/LinkScannerApp/LinkScannerApp.csproj
```

After startup, open the local address displayed in the console, for example:

```text
https://localhost:5001
```

or

```text
http://localhost:5000
```

The exact port may depend on local launch settings.

---

## Running tests

Run all tests:

```bash
dotnet test
```

Run only the test project:

```bash
dotnet test tests/LinkScanner.Tests/LinkScanner.Tests.csproj
```

---

## Docker

The project contains a Dockerfile in the web application project.

Example build command:

```bash
docker build -t linkscanner -f src/LinkScannerApp/Dockerfile .
```

Example run command:

```bash
docker run -p 8080:8080 linkscanner
```

Then open:

```text
http://localhost:8080
```

> Depending on the final Docker configuration, the exposed port may need to be adjusted.

---

## Current status

The project currently includes the core scanning flow, web interface, API endpoint, logging, rate limiting, configuration options and test project structure.

Planned improvements include AI-based classification, richer reporting, better UI presentation and deployment to a public environment.

---

## Roadmap

Planned or possible future improvements:

- [ ] Add ML.NET-based URL safety classifier.
- [ ] Train a model on labeled safe/suspicious URL data.
- [ ] Combine rule-based risk score with AI prediction.
- [ ] Add scan history.
- [ ] Add user accounts.
- [ ] Add public demo deployment.
- [ ] Add GitHub Actions CI pipeline.
- [ ] Add more unit and integration tests.
- [ ] Add OpenAPI/Swagger documentation.
- [ ] Add more screenshots and a short demo video.
- [ ] Add Google AdSense integration after deployment.
- [ ] Add more detailed explanation of each risk factor in the UI.

---

## Portfolio value

This project demonstrates:

- practical ASP.NET Core development,
- layered architecture,
- clean separation of responsibilities,
- working with untrusted user input,
- HTTP communication and redirect analysis,
- security-oriented thinking,
- structured logging,
- API design,
- configuration-driven application behavior,
- testable architecture,
- preparation for AI/ML integration.

---

## Author

**Patryk Mojs**  
GitHub: [PatrykMojs](https://github.com/PatrykMojs)

---

## Disclaimer

LinkScanner is a portfolio and educational project. It can help identify suspicious technical signals, but it should not be treated as a complete security product or a replacement for professional security tools.

