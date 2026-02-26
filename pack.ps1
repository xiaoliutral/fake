$ErrorActionPreference = "Stop"

# 获取脚本所在的目录
$currentDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
Write-Host "Current Dir: $currentDirectory"
Set-Location -Path $currentDirectory

$packagesDir = "$currentDirectory\packages"

# 删除 packages 目录下的所有文件
if (Test-Path $packagesDir)
{
    Remove-Item -Path "$packagesDir\*" -Recurse -Force -ErrorAction SilentlyContinue
}

# 还原解决方案
dotnet restore "$currentDirectory\Fake.sln"

# 先统一构建（避免 pack 时出现输出文件未生成）
dotnet build "$currentDirectory\Fake.sln" `
    -c Release `
    --no-restore `
    -p:RunAnalyzers=false `
    -p:GeneratePackageOnBuild=false

# 统一打包解决方案（仅打包 IsPackable 的项目）
dotnet pack "$currentDirectory\Fake.sln" `
    -c Release `
    --no-build `
    --no-restore `
    -p:Version=8.0.5.7 `
    -p:RunAnalyzers=false `
    -p:GeneratePackageOnBuild=false `
    -o "$packagesDir"

Write-Host "Executing full completed!"
