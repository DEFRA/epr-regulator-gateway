# epr-regulator-gateway

Core delivery C# ASP.NET backend template.

* [Testing](#testing)
* [Backing services](#backing-services)
* [Running](#running)
* [Dependabot](#dependabot)


### Docker Compose

A Docker Compose template is in [compose.yml](compose.yml).

A local environment with:

- Localstack for AWS services (S3, SQS)
- Redis
- This service.
- A commented out frontend example.

```bash
docker compose up --build -d
```

A more extensive setup is available in [github.com/DEFRA/cdp-local-environment](https://github.com/DEFRA/cdp-local-environment)

### Testing

```bash
dotnet test
```

Integration tests run a full `WebApplication` against a `WireMock.Net` instance
of the backing services. The same mock servers can also be run as standalone
projects for local development — see [Backing services](#backing-services).

Tests inherit from `IntegrationTestBase`, which resets the mock's mappings on
every test so the pre-canned responses from disk don't leak into tests as a
mystery guest — each test must explicitly stub what it needs.

### Backing services

The gateway calls one downstream service:

- `BackendAccountService` — configured via `BackendAccountService:BaseUrl` etc.
  in [appsettings.json](EprRegulatorGateway/appsettings.json)

For local development, the [EprRegulatorGateway.MockBackendAccount](EprRegulatorGateway.MockBackendAccount)
project hosts a `WireMock.Net` standalone server on `http://localhost:8086`
serving pre-canned JSON from
[`Responses/BackendAccountApi/`](EprRegulatorGateway.MockBackendAccount/Responses/BackendAccountApi).
Files named `{guid}.json` are matched on the `userId` query string; `default.json`
is the catch-all.

Run the mock:

```bash
dotnet run --project EprRegulatorGateway.MockBackendAccount
```

### Running

The default launch profile points at the dev Azure environment. To point the
gateway at the local mock instead, use the `local-acc-backend` profile (which
overrides `BackendAccountService:BaseUrl` and clears the OAuth scope):

```bash
dotnet run --project EprRegulatorGateway --launch-profile local-acc-backend
```

The default profile is still:

```bash
dotnet run --project EprRegulatorGateway --launch-profile EprRegulatorGateway
```

### SonarCloud

Example SonarCloud configuration are available in the GitHub Action workflows.

### Dependabot

We have added an example dependabot configuration file to the repository. You can enable it by renaming
the [.github/example.dependabot.yml](.github/example.dependabot.yml) to `.github/dependabot.yml`


### About the licence

The Open Government Licence (OGL) was developed by the Controller of Her Majesty's Stationery Office (HMSO) to enable
information providers in the public sector to license the use and re-use of their information under a common open
licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few conditions.
