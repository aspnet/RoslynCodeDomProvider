// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {
    internal static class CompilationUtil {
        private const int DefaultCompilerServerTTL = 10; // 10 seconds
        private const int DefaultCompilerServerTTLInDevEnvironment = 60 * 15; // 15 minutes

        static CompilationUtil()
        {
            CSC2 = GetProviderOptionsFor(".cs");
            VBC2 = GetProviderOptionsFor(".vb");

            if (IsDebuggerAttached)
            {
                Environment.SetEnvironmentVariable("IN_DEBUG_MODE", "1", EnvironmentVariableTarget.Process);
            }
        }

        public static IProviderOptions CSC2 { get; }

        public static IProviderOptions VBC2 { get; }

        internal static IProviderOptions CreateProviderOptions(IDictionary<string, string> options, IProviderOptions baseOptions)
        {
            Dictionary<string, string> allOptions = null;

            // Copy the base options
            ProviderOptions providerOpts = new ProviderOptions(baseOptions);

            // Update as necessary. Case-sensitive.
            foreach (var option in options)
            {
                if (String.IsNullOrWhiteSpace(option.Key))
                    continue;

                switch (option.Key)
                {
                    case "CompilerFullPath":
                        providerOpts.CompilerFullPath = option.Value;
                        break;

                    case "CompilerServerTimeToLive":
                        if (Int32.TryParse(option.Value, out int newTTL))
                            providerOpts.CompilerServerTimeToLive = newTTL;
                        break;

                    case "CompilerVersion":
                        providerOpts.CompilerVersion = option.Value;
                        break;

                    case "WarnAsError":
                        if (Boolean.TryParse(option.Value, out bool warnAsError))
                            providerOpts.WarnAsError = warnAsError;
                        break;

                    case "AllOptions":
                        allOptions = allOptions ?? new Dictionary<string, string>(providerOpts.AllOptions);
                        allOptions.Remove(option.Key);
                        allOptions.Add(option.Key, option.Value);
                        break;

                    default:
                        break;
                }
            }

            if (allOptions != null)
                providerOpts.AllOptions = allOptions;

            return providerOpts;
        }

        public static IProviderOptions GetProviderOptionsFor(string fileExt)
        {
            //
            // AllOptions
            //
            IDictionary<string, string> options = GetProviderOptionsCollection(fileExt);

            //
            // CompilerFullPath
            //
            string compilerFullPath = Environment.GetEnvironmentVariable("ROSLYN_COMPILER_LOCATION");
            if (String.IsNullOrEmpty(compilerFullPath))
                compilerFullPath = AppSettings.RoslynCompilerLocation;
            if (String.IsNullOrEmpty(compilerFullPath))
                options.TryGetValue("CompilerLocation", out compilerFullPath);
            if (String.IsNullOrEmpty(compilerFullPath))
                compilerFullPath = CompilerDefaultPath();

            if (!String.IsNullOrWhiteSpace(fileExt))
            {
                // If we have a file extension, try to infer the compiler to use
                // TODO: Should we also check compilerFullPath to assert it is a Directory and not a file?
                if (fileExt.Equals(".cs", StringComparison.InvariantCultureIgnoreCase) || fileExt.Equals("cs", StringComparison.InvariantCultureIgnoreCase))
                    compilerFullPath = Path.Combine(compilerFullPath, "csc.exe");
                else if (fileExt.Equals(".vb", StringComparison.InvariantCultureIgnoreCase) || fileExt.Equals("vb", StringComparison.InvariantCultureIgnoreCase))
                    compilerFullPath = Path.Combine(compilerFullPath, "vbc.exe");
            }


            //
            // CompilerServerTimeToLive - default 10 seconds in production, 15 minutes in dev environment.
            //
            int ttl;
            string ttlstr = Environment.GetEnvironmentVariable("VBCSCOMPILER_TTL");
            if (String.IsNullOrEmpty(ttlstr))
                options.TryGetValue("CompilerServerTTL", out ttlstr);
            if (!Int32.TryParse(ttlstr, out ttl))
            {
                ttl = DefaultCompilerServerTTL;

                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEV_ENVIRONMENT")) ||
                    !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("IN_DEBUG_MODE")) ||
                    CompilationUtil.IsDebuggerAttached)
                {
                    ttl = DefaultCompilerServerTTLInDevEnvironment;
                }
            }

            //
            // CompilerVersion - if this is null, we don't care.
            //
            string compilerVersion;
            options.TryGetValue("CompilerVersion", out compilerVersion);    // Failure to parse sets to null

            //
            // WarnAsError - default false.
            //
            bool warnAsError = false;
            if (options.TryGetValue("WarnAsError", out string sWAE))
            {
                Boolean.TryParse(sWAE, out warnAsError); // Failure to parse sets to 'false'
            }

            //
            // UseAspNetSettings - default true. This was meant to be an ASP.Net support package first and foremost.
            //
            bool useAspNetSettings = true;
            if (options.TryGetValue("UseAspNetSettings", out string sUANS))
            {
                // Failure to parse sets to 'false', but we want to keep the default 'true'.
                if (!Boolean.TryParse(sUANS, out useAspNetSettings))
                    useAspNetSettings = true;
            }

            ProviderOptions providerOptions = new ProviderOptions()
            {
                CompilerFullPath = compilerFullPath,
                CompilerServerTimeToLive = ttl,
                CompilerVersion = compilerVersion,
                WarnAsError = warnAsError,
                UseAspNetSettings = useAspNetSettings,
                AllOptions = options
            };

            return providerOptions;
        }

        internal static IDictionary<string, string> GetProviderOptionsCollection(string fileExt)
        {
            Dictionary<string, string> opts = new Dictionary<string, string>();

            if (!CodeDomProvider.IsDefinedExtension(fileExt))
                return new ReadOnlyDictionary<string, string>(opts);

            CompilerInfo ci = CodeDomProvider.GetCompilerInfo(CodeDomProvider.GetLanguageFromExtension(fileExt));

            if (ci == null)
                return new ReadOnlyDictionary<string, string>(opts);

            // There is a fun little comment about this property in the framework code about making it
            // public after 3.5. Guess that didn't happen. Oh well. :)
            PropertyInfo pi = ci.GetType().GetProperty("ProviderOptions",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
            if (pi == null)
                return new ReadOnlyDictionary<string, string>(opts);

            return new ReadOnlyDictionary<string, string>((IDictionary<string, string>)pi.GetValue(ci, null));
        }

        internal static void PrependCompilerOption(CompilerParameters compilParams, string compilerOptions)
        {
            if (compilParams.CompilerOptions == null)
            {
                compilParams.CompilerOptions = compilerOptions;
            }
            else
            {
                compilParams.CompilerOptions = compilerOptions + " " + compilParams.CompilerOptions;
            }
        }

        internal static string CompilerDefaultPath()
        {
            string webPath = @"bin\roslyn";
            string appPath = @"roslyn";

            // Check bin folder first
            string compilerFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, webPath);

            // Then appdomain base
            if (!File.Exists(compilerFullPath))
                compilerFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appPath);

            return compilerFullPath;
        }

        internal static bool IsDebuggerAttached
        {
            get {
                return IsDebuggerPresent() || Debugger.IsAttached;
            }
        }

        [DllImport("kernel32.dll")]
        private extern static bool IsDebuggerPresent();
    }
}
