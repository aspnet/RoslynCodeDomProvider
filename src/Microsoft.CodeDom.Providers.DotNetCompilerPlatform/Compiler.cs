// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using static Microsoft.CodeDom.Providers.DotNetCompilerPlatform.Constants.CustomCompilerParameters;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {
    internal abstract class Compiler : ICodeCompiler {
        private readonly CodeDomProvider _codeDomProvider;
        private readonly ICompilerSettings _compilerSettings;
        private const string CLR_PROFILING_SETTING = "COR_ENABLE_PROFILING";
        private const string DISABLE_PROFILING = "0";

        // Needs to be initialized using InitializeCompilerFullPath where the CompilerParameters are available.
        private string _compilerFullPath = null;

        public Compiler(CodeDomProvider codeDomProvider, ICompilerSettings compilerSettings) {
            _codeDomProvider = codeDomProvider;
            _compilerSettings = compilerSettings;
        }

        public CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit) {
            if (options == null) {
                throw new ArgumentNullException("options");
            }

            if (compilationUnit == null) {
                throw new ArgumentNullException("compilationUnit");
            }

            return CompileAssemblyFromDomBatch(options, new CodeCompileUnit[] { compilationUnit });
        }

        public CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits) {
            if (options == null) {
                throw new ArgumentNullException("options");
            }

            if (compilationUnits == null) {
                throw new ArgumentNullException("compilationUnits");
            }

            InitializeCompilerFullPath(options);

            try {
                var sources = compilationUnits.Select(c => {
                    var writer = new StringWriter();
                    _codeDomProvider.GenerateCodeFromCompileUnit(c, writer, new CodeGeneratorOptions());
                    return writer.ToString();
                });

                return FromSourceBatch(options, sources.ToArray());
            }
            finally {
                options.TempFiles.Delete();
            }
        }

        public CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName) {
            if (options == null) {
                throw new ArgumentNullException("options");
            }

            if (fileName == null) {
                throw new ArgumentNullException("fileName");
            }

            return CompileAssemblyFromFileBatch(options, new string[] { fileName });
        }

        public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames) {
            if (options == null) {
                throw new ArgumentNullException("options");
            }

            if (fileNames == null) {
                throw new ArgumentNullException("fileNames");
            }

            InitializeCompilerFullPath(options);

            try {
                // Try opening the files to make sure they exists.  This will throw an exception
                // if it doesn't
                foreach (var fileName in fileNames) {
                    using (var str = File.OpenRead(fileName)) { }
                }

                return FromFileBatch(options, fileNames);
            }
            finally {
                options.TempFiles.Delete();
            }
        }

        public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source) {
            if (options == null) {
                throw new ArgumentNullException("options");
            }

            if (source == null) {
                throw new ArgumentNullException("source");
            }

            return CompileAssemblyFromSourceBatch(options, new string[] { source });
        }

        public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources) {
            if (options == null) {
                throw new ArgumentNullException("options");
            }

            if (sources == null) {
                throw new ArgumentNullException("sources");
            }

            InitializeCompilerFullPath(options);

            try {
                return FromSourceBatch(options, sources);
            }
            finally {
                options.TempFiles.Delete();
            }
        }

        protected abstract string FileExtension {
            get;
        }

        protected void InitializeCompilerFullPath(CompilerParameters options = null) {
            if (string.IsNullOrEmpty(_compilerFullPath)) {
                if (options != null) {
                    // Determining whether the custom compiler path parameter is provided.
                    var customCompilerPathParameter = options.CompilerOptions.Split('/').FirstOrDefault(p => p.StartsWith(CustomCompilerPath));
                    if (!string.IsNullOrEmpty(customCompilerPathParameter)) {
                        if (!customCompilerPathParameter.Contains(":")) {
                            throw new ArgumentException($"There's no value defined for the \"{CustomCompilerPath}\" compiler parameter!");
                        }

                        // Removing trailing space (when this is not the last parameter) and extracting value.
                        var customCompilerPath = customCompilerPathParameter.TrimEnd(' ').Split(':')[1];

                        if (string.IsNullOrEmpty(customCompilerPath)) {
                            throw new ArgumentException($"The value of the \"{CustomCompilerPath}\" compiler parameter can't be empty!");
                        }

                        // Extracting the name of the compiler executable from the default path.
                        var compilerExecutable = _compilerSettings.CompilerFullPath.Substring(_compilerSettings.CompilerFullPath.LastIndexOf('\\'));

                        // And finally, we're able to construct the complete custom path to the compiler executable.
                        // If the custom path contains spaces, then it has to be surrounded by quotes, which we don't need now.
                        _compilerFullPath = CompilationSettingsHelper.CompilerFullPath($"{customCompilerPath.Trim('"')}\\{compilerExecutable}");

                        // Removing the custom parameter, as the compiler can't process it.
                        options.CompilerOptions = options.CompilerOptions.Replace($"/{CustomCompilerPath}:{customCompilerPath}", "");
                    }
                    // Falling back to the default behavior.
                    else _compilerFullPath = _compilerSettings.CompilerFullPath;
                }
                else _compilerFullPath = _compilerSettings.CompilerFullPath;

                // Try opening the file to make sure that the compiler exists.
                // This will throw an exception if it doesn't.
                using (var str = File.OpenRead(_compilerFullPath)) { }
            }
        }

        protected abstract void ProcessCompilerOutputLine(CompilerResults results, string line);

        protected abstract string CmdArgsFromParameters(CompilerParameters options);

        protected abstract string FullPathsOption {
            get;
        }

        protected virtual void FixUpCompilerParameters(CompilerParameters options) {
            FixTreatWarningsAsErrors(options);
        }

        private string GetCompilationArgumentString(CompilerParameters options) {
            FixUpCompilerParameters(options);

            return CmdArgsFromParameters(options);
        }

        // CodeDom sets TreatWarningAsErrors to true whenever warningLevel is non-zero.
        // However, TreatWarningAsErrors should be false by default.
        // And users should be able to set the value by set the value of option "WarnAsError".
        // ASP.Net does fix this "WarnAsError" option, but only for old CodeDom providers (CSharp/VB).
        // So we need to do this correction here.
        private static void FixTreatWarningsAsErrors(CompilerParameters parameters) {
            parameters.TreatWarningsAsErrors = false;
        }

        private CompilerResults FromSourceBatch(CompilerParameters options, string[] sources) {
            if (options == null) {
                throw new ArgumentNullException("options");
            }

            if (sources == null) {
                throw new ArgumentNullException("sources");
            }

            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();

            var filenames = new string[sources.Length];
            CompilerResults results = null;

            // the extra try-catch is here to mitigate exception filter injection attacks.
            try {
                WindowsImpersonationContext impersonation = RevertImpersonation();
                try {
                    for (int i = 0; i < sources.Length; i++) {
                        string name = options.TempFiles.AddExtension(i + FileExtension);
                        var temp = new FileStream(name, FileMode.Create, FileAccess.Write, FileShare.Read);
                        try {
                            using (var sw = new StreamWriter(temp, Encoding.UTF8)) {
                                sw.Write(sources[i]);
                                sw.Flush();
                            }
                        }
                        finally {
                            temp.Close();
                        }
                        filenames[i] = name;
                    }

                    results = FromFileBatch(options, filenames);
                }
                finally {
                    ReImpersonate(impersonation);
                }
            }
            catch {
                throw;
            }

            return results;
        }

        private CompilerResults FromFileBatch(CompilerParameters options, string[] fileNames) {
            if (options == null) {
                throw new ArgumentNullException("options");
            }

            if (fileNames == null) {
                throw new ArgumentNullException("fileNames");
            }

            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();

            string outputFile = null;
            int retValue = 0;
            var results = new CompilerResults(options.TempFiles);
            var perm1 = new SecurityPermission(SecurityPermissionFlag.ControlEvidence);
            perm1.Assert();
            try {

#pragma warning disable 618
                results.Evidence = options.Evidence;
#pragma warning restore 618

            }
            finally {
                SecurityPermission.RevertAssert();
            }

            bool createdEmptyAssembly = false;
            if (options.OutputAssembly == null || options.OutputAssembly.Length == 0) {
                string extension = (options.GenerateExecutable) ? "exe" : "dll";
                options.OutputAssembly = results.TempFiles.AddExtension(extension, !options.GenerateInMemory);

                // Create an empty assembly.  This is so that the file will have permissions that
                // we can later access with our current credential. If we don't do this, the compiler
                // could end up creating an assembly that we cannot open.
                new FileStream(options.OutputAssembly, FileMode.Create, FileAccess.ReadWrite).Close();
                createdEmptyAssembly = true;
            }

            var pdbname = "pdb";

            // Don't delete pdbs when debug=false but they have specified pdbonly.
            if (options.CompilerOptions != null
                    && -1 != CultureInfo.InvariantCulture.CompareInfo.IndexOf(options.CompilerOptions, "/debug:pdbonly", CompareOptions.IgnoreCase)) {
                results.TempFiles.AddExtension(pdbname, true);
            }
            else {
                results.TempFiles.AddExtension(pdbname);
            }

            string args = GetCompilationArgumentString(options) + " " + JoinStringArray(fileNames, " ");

            // Use a response file if the compiler supports it
            string responseFileArgs = GetResponseFileCmdArgs(options, args);
            string trueArgs = null;
            if (responseFileArgs != null) {
                trueArgs = args;
                args = responseFileArgs;
            }

            // Appending TTL to the command line arguments.
            if (_compilerSettings.CompilerServerTimeToLive > 0) {
                args = string.Format("/shared /keepalive:\"{0}\" {1}", _compilerSettings.CompilerServerTimeToLive, args);
            }

            Compile(options,
                _compilerFullPath,
                args,
                ref outputFile,
                ref retValue);

            results.NativeCompilerReturnValue = retValue;

            // only look for errors/warnings if the compile failed or the caller set the warning level
            if (retValue != 0 || options.WarningLevel > 0) {

                // The output of the compiler is in UTF8
                string[] lines = ReadAllLines(outputFile, Encoding.UTF8, FileShare.ReadWrite);
                bool replacedArgs = false;
                foreach (string line in lines) {
                    if (!replacedArgs && trueArgs != null && line.Contains(args)) {
                        replacedArgs = true;
                        var outputLine = string.Format("{0}>{1} {2}",
                            Environment.CurrentDirectory,
                            _compilerFullPath,
                            trueArgs);
                        results.Output.Add(outputLine);
                    }
                    else {
                        results.Output.Add(line);
                    }

                    ProcessCompilerOutputLine(results, line);
                }

                // Delete the empty assembly if we created one
                if (retValue != 0 && createdEmptyAssembly) {
                    File.Delete(options.OutputAssembly);
                }
            }

            if (retValue != 0 || results.Errors.HasErrors || !options.GenerateInMemory) {

                results.PathToAssembly = options.OutputAssembly;
                return results;
            }

            // Read assembly into memory:
            byte[] assemblyBuff = File.ReadAllBytes(options.OutputAssembly);

            // Read symbol file into memory and ignore any errors that may be encountered:
            // (This functionality was added in NetFx 4.5, errors must be ignored to ensure compatibility)
            byte[] symbolsBuff = null;
            try {

                string symbFileName = options.TempFiles.BasePath + "." + pdbname;

                if (File.Exists(symbFileName)) {
                    symbolsBuff = File.ReadAllBytes(symbFileName);
                }
            }
            catch {
                symbolsBuff = null;
            }

            // Now get permissions and load assembly from buffer into the CLR:
            var perm = new SecurityPermission(SecurityPermissionFlag.ControlEvidence);
            perm.Assert();

            try {

#pragma warning disable 618 // Load with evidence is obsolete - this warning is passed on via the options.Evidence property
                results.CompiledAssembly = Assembly.Load(assemblyBuff, symbolsBuff, options.Evidence);
#pragma warning restore 618

            }
            finally {
                SecurityPermission.RevertAssert();
            }

            return results;
        }

        private static void ReImpersonate(WindowsImpersonationContext impersonation) {
            impersonation.Undo();
        }

        [PermissionSet(SecurityAction.LinkDemand, Unrestricted = true), SecurityPermission(SecurityAction.Assert, ControlPrincipal = true, UnmanagedCode = true)]
        private static WindowsImpersonationContext RevertImpersonation() {
            return WindowsIdentity.Impersonate(new IntPtr(0));
        }

        private static string[] ReadAllLines(String file, Encoding encoding, FileShare share) {
            using (var stream = File.Open(file, FileMode.Open, FileAccess.Read, share)) {
                String line;
                var lines = new List<String>();

                using (var sr = new StreamReader(stream, encoding)) {
                    while ((line = sr.ReadLine()) != null) {
                        lines.Add(line);
                    }
                }

                return lines.ToArray();
            }
        }

        private void Compile(CompilerParameters options, string compilerFullPath, string arguments,
                              ref string outputFile, ref int nativeReturnValue) {
            string errorFile = null;
            string cmdLine = "\"" + compilerFullPath + "\" " + arguments;
            outputFile = options.TempFiles.AddExtension("out");

            bool profilingSettingIsUpdated = false;
            string originalClrProfilingSetting = null;
            if (AppSettings.DisableProfilingDuringCompilation) {
                // if CLR_PROFILING_SETTING is not set in environment variables, this returns null
                originalClrProfilingSetting = Environment.GetEnvironmentVariable(CLR_PROFILING_SETTING, EnvironmentVariableTarget.Process);
                // if CLR profiling is already disabled, don't bother to set it again
                if (originalClrProfilingSetting != DISABLE_PROFILING) {
                    Environment.SetEnvironmentVariable(CLR_PROFILING_SETTING, DISABLE_PROFILING, EnvironmentVariableTarget.Process);
                    profilingSettingIsUpdated = true;
                }
            }

            nativeReturnValue = Executor.ExecWaitWithCapture(
                options.UserToken,
                cmdLine,
                Environment.CurrentDirectory,
                options.TempFiles,
                ref outputFile,
                ref errorFile);

            if (profilingSettingIsUpdated) {
                Environment.SetEnvironmentVariable(CLR_PROFILING_SETTING, originalClrProfilingSetting, EnvironmentVariableTarget.Process);
            }
        }

        private string GetResponseFileCmdArgs(CompilerParameters options, string cmdArgs) {

            string responseFileName = options.TempFiles.AddExtension("cmdline");
            var responseFileStream = new FileStream(responseFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            try {
                using (var sw = new StreamWriter(responseFileStream, Encoding.UTF8)) {
                    sw.Write(cmdArgs);
                    sw.Flush();
                }
            }
            finally {
                responseFileStream.Close();
            }

            // Always specify the /noconfig flag (outside of the response file)
            return "/noconfig " + FullPathsOption + "@\"" + responseFileName + "\"";
        }

        private static string JoinStringArray(string[] sa, string separator) {
            if (sa == null || sa.Length == 0) {
                return String.Empty;
            }

            if (sa.Length == 1) {
                return "\"" + sa[0] + "\"";
            }

            var sb = new StringBuilder();
            for (int i = 0; i < sa.Length - 1; i++) {
                sb.Append("\"");
                sb.Append(sa[i]);
                sb.Append("\"");
                sb.Append(separator);
            }

            sb.Append("\"");
            sb.Append(sa[sa.Length - 1]);
            sb.Append("\"");

            return sb.ToString();
        }
    }
}
