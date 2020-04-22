using System;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatformTest {

    [TestClass]
    public class CSharpProviderTest {

        private CommonCodeDomProviderTests commonTests = new CommonCodeDomProviderTests();
        private static CodeDomProvider csharpCodeProvider;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext) {
            string frameworkFolder = Path.GetDirectoryName(typeof(object).Assembly.Location);
            string compilerPath = Path.Combine(frameworkFolder, "csc.exe");
            var codeDomProviderType = typeof(Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider);
            csharpCodeProvider = new CSharpCodeProvider(compilerSettings: CompilerSettingsHelper.CSC);
            AppContext.SetSwitch("Switch.System.DisableTempFileCollectionDirectoryFeature", true);
        }

        [TestMethod]
        public void FileExtension() {
            commonTests.FileExtension(csharpCodeProvider, "cs");
        }

        [TestMethod]
        public void CompileAssemblyFromSource_Parse_Error() {
            commonTests.CompileAssemblyFromSource_Parse_Error(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_WarningAsError() {
            commonTests.CompileAssemblyFromSource_WarningAsError(csharpCodeProvider,
                // the variable a is declared but not used
                "public class FooClass { public string Execute() { int a; return \"output\"; }}",
                "CS0168"/*errorNumber*/);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_ReferenceAssembly_AssemblyNameOnly() {
            commonTests.CompileAssemblyFromSource_ReferenceAssembly_AssemblyNameOnly(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_ReferenceAssembly_NameCannotBeResolved() {
            commonTests.CompileAssemblyFromSource_ReferenceAssembly_NameCannotBeResolved(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_ReferenceAssembly_LocalReference() {
            commonTests.CompileAssemblyFromSource_ReferenceAssembly_LocalReference(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_ReferenceAssembly_PathWithComma() {
            commonTests.CompileAssemblyFromSource_ReferenceAssembly_PathWithComma(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_GenerateInMemory_True() {
            commonTests.CompileAssemblyFromSource_GenerateInMemory_True(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_GenerateInMemory_False() {
            commonTests.CompileAssemblyFromSource_GenerateInMemory_False(csharpCodeProvider,
                "public class FooClass { public string Execute() { return \"output\";}}");
        }

        [TestMethod]
        public void CompileAssemblyFromSource_InvalidOutputPath() {
            commonTests.CompileAssemblyFromSource_InvalidOutputPath(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_GenerateExecutable_True() {
            commonTests.CompileAssemblyFromSource_GenerateExecutable_True(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_GenerateExecutable_True_Failed() {
            commonTests.CompileAssemblyFromSource_GenerateExecutable_True_Failed(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_CreateOutputFileFailed() {
            commonTests.CompileAssemblyFromSource_CreateOutputFileFailed(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_CreatePDBFileFailed() {
            commonTests.CompileAssemblyFromSource_CreatePDBFileFailed(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_IncludeDebugInformation_True() {
            commonTests.CompileAssemblyFromSource_IncludeDebugInformation_True(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromSource_IncludeDebugInformation_False() {
            commonTests.CompileAssemblyFromSource_IncludeDebugInformation_False(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromDom() {
            commonTests.CompileAssemblyFromDom(csharpCodeProvider);
        }

        [TestMethod]
        public void CompileAssemblyFromFile() {
            commonTests.CompileAssemblyFromFile(csharpCodeProvider);
        }
    }
}
