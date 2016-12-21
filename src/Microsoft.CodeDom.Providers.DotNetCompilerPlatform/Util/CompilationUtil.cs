using System.CodeDom.Compiler;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {
    internal static class CompilationUtil {
        internal static void PrependCompilerOption(CompilerParameters compilParams, string compilerOptions) {
            if (compilParams.CompilerOptions == null) {
                compilParams.CompilerOptions = compilerOptions;
            }
            else {
                compilParams.CompilerOptions = compilerOptions + " " + compilParams.CompilerOptions;
            }
        }
    }
}
