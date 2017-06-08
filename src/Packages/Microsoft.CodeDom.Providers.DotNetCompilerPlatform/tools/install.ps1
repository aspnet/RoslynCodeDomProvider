# Copyright (c) .NET Foundation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param($installPath, $toolsPath, $package, $project)

$compilerPackageName = 'Microsoft.Net.Compilers'
$roslynSubFolder = 'roslyn'

if ($project -eq $null) {
    $project = Get-Project
}

$libDirectory = Join-Path $installPath 'lib\net45'
$projectRoot = $project.Properties.Item('FullPath').Value
$binDirectory = Join-Path $projectRoot 'bin'

# We need to copy the provider assembly into the bin\ folder, otherwise
# Microsoft.VisualStudio.Web.Host.exe cannot find the assembly.
# However, users will see the error after they clean solutions.
New-Item $binDirectory -type directory -force | Out-Null
Copy-Item $libDirectory\* $binDirectory -force | Out-Null

# For Web Site, we need to copy the Roslyn toolset into
# the applicaiton's bin folder. 
# For Web Applicaiton project, this is done in csproj.
if ($project.Type -eq 'Web Site') {
    $packageDirectory = Split-Path $installPath

    # Get the installed Microsoft.Net.Compilers package.
    $compilerPackage = Get-Package -ProjectName $project.Name | Where-Object {$_.Id -eq $compilerPackageName}
    if ($compilerPackage -eq $null)
    {
        Write-Host "Package $compilerPackageName is not installed correctly."
        Write-Host 'The install.ps1 did not complete.'
        break
    }

    if($compilerPackage.Versions -eq $null)
    {
        $compilerVersion = $compilerPackage.Version
    }
    else
    {
		$compilerVersion = @($compilerPackage.Versions)[0]
    }

    $compilerPackageFolderName = $compilerPackage.Id + "." + $compilerVersion
    $compilerPackageDirectory = Join-Path $packageDirectory $compilerPackageFolderName
    if ((Get-Item $compilerPackageDirectory) -isnot [System.IO.DirectoryInfo])
    {
        Write-Host "The install.ps1 cannot find the installation location of package $compilerPackageName, or the pakcage is not installed correctly."
        Write-Host 'The install.ps1 did not complete.'
        break
    }

    $compilerPackageToolsDirectory = Join-Path $compilerPackageDirectory 'tools'
    $roslynSubDirectory = Join-Path $binDirectory $roslynSubFolder
    New-Item $roslynSubDirectory -type directory -force | Out-Null
    Copy-Item $compilerPackageToolsDirectory\* $roslynSubDirectory -force | Out-Null

    # Generate a .refresh file for each dll/exe file.
    Push-Location
    Set-Location $projectRoot
    $relativeAssemblySource = Resolve-Path -relative $compilerPackageToolsDirectory
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
}