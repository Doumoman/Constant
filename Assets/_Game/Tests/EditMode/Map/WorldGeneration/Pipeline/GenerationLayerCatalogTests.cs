using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Pipeline
{
    [Category("MAP09_02")]
    public sealed class GenerationLayerCatalogTests
    {
        private static readonly GenerationLayerId[] ExpectedLayerIds =
        {
            GenerationLayerId.RouteType,
            GenerationLayerId.SpecialRegion,
            GenerationLayerId.TerrainCluster,
            GenerationLayerId.MicroPattern,
            GenerationLayerId.ActivityStructure,
            GenerationLayerId.EventOverlay,
            GenerationLayerId.MicroChunk,
        };

        private static readonly PacingRole[] ExpectedPacingRoles =
        {
            PacingRole.Quiet,
            PacingRole.Traversal,
            PacingRole.Discovery,
            PacingRole.Risk,
            PacingRole.Recovery,
            PacingRole.Safe,
            PacingRole.Machinery,
            PacingRole.Flow,
            PacingRole.Activity,
            PacingRole.Narrative,
            PacingRole.Reward,
            PacingRole.Landmark,
            PacingRole.Resource,
            PacingRole.Boss,
            PacingRole.Integrated,
        };

        private static readonly string[] ExpectedPacingTokens =
        {
            "QUIET", "TRAVERSAL", "DISCOVERY", "RISK", "RECOVERY", "SAFE",
            "MACHINERY", "FLOW", "ACTIVITY", "NARRATIVE", "REWARD", "LANDMARK",
            "RESOURCE", "BOSS", "INTEGRATED",
        };

        private static readonly AccessClass[] ExpectedAccessClasses =
        {
            AccessClass.MandatoryNoTool,
            AccessClass.OptionalNoTool,
            AccessClass.OptionalTool,
            AccessClass.OptionalEnvironment,
            AccessClass.OptionalExplosive,
            AccessClass.OptionalHidden,
            AccessClass.ProgressionGate,
        };

        private static readonly string[] ExpectedAccessTokens =
        {
            "MANDATORY_NO_TOOL", "OPTIONAL_NO_TOOL", "OPTIONAL_TOOL",
            "OPTIONAL_ENVIRONMENT", "OPTIONAL_EXPLOSIVE", "OPTIONAL_HIDDEN",
            "PROGRESSION_GATE",
        };

        [Test]
        public void CatalogContainsExactSevenLayersInStableOrder()
        {
            Assert.That(GenerationLayerCatalog.Entries, Has.Count.EqualTo(7));
            Assert.That(GenerationLayerCatalog.Entries.Select(value => value.LayerId),
                Is.EqualTo(ExpectedLayerIds));
            Assert.That(GenerationLayerCatalog.Entries.Select(value => value.Order),
                Is.EqualTo(new[] { 10, 20, 30, 40, 50, 60, 70 }));
        }

        [Test]
        public void CatalogOwnsTheExactResponsibilityMatrix()
        {
            AssertResponsibilities(GenerationLayerId.RouteType,
                LayerResponsibilityId.SectorExternalConnectivity,
                LayerResponsibilityId.GeneralRouteAccess);
            AssertResponsibilities(GenerationLayerId.SpecialRegion,
                LayerResponsibilityId.WorldReservedLandmark,
                LayerResponsibilityId.SpecialEntryAccess);
            AssertResponsibilities(GenerationLayerId.TerrainCluster,
                LayerResponsibilityId.StaticTerrainTraversal);
            AssertResponsibilities(GenerationLayerId.MicroPattern,
                LayerResponsibilityId.LocalPatternTileOperation);
            AssertResponsibilities(GenerationLayerId.ActivityStructure,
                LayerResponsibilityId.StrongGameplayIncident);
            AssertResponsibilities(GenerationLayerId.EventOverlay,
                LayerResponsibilityId.MarkerOnlyRunVariation);
            AssertResponsibilities(GenerationLayerId.MicroChunk,
                LayerResponsibilityId.SliceStorageAndBoundaryProjection);
        }

        [Test]
        public void EveryResponsibilityHasExactlyOneOwner()
        {
            var responsibilities = GenerationLayerCatalog.Entries
                .SelectMany(layer => layer.OwnedResponsibilities.Select(value => new { layer.LayerId, Value = value }))
                .ToArray();
            Assert.That(responsibilities, Has.Length.EqualTo(9));
            Assert.That(responsibilities.Select(value => value.Value).Distinct().Count(), Is.EqualTo(9));
            Assert.That(responsibilities.GroupBy(value => value.Value).All(group => group.Count() == 1), Is.True);
        }

        [Test]
        public void DefaultCatalogValidationPassesWithoutErrors()
        {
            var result = GenerationLayerCatalogValidator.Validate(GenerationLayerCatalog.Entries);
            Assert.That(result.IsValid, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void DuplicateLayerIdFixtureHasExactAccounting()
        {
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[1] = Clone(fixture[1], layerId: GenerationLayerId.RouteType);
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.DuplicateLayerId), Is.EqualTo(1));
            Assert.That(result.Count(GenerationLayerValidationErrorCode.MissingLayerId), Is.EqualTo(1));
        }

        [Test]
        public void DuplicateStableOrderFixtureHasExactAccounting()
        {
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[1] = Clone(fixture[1], order: fixture[0].Order);
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.DuplicateStableOrder), Is.EqualTo(1));
        }

        [Test]
        public void MissingResponsibilityFixtureHasExactAccounting()
        {
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[0] = Clone(fixture[0], responsibilities: new[]
            {
                LayerResponsibilityId.GeneralRouteAccess,
            });
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.MissingResponsibility), Is.EqualTo(1));
            Assert.That(result.Errors.Single(value =>
                value.Code == GenerationLayerValidationErrorCode.MissingResponsibility).ResponsibilityId,
                Is.EqualTo(LayerResponsibilityId.SectorExternalConnectivity));
        }

        [Test]
        public void DuplicateResponsibilityOwnerFixtureHasExactAccounting()
        {
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[0] = Clone(fixture[0], responsibilities: fixture[0].OwnedResponsibilities
                .Concat(new[] { LayerResponsibilityId.WorldReservedLandmark }));
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.DuplicateResponsibilityOwner), Is.EqualTo(1));
        }

        [Test]
        public void WrongResponsibilityOwnerFixtureHasExactAccounting()
        {
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[0] = Clone(fixture[0], responsibilities: new[]
            {
                LayerResponsibilityId.GeneralRouteAccess,
            });
            fixture[1] = Clone(fixture[1], responsibilities: fixture[1].OwnedResponsibilities
                .Concat(new[] { LayerResponsibilityId.SectorExternalConnectivity }));
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.WrongResponsibilityOwner), Is.EqualTo(1));
            Assert.That(result.Count(GenerationLayerValidationErrorCode.MissingResponsibility), Is.Zero);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.DuplicateResponsibilityOwner), Is.Zero);
        }

        [Test]
        public void ReservationAndContentOrderIsExplicit()
        {
            AssertOrder(GenerationLayerId.SpecialRegion, GenerationLayerId.TerrainCluster);
            AssertOrder(GenerationLayerId.TerrainCluster, GenerationLayerId.MicroPattern);
            AssertOrder(GenerationLayerId.MicroPattern, GenerationLayerId.ActivityStructure);
            AssertOrder(GenerationLayerId.MicroPattern, GenerationLayerId.EventOverlay);
            AssertOrder(GenerationLayerId.ActivityStructure, GenerationLayerId.MicroChunk);
            AssertOrder(GenerationLayerId.EventOverlay, GenerationLayerId.MicroChunk);
        }

        [Test]
        public void LayerOrderViolationFixtureIsRejected()
        {
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[2] = Clone(fixture[2], order: 15);
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.LayerOrderInvariantViolation),
                Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void MicroChunkIsTheFinalProvenanceLayer()
        {
            Assert.That(GenerationLayerCatalog.Entries.Last().LayerId,
                Is.EqualTo(GenerationLayerId.MicroChunk));
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[6] = Clone(fixture[6], order: 55);
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.MicroChunkNotFinal), Is.EqualTo(1));
        }

        [Test]
        public void NoLayerClaimsPacingAssignmentAuthority()
        {
            Assert.That(GenerationLayerCatalog.Entries.All(value =>
                !value.ClaimsPacingAssignmentAuthority), Is.True);
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[0] = Clone(fixture[0], claimsPacingAssignmentAuthority: true);
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.PacingAssignmentAuthorityClaimed),
                Is.EqualTo(1));
        }

        [Test]
        public void PacingModesAreCompatibilityOrPreservationOnly()
        {
            Assert.That(Find(GenerationLayerId.RouteType).PacingMode, Is.EqualTo(LayerPacingMode.PreserveOnly));
            Assert.That(Find(GenerationLayerId.MicroChunk).PacingMode, Is.EqualTo(LayerPacingMode.PreserveOnly));
            Assert.That(GenerationLayerCatalog.Entries
                .Where(value => value.LayerId != GenerationLayerId.RouteType &&
                                value.LayerId != GenerationLayerId.MicroChunk)
                .All(value => value.PacingMode == LayerPacingMode.CompatibilityOnly), Is.True);
        }

        [Test]
        public void PacingRoleTokensRoundTripInCanonicalOrder()
        {
            Assert.That(PacingRoleTokenCodec.Entries.Select(value => value.Role),
                Is.EqualTo(ExpectedPacingRoles));
            Assert.That(PacingRoleTokenCodec.Entries.Select(value => value.Token),
                Is.EqualTo(ExpectedPacingTokens));
            foreach (var entry in PacingRoleTokenCodec.Entries)
            {
                Assert.That(PacingRoleTokenCodec.TryParse(entry.Token, out var parsed), Is.True);
                Assert.That(parsed, Is.EqualTo(entry.Role));
                Assert.That(PacingRoleTokenCodec.ToToken(parsed), Is.EqualTo(entry.Token));
            }
        }

        [Test]
        public void InvalidDefaultUndefinedAndDuplicatePacingRolesAreRejected()
        {
            Assert.That(PacingRoleTokenCodec.TryParse("None", out _), Is.False);
            Assert.That(PacingRoleTokenCodec.TryParse("QUIET ", out _), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PacingRoleSet(new[] { PacingRole.None }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PacingRoleSet(new[] { (PacingRole)999 }));
            Assert.Throws<ArgumentException>(() =>
                new PacingRoleSet(new[] { PacingRole.Quiet, PacingRole.Quiet }));
            Assert.Throws<ArgumentException>(() => new PacingRoleSet(Array.Empty<PacingRole>()));
        }

        [Test]
        public void AccessClassTokensRoundTripInCanonicalOrder()
        {
            Assert.That(AccessClassTokenCodec.Entries.Select(value => value.AccessClass),
                Is.EqualTo(ExpectedAccessClasses));
            Assert.That(AccessClassTokenCodec.Entries.Select(value => value.Token),
                Is.EqualTo(ExpectedAccessTokens));
            foreach (var entry in AccessClassTokenCodec.Entries)
            {
                Assert.That(AccessClassTokenCodec.TryParse(entry.Token, out var parsed), Is.True);
                Assert.That(parsed, Is.EqualTo(entry.AccessClass));
                Assert.That(AccessClassTokenCodec.ToToken(parsed), Is.EqualTo(entry.Token));
            }
        }

        [Test]
        public void InvalidDefaultCaseSpaceNumericAndUndefinedAccessValuesAreRejected()
        {
            Assert.That(AccessClassTokenCodec.TryParse("", out _), Is.False);
            Assert.That(AccessClassTokenCodec.TryParse("mandatory_no_tool", out _), Is.False);
            Assert.That(AccessClassTokenCodec.TryParse("MANDATORY_NO_TOOL ", out _), Is.False);
            Assert.That(AccessClassTokenCodec.TryParse("1", out _), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AccessClassTokenCodec.ToToken(AccessClass.Unspecified));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AccessClassTokenCodec.ToToken((AccessClass)999));
        }

        [Test]
        public void MandatoryRouteAndBoundaryMapOnlyToMandatoryNoTool()
        {
            Assert.That(AccessClassMappings.TryMapMandatoryRoute(true, "NONE", out var route), Is.True);
            Assert.That(route, Is.EqualTo(AccessClass.MandatoryNoTool));
            Assert.That(AccessClassMappings.TryMapMandatoryBoundary("NONE", out var boundary), Is.True);
            Assert.That(boundary, Is.EqualTo(AccessClass.MandatoryNoTool));
            Assert.That(AccessClassMappings.TryMapMandatoryRoute(true, "PICKAXE", out _), Is.False);
            Assert.That(AccessClassMappings.TryMapMandatoryRoute(false, "NONE", out _), Is.False);
        }

        [Test]
        public void ExistingOptionalRegionRulesMapExactly()
        {
            Assert.That(AccessClassMappings.FromOptionalRegionAccessRule(OptionalRegionAccessRule.Basic),
                Is.EqualTo(AccessClass.OptionalNoTool));
            Assert.That(AccessClassMappings.FromOptionalRegionAccessRule(OptionalRegionAccessRule.Tool),
                Is.EqualTo(AccessClass.OptionalTool));
            Assert.That(AccessClassMappings.FromOptionalRegionAccessRule(OptionalRegionAccessRule.Environment),
                Is.EqualTo(AccessClass.OptionalEnvironment));
            Assert.That(AccessClassMappings.FromOptionalRegionAccessRule(OptionalRegionAccessRule.Explosive),
                Is.EqualTo(AccessClass.OptionalExplosive));
            Assert.That(AccessClassMappings.FromOptionalRegionAccessRule(OptionalRegionAccessRule.Hidden),
                Is.EqualTo(AccessClass.OptionalHidden));
        }

        [Test]
        public void ProgressionGateCannotReplaceGeneralMandatoryAccess()
        {
            Assert.That(AccessClassMappings.IsValidForGeneralMandatory(AccessClass.MandatoryNoTool), Is.True);
            Assert.That(AccessClassMappings.IsValidForGeneralMandatory(AccessClass.ProgressionGate), Is.False);
            Assert.That(Find(GenerationLayerId.RouteType).CompatibleAccessClasses,
                Has.None.EqualTo(AccessClass.ProgressionGate));
            Assert.That(Find(GenerationLayerId.TerrainCluster).CompatibleAccessClasses,
                Has.None.EqualTo(AccessClass.ProgressionGate));
            Assert.That(Find(GenerationLayerId.MicroPattern).CompatibleAccessClasses,
                Has.None.EqualTo(AccessClass.ProgressionGate));
        }

        [Test]
        public void SamePacingSupportsDifferentAccessClasses()
        {
            var pacing = new PacingRoleSet(new[] { PacingRole.Quiet, PacingRole.Traversal });
            var first = new PacingAccessContract(2, pacing, AccessClass.MandatoryNoTool);
            var second = first.WithAccess(AccessClass.OptionalHidden);
            Assert.That(second.Pacing.Roles, Is.EqualTo(first.Pacing.Roles));
            Assert.That(second.Access, Is.Not.EqualTo(first.Access));
        }

        [Test]
        public void DifferentPacingSupportsTheSameAccessClass()
        {
            var first = new PacingAccessContract(1,
                new PacingRoleSet(new[] { PacingRole.Quiet }), AccessClass.OptionalNoTool);
            var second = first.WithPacing(new PacingRoleSet(new[] { PacingRole.Risk }));
            Assert.That(second.Access, Is.EqualTo(first.Access));
            Assert.That(second.Pacing.Roles, Is.Not.EqualTo(first.Pacing.Roles));
        }

        [Test]
        public void PacingChangesPreserveAccessClassAndExistingIntegerRouteType()
        {
            var source = new PacingAccessContract(4,
                new PacingRoleSet(new[] { PacingRole.Flow }), AccessClass.OptionalEnvironment);
            var changed = source.WithPacing(new PacingRoleSet(new[] { PacingRole.Recovery }));
            Assert.That(changed.RouteType, Is.EqualTo(4));
            Assert.That(changed.Access, Is.EqualTo(AccessClass.OptionalEnvironment));
        }

        [Test]
        public void ActivityAndEventDeclareRemoveSafeAccess()
        {
            Assert.That(Find(GenerationLayerId.ActivityStructure).PreservesAccessWhenRemoved, Is.True);
            Assert.That(Find(GenerationLayerId.EventOverlay).PreservesAccessWhenRemoved, Is.True);
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[4] = Clone(fixture[4], preservesAccessWhenRemoved: false);
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.RemovalChangesAccess), Is.EqualTo(1));
        }

        [Test]
        public void ActivityStructureAndEventOverlayRemainSeparateLayers()
        {
            Assert.That(Find(GenerationLayerId.ActivityStructure).OwnedResponsibilities,
                Is.EqualTo(new[] { LayerResponsibilityId.StrongGameplayIncident }));
            Assert.That(Find(GenerationLayerId.EventOverlay).OwnedResponsibilities,
                Is.EqualTo(new[] { LayerResponsibilityId.MarkerOnlyRunVariation }));
        }

        [Test]
        public void MicroChunkIsPreserveAndProvenanceOnly()
        {
            var microChunk = Find(GenerationLayerId.MicroChunk);
            Assert.That(microChunk.PacingMode, Is.EqualTo(LayerPacingMode.PreserveOnly));
            Assert.That(microChunk.AccessMode, Is.EqualTo(LayerAccessMode.PreserveOnly));
            Assert.That(microChunk.StoresAccessProvenanceOnly, Is.True);
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[6] = Clone(fixture[6], storesAccessProvenanceOnly: false);
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.MicroChunkNotProvenanceOnly),
                Is.EqualTo(1));
        }

        [Test]
        public void CatalogEntriesTokensAndRoleSetsRejectExternalMutation()
        {
            Assert.Throws<NotSupportedException>(() =>
                ((IList<GenerationLayerContract>)GenerationLayerCatalog.Entries).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<LayerResponsibilityId>)GenerationLayerCatalog.Entries[0].OwnedResponsibilities).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<PacingRole>)GenerationLayerCatalog.Entries[0].CompatiblePacingRoles).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<AccessClass>)GenerationLayerCatalog.Entries[0].CompatibleAccessClasses).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<PacingRoleToken>)PacingRoleTokenCodec.Entries).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<AccessClassToken>)AccessClassTokenCodec.Entries).Clear());
            var roles = new PacingRoleSet(new[] { PacingRole.Quiet });
            Assert.Throws<NotSupportedException>(() => ((IList<PacingRole>)roles.Roles).Clear());
        }

        [Test]
        public void ConstructorInputsAreDefensivelyCopied()
        {
            var responsibilities = new[] { LayerResponsibilityId.GeneralRouteAccess };
            var pacing = new[] { PacingRole.Quiet };
            var access = new[] { AccessClass.OptionalNoTool };
            var contract = new GenerationLayerContract(
                GenerationLayerId.RouteType, 10, responsibilities,
                LayerPacingMode.PreserveOnly, LayerAccessMode.GeneralAuthority,
                pacing, access, false, false, false, "DISPLAY");
            responsibilities[0] = LayerResponsibilityId.WorldReservedLandmark;
            pacing[0] = PacingRole.Risk;
            access[0] = AccessClass.OptionalTool;
            Assert.That(contract.OwnedResponsibilities.Single(),
                Is.EqualTo(LayerResponsibilityId.GeneralRouteAccess));
            Assert.That(contract.CompatiblePacingRoles.Single(), Is.EqualTo(PacingRole.Quiet));
            Assert.That(contract.CompatibleAccessClasses.Single(), Is.EqualTo(AccessClass.OptionalNoTool));
        }

        [Test]
        public void StableDigestRepeatsAndIgnoresEnumerationOrder()
        {
            var first = GenerationLayerCatalog.StableDigest;
            Assert.That(first, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(GenerationLayerCatalog.ComputeStableDigest(GenerationLayerCatalog.Entries),
                Is.EqualTo(first));
            Assert.That(GenerationLayerCatalog.ComputeStableDigest(GenerationLayerCatalog.Entries.Reverse()),
                Is.EqualTo(first));
        }

        [Test]
        public void StableDigestIsCultureIndependent()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;

            try
            {
                var expected = GenerationLayerCatalog.StableDigest;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");

                Assert.That(GenerationLayerCatalog.ComputeStableDigest(GenerationLayerCatalog.Entries),
                    Is.EqualTo(expected));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void DisplayTextDoesNotAffectStableDigest()
        {
            var renamed = GenerationLayerCatalog.Entries
                .Select((value, index) => value.WithDisplayId("LOCALIZED_DISPLAY_" + index));
            Assert.That(GenerationLayerCatalog.ComputeStableDigest(renamed),
                Is.EqualTo(GenerationLayerCatalog.StableDigest));
        }

        [Test]
        public void TokenSemanticSeparationFixturesHaveExactErrorAccounting()
        {
            var pacingTokens = PacingRoleTokenCodec.Entries
                .Select(value => value.Role == PacingRole.Quiet
                    ? new PacingRoleToken(value.Role, "MANDATORY")
                    : value)
                .ToArray();
            var accessTokens = AccessClassTokenCodec.Entries
                .Select(value => value.AccessClass == AccessClass.OptionalNoTool
                    ? new AccessClassToken(value.AccessClass, "QUIET")
                    : value)
                .ToArray();
            var result = GenerationLayerCatalogValidator.Validate(
                GenerationLayerCatalog.Entries,
                GenerationLayerCatalog.OrderInvariants,
                pacingTokens,
                accessTokens,
                AccessClass.MandatoryNoTool);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.PacingTokenContainsAccessMeaning),
                Is.EqualTo(1));
            Assert.That(result.Count(GenerationLayerValidationErrorCode.AccessTokenContainsPacingOrMovementMeaning),
                Is.EqualTo(1));
        }

        [Test]
        public void InvalidPacingAndAccessCompatibilityValuesAreAccumulated()
        {
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[0] = Clone(fixture[0],
                pacingRoles: new[] { PacingRole.None },
                accessClasses: new[] { AccessClass.Unspecified });
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.InvalidPacingRole), Is.EqualTo(1));
            Assert.That(result.Count(GenerationLayerValidationErrorCode.InvalidAccessClass), Is.EqualTo(1));
        }

        [Test]
        public void InvalidGeneralAndSpecialAuthorityClaimsAreRejected()
        {
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[2] = Clone(fixture[2], accessMode: LayerAccessMode.GeneralAuthority);
            fixture[5] = Clone(fixture[5], accessMode: LayerAccessMode.SpecialEntryAuthority);
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.InvalidGeneralAccessAuthority),
                Is.EqualTo(1));
            Assert.That(result.Count(GenerationLayerValidationErrorCode.InvalidSpecialEntryAuthority),
                Is.EqualTo(1));
        }

        [Test]
        public void InvalidMandatoryMappingFixtureHasExactAccounting()
        {
            var result = GenerationLayerCatalogValidator.Validate(
                GenerationLayerCatalog.Entries,
                GenerationLayerCatalog.OrderInvariants,
                PacingRoleTokenCodec.Entries,
                AccessClassTokenCodec.Entries,
                AccessClass.OptionalTool);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.InvalidMandatoryMapping),
                Is.EqualTo(1));
        }

        [Test]
        public void ValidationErrorsAreStableSortedAndDeduplicated()
        {
            var fixture = GenerationLayerCatalog.Entries.ToArray();
            fixture[0] = Clone(fixture[0], responsibilities: fixture[0].OwnedResponsibilities
                .Concat(new[]
                {
                    LayerResponsibilityId.WorldReservedLandmark,
                    LayerResponsibilityId.WorldReservedLandmark,
                }));
            var result = GenerationLayerCatalogValidator.Validate(fixture);
            Assert.That(result.Count(GenerationLayerValidationErrorCode.DuplicateResponsibilityOwner),
                Is.EqualTo(1));
            Assert.That(result.Errors.Select(value => (int)value.Code), Is.Ordered.Ascending);
        }

        [Test]
        public void NewProductionScopeContainsNoDuplicateRouteTypeOrForbiddenDependency()
        {
            var root = FullPath("Assets/_Game/Map/Runtime/WorldGeneration/Pipeline");
            var text = string.Join("\n", Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(File.ReadAllText));
            Assert.That(Regex.Matches(text, @"\b(?:enum|class|struct)\s+RouteType\b"), Has.Count.Zero);
            foreach (var forbidden in new[]
            {
                "StageMapGenerator", "GridWorld", "RoomTemplate", "RoomGridTransform",
                "TileMutationService", "SectorRecipeResolver", "UnityEditor",
            })
            {
                Assert.That(text, Does.Not.Contain(forbidden), forbidden);
            }
        }

        private static void AssertResponsibilities(
            GenerationLayerId layerId,
            params LayerResponsibilityId[] expected)
        {
            Assert.That(Find(layerId).OwnedResponsibilities, Is.EqualTo(expected));
        }

        private static void AssertOrder(GenerationLayerId before, GenerationLayerId after)
        {
            Assert.That(Find(before).Order, Is.LessThan(Find(after).Order));
        }

        private static GenerationLayerContract Find(GenerationLayerId layerId)
        {
            return GenerationLayerCatalog.Entries.Single(value => value.LayerId == layerId);
        }

        private static GenerationLayerContract Clone(
            GenerationLayerContract source,
            GenerationLayerId? layerId = null,
            int? order = null,
            IEnumerable<LayerResponsibilityId> responsibilities = null,
            LayerPacingMode? pacingMode = null,
            LayerAccessMode? accessMode = null,
            IEnumerable<PacingRole> pacingRoles = null,
            IEnumerable<AccessClass> accessClasses = null,
            bool? claimsPacingAssignmentAuthority = null,
            bool? preservesAccessWhenRemoved = null,
            bool? storesAccessProvenanceOnly = null)
        {
            return new GenerationLayerContract(
                layerId ?? source.LayerId,
                order ?? source.Order,
                responsibilities ?? source.OwnedResponsibilities,
                pacingMode ?? source.PacingMode,
                accessMode ?? source.AccessMode,
                pacingRoles ?? source.CompatiblePacingRoles,
                accessClasses ?? source.CompatibleAccessClasses,
                claimsPacingAssignmentAuthority ?? source.ClaimsPacingAssignmentAuthority,
                preservesAccessWhenRemoved ?? source.PreservesAccessWhenRemoved,
                storesAccessProvenanceOnly ?? source.StoresAccessProvenanceOnly,
                source.DisplayId);
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
