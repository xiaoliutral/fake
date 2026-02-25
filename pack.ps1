# 获取脚本所在的目录
$currentDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
Write-Host "Current Dir: $currentDirectory"
Set-Location -Path $currentDirectory

# 删除 packages 目录下的所有文件
Remove-Item -Path "$currentDirectory\packages\*" -Recurse -Force

# 还原解决方案
dotnet restore "$currentDirectory\Fake.sln"

# 统一打包解决方案（仅打包 IsPackable 的项目）
dotnet pack "$currentDirectory\Fake.sln" -c Debug -p:Version=8.0.5 -p:RunAnalyzers=false -o "$currentDirectory\packages" --no-restore

Write-Host "Executing full completed!"
