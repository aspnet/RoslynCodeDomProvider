# Copyright (c) .NET Foundation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param($installPath, $toolsPath, $package, $project)

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