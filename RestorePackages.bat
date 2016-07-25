@echo off
cd .\dependencies\NEventStore\
call RestorePackages.bat
cd ..\..\
powershell -NoProfile -ExecutionPolicy unrestricted -Command "& .\build\RestorePackages.ps1"