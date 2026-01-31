########################################################################################################
# Filename:     build.ps1
# Author:       Laim McKenzie
# Purpose:      Build script for MyFicDB.  Generates licences.json, runs tests, checks for missing migrations
#               and then builds the docker
#               image and pushes it if everything is successful
# Repo:         github.com/mckenziesoftware/myficdb
# License:      GPL-3.0 license
# Notice:       If any parameters are changed or added, ensure you update CONTRIBUTING.md!
########################################################################################################
#################################### Example command ###################################################
#
#   This will push version 0.0.1 to lyeuhm/myficdb 
#
#   .\build.ps1 -Version 0.0.1 -ImageName MyFicDB -RegistryImage lyeuhm/myficdb -Push 
#
#   This will push version 0.0.1 to lyeuhm/myficdb and tag it as 0.0.1 and latest, as well as running
#   tests with coverage reporting
#
#   .\build.ps1 -Version 0.0.1 -ImageName MyFicDB -RegistryImage lyeuhm/myficdb -Push -Latest -Coverage
#
########################################################################################################

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [Parameter(Mandatory=$true)]
    [string]$ImageName,

    [Parameter(Mandatory=$true)]
    [string]$RegistryImage,
    [switch]$Push,
    [switch]$Latest,
    [switch]$Coverage
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Require-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found: $Name"
    }
}

function Invoke-External {
    param(
        [Parameter(Mandatory)]
        [string]$Command
    )

    Write-Host ">> $Command"
    iex $Command

    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $Command"
    }
}

function Custom-Info-Output {
    param(
        [Parameter(Mandatory)]
        [string]$Message,

        [switch]$Clear
    )

    if ($Clear) { Clear-Host }

    Write-Host $Message -ForegroundColor Green
}

Require-Command "dotnet"
Require-Command "docker"
Require-Command "git"
Require-Command "dotnet-project-licenses"

$gitSha = (git rev-parse --short HEAD).Trim()
$buildDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

Custom-Info-Output "Building Version=$Version GitSha=$gitSha BuildDate=$buildDate"

# Check for missing migrations
Custom-Info-Output "Checking for missing migrations..."

Invoke-External 'dotnet ef migrations has-pending-model-changes --project MyFicDB.Core --startup-project MyFicDB.Web'

# Generate licence file
Custom-Info-Output "Generating licences.json file in wwwroot"

dotnet-project-licenses `
  --input MyFicDB.sln `
  --output-directory "MyFicDB.Web/wwwroot/licences" `
  --outfile "licences.json"

# Run tests (with or without coverage)
if($Coverage)
{
    Custom-Info-Output "Running tests with coverage reports"

    Invoke-External 'dotnet test MyFicDB.sln --collect:"XPlat Code Coverage" --settings "coverlet.runsettings" --results-directory .\tests\results'
} 
else 
{
    Custom-Info-Output "Running tests without coverage reports"

    Invoke-External 'dotnet test MyFicDB.sln'
}

# Build docker image
Custom-Info-Output "Building docker image" -clear

$env:DOCKER_BUILDKIT = "1"
Invoke-External "docker build --file MyFicDB.Web/Dockerfile --build-arg APP_VERSION=$Version --build-arg GIT_SHA=$gitSha --build-arg BUILD_DATE=$buildDate -t ${ImageName}:$Version ."

# Tag for registry
docker tag "${ImageName}:$Version" "${RegistryImage}:$Version"

# Optional: Tag latest
if($Latest)
{
    docker tag "${ImageName}:$Version" "${RegistryImage}:latest"
}

if ($Push) {
    
    Custom-Info-Output "Pushing to docker as tagged"
    Invoke-External "docker push ${RegistryImage}:$Version"

    # Optional: Tag latest
    if($Latest)
    {
        Custom-Info-Output "Pushing to docker as latest"
        Invoke-External "docker push ${RegistryImage}:latest"
    }
}