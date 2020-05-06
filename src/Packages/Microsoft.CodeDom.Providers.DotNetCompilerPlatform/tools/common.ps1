# Copyright (c) .NET Foundation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


##
## Assigning a "DefaultValue" to a ParameterDescription will result in emitting this parameter when
## writing out a default compiler declaration.
##
## Setting IsRequired to $true will require the attribute to be set on all declarations in config.
##
Add-Type @"
	using System;
	
	public class CompilerParameterDescription {
		public string Name;
		public string DefaultValue;
		public bool IsRequired;
		public bool IsProviderOption;
	}

	public class CodeDomProviderDescription {
		public string TypeName;
		public string Assembly;
		public string Version;
		public string FileExtension;
		public CompilerParameterDescription[] Parameters;
	}
"@

function InstallCodeDomProvider($providerDescription) {
	##### Update/Rehydrate config declarations #####
	$config = ReadConfigFile
	$rehydratedCount = RehydrateOldDeclarations $config $providerDescription
	$updatedCount = UpdateDeclarations $config $providerDescription

	##### Add the default provider if it wasn't rehydrated above
	$defaultProvider = $config.xml.configuration["system.codedom"].compilers.compiler | where { $_.extension -eq $providerDescription.FileExtension }
	if ($defaultProvider -eq $null) { AddDefaultDeclaration $config $providerDescription }
	SaveConfigFile $config
}

function UninstallCodeDomProvider($providerType) {
	##### Dehydrate config declarations #####
	$config = ReadConfigFile
	DehydrateDeclarations $config $providerType | Out-Null
	SaveConfigFile $config
}

function GetConfigFileName() {
	# Try web.config first. Then fall back to app.config.
	$configFile = $project.ProjectItems | where { $_.Name -ieq "web.config" }
	if ($configFile -eq $null) { $configFile = $project.ProjectItems | where { $_.Name -ieq "app.config" } }
	$configPath = $configFile.Properties | where { $_.Name -ieq "LocalPath" }
    if ($configPath -eq $null) { $configPath = $configFile.Properties | where { $_.Name -ieq "FullPath" } }
	return $configPath.Value
}

function GetTempFileName() {
	$uname = $project.UniqueName
	if ([io.path]::IsPathRooted($uname)) { $uname = $project.Name }
	return [io.path]::Combine($env:TEMP, "Microsoft.CodeDom.Providers.DotNetCompilerPlatform.Temp", $uname + ".xml")
}

function ReadConfigFile() {
	$configFile = GetConfigFileName
	$configObj = @{ fileName = $configFile; xml = (Select-Xml -Path "$configFile" -XPath /).Node }
	$configObj.xml.PreserveWhitespace = $true
	return $configObj
}

function DehydrateDeclarations($config, $typeName) {
	$tempFile = GetTempFileName
	$xml
	$count = 0

	if ([io.file]::Exists($tempFile)) {
		$xml = (Select-Xml -Path "$tempFile" -XPath /).Node
		$xml.PreserveWhitespace = $true
	} else {
		$xml = New-Object System.Xml.XmlDocument
		$xml.PreserveWhitespace = $true
		$dd = $xml.CreateElement("driedDeclarations")
		$xml.AppendChild($dd) | Out-Null
	}

	foreach ($rec in $config.xml.configuration["system.codedom"].compilers.compiler  | where { IsSameType $_.type $typeName }) {
		# Remove records from config.
		$config.xml.configuration["system.codedom"].compilers.RemoveChild($rec) | Out-Null

		# Add the record to the temp stash. Don't worry about duplicates.
		AppendChildNode $xml.ImportNode($rec, $true) $xml.DocumentElement
		$count++
	}

	# Save the dehydrated declarations
	$tmpFolder = Split-Path $tempFile
	md -Force $tmpFolder | Out-Null
	$xml.Save($tempFile) | Out-Null
	return $count
}

function RehydrateOldDeclarations($config, $providerDescription) {
	$tempFile = GetTempFileName
	if (![io.file]::Exists($tempFile)) { return 0 }

	$count = 0
	$xml = (Select-Xml -Path "$tempFile" -XPath /).Node
	$xml.PreserveWhitespace = $true

	foreach($rec in $xml.driedDeclarations.compiler | where { IsSameType $_.type ($providerDescription.TypeName + "," + $providerDescription.Assembly) }) {
		# Remove records that match type, even if we don't end up rehydrating them.
		$xml.driedDeclarations.RemoveChild($rec) | Out-Null

		# Skip if an existing record of the same file extension already exists.
		$existingRecord = $config.xml.configuration["system.codedom"].compilers.compiler | where { $_.extension -eq $rec.extension }
		if ($existingRecord -ne $null) { continue }

		# Bring the record back to life
		AppendChildNode $config.xml.ImportNode($rec, $true) $config.xml.configuration["system.codedom"]["compilers"]
		$count++
	}

	# Make dried record removal permanent
	$xml.Save($tempFile) | Out-Null

	return $count
}

