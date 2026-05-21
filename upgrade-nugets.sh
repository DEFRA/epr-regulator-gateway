#!/bin/sh
set -e # exit on error

if ! git diff --cached --quiet; then
  echo "Aborted: staged changes detected in git"
  exit 1
fi

if ! command -v dotnet-outdated
then
	# https://github.com/dotnet-outdated/dotnet-outdated
	dotnet tool install --global dotnet-outdated-tool
fi

dotnet outdated -u

if git diff --quiet global.json \
  EprRegulatorGateway/EprRegulatorGateway.csproj \
  EprRegulatorGateway.IntegrationTests/EprRegulatorGateway.IntegrationTests.csproj \
  EprRegulatorGateway.Test/EprRegulatorGateway.Test.csproj; then
  echo "No package updates available, nothing to do"
  exit 0
fi

echo "Running regression tests..."
dotnet test

echo "Committing upgrades..."
git commit --include global.json \
    --include EprRegulatorGateway/EprRegulatorGateway.csproj \
    --include EprRegulatorGateway.IntegrationTests/EprRegulatorGateway.IntegrationTests.csproj\
    --include EprRegulatorGateway.Test/EprRegulatorGateway.Test.csproj\
    --message "chore: Dependency upgrades (nuget)"
