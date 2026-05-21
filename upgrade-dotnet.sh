#!/bin/sh
set -e # exit on error

if ! git diff --cached --quiet; then
  echo "Aborted: staged changes detected in git"
  exit 1
fi

latest=`mise ls-remote dotnet-core | grep -P '^\d+\.\d+\.\d+$' | sort --version-sort | tail -n 1`
echo "Updating to dotnet-core@$latest"

# Update global.json (this is what dotnet actually honours)
sed -i "s|\"version\": \"[0-9.]*\"|\"version\": \"$latest\"|" global.json

# Extract major version (e.g., "10" from "10.0.100")
major_version=$(echo "$latest" | cut -d. -f1)
target_framework="net${major_version}.0"
echo "Updating TargetFramework to $target_framework"

for proj in EprRegulatorGateway EprRegulatorGateway.Test EprRegulatorGateway.IntegrationTests; do
  sed -i "s|<TargetFramework>net[0-9]*\.0</TargetFramework>|<TargetFramework>${target_framework}</TargetFramework>|g" "$proj/$proj.csproj"
done

if git diff --quiet global.json \
  EprRegulatorGateway/EprRegulatorGateway.csproj \
  EprRegulatorGateway.IntegrationTests/EprRegulatorGateway.IntegrationTests.csproj \
  EprRegulatorGateway.Test/EprRegulatorGateway.Test.csproj; then
  echo "Already on latest dotnet, nothing to do"
  exit 0
fi

echo "Running regression tests..."
mise install dotnet
dotnet test

echo "Committing upgrades..."
git commit --include global.json \
    --include EprRegulatorGateway/EprRegulatorGateway.csproj \
    --include EprRegulatorGateway.IntegrationTests/EprRegulatorGateway.IntegrationTests.csproj \
    --include EprRegulatorGateway.Test/EprRegulatorGateway.Test.csproj \
    --message "chore: Upgrade build to dotnet $latest"
