using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatformTest {


    [TestClass]
    public class VBCodeProviderTests {

        private const int Failed = 1;
        private const int Success = 0;

#pragma warning disable CS0618
        private CommonCodeDomProviderTests commonTests = new CommonCodeDomProviderTests();
        private CodeDomProvider _codeProvider = new VBCodeProvider(CompilerSettingsHelper.VB);
#pragma warning restore CS0618

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            VBCompiler.MySupport = " ";
            VBCompiler.VBImportsString = " ";
        }

        [TestMethod]
        public void FileExtension() {
            commonTests.FileExtension(_codeProvider, "vb");
        }

        [TestMethod]
        public void CompileAssemblyFromSource_DLL_GenerateInMemory_False() {
            commonTests.CompileAssemblyFromSource_GenerateInMemory_False(_codeProvider,
@"Public Class FooClass
   Public Function Execute() As String
      Return ""output""
   End Function
End Class");
        }

         [TestMethod]
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
    }

}
