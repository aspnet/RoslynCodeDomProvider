## Introduction
Replacement CodeDOM providers that use the new .NET Compiler Platform ("Roslyn") compiler as a service APIs. This provides support for new language features in systems using CodeDOM (e.g. ASP.NET runtime compilation) as well as improving the compilation performance of these systems.

Please see the blog [Enabling the .NET Compiler Platform (“Roslyn”) in ASP.NET applications](https://blogs.msdn.microsoft.com/webdev/2014/05/12/enabling-the-net-compiler-platform-roslyn-in-asp-net-applications/) 
for an introduction to Microsoft.CodeDom.Providers.DotNetCompilerPlatform.


## Breaking changes
+ #### Version 1.1.0
    - #### What is changed?

        Before 1.1.0 version, Microsoft.CodeDom.Providers.DotNetCompilerPlatform nupkg references Microsoft.Net.Compilers nupkg in order to deploy the Roslyn compiler assemblies unto you application folder. In version 1.1.0 version, the dependency is removed. Instead, Microsoft.CodeDom.Providers.DotNetCompilerPlatform nupkg includes all the Roslyn compiler assemblies under tools folder.
    - #### What does that mean?

        When you build your project in Visual Studio, Visual Studio(msbuild) will use the Roslyn compiler shipped with it to compile the source code in your project. However, if Microsoft.Net.Compilers nupkg is installed in your project, it overrides the compiler location and Visual Studio(msbuild) will use the Roslyn compiler from Microsoft.Net.Compilers nupkg. This causes two problems. 1. When you install the latest Visual Studio update which always contains new Roslyn Compiler and you configure Visual Studio to use latest language feature. And you do use the latest language feature in your code, the intellisense works and IDE doesn't show any syntax error. However, when you build your project, you see some compilation error. This is because your project is still using the old version of Microsoft.Net.Compilers nupkg. 2. The Roslyn compiler shipped with Visual Studio is NGen'd which means it has better cold startup performance. So it takes more time to build the project, if the project references Microsoft.Net.Compilers nupkg.
    - #### What shall I do?

        If you are using Visual Studio 2017 with latest update, you should upgrade Microsoft.CodeDom.Providers.DotNetCompilerPlatform nupkg to 1.1.0 and **remove Microsoft.Net.Compilers nupkg from your project**.


## Configurations
+ **Specify the path to load Roslyn compiler at runtime** - When asp.net compiles the views at runtime or precompile time(using aspnet_compiler to precompile the web app), Microsoft.CodeDom.Providers.DotNetCompilerPlatform needs a path to load Roslyn compiler. This setting can be used to specify this loading path. 
        
    1. **Environment variable** - This is the first setting Microsoft.CodeDom.Providers.DotNetCompilerPlatform reads. If this setting is used, Microsoft.CodeDom.Providers.DotNetCompilerPlatform will ignore the other setting.    
        **Setting name** - ROSLYN_COMPILER_LOCATION

        **How to use it** - ```setx ROSLYN_COMPILER_LOCATION [Roslyn compiler folder full path]```

        **Use case** - This is a machine wide setting. If you want to control the Roslyn compiler version used on your machine or you don't want a copy of Roslyn compiler under every application, then you can use this setting.

    2. **AppSetting in config file** - This is the second option to specify the location of Roslyn compiler and it's only valid if the environment variable setting is not used.
        
        **Setting name** - aspnet:RoslynCompilerLocation

        **How to use it** - Add this appSetting into your config file   ``` <add key="aspnet:RoslynCompilerLocation" value="[Roslyn compiler folder full path]"/> ```
        
        **Use case** - This is a application level setting. If you have multiple projects in a solution and you want to use Microsoft.CodeDom.Providers.DotNetCompilerPlatform in several projects but you only want to install Microsoft.CodeDom.Providers.DotNetCompilerPlatform nupkg on one project. See [issue #25](https://github.com/aspnet/RoslynCodeDomProvider/issues/25).

    3. **Default setting** - This applies to web application only and default location is bin\roslyn folder.


+ **Specify the path to copy Roslyn compiler at build time** - When building web application project, some targets added into the project by Microsoft.CodeDom.Providers.DotNetCompilerPlatform nupkg will copy Roslyn compiler unto bin\roslyn folder. With this setting, you can specify a path from which the build process will copy the Roslyn compiler.

    **Setting name** - RoslynToolPath

    **How to use it** - ```msbuild mysolution.sln /t:build /p:RoslynToolPath="[Roslyn compiler folder full path]"```

    **Use case** - In 1.1.0 version, Microsoft.CodeDom.Providers.DotNetCompilerPlatform nupkg removes the dependency on Microsoft.Net.Compilers nupkg. Instead, it embeds one version of Roslyn compiler inside the nupkg. It's possible that the embeded version of Roslyn compiler is not the one you want, so through this setting you can specify a version of Roslyn compiler at build time which will be copied to bin\roslyn folder.


+ **Specify the TTL of Roslyn compiler server** - Microsoft.CodeDom.Providers.DotNetCompilerPlatform leverages Roslyn compiler server(VBCSCompiler.exe) to compile the generated code. In order to save system resources, VBCSCompiler.exe will be shutdown after idling 10 seconds in the server environment. However, in the development environment(running your web application from visual studio) the idle time is set to 15 mininutes. The reason behind this is to improve the startup performance of your web application when you run/debug the application in Visual Studio, since VBCSCompiler.exe takes several seconds to start if relevant Roslyn assemblies are not NGen'd. With this setting, you can control the idle time of VBCSCompiler.exe.
        
    **Setting name** - VBCSCOMPILER_TTL

    **How to use it** - ```setx VBCSCOMPILER_TTL [num of seconds]```

    **Use case** - When you develop your web application in Visual Studio, you don't modify and run your application very frequently. In this scenario, you may use this setting to shorten the idle time of VBCSCompiler.exe and let the process end earlier which can release some system resources.