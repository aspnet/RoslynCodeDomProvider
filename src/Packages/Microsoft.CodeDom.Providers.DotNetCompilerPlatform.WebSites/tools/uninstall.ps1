# Copyright (c) .NET Foundation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

######################################################################################################
## The main MS.CD.Providers.DotNetCompilerPlatform package uses targets files to do all this work   ##
## at build time. WebSite 'projects' can't take advantage of msbuild/targets files, so this package ##
## brings back the install.ps1 functionality that shoehorns bits into WebSite builds.               ##
######################################################################################################

param($installPath, $toolsPath, $package, $project)

# This package is only for WebSite "projects"
if ($project.Type -eq 'Web Site') {

    # First save the code dom compiler declarations off to a temp file so they can be restored
    # in the event that this is a package upgrade scenario.
    . "$PSScriptRoot\common.ps1"
    UninstallCodeDomProvider "Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider, Microsoft.CodeDom.Providers.DotNetCompilerPlatform"
    UninstallCodeDomProvider "Microsoft.CodeDom.Providers.DotNetCompilerPlatform.VBCodeProvider, Microsoft.CodeDom.Providers.DotNetCompilerPlatform"


    # Then remove the compiler bits from the bin directory
    $roslynSubFolder = 'roslyn'

    if ($project -eq $null) {
        $project = Get-Project
    }

    $projectRoot = $project.Properties.Item('FullPath').Value
    $binDirectory = Join-Path $projectRoot 'bin'
    $targetDirectory = Join-Path $binDirectory $roslynSubFolder

    if (Test-Path $targetDirectory) {
        Get-Process -Name "VBCSCompiler" -ErrorAction SilentlyContinue | Stop-Process -Force -PassThru -ErrorAction SilentlyContinue | Wait-Process
        Remove-Item $targetDirectory -Force -Recurse -ErrorAction SilentlyContinue
    }
}