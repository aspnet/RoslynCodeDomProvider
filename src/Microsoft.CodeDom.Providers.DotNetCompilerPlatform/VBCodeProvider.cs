// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {
    /// <summary>
    /// Provides access to instances of the .NET Compiler Platform VB code generator and code compiler.
    /// </summary>
    [DesignerCategory("code")]
    public sealed class VBCodeProvider : Microsoft.VisualBasic.VBCodeProvider {
        private IProviderOptions _providerOptions;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public VBCodeProvider()
            : this((IProviderOptions)null) {
        }

        /// <summary>
        /// Creates an instance using the given ICompilerSettings
        /// </summary>
        /// <param name="compilerSettings"></param>
        [Obsolete("ICompilerSettings is obsolete. Please update code to use IProviderOptions instead.", false)]
        public VBCodeProvider(ICompilerSettings compilerSettings = null) {
            _providerOptions = compilerSettings == null ? CompilationUtil.VBC2 : new ProviderOptions(compilerSettings);
        }

        /// <summary>
        /// Creates an instance using the given ICompilerSettings
        /// </summary>
        /// <param name="providerOptions"></param>
        public VBCodeProvider(IProviderOptions providerOptions = null) {
            _providerOptions = providerOptions == null ? CompilationUtil.VBC2 : providerOptions;
        }

        /// <summary>
        /// Creates an instance using the given IDictionary to create IProviderOptions
        /// </summary>
        /// <param name="providerOptions"></param>
        public VBCodeProvider(IDictionary<string, string> providerOptions)
            : this(CompilationUtil.CreateProviderOptions(providerOptions, CompilationUtil.VBC2)) { }

        /// <summary>
        /// Gets an instance of the .NET Compiler Platform VB code compiler.
        /// </summary>
        /// <returns>An instance of the .NET Compiler Platform VB code compiler</returns>
        [Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class.")]
        public override ICodeCompiler CreateCompiler() {
            return new VBCompiler(this, _providerOptions);
        }
    }
}
