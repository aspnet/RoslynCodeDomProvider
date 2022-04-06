## Introduction
Replacement CodeDOM providers that use the new .NET Compiler Platform ("Roslyn") compiler as a service APIs. This provides support for new language features in systems using CodeDOM (e.g. ASP.NET runtime compilation) as well as improving the compilation performance of these systems.

Please see the blog [Enabling the .NET Compiler Platform (“Roslyn”) in ASP.NET applications](https://blogs.msdn.microsoft.com/webdev/2014/05/12/enabling-the-net-compiler-platform-roslyn-in-asp-net-applications/) 
for an introduction to Microsoft.CodeDom.Providers.DotNetCompilerPlatform.

## Updates
+ #### Version 4.1.0 (preview1)
    - #### Refreshed current compilers
        In keeping with the new versioning scheme for this project, the version has been revved to 4.1 to match the version of the compilers included.

    - #### No more old compilers
        Stop carrying old versions of compilers. If you upgrade to get new compilers, you get new compilers. The old compilers that might carry references to binaries that get flagged in security scans even though the binaries don't get copied to the ouput directory... they just won't be included in the package anymore.

    - #### .Net >= 4.7.2
        As a result of not keeping older compilers packaged in this project, we can no longer support versions before 4.7.2 because compiler versions 3.0 and newer only support 4.7.2+.

    - #### Drop install.ps1, Rely more on msbuild
        Nuget has moved on from install.ps1. We had one foot in the msbuild camp before, and one foot still in the install.ps1 camp. Time to just jump in with both feet. See the 'RoslynRegisterInConfig' setting description below.

+ #### Version 3.11.0 (preview1)
    - #### Refreshed compilers
        In keeping with the new versioning scheme for this project, the version has been revved to 3.11 to match the version of the compilers included.

    - #### Only support .Net >= 4.6.2
        Older versions of .Net are out of support, so this update also removes support for them and no longer carries the oldest version of the compiler tools that was used in previous versions.

    - #### Non-web apps and 'aspnet:RoslynCompilerLocation'
        The appSetting `aspnet:RoslynCompilerLocation` can still be used to point at a specific download of the Roslyn compiler tools, but this package is hopefully a little more forgiving when searching for a default location and should accomodate both web projects as well as non-web projects without requiring this setting.
      
+ #### Version 3.6.0
    - #### Refreshed compilers (and versioning)
        This is most likely the update everyone has been looking for. This package contains updated Roslyn bits for newer target frameworks. If your project is targeting 4.7.2 or above, this package will use `Microsoft.Net.Compilers` version 3.5 with your build. You might notice that we have revved our package version to match the most recent compiler version included. For target frameworks 4.6 through 4.7.1, the 2.10 version of compilers is used. (A slight update from 2.9 that shipped with our last package.) And as before, projects targeting 4.5.* will get version 1.3.2 of the compilers. (Note that the language version for 4.6 and above is set to "default", which means C# 7.3 max for full framework projects.)

    - #### Config restoration
        In the past, when updating or re-installing this package after re-targeting your project - the nuget package would overwrite your config entries for the codedom provider with the default options again. Borrowing a feature from Microsoft.Configuration.ConfigurationBuilders, the 3.5 packages now temporarily store existing config when uninstalling and attempt to restore it when installing instead of blindly writing defaults again. Unfortunately this won't help the 2.0* ==> 3.5 update scenario since 2.0* doesn't save configuration to the temp file. But future updates or retargeting from 3.5 will hopefully blow less custom configuration out of the water.

    - #### ProviderOptions for compilers
        Configuration options for these codedom providers has been a little haphazard in the past. Some things are set through environment variables, and some through appSettings. All such options apply to all codedom providers configured. This package still respects the old ways of setting those various config options, but also allows many of them to be set on individual codedom providers using the [providerOption](https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/file-schema/compiler/provideroption-element) collection in the config file. See the [Configurations](#Configurations) section below to see what options exist.

    - #### Turning off ASP.Net "magic"
        When this project was first started, it was intended as an extension for full-framework ASP.Net only. As such, it took the liberty of "massaging" some of the compiler options it was given before starting the compiler, and there was no way to prevent the modifications from happening. If the codedom providers in this package are created using the default constructor as ASP.Net uses, we still do the magic for compat reasons. If your code is calling directly into codedom providers from this package and passing compiler options in to the constructor, then the magic is turned off. This feature can be explicitly enabled or disabled using a new provider option. See the (Configurations)[#Configurations] section below for details.

    - #### dotnet buildable
        For the adventurous developer who likes to use `dotnet` to build their full-framework projects for whatever reason, some of the MSBuild tasks our package was creating were not compatible with that environment. This package update comes with custom MSBuild tasks that should work in both the `dotnet` and full MSBuild/VS environments.

+ #### Version 2.0.0
    - #### There is a **breaking change**?
        Before 2.0.0 version, Microsoft.CodeDom.Providers.DotNetCompilerPlatform nupkg references Microsoft.Net.Compilers nupkg in order to deploy the Roslyn compiler assemblies unto you application folder. In version 2.0.0 version, the dependency is removed. Instead, Microsoft.CodeDom.Providers.DotNetCompilerPlatform nupkg includes all the Roslyn compiler assemblies under tools folder.

    - #### What does that mean?
        When you build your project in Visual Studio, Visual Studio(msbuild) will use the Roslyn compiler shipped with it to compile the source code in your project. However, if Microsoft.Net.Compilers nupkg is installed in your project, it overrides the compiler location and Visual Studio(msbuild) will use the Roslyn compiler from Microsoft.Net.Compilers nupkg. This causes two problems. 1. When you install the latest Visual Studio update which always contains new Roslyn Compiler and you configure Visual Studio to use latest language feature. And you do use the latest language feature in your code, the intellisense works and IDE doesn't show any syntax error. However, when you build your project, you see some compilation error. This is because your project is still using the old version of Microsoft.Net.Compilers nupkg. 2. The Roslyn compiler shipped with Visual Studio is NGen'd which means it has better cold startup performance. So it takes more time to build the project, if the project references Microsoft.Net.Compilers nupkg.

    - #### What shall I do?
        If you are using Visual Studio 2017 with latest update, you should upgrade Microsoft.CodeDom.Providers.DotNetCompilerPlatform nupkg to 2.0.0 and **remove Microsoft.Net.Compilers nupkg from your project**.


## Configurations
Generally, command-line options for the codedom compilers can be specified using the `compilerOptions` attribute of the compiler when it is registered in configuration. There are however, a handful of options for controlling some behaviors of this package that are not command-line options. These options fall into two broad categories and can be set as follows:

### Build-time Options
+ **(V2) Specify the path to copy Roslyn compiler at build time** - When building projects, target files included by the Microsoft.CodeDom.Providers.DotNetCompilerPlatform nupkg will copy appropriate Roslyn compiler into bin\roslyn folder. With this setting, you can specify a custom path from which the build process will copy the Roslyn compiler, rather than using one of the pre-packaged Roslyn compilers.

    **Setting name** - RoslynToolPath

    **How to use it** - ```msbuild mysolution.sln /t:build /p:RoslynToolPath="[Roslyn compiler folder full path]"```

    **Use case** - In 2.0.0 version, Microsoft.CodeDom.Providers.DotNetCompilerPlatform nupkg removes the dependency on Microsoft.Net.Compilers nupkg. Instead, it embeds one version of Roslyn compiler inside the nupkg. It's possible that the embeded version of Roslyn compiler is not the one you want, so through this setting you can specify a version of Roslyn compiler at build time which will be copied to bin\roslyn folder.

+ **(V4) Skip copying Roslyn compiler at build time** - When building projects, target files will copy the appropriate binaries specified by the 'RoslynToolPath' setting described above and copy them into the project output for runtime use. This copy step can be skipped by using this project setting.

    **Setting name** - RoslynCopyToOutDir

    **How to use it** - ```msbuild mysolution.sln /t:build /p:RoslynCopyToOutDir="[true|false]"```

+ **(V4) Don't modify config at build time** - CodeDom providers are not magically picked up from referenced assemblies. They must be explicitly registered in config in order to be used. Prior to the Version 4 update, all modifications to config were performed via powershell scripts included in the nuget package. This powershell method worked with 'packages.config' apps, but does not work with 'PackageReference' apps. As more applications move towards the preferred 'PackageReference' way of doing things, we have updated our method of config registration to be an msbuild task instead of a powershell install script. We take care not to stomp over existing settings. But this step gets checked/performed on every build now instead of just on package install. Use this setting to skip the config update.

    **Setting name** - RoslynRegisterInConfig

    **How to use it** - ```msbuild mysolution.sln /t:build /p:RoslynRegisterInConfig="[true|false]"```

    **Use case** - This config-manipulation step happens on ever build. (Even designer builds.) We take care to be as non-invasive as possible, but if you want us to stay entirely hands-off and update your config registrations manually, this setting enables that.

### Run-time Options
+ **Specify the path to load Roslyn compiler at runtime** - When asp.net compiles the views at runtime or precompile time(using aspnet_compiler to precompile the web app), Microsoft.CodeDom.Providers.DotNetCompilerPlatform needs a path to load Roslyn compiler. This setting can be used to specify this loading path. 
        
    1. **Environment variable** - This is the first setting Microsoft.CodeDom.Providers.DotNetCompilerPlatform reads. If this setting is used, Microsoft.CodeDom.Providers.DotNetCompilerPlatform will ignore the other setting.    

        **Setting name** - ROSLYN_COMPILER_LOCATION

        **How to use it** - ```setx ROSLYN_COMPILER_LOCATION [Roslyn compiler folder full path]```

        **Use case** - This is a machine wide setting. If you want to control the Roslyn compiler version used on your machine or you don't want a copy of Roslyn compiler under every application, then you can use this setting.

    2. **AppSetting in config file** - This is the second option to specify the location of Roslyn compiler and it's only valid if the environment variable setting is not used.
        
        **Setting name** - aspnet:RoslynCompilerLocation

        **How to use it** - Add this appSetting into your config file   ``` <add key="aspnet:RoslynCompilerLocation" value="[Roslyn compiler folder full path]"/> ```
        
        **Use case** - This is a application level setting. If you have multiple projects in a solution and you want to use Microsoft.CodeDom.Providers.DotNetCompilerPlatform in several projects but you only want to install Microsoft.CodeDom.Providers.DotNetCompilerPlatform nupkg on one project. See [issue #25](https://github.com/aspnet/RoslynCodeDomProvider/issues/25).

    3. **Provider Option** - This is the third option to specify the location of the Roslyn compiler bits, and it's only valid if the previous two options are not used.

        **Setting name** - `<providerOption name="CompilerFullPath" value="[Roslyn compiler folder full path]" />`

        **How to use it** - Add this providerOption into your config file under the `system.codedom/compilers/compiler` to which you want it to apply.
        
        **Use case** - This is a provider level setting. If you want to use different roslyn deployments for the codedom providers registered for different languages.

    4. **Default setting** - The default location is bin\roslyn folder.

+ **Specify the TTL of Roslyn compiler server** - Microsoft.CodeDom.Providers.DotNetCompilerPlatform leverages Roslyn compiler server(VBCSCompiler.exe) to compile the generated code. In order to save system resources, VBCSCompiler.exe will be shutdown after idling 10 seconds in the server environment. However, in the development environment(running your web application from visual studio) the idle time is set to 15 mininutes. The reason behind this is to improve the startup performance of your web application when you run/debug the application in Visual Studio, since VBCSCompiler.exe takes several seconds to start if relevant Roslyn assemblies are not NGen'd. With this setting, you can control the idle time of VBCSCompiler.exe.

    1. **Environment variable** - This is the first setting Microsoft.CodeDom.Providers.DotNetCompilerPlatform reads. If this setting is used, Microsoft.CodeDom.Providers.DotNetCompilerPlatform will ignore the other setting.    

        **Setting name** - VBCSCOMPILER_TTL

        **How to use it** - ```setx VBCSCOMPILER_TTL [num of seconds]```

        **Use case** - When you develop your web application in Visual Studio, you don't modify and run your application very frequently. In this scenario, you may use this setting to shorten the idle time of VBCSCompiler.exe and let the process end earlier which can release some system resources.

    2. **Provider Option** - This is the second option to specify how long the Roslyn compiler server should stay alive, and it's only valid if the previous option is not used.

        **Setting name** - `<providerOption name="CompilerServerTimeToLive" value="[num of seconds]" />`

        **How to use it** - Add this providerOption into your config file under the `system.codedom/compilers/compiler` to which you want it to apply.
        
+ **Disable ASP.Net "Magic"** - If the helpful manipulation of `compilerOptions` for running smoothly in an ASP.Net environment is not so helpful for your environment, you can disable this and Microsoft.CodeDom.Providers.DotNetCompilerPlatform will get out of your way, using the `compilerOptions` provided to it with no additions or manipulations.

    **Provider Option** - This is the only option to specify whether `compilerOptions` manipulation should happen automatically or not.

    **Setting name** - `<providerOption name="UseAspNetSettings" value="[true|false]" />`

    **How to use it** - Add this providerOption into your config file under the `system.codedom/compilers/compiler` to which you want it to apply.

+ **Treat Warnings as Errors** - This `System.CodeDom.Compiler.CompilerParameters` property is unfortunately at a conflict with ability to tell the compiler how to behave directly with a `compilerOption`. Prior to v3.5, Microsoft.CodeDom.Providers.DotNetCompilerPlatform would always set this to false. Now developers have a choice in how to manage this conflict.

    **Provider Option** - `<providerOption name="WarnAsError" value="[true|false]" />` - The default is false.

    **How to use it** - Add this providerOption into your config file under the `system.codedom/compilers/compiler` to which you want it to apply.
