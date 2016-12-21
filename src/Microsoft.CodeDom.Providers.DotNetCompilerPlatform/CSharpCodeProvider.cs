using System;
using System.CodeDom.Compiler;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {
    /// <summary>
    /// Provides access to instances of the .NET Compiler Platform C# code generator and code compiler.
    /// </summary>
    public sealed class CSharpCodeProvider : Microsoft.CSharp.CSharpCodeProvider {
        private ICompilerSettings _compilerSettings;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public CSharpCodeProvider()
            : this(null) {
        }

        // Constructor used for unit test purpose
        internal CSharpCodeProvider(ICompilerSettings compilerSettings = null) {
            _compilerSettings = compilerSettings == null ? CompilationSettingsHelper.CSC2 : compilerSettings;
        }

        /// <summary>
        /// Gets an instance of the .NET Compiler Platform C# code compiler.
        /// </summary>
        /// <returns>An instance of the .NET Compiler Platform C# code compiler</returns>
        [Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class.")]
        public override ICodeCompiler CreateCompiler() {
            return new CSharpCompiler(this, _compilerSettings);
        }
    }
}