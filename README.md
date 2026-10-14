# epr-regulator-gateway

This repo is no longer in use and is being archived ([AMCR-469](https://eaflood.atlassian.net/browse/AMCR-469), October 2026).

It was a prototype backend-for-frontend (BFF) that let the regulator frontend prototype ([epr-regulator-frontend](https://github.com/DEFRA/epr-regulator-frontend), also archived) fetch regulator account data from `epr-backend-account-microservice`.

## What replaced it

- **Regulator account data:** [waste-regulator-dashboard-fe](https://github.com/DEFRA/waste-regulator-dashboard-fe) calls the account service directly, without this gateway.
- **Regulator path-based routing** (dashboard, certificate of compliance, organisations): handled by [regulators-waste-proxy](https://github.com/DEFRA/regulators-waste-proxy), a separate YARP service. This repo was not the waste proxy.

If you find config or docs that still mention `epr-regulator-gateway`, they are out of date.
