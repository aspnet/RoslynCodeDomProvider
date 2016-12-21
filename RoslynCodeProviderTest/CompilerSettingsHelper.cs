using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatformTest {
    internal static class CompilerSettingsHelper {

        private const int DefaultCompilerServerTTL = 0; // set TTL to 0 to turn of keepalive switch

        private static ICompilerSettings _csc = new CompilerSettings(CompilerFullPath(@"csc.exe"), DefaultCompilerServerTTL);
        private static ICompilerSettings _vb = new CompilerSettings(CompilerFullPath(@"vbc.exe"), DefaultCompilerServerTTL);

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
}
