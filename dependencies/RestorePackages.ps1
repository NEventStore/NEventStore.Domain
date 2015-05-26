$path = $(Split-Path -parent $MyInvocation.MyCommand.path)

gci $path\NEventStore\src -Recurse "packages.config" |% {
	"Restoring " + $_.FullName
	& $path\NEventStore\src\.nuget\nuget.exe i $_.FullName -o $path\NEventStore\src\packages
}