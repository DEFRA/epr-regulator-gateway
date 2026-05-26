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

The gateway talks to two downstream hosts:

- `BackendAccountService` — the account API; configured via
  `BackendAccountService:BaseUrl` in [appsettings.json](EprRegulatorGateway/appsettings.json)
- The OAuth token endpoint — configured via `BackendAccountService:TokenEndpoint`
  (a separate host in production, e.g. Azure AD)

For local development, each has its own runnable WireMock project:

- [EprRegulatorGateway.MockBackendAccount](EprRegulatorGateway.MockBackendAccount)
  on `http://localhost:8086` — serves canned JSON from
  [`Responses/BackendAccountApi/`](EprRegulatorGateway.MockBackendAccount/Responses/BackendAccountApi).
  Files named `{guid}.json` match on the `userId` query string; `default.json`
  is the catch-all.
- [EprRegulatorGateway.MockTokenServer](EprRegulatorGateway.MockTokenServer)
  on `http://localhost:8087` — accepts `POST /token` and returns the canned
  OAuth response from
  [`Responses/TokenApi/default.json`](EprRegulatorGateway.MockTokenServer/Responses/TokenApi/default.json).

Run them in separate terminals:

```bash
dotnet run --project EprRegulatorGateway.MockBackendAccount
dotnet run --project EprRegulatorGateway.MockTokenServer
```

### Running

The default launch profile points at the dev Azure environment. To point the
gateway at the local mocks instead, use the `local-acc-backend` profile (which
overrides `BackendAccountService:BaseUrl`, points `TokenEndpoint` at the mock
token server, and clears the OAuth scope so the token fetch is skipped):

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
