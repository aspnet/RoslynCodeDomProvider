using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatformTest {

#pragma warning disable CS0618
    internal class TestCompilerSettings : ICompilerSettings {
        public string CompilerFullPath { get; set; }
        public int CompilerServerTimeToLive { get; set; }
    }

    internal static class CompilerSettingsHelper {

        private const int DefaultCompilerServerTTL = 0; // set TTL to 0 to turn of keepalive switch

        private static ICompilerSettings _csc = new ProviderOptions(CompilerFullPath(@"csc.exe"), DefaultCompilerServerTTL);
        private static ICompilerSettings _vb = new ProviderOptions(CompilerFullPath(@"vbc.exe"), DefaultCompilerServerTTL);

        public static ICompilerSettings CSC {
            get {
                return _csc;
            }
        }

        public static ICompilerSettings VB {
            get {
                return _vb;
            }
        }

        private static string CompilerFullPath(string relativePath) {
            string frameworkFolder = Path.GetDirectoryName(typeof(object).Assembly.Location);
            string compilerFullPath  = Path.Combine(frameworkFolder, relativePath);

            return compilerFullPath;
        }
    }
#pragma warning restore CS0618
}
