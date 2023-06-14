## :warning: Project-less 'WebSite's should use [....DotNetCompilerPlatform.WebSites](https://www.nuget.org/packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform.WebSites) package :warning:
Version 4.X of this package has moved fully into an msbuild/targets model. But "Web Site"s - because they are "project-less" - have very limited support in msbuild and the package does not fully install like it did in the 3.X and earlier releases. Please use the [Microsoft.CodeDom.Providers.DotNetCompilerPlatform.WebSites](https://www.nuget.org/packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform.WebSites) package for proper integration with a "Web Site" project.

## Introduction
Replacement CodeDOM providers that use the new .NET Compiler Platform ("Roslyn") compiler as a service APIs. This provides support for new language features in systems using CodeDOM (e.g. ASP.NET runtime compilation) as well as improving the compilation performance of these systems.

Please see the blog [Enabling the .NET Compiler Platform (“Roslyn”) in ASP.NET applications](https://blogs.msdn.microsoft.com/webdev/2014/05/12/enabling-the-net-compiler-platform-roslyn-in-asp-net-applications/) 
for an introduction to Microsoft.CodeDom.Providers.DotNetCompilerPlatform. The [project github](https://github.com/aspnet/RoslynCodeDomProvider) also has the most up-to-date documentation of the various settings available for configuring this provider.

## Updates
+ #### Version 4.1.0 (preview1)
    - #### Drop install.ps1, Rely more on msbuild
        Nuget has moved on from install.ps1. We had one foot in the msbuild camp before, and one foot still in the install.ps1 camp. Time to just jump in with both feet. See the 'RoslynRegisterInConfig' setting description below.

    - #### Refreshed current compilers
        In keeping with the new versioning scheme for this project, the version has been revved to 4.1 to match the version of the compilers included.

    - #### No more old compilers
        Stop carrying old versions of compilers. If you upgrade to get new compilers, you get new compilers. The old compilers that might carry references to binaries that get flagged in security scans even though the binaries don't get copied to the ouput directory... they just won't be included in the package anymore.

    - #### .Net >= 4.7.2
        As a result of not keeping older compilers packaged in this project, we can no longer support versions before 4.7.2 because compiler versions 3.0 and newer only support 4.7.2+.

