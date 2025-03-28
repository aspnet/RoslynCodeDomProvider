# Copyright (c) .NET Foundation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

######################################################################################################
## The main MS.CD.Providers.DotNetCompilerPlatform package uses targets files to do all this work   ##
## at build time. WebSite 'projects' can't take advantage of msbuild/targets files, so this package ##
## brings back the install.ps1 functionality that shoehorns bits into WebSite builds.               ##
######################################################################################################

param($installPath, $toolsPath, $package, $project)

$assemblyVersion = '$providerVersion$'

if ($project -eq $null) {
    $project = Get-Project
}

# This package is only for WebSite "projects."
# Fail noisily if trying to install on any other project type.
if ($project.Type -ne 'Web Site')
{
    throw "This package is intended only for 'Web Site' projects. Aborting install."
}

$compilerVersion = $package.Version
if($package.Versions -ne $null) { $compilerVersion = @($package.Versions)[0] }
$projectRoot = $project.Properties.Item('FullPath').Value
$binDirectory = Join-Path $projectRoot 'bin'
$roslynSubFolder = 'roslyn'
$packageDirectory = Split-Path $installPath
$compilerPackageName = "Microsoft.CodeDom.Providers.DotNetCompilerPlatform." + $compilerVersion
$compilerPackageDirectory = Join-Path $packageDirectory $compilerPackageName
$providerBits = Join-Path $compilerPackageDirectory  "lib\net472"
$roslynBits = Join-Path $compilerPackageDirectory "tools\Roslyn-$compilerVersion"
$csLanguageVersion = '7.3'
$vbLanguageVersion = 'default'

# Check for existence of the roslyn codedom provider package before continuing
if ((Get-Item $compilerPackageDirectory) -isnot [System.IO.DirectoryInfo])
{
    throw "The install.ps1 cannot find the installation location of package $compilerPackageName, or the pakcage is not installed correctly."
}

# Fill out the config entries for these code dom providers here. Using powershell to do
# this allows us to cache and restore customized attribute values from previous versions of
# this package in the upgrade scenario.
. "$PSScriptRoot\common.ps1"
$csCodeDomProvider = [CodeDomProviderDescription]@{
	TypeName="Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider";
	Assembly="Microsoft.CodeDom.Providers.DotNetCompilerPlatform";
    Version=$assemblyVersion;
    FileExtension=".cs";
    Parameters=@(
		[CompilerParameterDescription]@{ Name="language"; DefaultValue="c#;cs;csharp"; IsRequired=$true; IsProviderOption=$false  },
		[CompilerParameterDescription]@{ Name="warningLevel"; DefaultValue="4"; IsRequired=$true; IsProviderOption=$false  },
		[CompilerParameterDescription]@{ Name="compilerOptions"; DefaultValue="/langversion:" + $csLanguageVersion + " /nowarn:1659;1699;1701;612;618"; IsRequired=$false; IsProviderOption=$false  });
}
InstallCodeDomProvider $csCodeDomProvider
$vbCodeDomProvider = [CodeDomProviderDescription]@{
	TypeName="Microsoft.CodeDom.Providers.DotNetCompilerPlatform.VBCodeProvider";
	Assembly="Microsoft.CodeDom.Providers.DotNetCompilerPlatform";
    Version=$assemblyVersion;
    FileExtension=".vb";
    Parameters=@(
		[CompilerParameterDescription]@{ Name="language"; DefaultValue="vb;vbs;visualbasic;vbscript"; IsRequired=$true; IsProviderOption=$false  },
		[CompilerParameterDescription]@{ Name="warningLevel"; DefaultValue="4"; IsRequired=$true; IsProviderOption=$false  },
		[CompilerParameterDescription]@{ Name="compilerOptions"; DefaultValue="/langversion:" + $vbLanguageVersion + " /nowarn:41008,40000,40008 /define:_MYTYPE=\""Web\"" /optionInfer+"; IsRequired=$false; IsProviderOption=$false  });
}
InstallCodeDomProvider $vbCodeDomProvider

# We need to copy the provider assembly into the bin\ folder, otherwise
# Microsoft.VisualStudio.Web.Host.exe cannot find the assembly.
# However, users will see the error after they clean solutions.
New-Item $binDirectory -type directory -force | Out-Null
Copy-Item $providerBits\* $binDirectory -force | Out-Null

# Copy the Roslyn toolset into the website's bin folder as well.
$roslynSubDirectory = Join-Path $binDirectory $roslynSubFolder
New-Item $roslynSubDirectory -type directory -force | Out-Null
Copy-Item $roslynBits\* $roslynSubDirectory -force | Out-Null

# Generate a .refresh file for each dll/exe file.
Push-Location
Set-Location $projectRoot
$relativeAssemblySource = Resolve-Path -relative $roslynBits
Pop-Location

Get-ChildItem -Path $roslynSubDirectory | `
Foreach-Object {
    if  (($_.Extension -eq ".dll") -or ($_.Extension -eq ".exe")) {
        $refreshFile = $_.FullName
        $refreshFile += ".refresh"
        $refreshContent = Join-Path $relativeAssemblySource $_.Name    
        Set-Content $refreshFile $refreshContent
    }
}