using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatformTest {
    [TestClass]
    public class CommonCodeDomProviderTests {

        private const int Failed = 1;
        private const int Success = 0;

        public void FileExtension(CodeDomProvider provider, string extension) {
            Assert.AreEqual(extension, provider.FileExtension);
        }


        public void CompileAssemblyFromSource_Parse_Error(CodeDomProvider provider) {
            var result = provider.CompileAssemblyFromSource(
                new CompilerParameters(),
                // a ; is missing at the end of the return statement
                "public class FooClass { public string Execute() { return \"output\" /*;*/ }}"
            );

            Assert.AreEqual(Failed, result.NativeCompilerReturnValue);
            Assert.IsTrue(result.Errors.HasErrors);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual("CS1002", result.Errors[0].ErrorNumber);
        }


        public void CompileAssemblyFromSource_WarningAsError(CodeDomProvider provider, string sourceCode, string errorNumber) {
            var param = new CompilerParameters();
            param.GenerateInMemory = true;
            param.WarningLevel = 4;
            param.TreatWarningsAsErrors = true;
            param.CompilerOptions = "/warnaserror+";
            var result = provider.CompileAssemblyFromSource(
                param,
                sourceCode
            );

            Assert.AreEqual(Failed, result.NativeCompilerReturnValue);
            Assert.IsTrue(result.Errors.HasErrors);
            Assert.AreEqual(errorNumber, result.Errors[0].ErrorNumber);
        }


        public void CompileAssemblyFromSource_ReferenceAssembly_AssemblyNameOnly(CodeDomProvider provider) {
            var assemblyNames = new string[] { "mscorlib.dll" };
            var param = new CompilerParameters(assemblyNames);
            param.GenerateInMemory = true;
            var result = provider.CompileAssemblyFromSource(
                param,
                // the variable a is declared but not used
                "public class FooClass { public string Execute() { return \"output\"; }}"
            );

            Assert.AreEqual(Success, result.NativeCompilerReturnValue);
            var type = result.CompiledAssembly.GetType("FooClass");
            var obj = Activator.CreateInstance(type);
            var output = type.GetMethod("Execute").Invoke(obj, new object[] { });
            Assert.IsNull(result.PathToAssembly);
            Assert.AreEqual(@"output", output);
        }


        public void CompileAssemblyFromSource_ReferenceAssembly_NameCannotBeResolved(CodeDomProvider provider) {
            // the referenced assembly below does not exist
            string assemblyName = "mscorlib1.dll";
            var assemblyNames = new string[] { assemblyName };
            var param = new CompilerParameters(assemblyNames);
            param.GenerateInMemory = true;
            var result = provider.CompileAssemblyFromSource(
                param,
                // the variable a is declared but not used
                "public class FooClass { public string Execute() { int a; return \"output\"; }}"
            );

            // Assert.IsNull(result.PathToAssembly);
            Assert.AreEqual(Failed, result.NativeCompilerReturnValue);

            bool referenceErrorInOutput = false;
            foreach (var line in result.Output) {
                if (line.Contains("error") && line.Contains(assemblyName)) {
                    referenceErrorInOutput = true;
                }
            }

            Assert.IsTrue(referenceErrorInOutput);
        }


        public void CompileAssemblyFromSource_ReferenceAssembly_LocalReference(CodeDomProvider provider) {
            List<string> tempFiles = new List<string>();
            try {
                Environment.CurrentDirectory = Path.GetTempPath();
                var param1 = new CompilerParameters();
                var result1 = provider.CompileAssemblyFromSource(
                    param1,
                    "public class FooClass1 { public static string Execute() { return \"output\";}}"
                );

                Assert.AreEqual(Success, result1.NativeCompilerReturnValue);
                Assert.IsNotNull(result1.PathToAssembly);
                tempFiles.Add(result1.PathToAssembly);
                Assert.AreEqual(".dll", Path.GetExtension(result1.PathToAssembly));

                string referenceName = Path.GetFileName(result1.PathToAssembly);
                var asm1 = GetAssemblyByName(result1.PathToAssembly);
                var type1 = asm1.GetType("FooClass1");
                var obj1 = Activator.CreateInstance(type1);
                var output1 = type1.GetMethod("Execute").Invoke(obj1, new object[] { });

                Assert.AreEqual(@"output", output1);

                var param2 = new CompilerParameters(new string[] { referenceName });
                param2.GenerateExecutable = true;
                var result2 = provider.CompileAssemblyFromSource(
                    param2,
                    "public class FooClass2 { public static void Main() { System.Console.Write(FooClass1.Execute());}}"
                );
                Assert.IsNotNull(result2.PathToAssembly);
                tempFiles.Add(result2.PathToAssembly);
                Assert.AreEqual(Success, result2.NativeCompilerReturnValue);
                AppDomain newAppDomain = null;
                try {
                    newAppDomain = System.AppDomain.CreateDomain("NewApplicationDomain");
                    newAppDomain.ExecuteAssembly(result2.PathToAssembly);
                }
                finally {
                    if (newAppDomain != null) {
                        AppDomain.Unload(newAppDomain);
                    }
                }
            }
            finally {
                DeleteFiles(tempFiles);
            }
        }


        public void CompileAssemblyFromSource_ReferenceAssembly_PathWithComma(CodeDomProvider provider) {
            List<string> tempFiles = new List<string>();
            try {
                Environment.CurrentDirectory = Path.GetTempPath();
                var param1 = new CompilerParameters() {
                    OutputAssembly = string.Format("With,Comma{0}.dll", DateTime.UtcNow.Ticks.ToString())
                };

                var result1 = provider.CompileAssemblyFromSource(
                    param1,
                    "public class FooClass1 { public static string Execute() { return \"output\";}}"
                );

                Assert.AreEqual(Success, result1.NativeCompilerReturnValue);
                Assert.IsNotNull(result1.PathToAssembly);
                tempFiles.Add(result1.PathToAssembly);
                Assert.AreEqual(".dll", Path.GetExtension(result1.PathToAssembly));

                string referenceName = Path.GetFileName(result1.PathToAssembly);
                var asm1 = GetAssemblyByName(result1.PathToAssembly);
                var type1 = asm1.GetType("FooClass1");
                var obj1 = Activator.CreateInstance(type1);
                var output1 = type1.GetMethod("Execute").Invoke(obj1, new object[] { });
                Assert.AreEqual(@"output", output1);

                var param2 = new CompilerParameters(new string[] { referenceName });
                param2.GenerateExecutable = true;
                var result2 = provider.CompileAssemblyFromSource(
                    param2,
                    "public class FooClass2 { public static void Main() { System.Console.Write(FooClass1.Execute());}}"
                );

                Assert.AreEqual(Success, result2.NativeCompilerReturnValue);
                Assert.IsNotNull(result2.PathToAssembly);
                tempFiles.Add(result2.PathToAssembly);
                AppDomain newAppDomain = null;
                try {
                    newAppDomain = System.AppDomain.CreateDomain("NewApplicationDomain");
                    newAppDomain.ExecuteAssembly(result2.PathToAssembly);
                }
                finally {
                    if (newAppDomain != null) {
                        AppDomain.Unload(newAppDomain);
                    }
                }
            }
            finally {
                DeleteFiles(tempFiles);
            }
        }


        public void CompileAssemblyFromSource_GenerateInMemory_True(CodeDomProvider provider) {
            var result = provider.CompileAssemblyFromSource(
                new CompilerParameters() {
                    GenerateInMemory = true
                },
                "using System.Runtime; public class FooClass { public string Execute() { return \"output\";}}"
            );

            Assert.AreEqual(Success, result.NativeCompilerReturnValue);
            var type = result.CompiledAssembly.GetType("FooClass");
            var obj = Activator.CreateInstance(type);
            var output = type.GetMethod("Execute").Invoke(obj, new object[] { });
            Assert.IsNull(result.PathToAssembly);
            Assert.AreEqual(@"output", output);
        }

        public void CompileAssemblyFromSource_GenerateInMemory_False(CodeDomProvider provider, string sourceCode) {
            List<string> tempFiles = new List<string>();
            try {
                CompilerParameters param = new CompilerParameters();
                param.OutputAssembly = Path.GetTempFileName() + ".dll";
                tempFiles.Add(param.OutputAssembly);
                param.GenerateInMemory = false;
                var result = provider.CompileAssemblyFromSource(
                    param,
                    sourceCode
                );

                Assert.AreEqual(Success, result.NativeCompilerReturnValue);
                Assert.IsNotNull(result.PathToAssembly);

                // Read assembly into memory:
                Assembly asm = GetAssemblyByName(result.PathToAssembly);

                var type = asm.GetType("FooClass");
                var obj = Activator.CreateInstance(type);
                var output = type.GetMethod("Execute").Invoke(obj, new object[] { });

                Assert.AreEqual(@"output", output);
                Assert.AreEqual(param.OutputAssembly, result.PathToAssembly);

                Assert.IsTrue(File.Exists(param.OutputAssembly));
            }
            finally {
                DeleteFiles(tempFiles);
            }
        }


        public void CompileAssemblyFromSource_InvalidOutputPath(CodeDomProvider provider) {
            List<string> tempFiles = new List<string>();
            try {
                CompilerParameters param = new CompilerParameters();
                param.OutputAssembly = Path.Combine(Path.GetTempPath(), @"inva\l*d?path,someName,public");
                tempFiles.Add(param.OutputAssembly);
                param.GenerateInMemory = false;
                var result = provider.CompileAssemblyFromSource(
                    param,
                    "public class FooClass { public string Execute() { return \"output\";}}"
                );

                // Assert.IsNull(result.PathToAssembly);
                Assert.AreEqual(Failed, result.NativeCompilerReturnValue);
            }
            finally {
                DeleteFiles(tempFiles);
            }
        }


        public void CompileAssemblyFromSource_GenerateExecutable_True(CodeDomProvider provider) {
            Environment.CurrentDirectory = Path.GetTempPath();
            List<string> tempFiles = new List<string>();
            try {
                var param = new CompilerParameters() {
                    GenerateExecutable = true,
                    MainClass = "FooClass"
                };

                var result = provider.CompileAssemblyFromSource(
                    param,
                    "public class FooClass { public static void Main(){} public string Execute() { return \"output\";}}"
                );

                Assert.AreEqual(Success, result.NativeCompilerReturnValue);
                Assert.IsNotNull(result.PathToAssembly);
                tempFiles.Add(result.PathToAssembly);
                Assembly asm = GetAssemblyByName(result.PathToAssembly);

                var type = asm.GetType("FooClass");
                var obj = Activator.CreateInstance(type);
                var output = type.GetMethod("Execute").Invoke(obj, new object[] { });

                Assert.AreEqual(".exe", Path.GetExtension(result.PathToAssembly));
                Assert.AreEqual(@"output", output);
            }
            finally {
                DeleteFiles(tempFiles);
            }
        }


        public void CompileAssemblyFromSource_GenerateExecutable_True_Failed(CodeDomProvider provider) {
            Environment.CurrentDirectory = Path.GetTempPath();
            var param = new CompilerParameters() {
                GenerateInMemory = false,
                GenerateExecutable = true,
                IncludeDebugInformation = true,
            };

            var result = provider.CompileAssemblyFromSource(
                param,
                // The source does not contain Main() method.
                "public class FooClass {public string Execute() { return \"output\";}}"
            );

            Assert.AreEqual(Failed, result.NativeCompilerReturnValue);
            // Assert.IsNull(result.PathToAssembly);
            Assert.AreEqual("CS5001"/*miss main entry*/, result.Errors[0].ErrorNumber);
        }


        public void CompileAssemblyFromSource_CreateOutputFileFailed(CodeDomProvider provider) {
            List<string> tempFiles = new List<string>();
            try {
                CompilerParameters param = new CompilerParameters();
                param.OutputAssembly = Path.GetTempFileName();
                tempFiles.Add(param.OutputAssembly);
                using (var conflictFile = File.Open(param.OutputAssembly, FileMode.Open)) {
                    param.GenerateInMemory = false;
                    var result = provider.CompileAssemblyFromSource(
                        param,
                        "public class FooClass { public string Execute() { return \"output\";}}"
                    );

                    Assert.AreEqual(Failed, result.NativeCompilerReturnValue);
                    // The InProc provider does not give error while the old provider
                    // does. We probably should fix the behavior of InProc provider.
                    // Assert.IsFalse(result.Errors.HasErrors);
                    bool filenameInOutput = false;
                    foreach (var line in result.Output) {
                        if (line.Contains(Path.GetFileName(param.OutputAssembly))) {
                            filenameInOutput = true;
                        }
                    }

                    Assert.IsTrue(filenameInOutput);
                }
            }
            finally {
                DeleteFiles(tempFiles);
            }
        }


        public void CompileAssemblyFromSource_CreatePDBFileFailed(CodeDomProvider provider) {
            List<string> tempFiles = new List<string>();
            try {
                CompilerParameters param = new CompilerParameters();
                param.OutputAssembly = Path.GetTempFileName();
                tempFiles.Add(param.OutputAssembly);
                param.IncludeDebugInformation = true;
                string pdbFilename = Path.ChangeExtension(param.OutputAssembly, ".pdb");
                tempFiles.Add(pdbFilename);
                using (var conflictFile = File.Create(pdbFilename)) {
                    param.GenerateInMemory = false;
                    var result = provider.CompileAssemblyFromSource(
                        param,
                        "public class FooClass { public string Execute() { return \"output\";}}"
                    );

                    Assert.AreEqual(Failed, result.NativeCompilerReturnValue);
                    // The InProc provider does not give error while the old provider
                    // does. We probably should fix the behavior of InProc provider.
                    // Assert.IsFalse(result.Errors.HasErrors);
                    bool filenameInOutput = false;
                    foreach (var line in result.Output) {
                        if (line.Contains(Path.GetFileName(pdbFilename))) {
                            filenameInOutput = true;
                        }
                    }

                    Assert.IsTrue(filenameInOutput);
                }
            }
            finally {
                DeleteFiles(tempFiles);
            }
        }


        public void CompileAssemblyFromSource_IncludeDebugInformation_True(CodeDomProvider provider) {
            List<string> tempFiles = new List<string>();
            try {
                CompilerParameters param = new CompilerParameters();
                param.OutputAssembly = Path.GetTempFileName();
                tempFiles.Add(param.OutputAssembly);
                param.IncludeDebugInformation = true;
                param.GenerateInMemory = false;
                string pdbFileName = Path.ChangeExtension(param.OutputAssembly, ".pdb");
                tempFiles.Add(pdbFileName);
                var result = provider.CompileAssemblyFromSource(
                    param,
                    "public class FooClass { public string Execute() { return \"output\";}}"
                );

                Assert.AreEqual(Success, result.NativeCompilerReturnValue);

                // In Debug mode, visual studio would try to load the pdb file.
                // Delete the file before it's held by VS.
                Assert.IsTrue(File.Exists(pdbFileName));
                File.Delete(pdbFileName);

                // Read assembly into memory:
                Assembly asm = GetAssemblyByName(param.OutputAssembly);
                var type = asm.GetType("FooClass");
                var obj = Activator.CreateInstance(type);
                var output = type.GetMethod("Execute").Invoke(obj, new object[] { });

                Assert.AreEqual(@"output", output);
                Assert.AreEqual(param.OutputAssembly, result.PathToAssembly);
                Assert.IsTrue(File.Exists(param.OutputAssembly));
            }
            finally {
                DeleteFiles(tempFiles);
            }
        }


        public void CompileAssemblyFromSource_IncludeDebugInformation_False(CodeDomProvider provider) {

            List<string> tempFiles = new List<string>();
            try {
                CompilerParameters param = new CompilerParameters();
                param.OutputAssembly = Path.GetTempFileName();
                tempFiles.Add(param.OutputAssembly);
                param.IncludeDebugInformation = false;
                param.GenerateInMemory = false;
                string pdbFileName = Path.ChangeExtension(param.OutputAssembly, ".pdb");
                var result = provider.CompileAssemblyFromSource(
                    param,
                    "public class FooClass { public string Execute() { return \"output\";}}"
                );

                Assert.AreEqual(Success, result.NativeCompilerReturnValue);

                // Read assembly into memory:
                Assembly asm = GetAssemblyByName(param.OutputAssembly);
                var type = asm.GetType("FooClass");
                var obj = Activator.CreateInstance(type);
                var output = type.GetMethod("Execute").Invoke(obj, new object[] { });

                Assert.AreEqual(@"output", output);
                Assert.AreEqual(param.OutputAssembly, result.PathToAssembly);
                Assert.IsTrue(File.Exists(param.OutputAssembly));
                Assert.IsFalse(File.Exists(pdbFileName));
            }
            finally {
                DeleteFiles(tempFiles);
            }

        }

        public void CompileAssemblyFromDom(CodeDomProvider provider) {
            string spaceName = "TestNameSpace";
            string className = "FooClass";
            string methodName = "Execute";

            CodeCompileUnit compileUnit = new CodeCompileUnit();
            CodeNamespace testNameSpace = new CodeNamespace(spaceName);
            compileUnit.Namespaces.Add(testNameSpace);

            // Declare a new type called fooClass.
            CodeTypeDeclaration fooClass = new CodeTypeDeclaration(className);
            fooClass.Attributes = MemberAttributes.Public;

            // Add the new type to the namespace type collection.
            testNameSpace.Types.Add(fooClass);

            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = methodName;
            method.Attributes = MemberAttributes.Public;
            method.ReturnType = new CodeTypeReference(typeof(System.String));

            CodeMethodReturnStatement returnStatement = new CodeMethodReturnStatement();
            returnStatement.Expression = new CodePrimitiveExpression("output");
            method.Statements.Add(returnStatement);

            fooClass.Members.Add(method);
            var result = provider.CompileAssemblyFromDom(
                new CompilerParameters() {
                    GenerateInMemory = true
                },
                compileUnit
            );

            Assert.AreEqual(Success, result.NativeCompilerReturnValue);
            var type = result.CompiledAssembly.GetType(string.Format("{0}.{1}", spaceName, className));
            var obj = Activator.CreateInstance(type);
            var output = type.GetMethod(methodName).Invoke(obj, new object[] { });
            Assert.AreEqual("output", output);
        }


        public void CompileAssemblyFromFile(CodeDomProvider provider)
        {
            CompileAssemblyFromFile_CheckArgs(provider, null, false);
        }

        public void CompileAssemblyFromFile_CheckArgs(CodeDomProvider provider, string argStringToFind, bool expected) {
            var sourcePath = Path.Combine(Path.GetTempPath(), "foobarSourcefile.cs");
            try {
                using (var sourceStream = File.Create(sourcePath)) {
                    var content = "public class FooClass { public string Execute() { return \"output\";}}";
                    // If we're checking cmd args, we actually want to fail compilation so we can examine output.
                    if (argStringToFind != null)
                        content = "nonsense that doesn't compile.";
                    var bytes = Encoding.ASCII.GetBytes(content);
                    sourceStream.Write(bytes, 0, bytes.Length);
                }

                var result = provider.CompileAssemblyFromFile(
                    new CompilerParameters() {
                        GenerateInMemory = true
                    },
                    sourcePath
                );

                if (argStringToFind != null)
                {
                    Assert.AreNotEqual(Success, result.NativeCompilerReturnValue);
                    Assert.AreEqual<bool>(expected, result.Output[0].Contains(argStringToFind));
                    return;
                }

                Assert.AreEqual(Success, result.NativeCompilerReturnValue);
                var type = result.CompiledAssembly.GetType("FooClass");
                var obj = Activator.CreateInstance(type);
                var output = type.GetMethod("Execute").Invoke(obj, new object[] { });
                Assert.AreEqual(@"output", output);
            }
            finally {
                File.Delete(sourcePath);
            }

        }

        private static Assembly GetAssemblyByName(string name) {
            byte[] assemblyBuff = File.ReadAllBytes(name);
            Assembly asm = Assembly.Load(assemblyBuff);
            return asm;
        }

        private static void DeleteFiles(List<string> tempFiles) {
            Exception error = null;
            foreach (var file in tempFiles) {
                try {
                    File.Delete(file);
                }
                catch (IOException e) {
                    error = e;
                }
                catch (UnauthorizedAccessException e) {
                    error = e;
                }
                catch {

                }
            }

            if (error != null) {
                throw error;
            }
        }
    }
}
