using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatformTest {


    [TestClass]
    public class ProviderOptionsTests {

        private const int Failed = 1;
        private const int Success = 0;

        private static bool IsDev = false;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEV_ENVIRONMENT")) ||
                !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("IN_DEBUG_MODE")) ||
                CompilationUtil.IsDebuggerAttached)
                IsDev = true;
        }

        [TestMethod]
        public void DefaultSettings()
        {
            IProviderOptions opts = CompilationUtil.GetProviderOptionsFor(".fakevb");
            Assert.IsNotNull(opts);
            Assert.AreEqual<string>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"bin\roslyn"), opts.CompilerFullPath);   // Would include csc.exe or vbc.exe if the extension we searched for wasn't fake.
            Assert.AreEqual<int>(IsDev ? 15 * 60 : 10, opts.CompilerServerTimeToLive);   // 10 in Production. 900 in a "dev" environment.
            Assert.IsTrue(opts.UseAspNetSettings);  // Default is false... except through the GetProviderOptionsFor factory method we used here.
            Assert.IsFalse(opts.WarnAsError);
            Assert.IsNull(opts.CompilerVersion);
            Assert.AreEqual<int>(2, opts.AllOptions.Count);
            Assert.AreEqual<string>("foo2", opts.AllOptions["CustomSetting"]);
            Assert.AreEqual<string>("bar2", opts.AllOptions["AnotherCoolSetting"]);
        }

        [TestMethod]
        public void FromShortConstructor()
        {
            IProviderOptions opts = new ProviderOptions(@"D:\My\Fun\Compiler\Path\compiles.exe", 123);
            Assert.IsNotNull(opts);
            Assert.AreEqual<string>(@"D:\My\Fun\Compiler\Path\compiles.exe", opts.CompilerFullPath);   // Would include csc.exe or vbc.exe if the extension we searched for wasn't fake.
            Assert.AreEqual<int>(123, opts.CompilerServerTimeToLive);   // 10 in Production. 900 in a "dev" environment.
            Assert.IsFalse(opts.UseAspNetSettings);  // Default via constructor is false.
            Assert.IsFalse(opts.WarnAsError);
            Assert.IsNull(opts.CompilerVersion);
            Assert.AreEqual<int>(0, opts.AllOptions.Count);
        }

        [TestMethod]
        public void FromICompilerSettings()
        {
#pragma warning disable CS0618
            IProviderOptions opts = new ProviderOptions((ICompilerSettings)(CompilerSettingsHelper.CSC));
#pragma warning restore CS0618
            Assert.IsNotNull(opts);
            Assert.AreEqual<string>(CompilerSettingsHelper.CSC.CompilerFullPath, opts.CompilerFullPath);   // Would include csc.exe or vbc.exe if the extension we searched for wasn't fake.
            Assert.AreEqual<int>(CompilerSettingsHelper.CSC.CompilerServerTimeToLive, opts.CompilerServerTimeToLive);   // 10 in Production. 900 in a "dev" environment.
            Assert.IsFalse(opts.UseAspNetSettings);  // Default via constructor is false.
            Assert.IsFalse(opts.WarnAsError);
            Assert.IsNull(opts.CompilerVersion);
            Assert.AreEqual<int>(0, opts.AllOptions.Count);
        }

        // <providerOptions> override defaults
        [TestMethod]
        public void FromProviderOptions()
        {
            IProviderOptions opts = CompilationUtil.GetProviderOptionsFor(".fakecs");
            Assert.IsNotNull(opts);
            Assert.AreEqual<string>(@"C:\Path\To\Nowhere\csc.exe", opts.CompilerFullPath);
            Assert.AreEqual<int>(42, opts.CompilerServerTimeToLive);
            Assert.IsFalse(opts.UseAspNetSettings);
            Assert.IsTrue(opts.WarnAsError);
            Assert.AreEqual<string>("v6.0", opts.CompilerVersion);
            Assert.AreEqual<int>(7, opts.AllOptions.Count);
            Assert.AreEqual<string>("foo", opts.AllOptions["CustomSetting"]);
            Assert.AreEqual<string>("bar", opts.AllOptions["AnotherCoolSetting"]);
        }

        // <appSettings> override <providerOptions> for location only
        // Actually, we can't do this because A) AppSettings can be added but not cleaned up after this test, and
        // B) the setting has probably already been read and cached by the AppSettings utility class, so updating
        // the value here wouldn't have any affect anyway.
        //[TestMethod]
        public void FromAppSettings()
        {
            ConfigurationManager.AppSettings.Set("aspnet:RoslynCompilerLocation", @"C:\Location\for\all\from\appSettings\compiler.exe");
            IProviderOptions opts = CompilationUtil.GetProviderOptionsFor(".fakecs");
            ConfigurationManager.AppSettings.Remove("aspnet:RoslynCompilerLocation");

            Assert.IsNotNull(opts);
            Assert.AreEqual<string>(@"C:\Location\for\all\from\appSettings\compiler.exe", opts.CompilerFullPath);
            Assert.AreEqual<int>(42, opts.CompilerServerTimeToLive);
            Assert.IsFalse(opts.UseAspNetSettings);
            Assert.IsTrue(opts.WarnAsError);
            Assert.AreEqual<string>("v6.0", opts.CompilerVersion);
            Assert.AreEqual<int>(7, opts.AllOptions.Count);
            Assert.AreEqual<string>("foo", opts.AllOptions["CustomSetting"]);
            Assert.AreEqual<string>("bar", opts.AllOptions["AnotherCoolSetting"]);
        }

        // Environment overrides all for location and TTL
        [TestMethod]
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

            Assert.IsNotNull(opts);
            Assert.AreEqual<string>(@"C:\My\Compiler\Location\vbcsc.exe", opts.CompilerFullPath);
            Assert.AreEqual<int>(98, opts.CompilerServerTimeToLive);
            Assert.IsFalse(opts.UseAspNetSettings);
            Assert.IsTrue(opts.WarnAsError);
            Assert.AreEqual<string>("v6.0", opts.CompilerVersion);
            Assert.AreEqual<int>(7, opts.AllOptions.Count);
            Assert.AreEqual<string>("foo", opts.AllOptions["CustomSetting"]);
            Assert.AreEqual<string>("bar", opts.AllOptions["AnotherCoolSetting"]);
        }

        // TTL must be int
        [TestMethod]
        public void TTL_MustBeInteger()
        {
            Environment.SetEnvironmentVariable("VBCSCOMPILER_TTL", "NotANumber");
            IProviderOptions opts = CompilationUtil.GetProviderOptionsFor(".fakevb");
            Environment.SetEnvironmentVariable("VBCSCOMPILER_TTL", null);

            Assert.IsNotNull(opts);
            Assert.AreEqual<string>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"bin\roslyn"), opts.CompilerFullPath);   // Would include csc.exe or vbc.exe if the extension we searched for wasn't fake.
            Assert.AreEqual<int>(IsDev ? 15 * 60 : 10, opts.CompilerServerTimeToLive);   // 10 in Production. 900 in a "dev" environment.
            Assert.IsTrue(opts.UseAspNetSettings);  // Default is false... except through the GetProviderOptionsFor factory method we used here.
            Assert.IsFalse(opts.WarnAsError);
            Assert.IsNull(opts.CompilerVersion);
            Assert.AreEqual<int>(2, opts.AllOptions.Count);
            Assert.AreEqual<string>("foo2", opts.AllOptions["CustomSetting"]);
            Assert.AreEqual<string>("bar2", opts.AllOptions["AnotherCoolSetting"]);
        }
    }
}
