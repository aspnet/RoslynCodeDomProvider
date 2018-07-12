// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {

	/// <summary>
	/// Provides settings for the C# and VB CodeProviders
	/// </summary>
	public interface ICompilerSettings {

		/// <summary>
		/// The full path to csc.exe or vbc.exe
		/// </summary>
		string CompilerFullPath { get; }

		/// <summary>
		/// TTL in seconds
		/// </summary>
		int CompilerServerTimeToLive { get; }
    }
}