function UpdateDeclarations($config, $providerDescription) {
	$count = 0

	foreach ($provider in $config.xml.configuration["system.codedom"].compilers.compiler | where { IsSameType $_.type ($providerDescription.TypeName + "," + $providerDescription.Assembly) }) {
		# Count the existing declaration as found
		$count++

		# Update type
		$provider.type = "$($providerDescription.TypeName), $($providerDescription.Assembly), Version=$($providerDescription.Version), Culture=neutral, PublicKeyToken=31bf3856ad364e35"

		# Add default attributes if they are required and not already present
		foreach ($p in $providerDescription.Parameters | where { ($_.IsRequired -eq $true) -and ($_.IsProviderOption -eq $false) }) {
			if ($provider.($p.Name) -eq $null) {
				if ($p.DefaultValue -eq $null) {
					Write-Host "Failed to add parameter to '$($provider.name)' codeDom provider: '$($p.Name)' is required, but does not have a default value."
					return
				}
				$attr = $config.xml.CreateAttribute($p.Name)
				$attr.Value = $p.DefaultValue
				$provider.Attributes.InsertBefore($attr, $provider.Attributes["type"]) | Out-Null
			}
		}

		# Do the same thing for default providerOptions if not already present
		foreach ($p in $providerDescription.Parameters | where { ($_.IsRequired -eq $true) -and ($_.IsProviderOption -eq $true)}) {
			$existing = $provider.providerOption | where { $_.name -eq $p.Name }
			if ($existing -eq $null) {
				if ($p.DefaultValue -eq $null) {
					Write-Host "Failed to add providerOption to '$($provider.name)' codeDom provider: '$($p.Name)' is required, but does not have a default value."
					return
				}
				$po = $config.xml.CreateElement("providerOption")
				$po.SetAttribute("name", $p.Name)
				$po.SetAttribute("value", $p.DefaultValue)
				AppendChildNode $po $provider 4
			}
		}
	}

	return $count
}

function AddDefaultDeclaration($config, $providerDescription) {
	$dd = $config.xml.CreateElement("compiler")

	# file extension first
	$dd.SetAttribute("extension", $providerDescription.FileExtension)

	# everything else in the middle
	foreach ($p in $providerDescription.Parameters) {
		if ($p.IsRequired -and ($p.DefaultValue -eq $null)) {
			Write-Host "Failed to add default declaration for code dom extension '$($providerDescription.FileExtension)': '$($p.Name)' is required, but does not have a default value."
			return
		}

		if ($p.DefaultValue -ne $null) {
			if ($p.IsProviderOption -eq $true) {
				$po = $config.xml.CreateElement("providerOption")
				$po.SetAttribute("name", $p.Name)
				$po.SetAttribute("value", $p.DefaultValue)
				AppendChildNode $po $dd 4
			} else {
				$dd.SetAttribute($p.Name, $p.DefaultValue)
			}
		}
	}

	# type last
	$dd.SetAttribute("type", "$($providerDescription.TypeName), $($providerDescription.Assembly), Version=$($providerDescription.Version), Culture=neutral, PublicKeyToken=31bf3856ad364e35")

	AppendChildNode $dd $config.xml.configuration["system.codedom"]["compilers"]
}

function AppendChildNode($provider, $parent, $indentLevel = 3) {
	$lastSibling = $parent.ChildNodes | where { $_ -isnot [System.Xml.XmlWhitespace] } | select -Last 1
	if ($lastSibling -ne $null) {
		# If not the first child, then copy the whitespace convention of the existing child
		$ws = "";
		$prev = $lastSibling.PreviousSibling | where { $_ -is [System.Xml.XmlWhitespace] }
		while ($prev -ne $null) {
			$ws = $prev.data + $ws
			$prev = $prev.PreviousSibling | where { $_ -is [System.Xml.XmlWhitespace] }
		}
		$parent.InsertAfter($provider, $lastSibling) | Out-Null
		if ($ws.length -gt 0) { $parent.InsertAfter($parent.OwnerDocument.CreateWhitespace($ws), $lastSibling) | Out-Null }
		return
	}

	# Add on a new line with indents. Make sure there is no existing whitespace mucking this up.
	foreach ($exws in $parent.ChildNodes | where { $_ -is [System.Xml.XmlWhitespace] }) { $parent.RemoveChild($exws) }
	$parent.AppendChild($parent.OwnerDocument.CreateWhitespace("`r`n")) | Out-Null
	$parent.AppendChild($parent.OwnerDocument.CreateWhitespace("  " * $indentLevel)) | Out-Null
	$parent.AppendChild($provider) | Out-Null
	$parent.AppendChild($parent.OwnerDocument.CreateWhitespace("`r`n")) | Out-Null
	$parent.AppendChild($parent.OwnerDocument.CreateWhitespace("  " * ($indentLevel - 1))) | Out-Null
}

function SaveConfigFile($config) {
	$config.xml.Save($config.fileName)
}

function IsSameType($typeString1, $typeString2) {

	if (($typeString1 -eq $null) -or ($typeString2 -eq $null)) { return $false }

	# First check the type
	$t1 = $typeString1.Split(',')[0].Trim()
	$t2 = $typeString2.Split(',')[0].Trim()
	if ($t1 -cne $t2) { return $false }

	# Then check for assembly match if possible
	$a1 = $typeString1.Split(',')[1]
	$a2 = $typeString2.Split(',')[1]
	if (($a1 -ne $null) -and ($a2 -ne $null)) {
		return ($a1.Trim() -eq $a2.Trim())
	}

	# Don't care about assembly. Match is good.
	return $true
}
