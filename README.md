# Overview

Tool for creating Visual Studio solution file(sln) from [MSBuild.SolutionSdk](https://github.com/JeffCyr/MSBuild.SolutionSdk).
This project aims to prevent conflicts with sln files by add `*.sln` to `.gitignore`.

# How to use

## Requirements

* msbuild after 15.3
    * dotnet-sdk 2.1.300 or later
    * Visual Studio 2017 or later

## Steps from scratch

1. creating any project
    `dotnet new [projecttype]` or by Visual Studio
2. creating MSBuild.SolutionSdk project file like following(called `slnproj`)
    ```
    <Project Sdk="MSBuild.SolutionSdk/0.1.0>
    </Project>
    ```
3. creating MSBuild.SolutionSdk.SlnGen project file like following(called `slngenproj`)
    ```
    <Project Sdk="MSBuild.SolutionSdk.SlnGen/0.1.0"/>
    ```
4. execute generating sln target by following command
    * by dotnet-sdk, execute `dotnet msbuild [path/to/slngenproj]`
    * by Visual Studio, execute `msbuild [path/to/slngenproj]`
    * **warning: if `sln` file have been already existing, it will be overwritten.**

Then you can see sln files in next to `slnproj` files.

## Configuration and Platform Mapping

if you want to map configuration and platform to different name(solution platform setting="AnyCPU", but individual project platform setting="Win32"),
you can do this by editing `slnproj` file like following.

note: wild card is allowed in project path.

### Mapping configuration

```xml
<Project>
    <ItemGroup>
        <Project Update="path/to/your/proj" Condition="'$(Configuration)' == 'Debug'">
            <Configuration>Abc</Configuration>
        </Project>
    </ItemGroup>
</Project>
```

### Mapping platform

```xml
<Project>
    <ItemGroup>
        <Project Update="path/to/your/proj" Condition="'$(Platform)' == 'AnyCPU'">
            <Platform>Win32</Platform>
        </Project>
    </ItemGroup>
</Project>
```

# References

## available MSBuild items in `slngenproj`

followings can be added to `ItemGroup` in `slngenproj`.

### SlnProj

This affects what `slnproj` file will be processed.
defaults are:
* `**/*.slnproj`
    * It means all `*.slnproj` under `slngenproj` is processed.

### SolutionConfiguration

This affects what `Configuration` property is specified when processing `slnproj`.
defaults are:
* Debug
* Release

### SolutionPlatform

This affects what `Platform` property is specified when processing `slnproj`.
defaults are:
* AnyCPU

## available MSBuild properties in `slngenproj`

followings can be added to `PropertyGroup` in `slngenproj`

### EnableDefaultProjectItems

If `true`(ignore case) is set, default `SlnProj` inclusion is disabled.

## affected MSBuild items in `slnproj`

following items in slnproj are affected to slngen behavior

### Project

If added, slngen processing for generating sln.
This can be added the following metadata:

* Configuration
* Platform
* SolutionFolder
    * logical solution folder path separated by `/` or `\`
* DependsOn
    * project dependency(projects must be built before project build.)

### SolutionItem

If added, solution item is added to generated sln.
This can be added the following metadata.

* SolutionFolder
    * logical solution folder path separated by `/` or `\`
    * if not set, all items are located in "Solution Items"
