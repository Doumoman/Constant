using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Generation
{
    public sealed class SeedReplayRecorderTests
    {
        private const string GenerationProfileId = "GEN_REPLAY";
        private const string WorldProfileId = "WORLD_REPLAY";
        private const string BuildId = "generator-build-1";
        private static readonly DateTimeOffset StartUtc =
            new DateTimeOffset(2026, 8, 12, 1, 2, 3, TimeSpan.Zero);

        [Test]
        public void Manifest_PreservesExactFields()
        {
            var manifest = Manifest(failures: new[] { "RULE_B", "RULE_A", "RULE_B" });
            Assert.That(manifest.WorldProfileId, Is.EqualTo(WorldProfileId));
            Assert.That(manifest.Seed, Is.EqualTo(42UL));
            Assert.That(manifest.ContentVersionHash, Is.EqualTo(Hash().Hex));
            Assert.That(manifest.GenerationProfileId, Is.EqualTo(GenerationProfileId));
            Assert.That(manifest.GeneratorBuildId, Is.EqualTo(BuildId));
            Assert.That(manifest.Approved, Is.False);
            Assert.That(manifest.GenerationStartedUtc, Is.EqualTo(StartUtc));
            Assert.That(manifest.GenerationDurationMilliseconds, Is.EqualTo(12));
            Assert.That(manifest.RetryCountTotal, Is.Zero);
            Assert.That(manifest.FailureRuleIds, Is.EqualTo(new[] { "RULE_B", "RULE_A", "RULE_B" }));
            Assert.That(manifest.Notes, Is.EqualTo(SeedManifest.GridCheckpointNotes));
        }

        [Test]
        public void Manifest_SnapshotsFailureIdsAsReadOnly()
        {
            var source = new List<string> { "A", "B" };
            var manifest = Manifest(failures: source);
            source.Clear();
            Assert.That(manifest.FailureRuleIds, Is.EqualTo(new[] { "A", "B" }));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)manifest.FailureRuleIds).Add("C"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        public void Manifest_RejectsInvalidConstructorState(int mutation)
        {
            var failures = mutation == 10 ? null : mutation == 11 ? new[] { "" } :
                mutation == 12 ? new[] { "A|B" } : Array.Empty<string>();
            Assert.Catch<ArgumentException>(() => new SeedManifest(
                mutation == 0 ? null : mutation == 1 ? "" : WorldProfileId,
                42,
                mutation == 2 ? null : mutation == 3 ? Hash().Hex.ToUpperInvariant() : Hash().Hex,
                mutation == 4 ? null : mutation == 5 ? "" : GenerationProfileId,
                mutation == 6 ? null : mutation == 7 ? "" : BuildId,
                false,
                mutation == 8 ? StartUtc.ToOffset(TimeSpan.FromHours(1)) : StartUtc,
                mutation == 9 ? -1 : 0,
                0,
                failures,
                SeedManifest.GridCheckpointNotes));
        }

        [Test]
        public void Manifest_RejectsNegativeRetryCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SeedManifest(
                WorldProfileId, 1, Hash().Hex, GenerationProfileId, BuildId, false,
                StartUtc, 0, -1, Array.Empty<string>(), SeedManifest.GridCheckpointNotes));
        }

        [Test]
        public void Manifest_RejectsNullNotes()
        {
            Assert.Throws<ArgumentNullException>(() => new SeedManifest(
                WorldProfileId, 1, Hash().Hex, GenerationProfileId, BuildId, false,
                StartUtc, 0, 0, Array.Empty<string>(), null));
        }

        [Test]
        public void Csv_HasExactFilenameHeaderTemplateLengthAndHash()
        {
            Assert.That(SeedManifestCsvSerializer.FileName, Is.EqualTo("seed_manifest.csv"));
            Assert.That(SeedManifestCsvSerializer.Header.Split(',').Length, Is.EqualTo(11));
            var bytes = SeedManifestCsvSerializer.SerializeHeaderOnly();
            Assert.That(bytes.Length, Is.EqualTo(184));
            Assert.That(Sha256(bytes), Is.EqualTo(
                "fb45bfbb905f165b4702515484b97c83232fca9aa7bf775dd46cc52421761b0c"));
        }

        [Test]
        public void Csv_SerializesExactEnvelopeAndRoundTrips()
        {
            var manifest = Manifest();
            var bytes = SeedManifestCsvSerializer.Serialize(manifest);
            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            var text = CsvText(bytes);
            Assert.That(text.Split(new[] { "\r\n" }, StringSplitOptions.None).Length, Is.EqualTo(3));
            Assert.That(text.EndsWith("\r\n", StringComparison.Ordinal), Is.True);
            CollectionAssert.AreEqual(bytes, SeedManifestCsvSerializer.Serialize(
                SeedManifestCsvSerializer.Deserialize(bytes)));
        }

        [TestCase("WORLD,ONE")]
        [TestCase("WORLD\"ONE")]
        [TestCase("WORLD\r\nONE")]
        public void Csv_Rfc4180EscapesStringFields(string value)
        {
            var manifest = Manifest(worldProfileId: value, notes: value);
            var bytes = SeedManifestCsvSerializer.Serialize(manifest);
            var parsed = SeedManifestCsvSerializer.Deserialize(bytes);
            Assert.That(parsed.WorldProfileId, Is.EqualTo(value));
            Assert.That(parsed.Notes, Is.EqualTo(value));
        }

        [TestCase(false, ",0,2026-")]
        [TestCase(true, ",1,2026-")]
        public void Csv_UsesZeroOrOneForBoolean(bool approved, string expected)
        {
            Assert.That(CsvText(SeedManifestCsvSerializer.Serialize(Manifest(approved: approved))),
                Does.Contain(expected));
        }

        [TestCase(0UL, ",0,")]
        [TestCase(ulong.MaxValue, ",18446744073709551615,")]
        public void Csv_UsesInvariantUnsignedSeed(ulong seed, string expected)
        {
            Assert.That(CsvText(SeedManifestCsvSerializer.Serialize(Manifest(seed: seed))),
                Does.Contain(expected));
        }

        [Test]
        public void Csv_PreservesFailureRuleOrderAndDuplicates()
        {
            var parsed = SeedManifestCsvSerializer.Deserialize(SeedManifestCsvSerializer.Serialize(
                Manifest(failures: new[] { "B", "A", "B" })));
            Assert.That(parsed.FailureRuleIds, Is.EqualTo(new[] { "B", "A", "B" }));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        public void Csv_RejectsNonCanonicalOrMalformedBytes(int mutation)
        {
            var original = SeedManifestCsvSerializer.Serialize(Manifest());
            var text = CsvText(original);
            byte[] bytes;
            switch (mutation)
            {
                case 0: bytes = original.Skip(1).ToArray(); break;
                case 1: bytes = original.Take(3).Concat(original).ToArray(); break;
                case 2: bytes = CsvBytes(text.Replace("\r\n", "\n")); break;
                case 3: bytes = CsvBytes("x" + text.Substring(1)); break;
                case 4: bytes = CsvBytes(text + "extra\r\n"); break;
                case 5: bytes = SeedManifestCsvSerializer.SerializeHeaderOnly(); break;
                case 6: bytes = (byte[])original.Clone(); bytes[bytes.Length - 3] = 0xff; break;
                case 7: bytes = CsvBytes(text.Replace(",0,2026-", ",2,2026-")); break;
                case 8: bytes = CsvBytes(text.Replace(",42,", ",042,")); break;
                case 9: bytes = CsvBytes(text.Replace(",12,0,,", ",-1,0,,")); break;
                case 10: bytes = CsvBytes(text.Replace(",12,0,,", ",12,-1,,")); break;
                case 11: bytes = CsvBytes(text.Replace("2026-08-12", "2026/08/12")); break;
                case 12: bytes = CsvBytes(text.Replace(SeedManifest.GridCheckpointNotes + "\r\n",
                    SeedManifest.GridCheckpointNotes + ",extra\r\n")); break;
                case 13: bytes = CsvBytes(text.Substring(0, text.Length - 2)); break;
                case 14: bytes = CsvBytes(text + "\r\n"); break;
                case 15: bytes = CsvBytes(text.Replace("," + SeedManifest.GridCheckpointNotes + "\r\n",
                    ",\"" + SeedManifest.GridCheckpointNotes + "\"x\r\n")); break;
                case 16: bytes = CsvBytes(text.Replace(Hash().Hex, Hash().Hex.ToUpperInvariant())); break;
                default: bytes = CsvBytes("\ufeff" + text); break;
            }
            Assert.Catch<ArgumentException>(() => SeedManifestCsvSerializer.Deserialize(bytes));
        }

        [TestCase(0UL, "GeneratedWorlds/WORLD_REPLAY/0000000000000000")]
        [TestCase(42UL, "GeneratedWorlds/WORLD_REPLAY/0000000000000042")]
        [TestCase(ulong.MaxValue, "GeneratedWorlds/WORLD_REPLAY/18446744073709551615")]
        public void Bundle_UsesFrozenRelativeDirectory(ulong seed, string expected)
        {
            Assert.That(SeedReplayBundle.GetRelativeDirectory(WorldProfileId, seed), Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase(".")]
        [TestCase("..")]
        [TestCase("A/B")]
        [TestCase("A\\B")]
        [TestCase("A:B")]
        [TestCase("A*B")]
        [TestCase("A?B")]
        [TestCase("A<B")]
        [TestCase("A>B")]
        [TestCase("A|B")]
        [TestCase("A\u0001B")]
        [TestCase("WORLD.")]
        [TestCase("WORLD ")]
        [TestCase("CON")]
        [TestCase("COM1")]
        [TestCase("LPT9.txt")]
        public void Bundle_RejectsUnsafeWorldProfileSegment(string value)
        {
            Assert.Catch<ArgumentException>(() => SeedReplayBundle.GetRelativeDirectory(value, 0));
        }

        [Test]
        public void Bundle_CopiesBytesAndExposesReadOnlyExactFileOrder()
        {
            var bundle = Bundle();
            var manifestBytes = bundle.SeedManifestBytes;
            var sectorBytes = bundle.GeneratedWorldSectorsBytes;
            manifestBytes[0] = 0;
            sectorBytes[0] = 0;
            Assert.That(bundle.SeedManifestBytes[0], Is.EqualTo(0xEF));
            Assert.That(bundle.GeneratedWorldSectorsBytes[0], Is.EqualTo(0xEF));
            Assert.That(bundle.FileNames, Is.EqualTo(new[]
            {
                SeedManifestCsvSerializer.FileName,
                GeneratedWorldDataCsvSerializer.FileName
            }));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)bundle.FileNames).Add("extra"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Bundle_RejectsWrongFileSet(int mutation)
        {
            var source = Bundle();
            IEnumerable<string> names = mutation == 0 ? new[] { SeedManifestCsvSerializer.FileName } :
                mutation == 1 ? source.FileNames.Reverse() :
                mutation == 2 ? source.FileNames.Concat(new[] { "extra" }) :
                new[] { SeedManifestCsvSerializer.FileName, SeedManifestCsvSerializer.FileName };
            Assert.Throws<ArgumentException>(() => new SeedReplayBundle(
                source.Manifest, source.RelativeDirectory, source.SeedManifestBytes,
                source.GeneratedWorldSectorsBytes, names));
        }

        [Test]
        public void Bundle_RejectsManifestObjectByteMismatch()
        {
            var source = Bundle();
            Assert.Throws<ArgumentException>(() => new SeedReplayBundle(
                Manifest(seed: 43), SeedReplayBundle.GetRelativeDirectory(WorldProfileId, 43),
                source.SeedManifestBytes, source.GeneratedWorldSectorsBytes));
        }

        [Test]
        public void Bundle_RejectsRelativeDirectoryMismatch()
        {
            var source = Bundle();
            Assert.Throws<ArgumentException>(() => new SeedReplayBundle(
                source.Manifest, source.RelativeDirectory + "x", source.SeedManifestBytes,
                source.GeneratedWorldSectorsBytes));
        }

        [Test]
        public void Bundle_RejectsTamperedSectorBytes()
        {
            var source = Bundle();
            var bytes = source.GeneratedWorldSectorsBytes;
            bytes[bytes.Length - 3] = (byte)'1';
            Assert.Throws<ArgumentException>(() => new SeedReplayBundle(
                source.Manifest, source.RelativeDirectory, source.SeedManifestBytes, bytes));
        }

        [Test]
        public void Recorder_MapsSuccessfulGridCheckpointExactly()
        {
            var bundle = Bundle(987);
            Assert.That(bundle.Manifest.WorldProfileId, Is.EqualTo(WorldProfileId));
            Assert.That(bundle.Manifest.Seed, Is.EqualTo(987UL));
            Assert.That(bundle.Manifest.ContentVersionHash, Is.EqualTo(Hash().Hex));
            Assert.That(bundle.Manifest.GenerationProfileId, Is.EqualTo(GenerationProfileId));
            Assert.That(bundle.Manifest.GeneratorBuildId, Is.EqualTo(BuildId));
            Assert.That(bundle.Manifest.Approved, Is.False);
            Assert.That(bundle.Manifest.FailureRuleIds, Is.Empty);
            Assert.That(bundle.Manifest.Notes, Is.EqualTo(SeedManifest.GridCheckpointNotes));
        }

        [Test]
        public void Recorder_DoesNotReexecuteGridPass()
        {
            var pass = new GridPassAdapter();
            var execution = Root(pass: pass).ExecuteThroughRecorded(GenerationProfileId, 9, GridInitializationPass.PassId);
            Assert.That(pass.InvocationCount, Is.EqualTo(1));
            new SeedReplayRecorder().Record(execution, Hash(), BuildId);
            Assert.That(pass.InvocationCount, Is.EqualTo(1));
        }

        [Test]
        public void Recorder_RejectsNonThroughExecution()
        {
            var execution = Root().ExecuteRecorded(GenerationProfileId, 9);
            Assert.Throws<ArgumentException>(() => new SeedReplayRecorder().Record(execution, Hash(), BuildId));
        }

        [Test]
        public void Recorder_RejectsFailedExecution()
        {
            var execution = Root(pass: new GridPassAdapter(fail: true))
                .ExecuteThroughRecorded(GenerationProfileId, 9, GridInitializationPass.PassId);
            Assert.Throws<ArgumentException>(() => new SeedReplayRecorder().Record(execution, Hash(), BuildId));
        }

        [Test]
        public void Recorder_RejectsNullHashAndEmptyBuild()
        {
            var execution = Root().ExecuteThroughRecorded(GenerationProfileId, 9, GridInitializationPass.PassId);
            Assert.Throws<ArgumentNullException>(() => new SeedReplayRecorder().Record(execution, null, BuildId));
            Assert.Throws<ArgumentException>(() => new SeedReplayRecorder().Record(execution, Hash(), ""));
        }

        [Test]
        public void Recorder_RepeatedSectorBytesAndHashesAreDeterministic()
        {
            var expected = Bundle(0x1234).GeneratedWorldSectorsBytes;
            var expectedHash = Sha256(expected);
            for (var iteration = 0; iteration < 100; iteration++)
            {
                var actual = Bundle(0x1234).GeneratedWorldSectorsBytes;
                CollectionAssert.AreEqual(expected, actual);
                Assert.That(Sha256(actual), Is.EqualTo(expectedHash));
            }
        }

        [Test]
        public void Recorder_DifferentClocksOnlyChangeManifestDiagnostics()
        {
            var first = Bundle(12, StartUtc, TimeSpan.FromMilliseconds(1));
            var second = Bundle(12, StartUtc.AddYears(2), TimeSpan.FromMilliseconds(9));
            CollectionAssert.AreEqual(first.GeneratedWorldSectorsBytes, second.GeneratedWorldSectorsBytes);
            CollectionAssert.AreNotEqual(first.SeedManifestBytes, second.SeedManifestBytes);
        }

        [Test]
        public void Publisher_PublishesAndLoadsExactBundle()
        {
            WithTempRoot(root =>
            {
                var expected = Bundle(22);
                var publisher = new SeedReplayPublisher();
                var published = publisher.Publish(root, expected);
                var loaded = publisher.Load(root, WorldProfileId, 22);
                AssertBundlesEqual(expected, published);
                AssertBundlesEqual(expected, loaded);
                Assert.That(Directory.GetFiles(Path.Combine(root,
                    expected.RelativeDirectory.Replace('/', Path.DirectorySeparatorChar))).Length, Is.EqualTo(2));
            });
        }

        [Test]
        public void Publisher_ReplacesWholeDirectoryAndLeavesNoResidue()
        {
            WithTempRoot(root =>
            {
                var publisher = new SeedReplayPublisher();
                var first = Bundle(23, StartUtc, TimeSpan.FromMilliseconds(1));
                var second = Bundle(23, StartUtc.AddDays(1), TimeSpan.FromMilliseconds(2));
                publisher.Publish(root, first);
                publisher.Publish(root, second);
                AssertBundlesEqual(second, publisher.Load(root, WorldProfileId, 23));
                var destination = Path.Combine(root, second.RelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
                Assert.That(Directory.Exists(destination + ".staging"), Is.False);
                Assert.That(Directory.Exists(destination + ".backup"), Is.False);
            });
        }

        [TestCase(".staging")]
        [TestCase(".backup")]
        public void Publisher_RejectsStaleSibling(string suffix)
        {
            WithTempRoot(root =>
            {
                var bundle = Bundle(24);
                var destination = Path.Combine(root, bundle.RelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(destination + suffix);
                Assert.Throws<IOException>(() => new SeedReplayPublisher().Publish(root, bundle));
            });
        }

        [TestCase("extra-file")]
        [TestCase("subdirectory")]
        [TestCase("case-variant")]
        public void Publisher_LoadRejectsNonExactFileSet(string mutation)
        {
            WithTempRoot(root =>
            {
                var bundle = Bundle(25);
                var publisher = new SeedReplayPublisher();
                publisher.Publish(root, bundle);
                var destination = Path.Combine(root, bundle.RelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
                if (mutation == "extra-file") File.WriteAllText(Path.Combine(destination, "extra.txt"), "x");
                else if (mutation == "subdirectory") Directory.CreateDirectory(Path.Combine(destination, "extra"));
                else File.Move(Path.Combine(destination, SeedManifestCsvSerializer.FileName),
                    Path.Combine(destination, "SEED_MANIFEST.CSV"));
                Assert.Throws<IOException>(() => publisher.Load(root, WorldProfileId, 25));
            });
        }

        [Test]
        public void Publisher_RejectsRelativeOrNonNormalizedRoot()
        {
            var publisher = new SeedReplayPublisher();
            Assert.Throws<ArgumentException>(() => publisher.Publish("relative", Bundle()));
            var root = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) +
                       Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar +
                       new DirectoryInfo(Path.GetTempPath()).Name;
            Assert.Throws<ArgumentException>(() => publisher.Publish(root, Bundle()));
        }

        [Test]
        public void VerificationResult_EnforcesSuccessAndStableFailureState()
        {
            var success = SeedReplayVerificationResult.Success();
            Assert.That(success.Succeeded, Is.True);
            Assert.That(success.Code, Is.Empty);
            Assert.That(success.Message, Is.Empty);
            Assert.Throws<ArgumentException>(() => new SeedReplayVerificationResult(true, "BAD", "bad"));
            Assert.Throws<ArgumentException>(() => SeedReplayVerificationResult.Failure("UNKNOWN", "bad"));
            Assert.Throws<ArgumentException>(() => SeedReplayVerificationResult.Failure(
                SeedReplayVerificationResult.InvalidBundleCode, ""));
        }

        [Test]
        public void Player_VerifiesFreshAndReusedInstancesOneHundredTimes()
        {
            var bundle = Bundle(30);
            var reused = new SeedReplayPlayer(Root());
            for (var iteration = 0; iteration < 100; iteration++)
            {
                var player = iteration % 2 == 0 ? reused : new SeedReplayPlayer(Root());
                var result = player.Verify(bundle, Hash(), BuildId);
                Assert.That(result.Succeeded, Is.True, result.Code + ": " + result.Message);
            }
        }

        [TestCase("hash")]
        [TestCase("build")]
        public void Player_PreconditionMismatchDoesNotInvokeRoot(string mutation)
        {
            var pass = new GridPassAdapter();
            var player = new SeedReplayPlayer(Root(pass: pass));
            var result = mutation == "hash"
                ? player.Verify(Bundle(31), Hash(1), BuildId)
                : player.Verify(Bundle(31), Hash(), BuildId + "-different");
            Assert.That(result.Code, Is.EqualTo(mutation == "hash"
                ? SeedReplayVerificationResult.ContentHashMismatchCode
                : SeedReplayVerificationResult.GeneratorBuildMismatchCode));
            Assert.That(pass.InvocationCount, Is.Zero);
        }

        [Test]
        public void Player_ReplayFailureReturnsStableCode()
        {
            var result = new SeedReplayPlayer(Root(pass: new GridPassAdapter(fail: true)))
                .Verify(Bundle(32), Hash(), BuildId);
            Assert.That(result.Code, Is.EqualTo(SeedReplayVerificationResult.ReplayExecutionFailedCode));
        }

        [Test]
        public void Player_WorldIdentityMismatchReturnsStableCode()
        {
            var result = new SeedReplayPlayer(Root(worldProfileId: "WORLD_OTHER"))
                .Verify(Bundle(33), Hash(), BuildId);
            Assert.That(result.Code, Is.EqualTo(SeedReplayVerificationResult.ReplayExecutionFailedCode));
        }

        [Test]
        public void Player_RejectsReflectedBundleCorruptionBeforeRootInvocation()
        {
            var bundle = Bundle(34);
            var field = typeof(SeedReplayBundle).GetField(
                "generatedWorldSectorsBytes", BindingFlags.Instance | BindingFlags.NonPublic);
            var bytes = (byte[])field.GetValue(bundle);
            bytes[bytes.Length - 5] ^= 1;
            var pass = new GridPassAdapter();
            var result = new SeedReplayPlayer(Root(pass: pass)).Verify(bundle, Hash(), BuildId);
            Assert.That(result.Code, Is.EqualTo(SeedReplayVerificationResult.InvalidBundleCode));
            Assert.That(pass.InvocationCount, Is.Zero);
        }

        [Test]
        public void Player_RejectsNonCheckpointManifestBeforeRootInvocation()
        {
            var manifest = Manifest(approved: true);
            var bytes = SeedManifestCsvSerializer.Serialize(manifest);
            var world = new GridInitializationPass().Execute(manifest.Seed).WorldData;
            var bundle = new SeedReplayBundle(manifest,
                SeedReplayBundle.GetRelativeDirectory(manifest.WorldProfileId, manifest.Seed),
                bytes, GeneratedWorldDataCsvSerializer.Serialize(world));
            var pass = new GridPassAdapter();
            var result = new SeedReplayPlayer(Root(pass: pass)).Verify(bundle, Hash(), BuildId);
            Assert.That(result.Code, Is.EqualTo(SeedReplayVerificationResult.InvalidManifestCode));
            Assert.That(pass.InvocationCount, Is.Zero);
        }

        [Test]
        public void Player_RejectsNullDependenciesAndInputs()
        {
            Assert.Throws<ArgumentNullException>(() => new SeedReplayPlayer(null));
            var player = new SeedReplayPlayer(Root());
            Assert.Throws<ArgumentNullException>(() => player.Verify(Bundle(), null, BuildId));
            Assert.Throws<ArgumentException>(() => player.Verify(Bundle(), Hash(), ""));
        }

        private static SeedManifest Manifest(
            string worldProfileId = WorldProfileId,
            ulong seed = 42,
            bool approved = false,
            IEnumerable<string> failures = null,
            string notes = SeedManifest.GridCheckpointNotes)
        {
            return new SeedManifest(
                worldProfileId, seed, Hash().Hex, GenerationProfileId, BuildId, approved,
                StartUtc, 12, 0, failures ?? Array.Empty<string>(), notes);
        }

        private static SeedReplayBundle Bundle(
            ulong seed = 42,
            DateTimeOffset? startUtc = null,
            TimeSpan? elapsedPerTimestamp = null)
        {
            var root = Root(clock: new ManualClock(
                startUtc ?? StartUtc,
                elapsedPerTimestamp ?? TimeSpan.FromMilliseconds(1)));
            var execution = root.ExecuteThroughRecorded(
                GenerationProfileId, seed, GridInitializationPass.PassId);
            return new SeedReplayRecorder().Record(execution, Hash(), BuildId);
        }

        private static WorldGenerationRoot Root(
            string worldProfileId = WorldProfileId,
            IWorldGenerationClock clock = null,
            GridPassAdapter pass = null)
        {
            var definition = Definition<GenerationPassDefinition>(
                Pair("GenerationProfileId", (object)GenerationProfileId),
                Pair("PassOrder", 0),
                Pair("PassId", GridInitializationPass.PassId),
                Pair("ClassName", "GridPassAdapter"),
                Pair("RngStreamId", ""),
                Pair("InputArtifacts", new ReadOnlyCollection<string>(new List<string>())),
                Pair("OutputArtifacts", new ReadOnlyCollection<string>(new List<string>
                {
                    GridInitializationPass.OutputArtifactId
                })),
                Pair("FailurePolicy", "FAIL_WORLD"),
                Pair("MaxRetryCount", 0),
                Pair("Enabled", true),
                Pair("Notes", ""));
            var profile = Definition<GenerationProfileDefinition>(
                Pair("GenerationProfileId", (object)GenerationProfileId),
                Pair("WorldProfileId", worldProfileId),
                Pair("Active", true));
            var definitions = Construct<WorldRouteDefinitionSet>(
                new[] { Definition<WorldProfileDefinition>(
                    Pair("WorldProfileId", (object)worldProfileId), Pair("Active", true)) },
                new[] { profile },
                new[] { definition },
                RequiredRngDefinitions(),
                Array.Empty<SectorRouteMaskDefinition>(),
                Array.Empty<SocketBandDefinition>(),
                Array.Empty<EdgeSignatureDefinition>(),
                Array.Empty<EdgeSignatureCompatibilityDefinition>(),
                Array.Empty<SectorRecipeDefinition>(),
                Array.Empty<SectorRecipeCellDefinition>(),
                Array.Empty<SectorRecipePathDefinition>(),
                Array.Empty<SectorExternalSocketDefinition>(),
                Array.Empty<SectorRecipePoolEntryDefinition>());
            var staticData = (StaticDataRegistry)FormatterServices.GetUninitializedObject(typeof(StaticDataRegistry));
            SetAutoProperty(staticData, "WorldRouteDefinitions", definitions);
            return new WorldGenerationRoot(
                staticData,
                new WorldGenerationPassRegistry(new IWorldGenerationPass[] { pass ?? new GridPassAdapter() }),
                clock ?? new ManualClock());
        }

        private static RngStreamDefinition[] RequiredRngDefinitions()
        {
            return new[]
            {
                Rng("RNG_WORLD_SITE", "A13C9E0B2F1044D1", "WORLD"),
                Rng("RNG_BIOME_PATCH", "B7A91D33E40C5F82", "PASS"),
                Rng("RNG_ROUTE", "C00FEE12AB341901", "PASS"),
                Rng("RNG_TYPE0", "D15EA5E007A4C883", "PASS"),
                Rng("RNG_SECTOR_RECIPE", "E9931A70C2D520F4", "SECTOR"),
                Rng("RNG_POPULATION", "F123456789ABCDEF", "SPAWN")
            };
        }

        private static RngStreamDefinition Rng(string id, string salt, string scope)
        {
            return Definition<RngStreamDefinition>(
                Pair("RngStreamId", (object)id),
                Pair("SaltHex", CreateHex(salt)),
                Pair("ResetScope", scope),
                Pair("DescriptionKo", "test"),
                Pair("Active", true));
        }

        private static CsvHexValue CreateHex(string value)
        {
            var bytes = Enumerable.Range(0, value.Length / 2)
                .Select(index => byte.Parse(value.Substring(index * 2, 2),
                    NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static ContentVersionHash Hash(byte offset = 0)
        {
            var bytes = Enumerable.Range(0, 32).Select(value => (byte)(value + offset)).ToArray();
            var constructor = typeof(ContentVersionHash).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(IEnumerable<byte>) }, null);
            return (ContentVersionHash)constructor.Invoke(new object[] { bytes });
        }

        private static T Definition<T>(params KeyValuePair<string, object>[] values)
        {
            var value = (T)FormatterServices.GetUninitializedObject(typeof(T));
            foreach (var pair in values) SetAutoProperty(value, pair.Key, pair.Value);
            return value;
        }

        private static T Construct<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T), BindingFlags.Instance | BindingFlags.NonPublic,
                null, arguments, CultureInfo.InvariantCulture);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            var field = target.GetType().GetField(
                "<" + propertyName + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, propertyName);
            field.SetValue(target, value);
        }

        private static KeyValuePair<string, object> Pair(string key, object value)
        {
            return new KeyValuePair<string, object>(key, value);
        }

        private static string CsvText(byte[] bytes)
        {
            return new UTF8Encoding(false, true).GetString(bytes, 3, bytes.Length - 3);
        }

        private static byte[] CsvBytes(string text)
        {
            var content = new UTF8Encoding(false, true).GetBytes(text);
            return new byte[] { 0xEF, 0xBB, 0xBF }.Concat(content).ToArray();
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        private static void AssertBundlesEqual(SeedReplayBundle expected, SeedReplayBundle actual)
        {
            Assert.That(actual.Manifest.WorldProfileId, Is.EqualTo(expected.Manifest.WorldProfileId));
            Assert.That(actual.Manifest.Seed, Is.EqualTo(expected.Manifest.Seed));
            Assert.That(actual.RelativeDirectory, Is.EqualTo(expected.RelativeDirectory));
            CollectionAssert.AreEqual(expected.SeedManifestBytes, actual.SeedManifestBytes);
            CollectionAssert.AreEqual(expected.GeneratedWorldSectorsBytes, actual.GeneratedWorldSectorsBytes);
            Assert.That(actual.FileNames, Is.EqualTo(expected.FileNames));
        }

        private static void WithTempRoot(Action<string> action)
        {
            var testName = new string(TestContext.CurrentContext.Test.Name.Select(character =>
                Path.GetInvalidFileNameChars().Contains(character) ? '_' : character).ToArray());
            var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "StarNight_MAP02_06", testName));
            if (Directory.Exists(root)) Directory.Delete(root, true);
            Directory.CreateDirectory(root);
            try
            {
                action(root);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private sealed class GridPassAdapter : IWorldGenerationPass
        {
            private readonly bool fail;

            public GridPassAdapter(bool fail = false)
            {
                this.fail = fail;
            }

            public string PassId => GridInitializationPass.PassId;
            public string ClassName => "GridPassAdapter";
            public int InvocationCount { get; private set; }

            public WorldGenerationPassResult Execute(WorldGenerationPassContext context)
            {
                InvocationCount++;
                return fail
                    ? WorldGenerationPassResult.Failure("EXPECTED", "expected failure")
                    : WorldGenerationPassResult.Success(
                        GridInitializationPass.OutputArtifactId,
                        new GridInitializationPass().Execute(context.WorldSeed));
            }
        }

        private sealed class ManualClock : IWorldGenerationClock
        {
            private readonly DateTimeOffset startUtc;
            private readonly TimeSpan elapsedPerTimestamp;
            private int utcCalls;
            private long timestampCalls;

            public ManualClock()
                : this(StartUtc, TimeSpan.FromMilliseconds(1))
            {
            }

            public ManualClock(DateTimeOffset startUtc, TimeSpan elapsedPerTimestamp)
            {
                this.startUtc = startUtc;
                this.elapsedPerTimestamp = elapsedPerTimestamp;
            }

            public DateTimeOffset GetUtcNow()
            {
                return startUtc.AddSeconds(utcCalls++);
            }

            public long GetTimestamp()
            {
                return timestampCalls++;
            }

            public TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp)
            {
                return TimeSpan.FromTicks((endTimestamp - startTimestamp) * elapsedPerTimestamp.Ticks);
            }
        }
    }
}
