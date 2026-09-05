Write-Host -fore green "=========================================="
dotnet build .
Write-Host -fore green "=========================================="
dotnet pack .
Write-Host -fore green "=========================================="
dotnet tool uninstall -g Krosoft.Github.CLI
Write-Host -fore green "=========================================="
dotnet tool install --global --add-source .\publish\ Krosoft.Github.CLI
Write-Host -fore green "=========================================="
dotnet tool list --global
Write-Host -fore green "=========================================="
krosoft-github help
Write-Host -fore green "=========================================="
