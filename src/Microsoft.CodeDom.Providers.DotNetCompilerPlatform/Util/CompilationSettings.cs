using System;
using System.IO;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {
    internal sealed class CompilerSettings : ICompilerSettings {

        private readonly string _compilerFullPath;
        private readonly int _compilerServerTimeToLive = 0; // seconds

        public CompilerSettings(string compilerFullPath, int compilerServerTimeToLive) {
            if (string.IsNullOrEmpty(compilerFullPath)) {
                throw new ArgumentNullException("compilerFullPath");
            }

            _compilerFullPath = compilerFullPath;
            _compilerServerTimeToLive = compilerServerTimeToLive;
        }

        string ICompilerSettings.CompilerFullPath {
            get {
                return _compilerFullPath;
            }
        }

        int ICompilerSettings.CompilerServerTimeToLive {
            get{
                return _compilerServerTimeToLive;
            }
        }
    }

    internal static class CompilationSettingsHelper {
        private const int DefaultCompilerServerTTL = 10; //seconds

        private static ICompilerSettings _csc = new CompilerSettings(CompilerFullPath(@"bin\roslyn\csc.exe"), DefaultCompilerServerTTL);
        private static ICompilerSettings _vb = new CompilerSettings(CompilerFullPath(@"bin\roslyn\vbc.exe"), DefaultCompilerServerTTL);

        public static ICompilerSettings CSC2 {
            get { 
                return _csc;
            }
        }

        public static ICompilerSettings VBC2 {
            get {
                return _vb;
            }
        }

        private static string CompilerFullPath(string relativePath) {
            string compilerFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            return compilerFullPath;
        }
    }
}
