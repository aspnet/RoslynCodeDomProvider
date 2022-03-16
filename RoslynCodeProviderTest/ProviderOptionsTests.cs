using System;
using System.Configuration;
using System.IO;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Xunit;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatformTest
{
    public class ProviderOptionsTests {

        private static bool IsDev = false;

        static ProviderOptionsTests() {
            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEV_ENVIRONMENT")) ||
                !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("IN_DEBUG_MODE")) ||
                CompilationUtil.IsDebuggerAttached)
                IsDev = true;
        }

        [Fact]
        public void DefaultSettings()
        {
            IProviderOptions opts = CompilationUtil.GetProviderOptionsFor(".fakevb");
            Assert.NotNull(opts);
            Assert.Equal(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"roslyn"), opts.CompilerFullPath);   // Would include csc.exe or vbc.exe if the extension we searched for wasn't fake.
            Assert.Equal(IsDev ? 15 * 60 : 10, opts.CompilerServerTimeToLive);   // 10 in Production. 900 in a "dev" environment.
            Assert.True(opts.UseAspNetSettings);  // Default is false... except through the GetProviderOptionsFor factory method we used here.
            Assert.False(opts.WarnAsError);
            Assert.Null(opts.CompilerVersion);
            Assert.Equal(2, opts.AllOptions.Count);
            Assert.Equal("foo2", opts.AllOptions["CustomSetting"]);
            Assert.Equal("bar2", opts.AllOptions["AnotherCoolSetting"]);
        }

        [Fact]
        public void FromShortConstructor()
        {
            IProviderOptions opts = new ProviderOptions(@"D:\My\Fun\Compiler\Path\compiles.exe", 123);
            Assert.NotNull(opts);
            Assert.Equal(@"D:\My\Fun\Compiler\Path\compiles.exe", opts.CompilerFullPath);   // Would include csc.exe or vbc.exe if the extension we searched for wasn't fake.
            Assert.Equal(123, opts.CompilerServerTimeToLive);   // 10 in Production. 900 in a "dev" environment.
            Assert.False(opts.UseAspNetSettings);  // Default via constructor is false.
            Assert.False(opts.WarnAsError);
            Assert.Null(opts.CompilerVersion);
            Assert.Equal(0, opts.AllOptions.Count);
        }

        [Fact]
        public void FromICompilerSettings()
        {
#pragma warning disable CS0618
            IProviderOptions opts = new ProviderOptions((ICompilerSettings)(CompilerSettingsHelper.CSC));
#pragma warning restore CS0618
            Assert.NotNull(opts);
            Assert.Equal(CompilerSettingsHelper.CSC.CompilerFullPath, opts.CompilerFullPath);   // Would include csc.exe or vbc.exe if the extension we searched for wasn't fake.
            Assert.Equal(CompilerSettingsHelper.CSC.CompilerServerTimeToLive, opts.CompilerServerTimeToLive);   // 10 in Production. 900 in a "dev" environment.
            Assert.False(opts.UseAspNetSettings);  // Default via constructor is false.
            Assert.False(opts.WarnAsError);
            Assert.Null(opts.CompilerVersion);
            Assert.Equal(0, opts.AllOptions.Count);
        }

        // <providerOptions> override defaults
        [Fact]
        public void FromProviderOptions()
        {
            IProviderOptions opts = CompilationUtil.GetProviderOptionsFor(".fakecs");
            Assert.NotNull(opts);
            Assert.Equal(@"C:\Path\To\Nowhere\csc.exe", opts.CompilerFullPath);
            Assert.Equal(42, opts.CompilerServerTimeToLive);
            Assert.False(opts.UseAspNetSettings);
            Assert.True(opts.WarnAsError);
            Assert.Equal("v6.0", opts.CompilerVersion);
            Assert.Equal(7, opts.AllOptions.Count);
            Assert.Equal("foo", opts.AllOptions["CustomSetting"]);
            Assert.Equal("bar", opts.AllOptions["AnotherCoolSetting"]);
        }

        // <appSettings> override <providerOptions> for location only
        // Actually, we can't do this because A) AppSettings can be added but not cleaned up after this test, and
        // B) the setting has probably already been read and cached by the AppSettings utility class, so updating
        // the value here wouldn't have any affect anyway.
        [Fact(Skip = "Need to fake config system first")]
        public void FromAppSettings()
        {
            ConfigurationManager.AppSettings.Set("aspnet:RoslynCompilerLocation", @"C:\Location\for\all\from\appSettings\compiler.exe");
            IProviderOptions opts = CompilationUtil.GetProviderOptionsFor(".fakecs");
            ConfigurationManager.AppSettings.Remove("aspnet:RoslynCompilerLocation");

            Assert.NotNull(opts);
            Assert.Equal(@"C:\Location\for\all\from\appSettings\compiler.exe", opts.CompilerFullPath);
            Assert.Equal(42, opts.CompilerServerTimeToLive);
            Assert.False(opts.UseAspNetSettings);
            Assert.True(opts.WarnAsError);
            Assert.Equal("v6.0", opts.CompilerVersion);
            Assert.Equal(7, opts.AllOptions.Count);
            Assert.Equal("foo", opts.AllOptions["CustomSetting"]);
            Assert.Equal("bar", opts.AllOptions["AnotherCoolSetting"]);
        }

        // Environment overrides all for location and TTL
        [Fact]
        public void FromEnvironment()
        {
            // See note on the 'FromAppSettings' test.
            //ConfigurationManager.AppSettings.Set("aspnet:RoslynCompilerLocation", @"C:\Location\for\all\from\appSettings\compiler.exe");
            Environment.SetEnvironmentVariable("ROSLYN_COMPILER_LOCATION", @"C:\My\Compiler\Location\vbcsc.exe");
            Environment.SetEnvironmentVariable("VBCSCOMPILER_TTL", "98");
            IProviderOptions opts = CompilationUtil.GetProviderOptionsFor(".fakecs");
            Environment.SetEnvironmentVariable("ROSLYN_COMPILER_LOCATION", null);
            Environment.SetEnvironmentVariable("VBCSCOMPILER_TTL", null);
            //ConfigurationManager.AppSettings.Remove("aspnet:RoslynCompilerLocation");

            Assert.NotNull(opts);
            Assert.Equal(@"C:\My\Compiler\Location\vbcsc.exe", opts.CompilerFullPath);
            Assert.Equal(98, opts.CompilerServerTimeToLive);
            Assert.False(opts.UseAspNetSettings);
            Assert.True(opts.WarnAsError);
            Assert.Equal("v6.0", opts.CompilerVersion);
            Assert.Equal(7, opts.AllOptions.Count);
            Assert.Equal("foo", opts.AllOptions["CustomSetting"]);
            Assert.Equal("bar", opts.AllOptions["AnotherCoolSetting"]);
        }

        // TTL must be int
        [Fact]
        public void TTL_MustBeInteger()
        {
            Environment.SetEnvironmentVariable("VBCSCOMPILER_TTL", "NotANumber");
            IProviderOptions opts = CompilationUtil.GetProviderOptionsFor(".fakevb");
            Environment.SetEnvironmentVariable("VBCSCOMPILER_TTL", null);

            Assert.NotNull(opts);
            Assert.Equal(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"roslyn"), opts.CompilerFullPath);   // Would include csc.exe or vbc.exe if the extension we searched for wasn't fake.
            Assert.Equal(IsDev ? 15 * 60 : 10, opts.CompilerServerTimeToLive);   // 10 in Production. 900 in a "dev" environment.
            Assert.True(opts.UseAspNetSettings);  // Default is false... except through the GetProviderOptionsFor factory method we used here.
            Assert.False(opts.WarnAsError);
            Assert.Null(opts.CompilerVersion);
            Assert.Equal(2, opts.AllOptions.Count);
            Assert.Equal("foo2", opts.AllOptions["CustomSetting"]);
            Assert.Equal("bar2", opts.AllOptions["AnotherCoolSetting"]);
        }
    }
}
