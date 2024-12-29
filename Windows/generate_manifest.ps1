# Define variables for project name and path
$projectName = $env:CURRENT_PROJECT
$projectPath = ".\" # Assuming the current directory contains the project files

# Extract information from.csproj file using PowerShell's XML parsing capabilities
$name = Get-Content "$projectPath\$projectName.csproj" | Select-Xml -XPath "//PropertyGroup/Product" | Select-Object -ExpandProperty Node.InnerText
$version = Get-Content "$projectPath\$projectName.csproj" | Select-Xml -XPath "//PropertyGroup/Version" | Select-Object -ExpandProperty Node.InnerText
$description = Get-Content "$projectPath\$projectName.csproj" | Select-Xml -XPath "//PropertyGroup/Description" | Select-Object -ExpandProperty Node.InnerText

# Generate JSON content
$manifest = @{
    name = $name
    version_number = $version
    website_url = ""
    description = $description
    dependencies = @("BepInEx-BepInExPack-5.4.2100", "TestAccount666-TestAccountCore-1.13.0", "Evaisa-LethalLib-0.16.2")
} | ConvertTo-Json

# Write JSON content to manifest file
$manifest | Out-File -FilePath ".\BuildOutput\manifest.json"

