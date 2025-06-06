$configurationdefault = "Release"
$artifacts = "../../artifacts"

$configuration = Read-Host 'Configuration to build [default: Release] ?'
if ($configuration -eq '') {
    $configuration = $configurationdefault
}
$runtests = Read-Host 'Run Tests (y / n) [default:n] ?'

# Install gitversion tool
dotnet tool restore

# Display minimal restore information
dotnet restore ./src/NEventStore.Domain.Core.sln --verbosity m

# GitVersion (for the main module)
Write-Host "Running GitVersion for the Project"
$str = dotnet tool run dotnet-gitversion /updateAssemblyInfo | out-string
$json = convertFrom-json $str
$nugetversion = $json.SemVer

# Now we need to patch the AssemblyInfo for submodules
Write-Host "Running GitVersion for the Dependencies"
dotnet tool run dotnet-gitversion ".\dependencies\NEventStore" /updateAssemblyInfo | Out-Null

# Build
Write-Host "Building: "$nugetversion" "$configuration
dotnet build ./src/NEventStore.Domain.Core.sln -c $configuration --no-restore /p:ContinuousIntegrationBuild=true

# Testing
if ($runtests -eq "y") {
    Write-Host "Executing Tests"
    dotnet test ./src/NEventStore.Domain.Core.sln -c $configuration --no-build
    Write-Host "Tests Execution Complated"
}

# NuGet packages
Write-Host "NuGet Packages creation"
# not working well, without a nuspec file ProjectReferences get the wrong version number
#dotnet pack ./src/NEventStore.Domain/NEventStore.Domain.Core.csproj -c $configuration --no-build -o $artifacts -p:PackageVersion=$nugetversion

# not working well, with a reference to a nuspec file, it seems i'm not able to pass in the $configuration to retrieve the correct files
#Write-Host "dotnet pack ./src/NEventStore.Domain/NEventStore.Domain.Core.csproj --no-build -c $configuration -o $artifacts -p:NuspecProperties=""pippo=$configuration;version=$nugetversion"""
#dotnet pack ./src/NEventStore.Domain/NEventStore.Domain.Core.csproj --no-build -c $configuration -o $artifacts -p:NuspecFile="" -p:NuspecProperties="pippo=$configuration;version=$nugetversion"

#Write-Host nuget pack ./src/NEventStore.Domain/NEventStore.Domain.Core.csproj -properties "version=$nugetversion;configuration=$configuration"
nuget pack ./src/.nuget/NEventStore.Domain.nuspec -properties "version=$nugetversion;configuration=$configuration" -OutputDirectory $artifacts -Symbols -SymbolPackageFormat snupkg