// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

﻿using System;
using System.CodeDom.Compiler;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {
    /// <summary>
    /// Provides access to instances of the .NET Compiler Platform VB code generator and code compiler.
    /// </summary>
    public sealed class VBCodeProvider : Microsoft.VisualBasic.VBCodeProvider {
        private ICompilerSettings _compilerSettings;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public VBCodeProvider()
            : this(null) {
        }

		/// <summary>
		/// Creates an instance using the given ICompilerSettings
		/// </summary>
		/// <param name="compilerSettings"></param>
		public VBCodeProvider(ICompilerSettings compilerSettings = null) {
            _compilerSettings = compilerSettings == null ? CompilationSettingsHelper.VBC2 : compilerSettings;
        }

        /// <summary>
        /// Gets an instance of the .NET Compiler Platform VB code compiler.
        /// </summary>
        /// <returns>An instance of the .NET Compiler Platform VB code compiler</returns>
        [Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class.")]
        public override ICodeCompiler CreateCompiler() {
            return new VBCompiler(this, _compilerSettings);
        }
    }
}
