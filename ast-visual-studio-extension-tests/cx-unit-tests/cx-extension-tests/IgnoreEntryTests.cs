using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Ignore;
using Newtonsoft.Json;
using System.Collections.Generic;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for <see cref="IgnoreEntry"/> — JSON contract and the per-scanner key formula
    /// (these are the fields a JetBrains-shared <c>.checkmarxIgnored</c> file must round-trip through).
    /// </summary>
    public class IgnoreEntryTests
    {
        [Fact]
        public void Json_RoundTrip_PreservesAllFields()
        {
            var original = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageManager = "npm",
                PackageName = "lodash",
                PackageVersion = "4.17.21",
                Severity = "High",
                Description = "Prototype pollution",
                Title = "lodash",
                DateAdded = "2026-05-25T10:30:45.123Z",
                Files = new List<IgnoreEntry.FileReference>
                {
                    new IgnoreEntry.FileReference { Path = "package.json", Active = true, Line = 15 }
                }
            };

            string json = JsonConvert.SerializeObject(original);
            var roundTripped = JsonConvert.DeserializeObject<IgnoreEntry>(json);

            Assert.Equal(ScannerType.OSS, roundTripped.Type);
            Assert.Equal("npm", roundTripped.PackageManager);
            Assert.Equal("lodash", roundTripped.PackageName);
            Assert.Equal("4.17.21", roundTripped.PackageVersion);
            Assert.Equal("High", roundTripped.Severity);
            Assert.Single(roundTripped.Files);
            Assert.Equal("package.json", roundTripped.Files[0].Path);
            Assert.True(roundTripped.Files[0].Active);
            Assert.Equal(15, roundTripped.Files[0].Line);
        }

        [Fact]
        public void Json_NullScannerSpecificFields_AreOmitted()
        {
            // An IaC entry shouldn't carry PackageManager / ImageName etc.
            var entry = new IgnoreEntry
            {
                Type = ScannerType.IaC,
                Title = "rule-x",
                SimilarityId = "abc",
                Severity = "High",
            };
            string json = JsonConvert.SerializeObject(entry);
            Assert.DoesNotContain("packageManager", json);
            Assert.DoesNotContain("imageName", json);
            Assert.DoesNotContain("secretValue", json);
        }

        [Fact]
        public void Key_Oss_IncludesManagerNameAndVersion()
        {
            string key = IgnoreEntry.BuildKey(ScannerType.OSS, null, null, null, "npm", "lodash", "4.17.21", null, null, null, "package.json");
            Assert.Equal("npm:lodash:4.17.21", key);
        }

        [Fact]
        public void Key_Secrets_IncludesTitleSecretValueAndFile()
        {
            string key = IgnoreEntry.BuildKey(ScannerType.Secrets, "aws-access-key", null, null, null, null, null, null, null, "AKIAIOSFODNN7EXAMPLE", "src/config.ts");
            Assert.Equal("aws-access-key:AKIAIOSFODNN7EXAMPLE:src/config.ts", key);
        }

        [Fact]
        public void Key_Iac_IncludesTitleSimilarityIdAndFile()
        {
            string key = IgnoreEntry.BuildKey(ScannerType.IaC, "S3-Bucket-Public", null, "sim-123", null, null, null, null, null, null, "infra/s3.tf");
            Assert.Equal("S3-Bucket-Public:sim-123:infra/s3.tf", key);
        }

        [Fact]
        public void Key_Asca_IncludesTitleRuleIdAndFile()
        {
            string key = IgnoreEntry.BuildKey(ScannerType.ASCA, "SQL-Injection", 123, null, null, null, null, null, null, null, "src/queries.cs");
            Assert.Equal("SQL-Injection:123:src/queries.cs", key);
        }

        [Fact]
        public void Key_Containers_OmitsFilePath()
        {
            // Containers ignore is global per image:tag (JetBrains parity).
            string key = IgnoreEntry.BuildKey(ScannerType.Containers, null, null, null, null, null, null, "nginx", "latest", null, "ignored");
            Assert.Equal("nginx:latest", key);
        }

        [Fact]
        public void Key_NullRuleId_RendersAsEmpty()
        {
            string key = IgnoreEntry.BuildKey(ScannerType.ASCA, "X", null, null, null, null, null, null, null, null, "f.cs");
            Assert.Equal("X::f.cs", key);
        }
    }
}
