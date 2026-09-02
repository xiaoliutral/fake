$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition

Set-Location $scriptPath
Set-Location ".\packages"

dotnet nuget push *.* -s https://baget.ideship.com/v3/index.json --skip-duplicate