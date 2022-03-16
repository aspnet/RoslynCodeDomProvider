using System.CodeDom.Compiler;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Xunit;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatformTest
{
    public class VBCodeProviderTests {

        private const int Failed = 1;
        private const int Success = 0;

#pragma warning disable CS0618
        private CommonCodeDomProviderTests commonTests = new CommonCodeDomProviderTests();
        private CodeDomProvider _codeProvider = new VBCodeProvider(CompilerSettingsHelper.VB);
#pragma warning restore CS0618

        static VBCodeProviderTests() {
            //VBCompiler.MySupport = " ";   // Don't need to do this anymore with UseAspNetSettings feature
            VBCompiler.VBImportsString = " ";
        }

        [Fact]
        public void AssemblyVersion()
        {
            commonTests.AssemblyVersion(_codeProvider);
        }

        [Fact]
        public void FileExtension() {
            commonTests.FileExtension(_codeProvider, "vb");
        }

        [Fact]
        public void CompileAssemblyFromSource_DLL_GenerateInMemory_False() {
            commonTests.CompileAssemblyFromSource_GenerateInMemory_False(_codeProvider,
@"Public Class FooClass
   Public Function Execute() As String
      Return ""output""
   End Function
End Class");
        }

        [Fact]
        public void CompileAssemblyFromSource_WarningAsError() {
            commonTests.CompileAssemblyFromSource_WarningAsError(_codeProvider,
                // the variable a is declared but not used
@"Public Class FooClass
   Public Function Execute() As String
      Dim a As String
      Return ""output""
   End Function
End Class",
          "BC42024");
        }

        [Fact]
        public void CompileAssemblyFromFile_ASPNet_Magic()
        {
            // Complete added frippery is: "/nowarn:41008,40000,40008 /define:_MYTYPE=\\\"Web\\\"    /optionInfer+"
            // But let's just check for _MYTYPE.
            ProviderOptions opts = new ProviderOptions(CompilerSettingsHelper.VB) { UseAspNetSettings = true };
            commonTests.CompileAssemblyFromFile_CheckArgs(new VBCodeProvider(opts), "/define:_MYTYPE=\\\"Web\\\"", true);
        }

        [Fact]
        public void CompileAssemblyFromFile_No_ASPNet_Magic()
        {
            // _codeProvider uses options (aka CompilerSettingsHelper.VB) created via constructor, so it should
            // have the ASP.Net frippery disabled.
            commonTests.CompileAssemblyFromFile_CheckArgs(_codeProvider, "/define:_MYTYPE=\"Web\"", false);
        }
    }

}
