NEventStore.Domain
===

NEventStore.Domain is a recipe for implementing event sourced domain objects with NEventStore.

NEventStore.Domain currently supports:

- dotnet framework 4.5
- dotnet standard 2.0, dotnet core 2.0 

Build Status
===

Branches:

- master [![Build status](https://ci.appveyor.com/api/projects/status/fx08cfosfajnq6np/branch/master?svg=true)](https://ci.appveyor.com/project/AGiorgetti/neventstore-domain/branch/master)
- develop [![Build status](https://ci.appveyor.com/api/projects/status/fx08cfosfajnq6np/branch/develop?svg=true)](https://ci.appveyor.com/project/AGiorgetti/neventstore-domain/branch/develop)
- feature/dotnetcore [![Build status](https://ci.appveyor.com/api/projects/status/fx08cfosfajnq6np/branch/feature/dotnetcore?svg=true)](https://ci.appveyor.com/project/AGiorgetti/neventstore-domain/branch/feature/dotnetcore)

Information
===

### Developed with:

[![Resharper](http://neventstore.org/images/logo_resharper_small.gif)](http://www.jetbrains.com/resharper/)
[![TeamCity](http://neventstore.org/images/logo_teamcity_small.gif)](http://www.jetbrains.com/teamcity/)
[![dotCover](http://neventstore.org/images/logo_dotcover_small.gif)](http://www.jetbrains.com/dotcover/)
[![dotTrace](http://neventstore.org/images/logo_dottrace_small.gif)](http://www.jetbrains.com/dottrace/)

## How to Build (locally)

- clone the repository with:

```
git clone --recursive https://github.com/NEventStore/NEventStore.Domain.git
```

or

```
git clone https://github.com/NEventStore/NEventStore.Domain.git
git submodule update
```

- execute 'RestorePackages.bat' to restore the NuGet packages for the main project and any submodule

```
RestorePackages.bat
```

To build the project locally use the following scripts:

"RestorePackages.bat": let NuGet download all the packages it needs, you need to do this at least once to download all the tools needed to compile the library outside Visual Studio.

"Build.RunTask.bat TaskName": executes the specified Task, available tasks are:

- Clean - clean up the output and publish folders
- UpdateVersion - update the assembly version info files 
- Compile - compiles the solution
- Test - executes unit tests
- Build - executes Clean, UpdateVersion, Compile and Test 
- Package - executes Build and publishes the artifacts

## How to contribute

### Git-Flow

This repository uses GitFlow to develop, if you are not familiar with GitFlow you can look at the following link.

* [A Successful Git Branching Model](http://nvie.com/posts/a-successful-git-branching-model/)
* [Git Flow Cheat-Sheet](http://danielkummer.github.io/git-flow-cheatsheet/)
* [Git Flow for GitHub](https://datasift.github.io/gitflow/GitFlowForGitHub.html)

### Installing and configuring Git Flow

Probably the most straightforward way to install GitFlow on your machine is installing [Git Command Line](https://git-for-windows.github.io/), then install the [Visual Studio Plugin for Git-Flow](https://visualstudiogallery.msdn.microsoft.com/27f6d087-9b6f-46b0-b236-d72907b54683). This plugin is accessible from the **Team Explorer** menu and allows you to install GitFlow extension directly from Visual Studio with a simple click. The installer installs standard GitFlow extension both for command line and for Visual Studio Plugin.

Once installed you can use GitFlow right from Visual Studio or from Command line, which one you prefer.

### Build machine and GitVersion

Build machine uses [GitVersion](https://github.com/GitTools/GitVersion) to manage automatic versioning of assemblies and Nuget Packages. You need to be aware that there are a rule that does not allow you to directly commit on master, or the build will fail. 

A commit on master can be done only following the [Git-Flow](http://nvie.com/posts/a-successful-git-branching-model/) model, as a result of a new release coming from develop, or with an hotfix. 

### Quick Info for NEventstore projects

Just clone the repository and from command line checkout develop branch with 

```
git checkout develop
```

Then from command line run GitFlow initialization scripts

```
git flow init
```

You can leave all values as default. Now your repository is GitFlow enabled.

### Note on Nuget version on Nuspec

While we are on develop branch, (suppose we just bumped major number so the driver version number is 6.0.0-unstablexxxx), we need to declare that this persistence driver depends from a version greater than the latest published. If the latest version of NEventStore 5.x.x wave iw 5.4.0 we need to declare this package dependency as

(5.4, 7)

This means, that we need a NEventStore greater than the latest published, but lesser than the next main version. This allows version 6.0.0-unstable of NEventStore to satisfy the dependency. We remember that prerelease package are considered minor than the stable package. Es.

5.4.0
5.4.1
6.0.0-unstable00001
6.0.0

