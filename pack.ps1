# 获取脚本所在的目录
$currentDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
Write-Host "Current Dir: $currentDirectory"
Set-Location -Path $currentDirectory

# 删除 packages 目录下的所有文件
Remove-Item -Path "$currentDirectory\packages\*" -Recurse -Force

# 切换到 ../src 目录
Set-Location -Path "$currentDirectory\src"

# 获取当前目录下的所有子目录
$directories = Get-ChildItem -Directory

# 使用 Start-Job 来并行执行任务
$jobs = @()
foreach ($dir in $directories) {
    $jobs += Start-Job -ScriptBlock {
        param($dirPath, $outputPath)
        Set-Location $dirPath
        Write-Host "Executing command in $dirPath"
        dotnet build
        dotnet pack /p:Version=8.0.0-preview18 -c Debug --output "$outputPath"
    } -ArgumentList $dir.FullName, "$currentDirectory\packages"
}

# 等待所有作业完成
$jobs | ForEach-Object { 
    $_ | Wait-Job | Receive-Job
    Remove-Job $_
}

Write-Host "Executing full completed!"