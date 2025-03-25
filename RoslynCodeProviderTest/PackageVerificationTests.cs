using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Xunit;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatformTest
{
    /// <summary>
    /// Holds ZipArchive objects for the two packages, so we only open them once.
    /// </summary>
    public class PackageVerificationFixture : IDisposable
    {
#if DEBUG
        private const string PackageOutDir = @"..\..\..\.binaries\Packages\Debug";
#else
        private const string PackageOutDir = @"..\..\..\.binaries\Packages\Release";
#endif

        public ZipArchive CompilerPlatformZip { get; }
        public ZipArchive WebSitesZip { get; }

        public PackageVerificationFixture()
        {
            string codedomProvider = Path.Combine(PackageOutDir, $"Microsoft.CodeDom.Providers.DotNetCompilerPlatform.{CommonCodeDomProviderTests.ExpectedNugetVersion}.nupkg");
            Assert.True(File.Exists(codedomProvider), $"File not found: {codedomProvider}");

            string websites = Path.Combine(PackageOutDir, $"Microsoft.CodeDom.Providers.DotNetCompilerPlatform.WebSites.{CommonCodeDomProviderTests.ExpectedNugetVersion}.nupkg");
            Assert.True(File.Exists(websites), $"File not found: {websites}");

            CompilerPlatformZip = ZipFile.OpenRead(codedomProvider);
            WebSitesZip = ZipFile.OpenRead(websites);
        }

        public void Dispose()
        {
            CompilerPlatformZip?.Dispose();
            WebSitesZip?.Dispose();
        }
    }

    /// <summary>
    /// A collection definition so that xUnit ties our fixture to this group of tests.
    /// </summary>
    [CollectionDefinition("PackageVerificationCollection")]
    public class PackageVerificationCollection : ICollectionFixture<PackageVerificationFixture> { }

    [Collection("PackageVerificationCollection")]
    public class PackageVerificationTests
    {
        private readonly PackageVerificationFixture _fixture;

        public PackageVerificationTests(PackageVerificationFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void CompilerPlatformPackage_ContainsNet472TargetsFileAndNoForbiddenText()
        {
            var targetFiles = VerifyPlatforms(_fixture.CompilerPlatformZip, "build/", new[] { "net472" }, exclusive: true);
            VerifyAllowedExtensions(targetFiles, new[] { ".targets" });
            Assert.Single(targetFiles);

            using (var reader = new StreamReader(targetFiles[0].Open()))
            {
                var content = reader.ReadToEnd();
                Assert.DoesNotContain("$compilerPlatformFQAN$", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("$roslynToolPath$", content, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void WebSitesPlatformPackage_ContainsNet472Scripts()
        {
            var contentFiles = VerifyPlatforms(_fixture.WebSitesZip, "content/", new[] { "net472" }, exclusive: true);
            VerifyAllowedExtensions(contentFiles, new[] { ".xdt" });
            Assert.Equal(2, contentFiles.Count);
        }

        [Fact]
        public void BothPackages_ContainDocsReadmeAndIconsDotnet()
        {
            VerifyCommonFiles(_fixture.CompilerPlatformZip);
            VerifyCommonFiles(_fixture.WebSitesZip);
        }

        [Fact]
        public void CompilerPlatformPackage_ContainsRoslynDirectoryWithExpectedFiles()
        {
            var roslynDir = $"tools/Roslyn-{CommonCodeDomProviderTests.ExpectedVersion.Major}.{CommonCodeDomProviderTests.ExpectedVersion.Minor}.{CommonCodeDomProviderTests.ExpectedVersion.Revision}";
            var roslynDirEntries = _fixture.CompilerPlatformZip.Entries
                .Where(e => e.FullName.StartsWith(roslynDir, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.True(roslynDirEntries.Count > 30, "Expected Roslyn directory with more than 30 files.");

            Assert.Contains(roslynDirEntries, e => e.Name.Equals("csc.exe", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(roslynDirEntries, e => e.Name.Equals("vbc.exe", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(roslynDirEntries, e => e.Name.Equals("VBCSCompiler.exe", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void PackageVersionsAndLibDllsMatch_IfPresent()
        {
            // Example placeholder test for future version checks
            // Could parse .nuspec or compare with anything in /lib
            // Implementation depends on specific project constraints
            Assert.NotNull(_fixture.CompilerPlatformZip);
        }

        private static void VerifyCommonFiles(ZipArchive zip)
        {
            Assert.Contains(zip.Entries, e => e.FullName.Equals("docs/Readme.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(zip.Entries, e => e.FullName.Equals("icons/dotnet.png", StringComparison.OrdinalIgnoreCase));
        }

        private static IList<ZipArchiveEntry> VerifyPlatforms(ZipArchive zip, string dir, string[] platforms, bool exclusive = false, string extension = null)
        {
            dir = dir.TrimEnd('/') + "/";
            var filesInDir = zip.Entries
                .Where(e => e.FullName.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                .Where(e => extension == null || e.FullName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Verify all platform directories exist
            var platformDirs = platforms.Select(p => $"{dir}{p}/").ToList();
            foreach (var platformDir in platformDirs)
            {
                Assert.Contains(zip.Entries, e => e.FullName.StartsWith(platformDir, StringComparison.OrdinalIgnoreCase));
            }

            // Verify all files in the base directory are in an allowed platform folder
            if (exclusive)
            {
                foreach (var entry in filesInDir)
                {
                    var entryIsInAllowedPlatformFolder = platforms.Any(platform => entry.FullName.StartsWith($"{dir}{platform}/", StringComparison.OrdinalIgnoreCase));
                    Assert.True(entryIsInAllowedPlatformFolder, $"Unexpected file: {entry.FullName}");
                }
            }

            return filesInDir;
        }

        private static void VerifyAllowedExtensions(IList<ZipArchiveEntry> entries, string[] allowedExtensions)
        {
            foreach (var entry in entries)
            {
                var extension = Path.GetExtension(entry.FullName);
                Assert.Contains(allowedExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
