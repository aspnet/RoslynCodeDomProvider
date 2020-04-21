// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Configuration;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {
    internal class VBCompiler : Compiler {
        // Command line string for My.* support
        internal static string MySupport = @"/define:_MYTYPE=\""Web\""";
        internal static string VBImportsString;

        private static volatile Regex outputReg;

        public VBCompiler(CodeDomProvider codeDomProvider, IProviderOptions providerOptions = null)
            : base(codeDomProvider, providerOptions) {
        }

        protected override string FileExtension {
            get {
                return ".vb";
            }
        }

        protected override void ProcessCompilerOutputLine(CompilerResults results, string line) {
            if (outputReg == null) {
                outputReg = new Regex(@"^([^(]*)\(?([0-9]*)\)? ?:? ?(error|warning) ([A-Z]+[0-9]+) ?: ((.|\n)*)");
            }
            Match m = outputReg.Match(line);
            if (m.Success) {
                var ce = new CompilerError();
                ce.FileName = m.Groups[1].Value;
                string rawLine = m.Groups[2].Value;
                if (rawLine != null && rawLine.Length > 0) {
                    ce.Line = int.Parse(rawLine, CultureInfo.InvariantCulture);
                }
                if (string.Compare(m.Groups[3].Value, "warning", StringComparison.OrdinalIgnoreCase) == 0) {
                    ce.IsWarning = true;
                }
                ce.ErrorNumber = m.Groups[4].Value;
                ce.ErrorText = m.Groups[5].Value;
                results.Errors.Add(ce);
            }
        }

        protected override void FixUpCompilerParameters(CompilerParameters options) {
            base.FixUpCompilerParameters(options);
            // Hard code OptionInfer to true, which is the default value in the root web config.
            // TODO This code should be removed once CodeDom directly supports provider options such as WarnAsError, OptionInfer
            CompilationUtil.PrependCompilerOption(options, " /optionInfer+");

            List<string> noWarnStrings = new List<string>(3);

            // If VB, add all the imported namespaces on the command line (DevDiv 21499).
            // This is VB only because other languages don't support global command line
            // namespace imports.
            AddVBGlobalNamespaceImports(options);

            // Add any command line flags needed to support the My.* feature
            AddVBMyFlags(options);

            // Ignore vb warning that complains about assemblyKeyName (Dev10 662544)
            // but only for target 3.5 and above (715329)
            noWarnStrings.Add("41008");

            // disable ObsoleteWarnings
            noWarnStrings.Add("40000"); // [Obsolete("with message")]
            noWarnStrings.Add("40008"); // [Obsolete] without message

            if (noWarnStrings.Count > 0) {
                CompilationUtil.PrependCompilerOption(options, "/nowarn:" + String.Join(",", noWarnStrings));
            }
        }

        // The code is copied from NDP\fx\src\CompMod\Microsoft\VisualBasic\VBCodeProvider.cs
        protected override string CmdArgsFromParameters(CompilerParameters parameters) {
            var allArgsBuilder = new StringBuilder();
            if (parameters.GenerateExecutable) {
                allArgsBuilder.Append("/t:exe ");
                if (parameters.MainClass != null && parameters.MainClass.Length > 0) {
                    allArgsBuilder.Append("/main:");
                    allArgsBuilder.Append(parameters.MainClass);
                    allArgsBuilder.Append(" ");
                }
            }
            else {
                allArgsBuilder.Append("/t:library ");
            }

            // Get UTF8 output from the compiler
            allArgsBuilder.Append("/utf8output ");

            string coreAssemblyFileName = parameters.CoreAssemblyFileName;

            if (String.IsNullOrWhiteSpace(parameters.CoreAssemblyFileName)) {
                string probableCoreAssemblyFilePath;
                if (TryGetProbableCoreAssemblyFilePath(parameters, out probableCoreAssemblyFilePath)) {
                    coreAssemblyFileName = probableCoreAssemblyFilePath;
                }
            }

            if (!String.IsNullOrWhiteSpace(coreAssemblyFileName)) {

                string asmblFilePath = coreAssemblyFileName.Trim();
                string asmblFileDir = Path.GetDirectoryName(asmblFilePath);

                allArgsBuilder.Append("/nostdlib ");
                allArgsBuilder.Append("/sdkpath:\"").Append(asmblFileDir).Append("\" ");
                allArgsBuilder.Append("/R:\"").Append(asmblFilePath).Append("\" ");
            }

            // Bug 913691: Explicitly add System.Runtime as a reference.
            string systemRuntimeAssemblyPath = null;
            try {
                var systemRuntimeAssembly = Assembly.Load("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                systemRuntimeAssemblyPath = systemRuntimeAssembly.Location;
            }
            catch {
                // swallow any exceptions if we cannot find the assembly
            }

            if (systemRuntimeAssemblyPath != null) {
                allArgsBuilder.Append(string.Format("/R:\"{0}\" ", systemRuntimeAssemblyPath));
            }

            foreach (string s in parameters.ReferencedAssemblies) {

                // Ignore any Microsoft.VisualBasic.dll, since Visual Basic implies it (bug 72785)
                string fileName = Path.GetFileName(s);
                if (string.Compare(fileName, "Microsoft.VisualBasic.dll", StringComparison.OrdinalIgnoreCase) == 0)
                    continue;

                // Same deal for mscorlib (bug ASURT 81568)
                if (string.Compare(fileName, "mscorlib.dll", StringComparison.OrdinalIgnoreCase) == 0)
                    continue;

                allArgsBuilder.Append("/R:");
                allArgsBuilder.Append("\"");
                allArgsBuilder.Append(s);
                allArgsBuilder.Append("\"");
                allArgsBuilder.Append(" ");
            }

            allArgsBuilder.Append("/out:");
            allArgsBuilder.Append("\"");
            allArgsBuilder.Append(parameters.OutputAssembly);
            allArgsBuilder.Append("\"");
            allArgsBuilder.Append(" ");

            if (parameters.IncludeDebugInformation) {
                allArgsBuilder.Append("/D:DEBUG=1 ");
                allArgsBuilder.Append("/debug+ ");
            }
            else {
                allArgsBuilder.Append("/debug- ");
            }

            if (parameters.Win32Resource != null) {
                allArgsBuilder.Append("/win32resource:\"" + parameters.Win32Resource + "\" ");
            }

            foreach (string s in parameters.EmbeddedResources) {
                allArgsBuilder.Append("/res:\"");
                allArgsBuilder.Append(s);
                allArgsBuilder.Append("\" ");
            }

            foreach (string s in parameters.LinkedResources) {
                allArgsBuilder.Append("/linkres:\"");
                allArgsBuilder.Append(s);
                allArgsBuilder.Append("\" ");
            }

            if (parameters.TreatWarningsAsErrors) {
                allArgsBuilder.Append("/warnaserror+ ");
            }

            if (parameters.CompilerOptions != null) {
                allArgsBuilder.Append(parameters.CompilerOptions + " ");
            }

            return allArgsBuilder.ToString();
        }

        protected override string FullPathsOption {
            get {
                // VBC does not recognize this option.
                return "";
            }
        }

        // The code is copied from NDP\fx\src\CompMod\System\CodeDOM\Compiler\CodeDOMProvider.cs
        internal static bool TryGetProbableCoreAssemblyFilePath(CompilerParameters parameters, out string coreAssemblyFilePath) {
            string multiTargetingPackRoot = null;
            char[] pathSeperators = new char[] { Path.DirectorySeparatorChar };

            // Valid paths look like "...\Reference Assemblies\Microsoft\Framework\<SkuName>\v<Version>\..."
            string referenceAssemblyFolderPrefix = Path.Combine("Reference Assemblies", "Microsoft", "Framework");

            foreach (string s in parameters.ReferencedAssemblies) {

                if (Path.GetFileName(s).Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase)) {
                    // They already have their own mscorlib.dll, so let's not add another one.
                    coreAssemblyFilePath = string.Empty;
                    return false;
                }

                if (s.IndexOf(referenceAssemblyFolderPrefix, StringComparison.OrdinalIgnoreCase) >= 0) {

                    String[] dirs = s.Split(pathSeperators, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < dirs.Length - 5; i++) {

                        if (String.Equals(dirs[i], "Reference Assemblies", StringComparison.OrdinalIgnoreCase)) {
                            // Here i+5 is the index of the thing after the vX.XXX folder (if one exists) and i+4 should look like a version.
                            // (i.e. start with a v).
                            if (dirs[i + 4].StartsWith("v", StringComparison.OrdinalIgnoreCase)) {
                                if (multiTargetingPackRoot != null) {
                                    if (!String.Equals(multiTargetingPackRoot, Path.GetDirectoryName(s), StringComparison.OrdinalIgnoreCase)) {
                                        // We found one reference to one targeting pack and one referece to another.  Bail out.
                                        coreAssemblyFilePath = string.Empty;
                                        return false;
                                    }
                                }
                                else {
                                    multiTargetingPackRoot = Path.GetDirectoryName(s);
                                }
                            }
                        }
                    }
                }
            }

            if (multiTargetingPackRoot != null) {
                coreAssemblyFilePath = Path.Combine(multiTargetingPackRoot, "mscorlib.dll");
                return true;
            }

            coreAssemblyFilePath = string.Empty;
            return false;
        }

        // The code is copied from NDP\fx\src\xsp\system\web\compilation\assemblybuilder.cs
        private static void AddVBMyFlags(CompilerParameters compilParams) {

            // Prepend it to the compilerOptions
            if (compilParams.CompilerOptions == null)
                compilParams.CompilerOptions = MySupport;
            else
                compilParams.CompilerOptions = MySupport + " " + compilParams.CompilerOptions;
        }

        // The code is copied from NDP\fx\src\xsp\system\web\compilation\assemblybuilder.cs
        private static void AddVBGlobalNamespaceImports(CompilerParameters compilParams) {
            // Put together the VB import string on demand
            if (VBImportsString == null) {
                // Get the Web application configuration.
                PagesSection pagesConfig =
                    (PagesSection)WebConfigurationManager.GetSection("system.web/pages");
                if (pagesConfig.Namespaces == null) {
                    VBImportsString = String.Empty;
                }
                else {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("/imports:");

                    bool nextItemNeedsComma = false;

                    // Auto-import Microsoft.VisualBasic is needed
                    if (pagesConfig.Namespaces.AutoImportVBNamespace) {
                        sb.Append("Microsoft.VisualBasic");
                        nextItemNeedsComma = true;
                    }

                    // Add all the namespaces from the config <namespaces> section
                    foreach (NamespaceInfo entry in pagesConfig.Namespaces) {

                        // If there was a previous entry, we need a comma separator
                        if (nextItemNeedsComma)
                            sb.Append(',');

                        sb.Append(entry.Namespace);

                        nextItemNeedsComma = true;
                    }

                    VBImportsString = sb.ToString();
                }
            }

            // Prepend it to the compilerOptions
            if (VBImportsString.Length > 0) {
                CompilationUtil.PrependCompilerOption(compilParams, VBImportsString);
            }
        }
    }
}
