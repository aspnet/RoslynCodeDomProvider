## Web Site integration for 4.X DotNetCompilerPlatform CodeDom Provider
This is a support package to enable proper integration of the 4.X series of the [Microsoft.CodeDom.Providers.DotNetCompilerPlatform](https://www.nuget.org/packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform) package with project-less "Web Sites." This package does not contain any libraries or providers of it's own. It simply restores the old 'install.ps1' nuget functionality to its tightly coupled 'DotNetCompilerPlatform' package dependency. Powershell installation was the only way to integrate with "Web Sites" which have very limited msbuild support.

This package has an exact dependency on the _DotNetCompilerPlatform_ package of the same version.

This package will fail to install on non-"Web Site" projects.
