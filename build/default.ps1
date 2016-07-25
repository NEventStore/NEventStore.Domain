properties {
    $base_directory = Resolve-Path ..
    $publish_directory = "$base_directory\publish-net40"
    #$build_directory = "$base_directory\build"
    $src_directory = "$base_directory\src"
    $output_directory = "$base_directory\output"
    #$packages_directory = "$src_directory\packages"
    $sln_file = "$src_directory\NEventStore.Domain.sln"
    $target_config = "Release"
    #$framework_version = "v4.0"
    #$assemblyInfoFilePath = "$src_directory\AssemblyInfo.cs"

    $xunit_path = "$base_directory\bin\xunit.runners.1.9.1\tools\xunit.console.clr4.exe"
    $ilMergeModule.ilMergePath = "$base_directory\bin\ilmerge-bin\ILMerge.exe"
    $nuget_dir = "$src_directory\.nuget"
}

task default -depends Build

task Build -depends Clean, UpdateVersion, Compile, Test

task UpdateVersion {
	# a task to invoke GitVersion using the configuration file found in the 
	# root of the repository (GitVersionConfig.yaml)
	& ..\src\packages\GitVersion.CommandLine.3.5.4\tools\GitVersion.exe $base_directory /nofetch /updateassemblyinfo

    # outdated code that was using parameters passed to the build script
	#$version = Get-Version $assemblyInfoFilePath
    #"Version: $version"
	#$oldVersion = New-Object Version $version
	#$newVersion = New-Object Version ($oldVersion.Major, $oldVersion.Minor, $oldVersion.Build, $build_number)
	#Update-Version $newVersion $assemblyInfoFilePath
}

task Compile {
	EnsureDirectory $output_directory
	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$target_config /t:Clean }
	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$target_config /p:TargetFrameworkVersion=v4.0 }
}

task Test -depends RunUnitTests

task RunUnitTests {
	"Unit Tests"
	EnsureDirectory $output_directory
	Invoke-XUnit -Path $src_directory -TestSpec '*NEventStore.Domain.Tests.dll' `
    -SummaryPath $output_directory\unit_tests.xml `
    -XUnitPath $xunit_path
}

task Package -depends Build {
	mkdir $publish_directory\bin | out-null
    copy "$src_directory\NEventStore.Domain\bin\$target_config\NEventStore.Domain.???" "$publish_directory\bin"
}

task Clean {
	Clean-Item $publish_directory -ea SilentlyContinue
    Clean-Item $output_directory -ea SilentlyContinue
}

# todo: review this action, this is not going to work
#task NuGetPack -depends Package {
#    $versionString = Get-Version $assemblyInfoFilePath
#	$version = New-Object Version $versionString
#	$packageVersion = $version.Major.ToString() + "." + $version.Minor.ToString() + "." + $version.Build.ToString() + "." + $build_number.ToString()
#	"Package Version: $packageVersion"
#	gci -r -i *.nuspec "$nuget_dir" |% { .$nuget_dir\nuget.exe pack $_ -basepath $base_directory -o $publish_directory -version $packageVersion }
#}

function EnsureDirectory {
	param($directory)

	if(!(test-path $directory))
	{
		mkdir $directory
	}
}
