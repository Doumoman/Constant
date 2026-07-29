using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace StarFetchingNight.Tests
{
    public sealed class StarNightCoreTests
    {
        private GameObject runObject;
        private readonly List<GameObject> spawnedObjects = new();

        [TearDown]
        public void TearDown()
        {
            if (runObject != null)
            {
                Object.DestroyImmediate(runObject);
            }
            foreach (GameObject spawned in spawnedObjects)
            {
                if (spawned != null)
                {
                    Object.DestroyImmediate(spawned);
                }
            }
            spawnedObjects.Clear();
        }

        [TestCase(0f, StarScentStage.Quiet)]
        [TestCase(24.9f, StarScentStage.Quiet)]
        [TestCase(25f, StarScentStage.Scent)]
        [TestCase(50f, StarScentStage.Footprints)]
        [TestCase(75f, StarScentStage.Bell)]
        [TestCase(100f, StarScentStage.ReturnTime)]
        public void ScentThresholds_AreDeterministic(float scent, StarScentStage expected)
        {
            Assert.That(StarScentRules.FromValue(scent), Is.EqualTo(expected));
        }

        [Test]
        public void FourthModification_OverloadsObject()
        {
            GameObject gameObject = new("Test Fable");
            gameObject.AddComponent<SpriteRenderer>();
            FableObject target = gameObject.AddComponent<FableObject>();
            target.Configure("test", "시험 달떡", StarItemKind.General,
                FableTraits.Resizable | FableTraits.Floatable | FableTraits.Linkable, 1f);

            Assert.That(target.Apply(FableVerb.Resize, ResizeIntent.Enlarge).overloaded, Is.False);
            Assert.That(target.Apply(FableVerb.Float, ResizeIntent.Enlarge).overloaded, Is.False);
            Assert.That(target.Apply(FableVerb.Link, ResizeIntent.Enlarge).overloaded, Is.False);
            FableToolResult fourth = target.Apply(FableVerb.Resize, ResizeIntent.Shrink);

            Assert.That(fourth.overloaded, Is.True);
            Assert.That(target.IsOverloaded, Is.True);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void AccidentRecords_OutrankRoutineToolUse()
        {
            GameObject gameObject = new("Recorder");
            StarNightActionRecorder recorder = gameObject.AddComponent<StarNightActionRecorder>();
            recorder.Record(new StarActionContext { actionType = StarActionType.ToolApplied, detail = "작아졌다" });
            recorder.Record(new StarActionContext { actionType = StarActionType.ToolOverloaded, causedAccident = true, detail = "터졌다" });

            StarActionRecord chosen = recorder.SelectForRani(1).Single();

            Assert.That(chosen.causedAccident, Is.True);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void MoonMillGraph_ContainsGuaranteedDepartureAndTemptation()
        {
            var graph = StarNightRoomGraphGenerator.GenerateMoonMill(173, 11);

            Assert.That(graph, Has.Count.EqualTo(11));
            Assert.That(graph.Any(room => room.displayName == "달배 선착장" && room.guaranteed), Is.True);
            Assert.That(graph.Any(room => room.temptation), Is.True);
            Assert.That(graph.All(room => room.links.Count > 0), Is.True);
        }

        [Test]
        public void RedThreadTension_OnlyAppearsPastRestLength()
        {
            Assert.That(RedThreadConnection.CalculateTension(3f, 4f, 10f), Is.Zero);
            Assert.That(RedThreadConnection.CalculateTension(4f, 4f, 10f), Is.Zero);
            Assert.That(RedThreadConnection.CalculateTension(6f, 4f, 10f), Is.EqualTo(20f));
        }

        [Test]
        public void MagpieChapter_UnlocksResizeAndLink()
        {
            StarNightRunState run = CreateRun(StarChapterId.MagpieBridge, FableVerb.Link);

            Assert.That(run.IsToolUnlocked(FableVerb.Resize), Is.True);
            Assert.That(run.IsToolUnlocked(FableVerb.Link), Is.True);
            Assert.That(run.RedThread.ConnectionLimit, Is.EqualTo(3));
        }

        [Test]
        public void RedThread_SelectsTwoEndpointsAndCreatesConnection()
        {
            StarNightRunState run = CreateRun(StarChapterId.MagpieBridge, FableVerb.Link);
            GameObject firstObject = CreateLinkable("anchor-a", new Vector2(0f, 0f));
            GameObject secondObject = CreateLinkable("anchor-b", new Vector2(5f, 0f));

            try
            {
                FableObject first = firstObject.GetComponent<FableObject>();
                FableObject second = secondObject.GetComponent<FableObject>();

                FableToolResult pending = run.RedThread.Use(first);
                FableToolResult connected = run.RedThread.Use(second);

                Assert.That(pending.awaitingSecondTarget, Is.True);
                Assert.That(connected.connectionChanged, Is.True);
                Assert.That(run.RedThread.PendingEndpoint, Is.Null);
                Assert.That(run.RedThread.Connections, Has.Count.EqualTo(1));
                Assert.That(first.IsLinked, Is.True);
                Assert.That(second.IsLinked, Is.True);
                Assert.That(run.RedThread.FindConnection(first, second), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
            }
        }

        [Test]
        public void MoonMillRepair_ReducesMagpieBridgeScent()
        {
            StarNightRunState run = CreateRun(StarChapterId.MoonRabbitMill, FableVerb.Resize);
            run.SetFlag("moonmill.mill.repaired");
            run.ConsequenceResolver.ResolveMoonMill();
            run.BeginChapter(CreateDefinition(StarChapterId.MagpieBridge, FableVerb.Link));

            Assert.That(run.GetFlag("CH1_MILL_REPAIRED"), Is.True);
            Assert.That(run.ConsequenceResolver.ModifyScent(10f), Is.EqualTo(8.5f).Within(0.001f));
            Assert.That(run.ConsequenceResolver.GetStartingScent(StarChapterId.MagpieBridge), Is.Zero);
        }

        [Test]
        public void UnrepairedMoonMill_StartsMagpieBridgeWithPenalty()
        {
            StarNightRunState run = CreateRun(StarChapterId.MoonRabbitMill, FableVerb.Resize);
            run.ConsequenceResolver.ResolveMoonMill();
            run.BeginChapter(CreateDefinition(StarChapterId.MagpieBridge, FableVerb.Link));

            Assert.That(run.GetFlag("CH1_MILL_DAMAGED"), Is.True);
            Assert.That(run.ConsequenceResolver.ModifyScent(10f), Is.EqualTo(10.8f).Within(0.001f));
            Assert.That(run.ConsequenceResolver.GetStartingScent(StarChapterId.MagpieBridge), Is.EqualTo(5f));
        }

        [Test]
        public void Rani_PraisesForcedHaechiStayAsResponsibility()
        {
            StarNightRunState run = CreateRun(StarChapterId.MagpieBridge, FableVerb.Link);
            run.SetFlag("CH2_HAECHI_FORCED");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.NpcForcedReturn,
                actorId = "Player",
                targetId = "Haechi",
                detail = "해치를 정거장에 남겼다",
                witnessed = true
            });

            string summary = run.Watcher.ResolveRaniSummary(StarChapterId.MagpieBridge);

            Assert.That(summary, Does.Contain("책임"));
            Assert.That(summary, Does.Contain("안전"));
        }

        [Test]
        public void StarLadderUpgrade_AddsOneThreadConnection()
        {
            StarNightRunState run = CreateRun(StarChapterId.MagpieBridge, FableVerb.Link);

            int bonus = run.RedThread.AddConnectionCapacity();

            Assert.That(bonus, Is.EqualTo(1));
            Assert.That(run.RedThread.ConnectionLimit, Is.EqualTo(4));
        }

        [Test]
        public void CloudBottle_MovesWeightWithoutChangingTotalMass()
        {
            StarNightRunState run = CreateRun(StarChapterId.CloudWhaleRanch, FableVerb.Float);
            GameObject sourceObject = CreateWeighted("source-weight", 3f, 2f);
            GameObject targetObject = CreateWeighted("rain-cloud", 0.7f, -0.2f, FableTraits.RainCloud);

            try
            {
                FableObject source = sourceObject.GetComponent<FableObject>();
                FableObject target = targetObject.GetComponent<FableObject>();
                float totalBefore = source.Body.mass + target.Body.mass;

                FableToolResult collected = run.CloudBottle.Use(source);
                FableToolResult transferred = run.CloudBottle.Use(target);
                float totalAfter = source.Body.mass + target.Body.mass;

                Assert.That(collected.awaitingWeightTarget, Is.True);
                Assert.That(transferred.weightChanged, Is.True);
                Assert.That(run.CloudBottle.HeldWeight, Is.Zero);
                Assert.That(totalAfter, Is.EqualTo(totalBefore).Within(0.001f));
                Assert.That(source.Body.gravityScale, Is.LessThan(2f));
                Assert.That(target.Body.gravityScale, Is.GreaterThan(-0.2f));
            }
            finally
            {
                Object.DestroyImmediate(sourceObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void CloudBottle_ReturnsHeldWeightToOriginalSource()
        {
            StarNightRunState run = CreateRun(StarChapterId.CloudWhaleRanch, FableVerb.Float);
            GameObject sourceObject = CreateWeighted("return-source", 2.5f, 2f);

            try
            {
                FableObject source = sourceObject.GetComponent<FableObject>();
                float massBefore = source.Body.mass;

                run.CloudBottle.Use(source);
                FableToolResult returned = run.CloudBottle.Use(source);

                Assert.That(returned.weightChanged, Is.True);
                Assert.That(source.Body.mass, Is.EqualTo(massBefore).Within(0.001f));
                Assert.That(run.CloudBottle.Source, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(sourceObject);
            }
        }

        [Test]
        public void RainDock_DeliversHeavyCloudAndAddsDepartureProgress()
        {
            StarNightRunState run = CreateRun(StarChapterId.CloudWhaleRanch, FableVerb.Float);
            GameObject cloudObject = CreateWeighted("dock-cloud", 1.6f, 0.5f, FableTraits.RainCloud);
            GameObject dockObject = new("Rain Dock");
            dockObject.transform.position = cloudObject.transform.position;

            try
            {
                CloudRainDock dock = dockObject.AddComponent<CloudRainDock>();
                dock.Configure("T", cloudObject.GetComponent<FableObject>(), 1.35f);

                bool delivered = dock.TryDeliver();

                Assert.That(delivered, Is.True);
                Assert.That(dock.Delivered, Is.True);
                Assert.That(run.Chapter.DepartureProgress, Is.EqualTo(1));
                Assert.That(run.GetFlag("CH3_RAIN_CLOUD_T_DELIVERED"), Is.True);
                Assert.That(cloudObject.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            }
            finally
            {
                Object.DestroyImmediate(cloudObject);
                Object.DestroyImmediate(dockObject);
            }
        }

        [Test]
        public void CloudWhaleDrought_TransfersToStarPostOffice()
        {
            StarNightRunState run = CreateRun(StarChapterId.CloudWhaleRanch, FableVerb.Float);
            run.SetFlag("CH3_GURU_RELEASED");

            run.ConsequenceResolver.ResolveCloudWhaleRanch();
            run.BeginChapter(CreateDefinition(StarChapterId.StarPostOffice, FableVerb.Deliver));

            Assert.That(run.GetFlag("CH3_DROUGHT"), Is.True);
            Assert.That(run.ConsequenceResolver.GetStartingScent(StarChapterId.StarPostOffice), Is.EqualTo(10f));
            Assert.That(run.ConsequenceResolver.ModifyScent(10f), Is.EqualTo(11.2f).Within(0.001f));
        }

        [Test]
        public void Rani_CallsGuruReleaseAnAssumptionWhenRainIsNotRebuilt()
        {
            StarNightRunState run = CreateRun(StarChapterId.CloudWhaleRanch, FableVerb.Float);
            run.SetFlag("CH3_GURU_RELEASED");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.GuruReleased,
                actorId = "Player",
                targetId = "Guru",
                detail = "구루의 닻을 풀었다",
                witnessed = true
            });

            string summary = run.Watcher.ResolveRaniSummary(StarChapterId.CloudWhaleRanch);

            Assert.That(summary, Does.Contain("감옥"));
            Assert.That(summary, Does.Contain("비가 멎은"));
        }

        [Test]
        public void Delivery_SelectParcelThenAddress_TeleportsParcel()
        {
            StarNightRunState run = CreateRun(StarChapterId.StarPostOffice, FableVerb.Deliver);
            GameObject parcelObject = CreatePostalParcel("test-parcel", Vector2.zero);
            GameObject addressObject = CreateAddress("MOON", new Vector2(12f, 3f));

            try
            {
                FableObject parcel = parcelObject.GetComponent<FableObject>();
                FableObject address = addressObject.GetComponent<FableObject>();

                FableToolResult selected = run.Delivery.Use(parcel);
                FableToolResult delivered = run.Delivery.Use(address);

                Assert.That(selected.awaitingDestination, Is.True);
                Assert.That(delivered.deliveryChanged, Is.True);
                Assert.That(run.Delivery.PendingParcel, Is.Null);
                Assert.That(run.Delivery.DeliveryCount, Is.EqualTo(1));
                Assert.That(parcel.transform.position.x, Is.EqualTo(12f).Within(0.01f));
                Assert.That(parcel.transform.position.y, Is.EqualTo(4.2f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(parcelObject);
                Object.DestroyImmediate(addressObject);
            }
        }

        [Test]
        public void DryInk_FirstAddressAttemptFailsAndKeepsParcelSelected()
        {
            StarNightRunState run = CreateRun(StarChapterId.StarPostOffice, FableVerb.Deliver);
            run.SetFlag("CH4_DRY_INK");
            GameObject parcelObject = CreatePostalParcel("dry-parcel", Vector2.zero);
            GameObject addressObject = CreateAddress("DRY", new Vector2(6f, 0f));

            try
            {
                FableObject parcel = parcelObject.GetComponent<FableObject>();
                FableObject address = addressObject.GetComponent<FableObject>();
                run.Delivery.Use(parcel);

                FableToolResult failed = run.Delivery.Use(address);
                Assert.That(failed.success, Is.False);
                Assert.That(run.Delivery.PendingParcel, Is.SameAs(parcel));
                Assert.That(run.GetFlag("CH4_DRY_STAMP_DELAYED"), Is.True);

                FableToolResult retried = run.Delivery.Use(address);
                Assert.That(retried.success, Is.True);
                Assert.That(run.Delivery.PendingParcel, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(parcelObject);
                Object.DestroyImmediate(addressObject);
            }
        }

        [Test]
        public void WetInk_FirstDeliveryMisroutesAndCreatesAccident()
        {
            StarNightRunState run = CreateRun(StarChapterId.StarPostOffice, FableVerb.Deliver);
            run.SetFlag("CH4_WET_INK");
            GameObject parcelObject = CreatePostalParcel("wet-parcel", Vector2.zero);
            GameObject requestedObject = CreateAddress("ZZ_REQUESTED", new Vector2(30f, 0f));
            GameObject alternateObject = CreateAddress("AA_ALTERNATE", new Vector2(9f, 1f));

            try
            {
                FableObject parcel = parcelObject.GetComponent<FableObject>();
                run.Delivery.Use(parcel);
                FableToolResult result = run.Delivery.Use(requestedObject.GetComponent<FableObject>());

                Assert.That(result.overloaded, Is.True);
                Assert.That(run.GetFlag("CH4_SPLIT_DELIVERY_OCCURRED"), Is.True);
                Assert.That(run.AccidentReport.BuildReport(), Is.Not.Empty);
                Assert.That(parcel.transform.position.x, Is.Not.EqualTo(30f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(parcelObject);
                Object.DestroyImmediate(requestedObject);
                Object.DestroyImmediate(alternateObject);
            }
        }

        [Test]
        public void SealedLastLetter_DeliveredToRaniUnlocksStarPathAndEndsComms()
        {
            StarNightRunState run = CreateRun(StarChapterId.StarPostOffice, FableVerb.Deliver);
            GameObject letterObject = CreatePostalParcel("last-letter", Vector2.zero, FableTraits.LastLetter);
            GameObject raniObject = CreateAddress("RANI", new Vector2(18f, 0f));

            try
            {
                run.Delivery.Use(letterObject.GetComponent<FableObject>());
                FableToolResult result = run.Delivery.Use(raniObject.GetComponent<FableObject>());

                Assert.That(result.success, Is.True);
                Assert.That(run.GetFlag("CH4_LETTER_STATE_DELIVERED"), Is.True);
                Assert.That(run.GetFlag("CH4_RANI_DISCONNECTED"), Is.True);
                Assert.That(run.GetFlag("STARPATH_LAST_LETTER_DELIVERED"), Is.True);
                Assert.That(run.GetFlag("STARPATH_ROUTE_CLUE"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(letterObject);
                Object.DestroyImmediate(raniObject);
            }
        }

        [Test]
        public void OpenedLetter_CarriesArgumentPenaltyIntoSleepingSunGarden()
        {
            StarNightRunState run = CreateRun(StarChapterId.StarPostOffice, FableVerb.Deliver);
            run.SetFlag("CH4_LETTER_STATE_OPENED");

            run.ConsequenceResolver.ResolveStarPostOffice();
            run.BeginChapter(CreateDefinition(StarChapterId.SleepingSunGarden, FableVerb.Deliver));

            Assert.That(run.ConsequenceResolver.GetStartingScent(StarChapterId.SleepingSunGarden),
                Is.EqualTo(6f));
            Assert.That(run.ConsequenceResolver.ModifyScent(10f), Is.EqualTo(10.8f).Within(0.001f));
        }

        [Test]
        public void Maru_PrioritizesLastLetterAboveOrdinaryParcel()
        {
            GameObject ordinaryObject = CreatePostalParcel("ordinary", Vector2.zero);
            GameObject letterObject = CreatePostalParcel("last-letter", Vector2.zero, FableTraits.LastLetter);
            try
            {
                float ordinaryScore = MaruDirector.CalculateTargetScore(
                    ordinaryObject.GetComponent<FableObject>(), 0f);
                float letterScore = MaruDirector.CalculateTargetScore(
                    letterObject.GetComponent<FableObject>(), 0f);

                Assert.That(letterScore, Is.GreaterThanOrEqualTo(ordinaryScore + 25f));
            }
            finally
            {
                Object.DestroyImmediate(ordinaryObject);
                Object.DestroyImmediate(letterObject);
            }
        }

        [Test]
        public void SunSeed_ConsumesStoredLightAndAdvancesGrowth()
        {
            StarNightRunState run = CreateRun(StarChapterId.SleepingSunGarden, FableVerb.Awaken);
            GameObject growthObject = CreateSunGrowthTarget("test-sprout", SunGrowthKind.GardenPlant);

            try
            {
                run.SunSeeds.AddCharges(1);
                FableToolResult result = run.SunSeeds.Use(growthObject.GetComponent<FableObject>());
                SunGrowthState growth = growthObject.GetComponent<SunGrowthState>();

                Assert.That(result.success, Is.True);
                Assert.That(result.growthChanged, Is.True);
                Assert.That(run.SunSeeds.Charges, Is.Zero);
                Assert.That(growth.Stage, Is.EqualTo(SunGrowthStage.Awakened));
                Assert.That(growth.LightExposure, Is.EqualTo(1));
                Assert.That(run.Heat.Heat, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(growthObject);
            }
        }

        [Test]
        public void SunGrowth_ExcessLightProgressesFromBloomToBurned()
        {
            GameObject growthObject = CreateSunGrowthTarget("burn-test", SunGrowthKind.GardenPlant,
                bloomAt: 2, burnAt: 5);
            try
            {
                SunGrowthState growth = growthObject.GetComponent<SunGrowthState>();
                growth.ApplySunlight(2);
                Assert.That(growth.Stage, Is.EqualTo(SunGrowthStage.Blooming));

                growth.ApplySunlight(1);
                Assert.That(growth.Stage, Is.EqualTo(SunGrowthStage.Overgrown));

                growth.ApplySunlight(2);
                Assert.That(growth.Stage, Is.EqualTo(SunGrowthStage.Burned));
            }
            finally
            {
                Object.DestroyImmediate(growthObject);
            }
        }

        [Test]
        public void GardenHeat_CrossingThresholdRecordsOverheatAndCanBeRestored()
        {
            StarNightRunState run = CreateRun(StarChapterId.SleepingSunGarden, FableVerb.Awaken);

            run.Heat.AddHeat(75f, "test overheat", "test");
            bool restored = run.Heat.RestoreGarden(55f, "희귀 씨앗으로 정원을 되살렸다");

            Assert.That(run.GetFlag("CH5_GARDEN_OVERHEATED"), Is.True);
            Assert.That(restored, Is.True);
            Assert.That(run.GetFlag("CH5_GARDEN_RESTORED"), Is.True);
            Assert.That(run.Heat.Heat, Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void HaoreumForcedAwake_ConsumesLightAndInstantlyGrowsStarPath()
        {
            StarNightRunState run = CreateRun(StarChapterId.SleepingSunGarden, FableVerb.Awaken);
            GameObject treeObject = CreateSunGrowthTarget("force-tree", SunGrowthKind.StarPathTree,
                bloomAt: 3, burnAt: 5);
            GameObject decisionObject = new("Force Haoreum");

            try
            {
                StarPathTreeController tree = treeObject.AddComponent<StarPathTreeController>();
                tree.Configure(treeObject.GetComponent<SunGrowthState>(), null, null, null);
                HaoreumDecision decision = decisionObject.AddComponent<HaoreumDecision>();
                decision.Configure(tree);
                run.SunSeeds.AddCharges(1);

                decision.Interact(null);

                Assert.That(run.GetFlag("CH5_SUN_AWAKENED_FORCEFULLY"), Is.True);
                Assert.That(run.GetNpcState("Haoreum"), Is.EqualTo(StarNpcState.Tired));
                Assert.That(run.SunSeeds.Charges, Is.Zero);
                Assert.That(tree.Growth.Stage, Is.EqualTo(SunGrowthStage.Blooming));
                Assert.That(run.Heat.Heat, Is.EqualTo(45f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(decisionObject);
                Object.DestroyImmediate(treeObject);
            }
        }

        [Test]
        public void NaturalHaoreumWake_ReducesPolarisScentPressure()
        {
            StarNightRunState run = CreateRun(StarChapterId.SleepingSunGarden, FableVerb.Awaken);
            run.SetFlag("CH5_HAOREUM_NATURAL_WAKE");

            run.ConsequenceResolver.ResolveSleepingSunGarden();
            run.BeginChapter(CreateDefinition(StarChapterId.PolarisObservatory, FableVerb.Awaken));

            Assert.That(run.ConsequenceResolver.ModifyScent(10f), Is.EqualTo(9f).Within(0.001f));
            Assert.That(run.ConsequenceResolver.GetStartingScent(StarChapterId.PolarisObservatory),
                Is.Zero);
        }

        [TestCase("CH5_ROUTE_STORED_SUNLIGHT", "CH5_ROUTE_GREENHOUSE_TOP")]
        [TestCase("CH5_ROUTE_STORED_SUNLIGHT", "CH5_ROUTE_HAOREUM_WAKE")]
        [TestCase("CH5_ROUTE_GREENHOUSE_TOP", "CH5_ROUTE_HAOREUM_WAKE")]
        public void EverySunGardenRoutePair_ReachesGateReady(
            string firstRoute,
            string secondRoute)
        {
            StarNightRunState run = CreateSunGateLoopRun();
            run.ChapterLoop.OpenRoutes();

            Assert.That(run.ChapterLoop.CompleteRoute(firstRoute), Is.True);
            Assert.That(run.ChapterLoop.CompleteRoute(secondRoute), Is.True);
            Assert.That(run.ChapterLoop.TryContribute(firstRoute), Is.True);
            Assert.That(run.ChapterLoop.TryContribute(secondRoute), Is.True);

            Assert.That(run.Chapter.GateReady, Is.True);
            Assert.That(run.Chapter.GateContributions, Is.EqualTo(2));
            Assert.That(run.ChapterLoop.State, Is.EqualTo(ChapterLoopState.GateReady));
        }

        [Test]
        public void StoredSunlightRoute_RequiresThreeDistinctActualSources()
        {
            StarNightRunState run = CreateSunGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject routeObject = new("Stored Light Route Test");
            GameObject[] sourceObjects =
            {
                new("Stored Light A"),
                new("Stored Light B"),
                new("Stored Light C")
            };
            try
            {
                GateRouteObjective objective = routeObject.AddComponent<GateRouteObjective>();
                objective.Configure("CH5_ROUTE_STORED_SUNLIGHT");
                SunGardenStoredLightRoute route =
                    routeObject.AddComponent<SunGardenStoredLightRoute>();
                route.Configure(objective, 3);

                for (int i = 0; i < sourceObjects.Length; i++)
                {
                    StoredSunlightSource source =
                        sourceObjects[i].AddComponent<StoredSunlightSource>();
                    source.Configure($"stored-{i}", $"저장 햇빛 {i + 1}");
                    source.ConfigureRoute(route);
                    source.Interact(null);
                }

                Assert.That(route.CollectedCount, Is.EqualTo(3));
                Assert.That(route.Completed, Is.True);
                Assert.That(run.GetFlag("CH5_ROUTE_STORED_SUNLIGHT_COMPLETE"), Is.True);
                Assert.That(run.GateContributions.ContainsRoute(
                    "CH5_ROUTE_STORED_SUNLIGHT"), Is.True);
                Assert.That(run.Chapter.DepartureProgress, Is.Zero);
            }
            finally
            {
                foreach (GameObject sourceObject in sourceObjects)
                {
                    Object.DestroyImmediate(sourceObject);
                }
                Object.DestroyImmediate(routeObject);
            }
        }

        [Test]
        public void GreenhouseTopRoute_RequiresTwoReflectionsAndLeavesHeat()
        {
            StarNightRunState run = CreateSunGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject routeObject = new("Greenhouse Top Route Test");
            GameObject vineObject = CreateSunGrowthTarget(
                "greenhouse-vine-test", SunGrowthKind.GardenPlant, bloomAt: 2, burnAt: 4);
            GameObject creatureObject = CreateSunGrowthTarget(
                "greenhouse-creature-test", SunGrowthKind.SleepingCreature, bloomAt: 2, burnAt: 4);
            GameObject blocker = new("Greenhouse Escape Blocker Test");
            try
            {
                GateRouteObjective objective = routeObject.AddComponent<GateRouteObjective>();
                objective.Configure("CH5_ROUTE_GREENHOUSE_TOP");
                GreenhouseTopLightRoute route =
                    routeObject.AddComponent<GreenhouseTopLightRoute>();
                route.Configure(objective, 2);
                route.ConfigureHazards(
                    vineObject.GetComponent<SunGrowthState>(),
                    creatureObject.GetComponent<SunGrowthState>(),
                    blocker);

                route.Interact(null);
                Assert.That(objective.Completed, Is.False);
                route.Interact(null);

                Assert.That(route.Reflected, Is.EqualTo(2));
                Assert.That(route.Recovered, Is.True);
                Assert.That(objective.Completed, Is.True);
                Assert.That(run.GetFlag("CH5_ROUTE_GREENHOUSE_TOP_COMPLETE"), Is.True);
                Assert.That(run.SunSeeds.Charges, Is.EqualTo(1));
                Assert.That(run.Heat.Heat, Is.EqualTo(26f).Within(0.001f));
                Assert.That(vineObject.GetComponent<SunGrowthState>().Stage,
                    Is.EqualTo(SunGrowthStage.Overgrown));
                Assert.That(creatureObject.GetComponent<SunGrowthState>().Stage,
                    Is.EqualTo(SunGrowthStage.Blooming));
                Assert.That(blocker.activeSelf, Is.True);
                Assert.That(run.GetFlag("CH5_GREENHOUSE_ESCAPE_BLOCKED"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(blocker);
                Object.DestroyImmediate(creatureObject);
                Object.DestroyImmediate(vineObject);
                Object.DestroyImmediate(routeObject);
            }
        }

        [Test]
        public void HaoreumForcedWake_GateLoopCompletesMappedLightRoute()
        {
            StarNightRunState run = CreateSunGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject decisionObject = new("Haoreum Gate Route Test");
            try
            {
                GateRouteObjective objective =
                    decisionObject.AddComponent<GateRouteObjective>();
                objective.Configure("CH5_ROUTE_HAOREUM_WAKE");
                HaoreumDecision decision =
                    decisionObject.AddComponent<HaoreumDecision>();
                decision.ConfigureRouteObjective(objective);
                run.SunSeeds.AddCharges(1);

                decision.Interact(null);

                Assert.That(objective.Completed, Is.True);
                Assert.That(run.GetFlag("CH5_ROUTE_HAOREUM_WAKE_COMPLETE"), Is.True);
                Assert.That(run.GetFlag("CH5_SUN_AWAKENED_FORCEFULLY"), Is.True);
                Assert.That(run.GetNpcState("Haoreum"), Is.EqualTo(StarNpcState.Tired));
                Assert.That(run.GateContributions.ContainsRoute(
                    "CH5_ROUTE_HAOREUM_WAKE"), Is.True);
                Assert.That(run.Heat.Heat, Is.EqualTo(45f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(decisionObject);
            }
        }

        [Test]
        public void NaturalHaoreumWake_DoesNotPretendForcedRouteWasCompleted()
        {
            StarNightRunState run = CreateSunGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject benchObject = new("Natural Wake Route Separation Test");
            try
            {
                GardenRestBench bench = benchObject.AddComponent<GardenRestBench>();
                bench.Configure(1);
                bench.Interact(null);

                Assert.That(run.GetFlag("CH5_HAOREUM_NATURAL_WAKE"), Is.True);
                Assert.That(run.GetFlag("CH5_SUN_AWAKENED_FORCEFULLY"), Is.False);
                Assert.That(run.ChapterLoop.FindRoute("CH5_ROUTE_HAOREUM_WAKE").state,
                    Is.EqualTo(GateRouteState.Available));
                Assert.That(run.GateContributions.Count, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(benchObject);
            }
        }

        [Test]
        public void SunflowerStoppedRoom_RequiresGateActivationEntryAndThenUnlocksReward()
        {
            StarNightRunState run = CreateReadySunGateLoopRun();
            GameObject entranceObject = new("Stopped Room Entrance Test");
            GameObject blocker = new("Stopped Room Blocker Test");
            GameObject rewardObject = new("Stopped Room Reward Test");
            try
            {
                SunflowerStoppedRoomTemptation temptation =
                    entranceObject.AddComponent<SunflowerStoppedRoomTemptation>();
                temptation.Configure(blocker);
                temptation.BindForCurrentChapter();
                PocketSunTemptation reward =
                    rewardObject.AddComponent<PocketSunTemptation>();

                reward.Interact(null);
                Assert.That(run.GetFlag("CH5_POCKET_SUN_TAKEN"), Is.False);
                temptation.Interact(null);
                Assert.That(run.GetFlag("CH5_STOPPED_ROOM_ENTERED"), Is.False);

                Assert.That(run.ChapterLoop.TryActivateGate(), Is.True);
                temptation.Interact(null);
                reward.Interact(null);

                Assert.That(run.GetFlag("CH5_STOPPED_ROOM_ENTERED"), Is.True);
                Assert.That(blocker.activeSelf, Is.False);
                Assert.That(run.GetFlag("CH5_MARU_ORIGINAL_COMMAND_FOUND"), Is.True);
                Assert.That(run.GetFlag("STARPATH_MARU_ORIGINAL_COMMAND_KNOWN"), Is.True);
                Assert.That(run.GetFlag("CH5_FINAL_LIGHT_SUPPORT"), Is.True);
                Assert.That(run.Actions.Records.Any(record =>
                    record.actionType == StarActionType.EnteredTemptationRoom &&
                    record.targetId == "SunflowerStoppedRoom"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(rewardObject);
                Object.DestroyImmediate(blocker);
                Object.DestroyImmediate(entranceObject);
            }
        }

        [Test]
        public void SunGardenCommandEcho_RecordsMandatoryMaruBeatOnce()
        {
            StarNightRunState run = CreateSunGateLoopRun();
            GameObject storyObject = new("Sun Garden Command Echo Test");
            try
            {
                SunGardenMaruCommandEcho story =
                    storyObject.AddComponent<SunGardenMaruCommandEcho>();

                Assert.That(story.PlayForCurrentChapter(), Is.True);
                Assert.That(story.PlayForCurrentChapter(), Is.False);
                Assert.That(run.GetFlag("CH5_MARU_COMMAND_ECHO_HEARD"), Is.True);
                Assert.That(run.Actions.Records.Count(record =>
                    record.targetId == "MaruCommandEcho" &&
                    record.detail.Contains("모두 집으로")), Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(storyObject);
            }
        }

        [Test]
        public void SunGardenDeparture_RequiresManualGateActivation()
        {
            StarNightRunState run = CreateReadySunGateLoopRun();
            run.SetFlag("CH5_MARU_COMMAND_ECHO_HEARD");
            GameObject departureObject = new("Sun Garden Departure Test");
            try
            {
                SunGardenDepartureGate departure =
                    departureObject.AddComponent<SunGardenDepartureGate>();

                departure.Interact(null);

                Assert.That(run.Chapter.Departed, Is.False);
                Assert.That(departure.Prompt, Does.Contain("손잡이"));
            }
            finally
            {
                Object.DestroyImmediate(departureObject);
            }
        }

        [Test]
        public void Maru_PrioritizesBrightSunlightSource()
        {
            GameObject ordinaryObject = CreatePostalParcel("ordinary-light-test", Vector2.zero);
            GameObject brightObject = CreatePostalParcel("bright-light-test", Vector2.zero,
                FableTraits.SunlightSource | FableTraits.BrightSource);
            try
            {
                float ordinaryScore = MaruDirector.CalculateTargetScore(
                    ordinaryObject.GetComponent<FableObject>(), 0f);
                float brightScore = MaruDirector.CalculateTargetScore(
                    brightObject.GetComponent<FableObject>(), 0f);

                Assert.That(brightScore, Is.GreaterThanOrEqualTo(ordinaryScore + 20f));
            }
            finally
            {
                Object.DestroyImmediate(ordinaryObject);
                Object.DestroyImmediate(brightObject);
            }
        }

        [Test]
        public void Maru_PrioritizesAirborneObjects()
        {
            GameObject groundObject = CreateWeighted("ground-target", 1f, 1f);
            GameObject airObject = CreateWeighted("air-target", 1f, -0.2f, FableTraits.RainCloud);
            try
            {
                float groundScore = MaruDirector.CalculateTargetScore(
                    groundObject.GetComponent<FableObject>(), 0f);
                float airScore = MaruDirector.CalculateTargetScore(
                    airObject.GetComponent<FableObject>(), 0f);

                Assert.That(airScore, Is.GreaterThan(groundScore + 6f));
            }
            finally
            {
                Object.DestroyImmediate(groundObject);
                Object.DestroyImmediate(airObject);
            }
        }

        [Test]
        public void GateLoop_TwoDifferentRoutesCreateGateReady()
        {
            StarNightRunState run = CreateGateLoopRun();

            Assert.That(run.ChapterLoop.OpenRoutes(), Is.True);
            Assert.That(run.ChapterLoop.CompleteRoute("CH1_ROUTE_MILL"), Is.True);
            Assert.That(run.ChapterLoop.CompleteRoute("CH1_ROUTE_MINE"), Is.True);
            Assert.That(run.GateContributions.Count, Is.EqualTo(2));

            Assert.That(run.ChapterLoop.TryContribute("CH1_ROUTE_MILL"), Is.True);
            Assert.That(run.Chapter.GateReady, Is.False);
            Assert.That(run.ChapterLoop.TryContribute("CH1_ROUTE_MINE"), Is.True);

            Assert.That(run.Chapter.GateContributions, Is.EqualTo(2));
            Assert.That(run.Chapter.DepartureProgress, Is.EqualTo(2));
            Assert.That(run.Chapter.GateReady, Is.True);
            Assert.That(run.Chapter.DepartureReady, Is.True);
            Assert.That(run.ChapterLoop.State, Is.EqualTo(ChapterLoopState.GateReady));
        }

        [Test]
        public void GateLoop_SameRouteCannotIssueDuplicateContribution()
        {
            StarNightRunState run = CreateGateLoopRun();
            run.ChapterLoop.OpenRoutes();

            Assert.That(run.ChapterLoop.CompleteRoute("CH1_ROUTE_MILL"), Is.True);
            Assert.That(run.ChapterLoop.CompleteRoute("CH1_ROUTE_MILL"), Is.False);
            Assert.That(run.GateContributions.Count, Is.EqualTo(1));

            Assert.That(run.ChapterLoop.TryContribute("CH1_ROUTE_MILL"), Is.True);
            Assert.That(run.ChapterLoop.TryContribute("CH1_ROUTE_MILL"), Is.False);
            Assert.That(run.Chapter.GateContributions, Is.EqualTo(1));
        }

        [Test]
        public void GateReady_DoesNotOpenDepartureBeforeManualActivation()
        {
            StarNightRunState run = CreateReadyGateLoopRun();

            Assert.That(run.Chapter.GateReady, Is.True);
            Assert.That(run.Chapter.GateActivated, Is.False);
            Assert.That(run.Chapter.DepartureOpen, Is.False);
            Assert.That(run.Chapter.TemptationOpen, Is.False);
            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.None));
            Assert.That(run.CompleteCurrentChapter(), Is.Null);
        }

        [Test]
        public void GateActivation_OpensBothPathsAndStartsOnlyFirstBell()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            run.Chapter.AddScent(82f, "가동 전 도구 사용", "test");

            Assert.That(run.ChapterLoop.TryActivateGate(), Is.True);

            Assert.That(run.Chapter.GateActivated, Is.True);
            Assert.That(run.Chapter.DepartureOpen, Is.True);
            Assert.That(run.Chapter.TemptationOpen, Is.True);
            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.First));
            Assert.That(run.Chapter.LoopState, Is.EqualTo(ChapterLoopState.Bell1));
            Assert.That(run.Chapter.GateActivationScentBaseline, Is.EqualTo(82f).Within(0.001f));
            Assert.That(run.Actions.Records.Any(record =>
                record.actionType == StarActionType.GateActivated &&
                record.gateActivated &&
                record.bellPhase == 1), Is.True);
        }

        [Test]
        public void BellPhases_CannotSkipAndThirdBellClosesGate()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            run.ChapterLoop.TryActivateGate();

            Assert.That(run.ChapterLoop.TryAdvanceBell(StarBellPhase.Third), Is.False);
            Assert.That(run.ChapterLoop.TryAdvanceBell(StarBellPhase.Second), Is.True);
            Assert.That(run.Chapter.GateClosing, Is.False);
            Assert.That(run.ChapterLoop.TryAdvanceBell(StarBellPhase.Third), Is.True);

            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.Third));
            Assert.That(run.Chapter.LoopState, Is.EqualTo(ChapterLoopState.Bell3));
            Assert.That(run.Chapter.GateClosing, Is.True);
        }

        [Test]
        public void GateAlert_NegativeScentBuysTimeWithoutUndoingFirstBell()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            run.Chapter.AddScent(96f, "가동 전 오래 머물렀다", "BeforeGate");
            run.ChapterLoop.TryActivateGate();

            run.Chapter.AddScent(12f, "가동 뒤 절구를 사용했다", "AfterGate");
            run.Chapter.AddScent(-20f, "마루가 냄새나는 물건을 물어 갔다", "Maru");

            Assert.That(run.Chapter.Scent, Is.EqualTo(80f).Within(0.001f));
            Assert.That(run.Chapter.GateActivationScentBaseline, Is.EqualTo(96f).Within(0.001f));
            Assert.That(run.Chapter.PostGateAlert, Is.Zero.Within(0.001f));
            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.First));
        }

        [Test]
        public void BellPresenter_AdvancesAtThirtyAndSixtyPostGateAlert()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            GameObject presenterObject = new("Bell Presenter Test");
            try
            {
                MaruDirector maru = presenterObject.AddComponent<MaruDirector>();
                BellChasePresenter presenter = presenterObject.AddComponent<BellChasePresenter>();
                presenter.BindForCurrentChapter(maru);
                run.ChapterLoop.TryActivateGate();

                run.Chapter.AddScent(29.9f, "첫 방울 뒤 작은 흔적", "Alert");
                Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.First));
                Assert.That(maru.HuntMode, Is.EqualTo(MaruHuntMode.TraceOnly));
                Assert.That(maru.CanTargetPlayer, Is.False);

                run.Chapter.AddScent(0.1f, "흔적이 정거장까지 이어졌다", "Alert");
                Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.Second));
                Assert.That(maru.HuntMode, Is.EqualTo(MaruHuntMode.StationHunt));
                Assert.That(maru.CanTargetPlayer, Is.False);

                run.Chapter.AddScent(30f, "냄새가 플레이어에게 모였다", "Alert");
                Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.Third));
                Assert.That(maru.HuntMode, Is.EqualTo(MaruHuntMode.PlayerHunt));
                Assert.That(maru.CanTargetPlayer, Is.True);
                Assert.That(run.Chapter.GateClosing, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void BellPresenter_LargeAlertStillRecordsSecondBeforeThirdBell()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            GameObject presenterObject = new("Bell Presenter Sequence Test");
            try
            {
                BellChasePresenter presenter = presenterObject.AddComponent<BellChasePresenter>();
                presenter.BindForCurrentChapter();
                run.ChapterLoop.TryActivateGate();
                run.Chapter.AddScent(65f, "한 번에 큰 사고가 났다", "LargeAlert");

                StarActionRecord[] bellRecords = run.Actions.Records
                    .Where(record => record.actionType == StarActionType.BellPhaseChanged)
                    .ToArray();
                Assert.That(bellRecords.Select(record => record.bellPhase),
                    Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.Third));
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void GateLoopDeparture_RecordKeepsGateAndBellContext()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            run.ChapterLoop.TryActivateGate();
            run.ChapterLoop.TryAdvanceBell(StarBellPhase.Second);
            run.CompleteCurrentChapter();

            StarActionRecord departure = run.Actions.Records.Last(record =>
                record.actionType == StarActionType.ChapterDeparted);
            Assert.That(departure.gateContributions, Is.EqualTo(2));
            Assert.That(departure.gateReady, Is.True);
            Assert.That(departure.gateActivated, Is.True);
            Assert.That(departure.bellPhase, Is.EqualTo(2));
        }

        [Test]
        public void MaruNpcTarget_MarksResidentMissingWhenTaken()
        {
            StarNightRunState run = CreateGateLoopRun();
            GameObject resident = new("Resident Target Test");
            try
            {
                MaruNpcTarget target = resident.AddComponent<MaruNpcTarget>();
                target.Configure("Rabbit_Test", "시험 달토끼");

                Assert.That(target.TryTake(), Is.True);
                Assert.That(run.GetNpcState("Rabbit_Test"), Is.EqualTo(StarNpcState.Missing));
                Assert.That(target.Taken, Is.True);
                Assert.That(target.TryTake(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(resident);
            }
        }

        [TestCase("CH1_ROUTE_MILL", "CH1_ROUTE_MINE")]
        [TestCase("CH1_ROUTE_MILL", "CH1_ROUTE_STORAGE")]
        [TestCase("CH1_ROUTE_MINE", "CH1_ROUTE_STORAGE")]
        public void EveryMoonMillRoutePair_ReachesGateReady(string firstRoute, string secondRoute)
        {
            StarNightRunState run = CreateGateLoopRun();
            run.ChapterLoop.OpenRoutes();

            Assert.That(run.ChapterLoop.CompleteRoute(firstRoute), Is.True);
            Assert.That(run.ChapterLoop.CompleteRoute(secondRoute), Is.True);
            Assert.That(run.ChapterLoop.TryContribute(firstRoute), Is.True);
            Assert.That(run.ChapterLoop.TryContribute(secondRoute), Is.True);

            Assert.That(run.Chapter.GateReady, Is.True);
            Assert.That(run.Chapter.GateContributions, Is.EqualTo(2));
            Assert.That(run.ChapterLoop.State, Is.EqualTo(ChapterLoopState.GateReady));
        }

        [TestCase("CH2_ROUTE_NEW_ANCHOR", "CH2_ROUTE_STORM_ANCHOR")]
        [TestCase("CH2_ROUTE_NEW_ANCHOR", "CH2_ROUTE_OLD_BRIDGE")]
        [TestCase("CH2_ROUTE_STORM_ANCHOR", "CH2_ROUTE_OLD_BRIDGE")]
        public void EveryMagpieBridgeRoutePair_ReachesGateReady(string firstRoute, string secondRoute)
        {
            StarNightRunState run = CreateMagpieGateLoopRun();
            run.ChapterLoop.OpenRoutes();

            Assert.That(run.ChapterLoop.CompleteRoute(firstRoute), Is.True);
            Assert.That(run.ChapterLoop.CompleteRoute(secondRoute), Is.True);
            Assert.That(run.ChapterLoop.TryContribute(firstRoute), Is.True);
            Assert.That(run.ChapterLoop.TryContribute(secondRoute), Is.True);

            Assert.That(run.Chapter.GateReady, Is.True);
            Assert.That(run.Chapter.GateContributions, Is.EqualTo(2));
            Assert.That(run.ChapterLoop.State, Is.EqualTo(ChapterLoopState.GateReady));
        }

        [Test]
        public void HaechiDecision_IsRequiredStoryButNeverAddsGateContribution()
        {
            StarNightRunState run = CreateMagpieGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject decisionObject = new("Haechi Decision Test");
            try
            {
                MagpieHaechiDecision decision = decisionObject.AddComponent<MagpieHaechiDecision>();
                decision.Configure(HaechiDecisionMode.LeaveDepartureOpen);
                decision.Interact(null);

                Assert.That(run.GetFlag("CH2_HAECHI_RESOLVED"), Is.True);
                Assert.That(run.GetFlag("CH2_HAECHI_ALLOWED"), Is.True);
                Assert.That(run.GateContributions.Count, Is.Zero);
                Assert.That(run.Chapter.GateContributions, Is.Zero);
                Assert.That(run.ChapterLoop.Routes.All(route =>
                    route.state == GateRouteState.Available), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(decisionObject);
            }
        }

        [Test]
        public void OldBridgeRoute_CanBeRecoveredBeforeGateReady()
        {
            StarNightRunState run = CreateMagpieGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject switchObject = new("Old Bridge Route Test");
            try
            {
                GateRouteObjective objective = switchObject.AddComponent<GateRouteObjective>();
                objective.Configure("CH2_ROUTE_OLD_BRIDGE");
                MagpieOldBridgeSwitch bridgeSwitch = switchObject.AddComponent<MagpieOldBridgeSwitch>();
                bridgeSwitch.ConfigureRouteObjective(objective);

                bridgeSwitch.Interact(null);
                Assert.That(run.GateContributions.ContainsRoute("CH2_ROUTE_OLD_BRIDGE"), Is.True);
                Assert.That(run.GetFlag("CH2_OLD_BRIDGE_CUT"), Is.True);

                bridgeSwitch.Interact(null);
                Assert.That(run.GetFlag("CH2_OLD_BRIDGE_RESTORED"), Is.True);
                Assert.That(run.GateContributions.ContainsRoute("CH2_ROUTE_OLD_BRIDGE"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(switchObject);
            }
        }

        [Test]
        public void StarLadderTemptation_OpensOnlyAfterGateActivation()
        {
            StarNightRunState run = CreateMagpieGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject gateObject = new("Star Ladder Gate Test");
            GameObject blocker = new("Star Ladder Blocker Test");
            try
            {
                MagpieStarLadderTemptation temptation =
                    gateObject.AddComponent<MagpieStarLadderTemptation>();
                temptation.Configure(blocker);
                temptation.BindForCurrentChapter();

                temptation.Interact(null);
                Assert.That(run.GetFlag("magpie.temptation.open"), Is.False);
                Assert.That(blocker.activeSelf, Is.True);

                run.ChapterLoop.CompleteRoute("CH2_ROUTE_NEW_ANCHOR");
                run.ChapterLoop.CompleteRoute("CH2_ROUTE_STORM_ANCHOR");
                run.ChapterLoop.TryContribute("CH2_ROUTE_NEW_ANCHOR");
                run.ChapterLoop.TryContribute("CH2_ROUTE_STORM_ANCHOR");
                run.ChapterLoop.TryActivateGate();

                Assert.That(blocker.activeSelf, Is.True);
                temptation.Interact(null);
                Assert.That(run.GetFlag("magpie.temptation.open"), Is.True);
                Assert.That(blocker.activeSelf, Is.False);
                Assert.That(run.Chapter.PostGateAlert, Is.EqualTo(12f));
                Assert.That(run.Actions.Records.Any(record =>
                    record.actionType == StarActionType.EnteredTemptationRoom &&
                    record.targetId == "EndlessStarLadder"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gateObject);
                Object.DestroyImmediate(blocker);
            }
        }

        [Test]
        public void MagpieDeparture_RequiresExplicitHaechiResolution()
        {
            StarNightRunState run = CreateMagpieGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            run.ChapterLoop.CompleteRoute("CH2_ROUTE_NEW_ANCHOR");
            run.ChapterLoop.CompleteRoute("CH2_ROUTE_STORM_ANCHOR");
            run.ChapterLoop.TryContribute("CH2_ROUTE_NEW_ANCHOR");
            run.ChapterLoop.TryContribute("CH2_ROUTE_STORM_ANCHOR");
            run.ChapterLoop.TryActivateGate();
            GameObject departureObject = new("Magpie Departure Test");
            try
            {
                MagpieBridgeDepartureGate departure =
                    departureObject.AddComponent<MagpieBridgeDepartureGate>();
                departure.Interact(null);

                Assert.That(run.Chapter.Departed, Is.False);
                Assert.That(run.GetFlag("CH2_HAECHI_RESOLVED"), Is.False);
                Assert.That(departure.Prompt, Does.Contain("해치"));
            }
            finally
            {
                Object.DestroyImmediate(departureObject);
            }
        }

        [Test]
        public void M2Telemetry_RecordsRouteChoiceTemptationAndDeparture()
        {
            StarNightRunState run = CreateGateLoopRun();
            GameObject telemetryObject = new("M2 Telemetry Test");
            try
            {
                ChapterPlaytestTelemetry telemetry =
                    telemetryObject.AddComponent<ChapterPlaytestTelemetry>();
                telemetry.BeginTracking();
                run.ChapterLoop.OpenRoutes();
                run.ChapterLoop.CompleteRoute("CH1_ROUTE_MILL");
                run.ChapterLoop.CompleteRoute("CH1_ROUTE_STORAGE");
                run.ChapterLoop.TryContribute("CH1_ROUTE_MILL");
                run.ChapterLoop.TryContribute("CH1_ROUTE_STORAGE");
                run.ChapterLoop.TryActivateGate();
                run.ChapterLoop.TryAdvanceBell(StarBellPhase.Second);
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.EnteredTemptationRoom,
                    targetId = "MoonBackStorage"
                });
                run.CompleteCurrentChapter();

                Assert.That(telemetry.RouteCombination,
                    Is.EqualTo("CH1_ROUTE_MILL+CH1_ROUTE_STORAGE"));
                Assert.That(telemetry.EnteredTemptation, Is.True);
                Assert.That(telemetry.Departed, Is.True);
                Assert.That(telemetry.CaughtByMaru, Is.False);
                Assert.That(telemetry.BuildTechnicalReport(), Does.Contain("outcome=Departed"));
            }
            finally
            {
                Object.DestroyImmediate(telemetryObject);
            }
        }

        [Test]
        public void StarGatePrompt_SeparatesReadyFromActive()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            GameObject gateObject = new("Gate Prompt Test");
            try
            {
                StarGateController gate = gateObject.AddComponent<StarGateController>();
                Assert.That(gate.Prompt, Does.Contain("손잡이"));

                run.ChapterLoop.TryActivateGate();
                Assert.That(gate.Prompt, Does.Contain("가동 중"));
            }
            finally
            {
                Object.DestroyImmediate(gateObject);
            }
        }

        [Test]
        public void ActiveStarGate_IgnoresUnusedThirdRouteContribution()
        {
            StarNightRunState run = CreatePostGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            run.ChapterLoop.CompleteRoute("CH4_ROUTE_REGULAR_POST");
            run.ChapterLoop.CompleteRoute("CH4_ROUTE_DEAD_LETTER");
            run.ChapterLoop.CompleteRoute("CH4_ROUTE_SEALED_LETTER");
            run.ChapterLoop.TryContribute("CH4_ROUTE_REGULAR_POST");
            run.ChapterLoop.TryContribute("CH4_ROUTE_DEAD_LETTER");
            Assert.That(run.ChapterLoop.TryActivateGate(), Is.True);
            GameObject gateObject = new("Active Gate With Spare Contribution Test");
            try
            {
                StarGateController gate = gateObject.AddComponent<StarGateController>();

                Assert.That(run.GateContributions.Count, Is.EqualTo(1));
                Assert.That(gate.Prompt, Does.Contain("가동 중"));

                gate.Interact(null);
                Assert.That(run.GateContributions.Count, Is.EqualTo(1));
                Assert.That(run.Chapter.GateContributions, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(gateObject);
            }
        }

        [Test]
        public void ThirdBellForcedReturn_RecordsUnderstandableCauseAndBellContext()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            run.ChapterLoop.TryActivateGate();
            run.ChapterLoop.TryAdvanceBell(StarBellPhase.Second);
            run.ChapterLoop.TryAdvanceBell(StarBellPhase.Third);
            GameObject playerObject = new("Forced Return Player Test");
            try
            {
                StarNightPlayerAgent player = playerObject.AddComponent<StarNightPlayerAgent>();
                player.ForcedReturn();

                StarActionRecord caught = run.Actions.Records.Last(record =>
                    record.actionType == StarActionType.PlayerCaught);
                Assert.That(caught.detail, Does.Contain("세 번째 방울"));
                Assert.That(caught.gateActivated, Is.True);
                Assert.That(caught.bellPhase, Is.EqualTo(3));
                Assert.That(run.EndReason, Is.EqualTo(StarRunEndReason.ForcedReturnByMaru));
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void CompletedRoute_CanBeInvalidatedBeforeContribution()
        {
            StarNightRunState run = CreateGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            run.ChapterLoop.CompleteRoute("CH1_ROUTE_STORAGE");

            Assert.That(run.GateContributions.Count, Is.EqualTo(1));
            Assert.That(run.ChapterLoop.InvalidateRoute("CH1_ROUTE_STORAGE"), Is.True);
            Assert.That(run.GateContributions.Count, Is.Zero);
            Assert.That(run.ChapterLoop.FindRoute("CH1_ROUTE_STORAGE").state,
                Is.EqualTo(GateRouteState.Invalidated));
            Assert.That(run.ChapterLoop.TryContribute("CH1_ROUTE_STORAGE"), Is.False);
        }

        [Test]
        public void CompletedRouteContribution_CanBeReturnedAndRetakenBeforeMounting()
        {
            StarNightRunState run = CreateGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            run.ChapterLoop.CompleteRoute("CH1_ROUTE_STORAGE");

            Assert.That(run.ChapterLoop.ReturnRouteContribution(
                "CH1_ROUTE_STORAGE", "저장 길떡을 돌려놓았다"), Is.True);
            Assert.That(run.GateContributions.Count, Is.Zero);
            Assert.That(run.ChapterLoop.FindRoute("CH1_ROUTE_STORAGE").state,
                Is.EqualTo(GateRouteState.Available));
            Assert.That(run.GetFlag("CH1_ROUTE_STORAGE_RETURNED"), Is.True);

            Assert.That(run.ChapterLoop.CompleteRoute("CH1_ROUTE_STORAGE"), Is.True);
            Assert.That(run.GateContributions.Count, Is.EqualTo(1));
        }

        [Test]
        public void MoonMillPress_RequiresRepairBeforeIssuingNewPathCake()
        {
            StarNightRunState run = CreateGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject stationObject = new("Test Moon Mill Press");
            try
            {
                GateRouteObjective objective = stationObject.AddComponent<GateRouteObjective>();
                objective.Configure("CH1_ROUTE_MILL");
                MoonMillPathCakePress press = stationObject.AddComponent<MoonMillPathCakePress>();
                press.Configure(objective);

                press.Interact(null);
                Assert.That(run.ChapterLoop.FindRoute("CH1_ROUTE_MILL").state,
                    Is.EqualTo(GateRouteState.Available));

                run.SetFlag("moonmill.mill.repaired");
                press.Interact(null);

                Assert.That(run.GetFlag("CH1_ROUTE_MILL_COMPLETE"), Is.True);
                Assert.That(run.ChapterLoop.FindRoute("CH1_ROUTE_MILL").state,
                    Is.EqualTo(GateRouteState.Complete));
                Assert.That(run.GateContributions.ContainsRoute("CH1_ROUTE_MILL"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(stationObject);
            }
        }

        [Test]
        public void WinterStorage_CanReturnCakeBeforeGateContribution()
        {
            StarNightRunState run = CreateGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject storageObject = new("Test Winter Storage");
            try
            {
                GateRouteObjective objective = storageObject.AddComponent<GateRouteObjective>();
                objective.Configure("CH1_ROUTE_STORAGE");
                MoonMillWinterStorage storage = storageObject.AddComponent<MoonMillWinterStorage>();
                storage.Configure(objective);

                storage.Interact(null);
                Assert.That(run.GetFlag("CH1_STORAGE_WARNING_HEARD"), Is.True);
                Assert.That(run.GateContributions.ContainsRoute("CH1_ROUTE_STORAGE"), Is.True);

                storage.Interact(null);
                Assert.That(run.GetFlag("CH1_STORAGE_CAKE_RETURNED"), Is.True);
                Assert.That(run.GateContributions.ContainsRoute("CH1_ROUTE_STORAGE"), Is.False);
                Assert.That(run.ChapterLoop.FindRoute("CH1_ROUTE_STORAGE").state,
                    Is.EqualTo(GateRouteState.Available));
            }
            finally
            {
                Object.DestroyImmediate(storageObject);
            }
        }

        [Test]
        public void MountedWinterCake_AddsSupplyPenaltyToMagpieBridge()
        {
            StarNightRunState run = CreateGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            run.ChapterLoop.CompleteRoute("CH1_ROUTE_STORAGE");
            run.ChapterLoop.CompleteRoute("CH1_ROUTE_MINE");
            run.ChapterLoop.TryContribute("CH1_ROUTE_STORAGE");
            run.ChapterLoop.TryContribute("CH1_ROUTE_MINE");
            run.SetFlag("moonmill.mill.repaired");

            run.ConsequenceResolver.ResolveMoonMill();
            run.BeginChapter(CreateDefinition(StarChapterId.MagpieBridge, FableVerb.Link));

            Assert.That(run.GetFlag("CH1_WINTER_FOOD_USED"), Is.True);
            Assert.That(run.ConsequenceResolver.GetStartingScent(StarChapterId.MagpieBridge),
                Is.EqualTo(3f));
            Assert.That(run.ConsequenceResolver.ModifyScent(10f), Is.EqualTo(8.84f).Within(0.001f));
        }

        [TestCase("CH3_ROUTE_RANCH_WHEEL", "CH3_ROUTE_STORM_RIDGE")]
        [TestCase("CH3_ROUTE_RANCH_WHEEL", "CH3_ROUTE_GURU_BREATH")]
        [TestCase("CH3_ROUTE_STORM_RIDGE", "CH3_ROUTE_GURU_BREATH")]
        public void EveryCloudRanchRoutePair_ReachesGateReady(
            string firstRoute,
            string secondRoute)
        {
            StarNightRunState run = CreateCloudGateLoopRun();
            run.ChapterLoop.OpenRoutes();

            Assert.That(run.ChapterLoop.CompleteRoute(firstRoute), Is.True);
            Assert.That(run.ChapterLoop.CompleteRoute(secondRoute), Is.True);
            Assert.That(run.ChapterLoop.TryContribute(firstRoute), Is.True);
            Assert.That(run.ChapterLoop.TryContribute(secondRoute), Is.True);

            Assert.That(run.Chapter.GateReady, Is.True);
            Assert.That(run.Chapter.GateContributions, Is.EqualTo(2));
            Assert.That(run.ChapterLoop.State, Is.EqualTo(ChapterLoopState.GateReady));
        }

        [Test]
        public void CloudRainDock_GateLoopCompletesMappedWindRoute()
        {
            StarNightRunState run = CreateCloudGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject cloudObject =
                CreateWeighted("route-rain-cloud", 2f, 0f);
            GameObject dockObject = new("Mapped Cloud Rain Dock Test");
            try
            {
                cloudObject.transform.position = Vector2.zero;
                dockObject.transform.position = Vector2.zero;
                GateRouteObjective route =
                    dockObject.AddComponent<GateRouteObjective>();
                route.Configure("CH3_ROUTE_RANCH_WHEEL");
                CloudRainDock dock = dockObject.AddComponent<CloudRainDock>();
                dock.Configure(
                    "ROUTE_A",
                    cloudObject.GetComponent<FableObject>(),
                    1.35f);
                dock.ConfigureRouteObjective(
                    route,
                    "CH3_ROUTE_RANCH_WHEEL_COMPLETE");

                Assert.That(dock.TryDeliver(), Is.True);

                Assert.That(dock.Delivered, Is.True);
                Assert.That(route.Completed, Is.True);
                Assert.That(run.GetFlag("CH3_ROUTE_RANCH_WHEEL_COMPLETE"), Is.True);
                Assert.That(run.GateContributions.ContainsRoute(
                    "CH3_ROUTE_RANCH_WHEEL"), Is.True);
                Assert.That(run.Chapter.DepartureProgress, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(dockObject);
                Object.DestroyImmediate(cloudObject);
            }
        }

        [Test]
        public void CloudGuruBell_ThirdRingCompletesBreathRoute()
        {
            StarNightRunState run = CreateCloudGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject bellObject = new("Guru Breath Route Test");
            try
            {
                GateRouteObjective route =
                    bellObject.AddComponent<GateRouteObjective>();
                route.Configure("CH3_ROUTE_GURU_BREATH");
                CloudGuruBell bell = bellObject.AddComponent<CloudGuruBell>();
                bell.ConfigureRouteObjective(route);

                bell.Interact(null);
                bell.Interact(null);
                Assert.That(route.Completed, Is.False);

                bell.Interact(null);

                Assert.That(bell.Rings, Is.EqualTo(3));
                Assert.That(route.Completed, Is.True);
                Assert.That(run.GetFlag("CH3_GURU_AWAKENED_FORCEFULLY"), Is.True);
                Assert.That(run.GetFlag("CH3_ROUTE_GURU_BREATH_COMPLETE"), Is.True);
                Assert.That(run.GetCounter("CH3_STORM_DAMAGE"), Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(bellObject);
            }
        }

        [Test]
        public void CloudGuruRest_BeforeGateReadyRepairsForcedWakeCost()
        {
            StarNightRunState run = CreateCloudGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject bellObject = new("Guru Wake For Rest Test");
            GameObject restObject = new("Guru Rest Station Test");
            try
            {
                GateRouteObjective route =
                    bellObject.AddComponent<GateRouteObjective>();
                route.Configure("CH3_ROUTE_GURU_BREATH");
                CloudGuruBell bell = bellObject.AddComponent<CloudGuruBell>();
                bell.ConfigureRouteObjective(route);
                bell.Interact(null);
                bell.Interact(null);
                bell.Interact(null);

                CloudGuruRestStation rest =
                    restObject.AddComponent<CloudGuruRestStation>();
                rest.Interact(null);

                Assert.That(run.GetFlag("CH3_GURU_RESTED_AFTER_WAKE"), Is.True);
                Assert.That(run.GetFlag("CH3_DAMAGE_REPAIRED"), Is.True);
                Assert.That(run.GetCounter("CH3_STORM_DAMAGE"), Is.Zero);
                Assert.That(run.GetNpcState("Guru"), Is.EqualTo(StarNpcState.Calm));
                Assert.That(run.Actions.Records.Any(record =>
                    record.actionType == StarActionType.GuruReturned &&
                    record.routeId == "CH3_ROUTE_GURU_BREATH"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(bellObject);
                Object.DestroyImmediate(restObject);
            }
        }

        [Test]
        public void CloudRainbowRanch_RequiresGateActivationAndExplicitEntry()
        {
            StarNightRunState run = CreateReadyCloudGateLoopRun();
            GameObject entranceObject = new("Rainbow Ranch Entrance Test");
            GameObject blocker = new("Rainbow Ranch Blocker Test");
            try
            {
                CloudRainbowRanchTemptation temptation =
                    entranceObject.AddComponent<CloudRainbowRanchTemptation>();
                temptation.Configure(blocker);
                temptation.BindForCurrentChapter();

                temptation.Interact(null);
                Assert.That(run.GetFlag("CH3_RAINBOW_RANCH_ENTERED"), Is.False);
                Assert.That(blocker.activeSelf, Is.True);

                Assert.That(run.ChapterLoop.TryActivateGate(), Is.True);
                Assert.That(blocker.activeSelf, Is.True);

                temptation.Interact(null);
                Assert.That(run.GetFlag("CH3_RAINBOW_RANCH_ENTERED"), Is.True);
                Assert.That(blocker.activeSelf, Is.False);
                Assert.That(run.Chapter.PostGateAlert, Is.EqualTo(12f));
                Assert.That(run.Actions.Records.Any(record =>
                    record.actionType == StarActionType.EnteredTemptationRoom &&
                    record.targetId == "RainbowUpperRanch"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(entranceObject);
                Object.DestroyImmediate(blocker);
            }
        }

        [Test]
        public void CloudGuruRelease_IsSeparateFromWindContributions()
        {
            StarNightRunState run = CreateCloudGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject guruObject =
                CreateWeighted("guru-release-test", 5f, 0f, FableTraits.CloudWhale);
            GameObject anchorObject = new("Guru Anchor Test");
            GameObject decisionObject = new("Guru Release Decision Test");
            try
            {
                CloudGuruDecision decision =
                    decisionObject.AddComponent<CloudGuruDecision>();
                decision.Configure(
                    GuruDecisionMode.ReleaseAnchor,
                    guruObject.GetComponent<FableObject>(),
                    anchorObject);

                decision.Interact(null);

                Assert.That(run.GetFlag("CH3_GURU_RELEASED"), Is.True);
                Assert.That(run.GetNpcState("Guru"), Is.EqualTo(StarNpcState.Autonomous));
                Assert.That(run.GateContributions.Count, Is.Zero);
                Assert.That(run.Chapter.GateContributions, Is.Zero);
                Assert.That(run.ChapterLoop.Routes.All(route =>
                    route.state == GateRouteState.Available), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(guruObject);
                Object.DestroyImmediate(anchorObject);
                Object.DestroyImmediate(decisionObject);
            }
        }

        [Test]
        public void CloudCalfReturnStory_RecordsMandatoryMaruBeatOnce()
        {
            StarNightRunState run = CreateCloudGateLoopRun();
            GameObject calfObject =
                CreateWeighted("cloud-calf-test", 1f, 0f, FableTraits.CloudWhale);
            GameObject motherSide = new("Mother Side Test");
            GameObject storyObject = new("Calf Return Story Test");
            try
            {
                motherSide.transform.position = new Vector3(9f, 2f, 0f);
                CloudCalfReturnStory story =
                    storyObject.AddComponent<CloudCalfReturnStory>();
                story.Configure(
                    calfObject.GetComponent<FableObject>(),
                    motherSide.transform);

                Assert.That(story.PlayForCurrentChapter(), Is.True);
                Assert.That(story.PlayForCurrentChapter(), Is.False);
                Assert.That(run.GetFlag("CH3_CALF_RETURN_WITNESSED"), Is.True);
                Assert.That(calfObject.transform.position,
                    Is.EqualTo(motherSide.transform.position));
                Assert.That(run.Actions.Records.Count(record =>
                    record.actionType == StarActionType.CalfReturnedByMaru),
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(calfObject);
                Object.DestroyImmediate(motherSide);
                Object.DestroyImmediate(storyObject);
            }
        }

        [Test]
        public void CloudRanchDeparture_RequiresManualGateActivation()
        {
            StarNightRunState run = CreateReadyCloudGateLoopRun();
            GameObject departureObject = new("Cloud Ranch Departure Test");
            try
            {
                CloudRanchDepartureGate departure =
                    departureObject.AddComponent<CloudRanchDepartureGate>();

                departure.Interact(null);

                Assert.That(run.Chapter.Departed, Is.False);
                Assert.That(departure.Prompt, Does.Contain("손잡이"));
            }
            finally
            {
                Object.DestroyImmediate(departureObject);
            }
        }

        [TestCase("CH4_ROUTE_REGULAR_POST", "CH4_ROUTE_DEAD_LETTER")]
        [TestCase("CH4_ROUTE_REGULAR_POST", "CH4_ROUTE_SEALED_LETTER")]
        [TestCase("CH4_ROUTE_DEAD_LETTER", "CH4_ROUTE_SEALED_LETTER")]
        public void EveryStarPostRoutePair_ReachesGateReady(
            string firstRoute,
            string secondRoute)
        {
            StarNightRunState run = CreatePostGateLoopRun();
            run.ChapterLoop.OpenRoutes();

            Assert.That(run.ChapterLoop.CompleteRoute(firstRoute), Is.True);
            Assert.That(run.ChapterLoop.CompleteRoute(secondRoute), Is.True);
            Assert.That(run.ChapterLoop.TryContribute(firstRoute), Is.True);
            Assert.That(run.ChapterLoop.TryContribute(secondRoute), Is.True);

            Assert.That(run.Chapter.GateReady, Is.True);
            Assert.That(run.Chapter.GateContributions, Is.EqualTo(2));
            Assert.That(run.ChapterLoop.State, Is.EqualTo(ChapterLoopState.GateReady));
        }

        [Test]
        public void StarPostDelivery_CompletesOnlyTheMatchingRoute()
        {
            StarNightRunState run = CreatePostGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject trackerObject = new("Regular Post Route Tracker Test");
            GameObject parcelObject =
                CreatePostalParcel("training_moon_box", Vector2.zero);
            GameObject addressObject =
                CreateAddress("MOON", new Vector2(4f, 0f));
            try
            {
                GateRouteObjective route =
                    trackerObject.AddComponent<GateRouteObjective>();
                route.Configure("CH4_ROUTE_REGULAR_POST");
                StarPostDeliveryRouteObjective tracker =
                    trackerObject.AddComponent<StarPostDeliveryRouteObjective>();
                tracker.Configure(
                    route,
                    "training_moon_box",
                    "MOON",
                    "CH4_ROUTE_REGULAR_COMPLETE");
                tracker.BindForCurrentChapter();

                FableToolResult result = run.Delivery.DeliverDirect(
                    parcelObject.GetComponent<FableObject>(),
                    addressObject.GetComponent<StarPostalAddress>(),
                    "Test",
                    false);

                Assert.That(result.success, Is.True);
                Assert.That(tracker.Completed, Is.True);
                Assert.That(route.Completed, Is.True);
                Assert.That(run.GetFlag("CH4_ROUTE_REGULAR_COMPLETE"), Is.True);
                Assert.That(run.GetCounter("postal.shop_discount"), Is.EqualTo(1));
                Assert.That(run.ChapterLoop.FindRoute("CH4_ROUTE_REGULAR_POST").state,
                    Is.EqualTo(GateRouteState.Complete));
                Assert.That(run.ChapterLoop.FindRoute("CH4_ROUTE_DEAD_LETTER").state,
                    Is.EqualTo(GateRouteState.Available));
            }
            finally
            {
                Object.DestroyImmediate(trackerObject);
                Object.DestroyImmediate(parcelObject);
                Object.DestroyImmediate(addressObject);
            }
        }

        [Test]
        public void StarLetterSeal_CopyPreservesLetterAndCompletesRoute()
        {
            StarNightRunState run = CreatePostGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            run.SetFlag("CH4_ROUTE_REGULAR_COMPLETE");
            GameObject letterObject =
                CreatePostalParcel("last-letter", Vector2.zero, FableTraits.LastLetter);
            GameObject sealObject = new("Copy Letter Seal Test");
            try
            {
                GateRouteObjective route =
                    sealObject.AddComponent<GateRouteObjective>();
                route.Configure("CH4_ROUTE_SEALED_LETTER");
                StarLetterGateSeal seal =
                    sealObject.AddComponent<StarLetterGateSeal>();
                seal.Configure(
                    StarLetterGateSealMode.CopyAddress,
                    letterObject.GetComponent<FableObject>(),
                    route);

                seal.Interact(null);

                Assert.That(run.GetFlag("CH4_LETTER_STATE_COPIED"), Is.True);
                Assert.That(run.GetFlag("CH4_LETTER_STATE_SEALED"), Is.True);
                Assert.That(run.GetFlag("CH4_LETTER_PRESERVED"), Is.True);
                Assert.That(run.GetFlag("CH4_LETTER_SEAL_DAMAGED"), Is.False);
                Assert.That(route.Completed, Is.True);
                Assert.That(run.Actions.Records.Any(record =>
                    record.actionType == StarActionType.LetterSealCopied), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(sealObject);
                Object.DestroyImmediate(letterObject);
            }
        }

        [Test]
        public void StarLetterSeal_QuickUseDamagesSealWithoutOpeningLetter()
        {
            StarNightRunState run = CreatePostGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            GameObject letterObject =
                CreatePostalParcel("last-letter", Vector2.zero, FableTraits.LastLetter);
            GameObject sealObject = new("Damage Letter Seal Test");
            try
            {
                GateRouteObjective route =
                    sealObject.AddComponent<GateRouteObjective>();
                route.Configure("CH4_ROUTE_SEALED_LETTER");
                StarLetterGateSeal seal =
                    sealObject.AddComponent<StarLetterGateSeal>();
                seal.Configure(
                    StarLetterGateSealMode.UseSeal,
                    letterObject.GetComponent<FableObject>(),
                    route);

                seal.Interact(null);

                Assert.That(run.GetFlag("CH4_LETTER_SEAL_DAMAGED"), Is.True);
                Assert.That(run.GetFlag("CH4_LETTER_STATE_OPENED"), Is.False);
                Assert.That(run.GetFlag("CH4_LETTER_CONTENT_KNOWN"), Is.False);
                Assert.That(run.GetFlag("CH4_RANI_ARGUMENT"), Is.True);
                Assert.That(route.Completed, Is.True);
                Assert.That(run.Actions.Records.Any(record =>
                    record.actionType == StarActionType.LetterSealDamaged), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(sealObject);
                Object.DestroyImmediate(letterObject);
            }
        }

        [Test]
        public void StarPostDeepTruth_RequiresActiveGateAndExplicitVaultEntry()
        {
            StarNightRunState run = CreateReadyPostGateLoopRun();
            Assert.That(run.ChapterLoop.TryActivateGate(), Is.True);
            GameObject archiveObject = new("Full Context Archive Test");
            GameObject vaultObject = new("Return Vault Test");
            GameObject blocker = new("Return Vault Blocker Test");
            try
            {
                StarPostTruthArchive archive =
                    archiveObject.AddComponent<StarPostTruthArchive>();
                archive.Configure(StarPostTruthArchiveMode.FullContext);
                StarReturnVaultDoor vault =
                    vaultObject.AddComponent<StarReturnVaultDoor>();
                vault.Configure(blocker);

                archive.Interact(null);
                Assert.That(run.GetFlag("CH4_RANI_COMMAND_CONTEXT_READ"), Is.False);

                vault.Interact(null);
                Assert.That(run.GetFlag("CH4_RETURN_VAULT_OPENED"), Is.True);
                Assert.That(blocker.activeSelf, Is.False);

                archive.Interact(null);
                Assert.That(run.GetFlag("CH4_RANI_COMMAND_CONTEXT_READ"), Is.True);
                Assert.That(run.GetFlag("STARPATH_RANI_COMMAND_CONTEXT_KNOWN"), Is.True);
                Assert.That(run.Actions.Records.Any(record =>
                    record.actionType == StarActionType.EnteredTemptationRoom &&
                    record.targetId == "DeepReturnVault"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(archiveObject);
                Object.DestroyImmediate(vaultObject);
                Object.DestroyImmediate(blocker);
            }
        }

        [Test]
        public void StarPostDeparture_RequiresMainCommandFragment()
        {
            StarNightRunState run = CreateReadyPostGateLoopRun();
            Assert.That(run.ChapterLoop.TryActivateGate(), Is.True);
            GameObject departureObject = new("Star Post Departure Test");
            GameObject archiveObject = new("Main Command Archive Test");
            try
            {
                StarPostOfficeDepartureGate departure =
                    departureObject.AddComponent<StarPostOfficeDepartureGate>();
                StarPostTruthArchive archive =
                    archiveObject.AddComponent<StarPostTruthArchive>();
                archive.Configure(StarPostTruthArchiveMode.CommandFragment);

                departure.Interact(null);

                Assert.That(run.Chapter.Departed, Is.False);
                Assert.That(departure.Prompt, Does.Contain("메인 통신"));

                archive.Interact(null);
                Assert.That(run.GetFlag("CH4_RANI_COMMAND_FRAGMENT_READ"), Is.True);
                Assert.That(departure.Prompt, Does.Contain("우편선"));
            }
            finally
            {
                Object.DestroyImmediate(departureObject);
                Object.DestroyImmediate(archiveObject);
            }
        }

        [Test]
        public void GateLoopDeparture_StampsRunRouteMapAndEntersIntermission()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            run.ChapterLoop.TryActivateGate();

            StarChapterReport report = run.CompleteCurrentChapter();

            Assert.That(report, Is.Not.Null);
            Assert.That(run.RouteMap.RestoredGateCount, Is.EqualTo(1));
            Assert.That(run.RouteMap.IsGateRestored(StarChapterId.MoonRabbitMill), Is.True);
            Assert.That(run.Chapter.Departed, Is.True);
            Assert.That(run.ChapterLoop.State, Is.EqualTo(ChapterLoopState.Intermission));
        }

        [Test]
        public void LegacyChapter_StillUsesOriginalDepartureProgress()
        {
            StarNightRunState run = CreateRun(StarChapterId.MoonRabbitMill, FableVerb.Resize);

            Assert.That(run.Chapter.GateLoopEnabled, Is.False);
            Assert.That(run.Chapter.AddDepartureProgress(1, "legacy-cake"), Is.True);
            Assert.That(run.Chapter.DepartureProgress, Is.EqualTo(1));
            Assert.That(run.Chapter.RequiredDepartureProgress, Is.EqualTo(3));
            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.None));
        }

        [Test]
        public void PrologueEngine_RequiresSignAndCompanionChecks()
        {
            StarNightRunState run = CreatePrologueRun();
            PrologueJourneyBeat engine = CreatePrologueBeat(PrologueBeatMode.ReturnCakeEngine);

            Assert.That(engine.ExecuteForTests(), Is.False);
            run.SetFlag("PROLOGUE_CHECKED_SIGN");
            Assert.That(engine.ExecuteForTests(), Is.False);
            run.SetFlag("PROLOGUE_CHECKED_COMPANION");

            Assert.That(engine.ExecuteForTests(), Is.True);
            Assert.That(run.GetFlag("PROLOGUE_USED_RETURN_CAKE"), Is.True);
        }

        [Test]
        public void PrologueMaruRescue_RequiresReturnCakeIncident()
        {
            StarNightRunState run = CreatePrologueRun();
            PrologueJourneyBeat rescue = CreatePrologueBeat(PrologueBeatMode.MaruRescue);

            Assert.That(rescue.ExecuteForTests(), Is.False);
            run.SetFlag("PROLOGUE_USED_RETURN_CAKE");

            Assert.That(rescue.ExecuteForTests(), Is.True);
            Assert.That(run.GetFlag("PROLOGUE_MARU_RESCUE_SEEN"), Is.True);
            Assert.That(run.Actions.Records.Any(record =>
                record.actionType == StarActionType.MaruRescuedShip), Is.True);
        }

        [Test]
        public void PrologueGuideStarLoss_UnlocksTicketAndDeparture()
        {
            StarNightRunState run = CreatePrologueRun();
            run.SetFlag("PROLOGUE_MARU_RESCUE_SEEN");
            PrologueJourneyBeat guideStar = CreatePrologueBeat(PrologueBeatMode.GuideStarLoss);

            Assert.That(guideStar.ExecuteForTests(), Is.True);

            Assert.That(run.GetFlag("PROLOGUE_GUIDE_STAR_TAKEN"), Is.True);
            Assert.That(run.GetFlag("TICKET_MAP_UNLOCKED"), Is.True);
            Assert.That(run.GetFlag("PROLOGUE_FINAL_OBJECTIVE_HEARD"), Is.True);
            Assert.That(run.Chapter.DepartureReady, Is.True);
        }

        [Test]
        public void PrologueDeparture_KeepsIncidentFactsForNextChapter()
        {
            StarNightRunState run = CreatePrologueRun();
            run.SetFlag("PROLOGUE_USED_RETURN_CAKE");
            run.SetFlag("PROLOGUE_MARU_RESCUE_SEEN");
            run.SetFlag("PROLOGUE_GUIDE_STAR_TAKEN");
            run.SetFlag("TICKET_MAP_UNLOCKED");
            run.Chapter.AddDepartureProgress(1, "test-guide-star");
            PrologueJourneyBeat departure = CreatePrologueBeat(PrologueBeatMode.Departure);

            Assert.That(departure.ExecuteForTests(), Is.True);
            run.BeginChapter(CreateGateLoopDefinition());

            Assert.That(run.GetFlag("PROLOGUE_USED_RETURN_CAKE"), Is.True);
            Assert.That(run.GetFlag("PROLOGUE_MARU_RESCUE_SEEN"), Is.True);
            Assert.That(run.GetFlag("TICKET_MAP_UNLOCKED"), Is.True);
            Assert.That(run.ChapterReports.Select(report => report.chapter),
                Does.Contain(StarChapterId.Prologue));
        }

        [Test]
        public void TravelTicket_ListsFiveGatesAndPolaris()
        {
            StarNightRunState run = CreatePrologueRun();

            string ticket = run.RouteMap.BuildTicketText();

            Assert.That(ticket, Does.Contain("되찾은 별문 0/5"));
            Assert.That(ticket, Does.Contain("달토끼 방앗간"));
            Assert.That(ticket, Does.Contain("잠든 해의 정원"));
            Assert.That(ticket, Does.Contain("북극성 관측소"));
            Assert.That(ticket, Does.Contain("〈나〉"));
            Assert.That(ticket, Does.Contain("〈마루〉"));
        }

        [Test]
        public void TravelTicket_SnapshotRoundTripPreservesStampsAndPositions()
        {
            StarNightRunState run = CreatePrologueRun();
            run.RouteMap.RegisterGateRestored(StarChapterId.MoonRabbitMill);
            run.RouteMap.RegisterGateRestored(StarChapterId.MagpieBridge);
            string json = JsonUtility.ToJson(run.RouteMap.CaptureSnapshot());

            run.RouteMap.ResetForRun();
            run.RouteMap.RestoreSnapshot(JsonUtility.FromJson<RunRouteMapSnapshot>(json));

            Assert.That(run.RouteMap.RestoredGateCount, Is.EqualTo(2));
            Assert.That(run.RouteMap.IsGateRestored(StarChapterId.MoonRabbitMill), Is.True);
            Assert.That(run.RouteMap.IsGateRestored(StarChapterId.MagpieBridge), Is.True);
            Assert.That(run.RouteMap.PlayerStationIndex, Is.EqualTo(2));
            Assert.That(run.RouteMap.MaruStationIndex, Is.EqualTo(3));
        }

        [Test]
        public void TravelTicket_DuplicateStampDoesNotAdvanceMaruTwice()
        {
            StarNightRunState run = CreatePrologueRun();

            Assert.That(run.RouteMap.RegisterGateRestored(StarChapterId.MoonRabbitMill), Is.True);
            int maruStation = run.RouteMap.MaruStationIndex;
            Assert.That(run.RouteMap.RegisterGateRestored(StarChapterId.MoonRabbitMill), Is.False);

            Assert.That(run.RouteMap.RestoredGateCount, Is.EqualTo(1));
            Assert.That(run.RouteMap.MaruStationIndex, Is.EqualTo(maruStation));
        }

        [Test]
        public void IntermissionSummary_UsesStandardSixStepOrder()
        {
            StarNightRunState run = CreateRun(StarChapterId.MoonRabbitMill, FableVerb.Resize);
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.DamageRepaired,
                detail = "부서진 방앗간 물레를 고쳤다."
            });

            string summary = JourneyIntermissionFormatter.Build(run, "라니는 수리한 물레를 기록했다.");

            int previous = -1;
            for (int step = 1; step <= 6; step++)
            {
                int current = summary.IndexOf($"{step}.");
                Assert.That(current, Is.GreaterThan(previous));
                previous = current;
            }
            Assert.That(summary, Does.Contain("행동 되감기"));
            Assert.That(summary, Does.Contain("마루의 발자국"));
        }

        [Test]
        public void IntermissionSummary_PointsFromGardenToPolaris()
        {
            StarNightRunState run = CreateRun(StarChapterId.SleepingSunGarden, FableVerb.Awaken);

            string summary = JourneyIntermissionFormatter.Build(run, "기다림도 행동이었다.");

            Assert.That(summary, Does.Contain("기다리는 동안에도"));
            Assert.That(summary, Does.Contain("북극성 관측소에서 마루를 멈추기"));
        }

        [Test]
        public void PrologueDefinition_StatesSystemicDisasterPremise()
        {
            StarChapterDefinition definition = StarNightPrologueBootstrap.CreateDefinition();

            Assert.That(definition.chapter, Is.EqualTo(StarChapterId.Prologue));
            Assert.That(definition.oneSentenceRule, Does.Contain("잃어버린 것을 집으로"));
            Assert.That(definition.oneSentenceRule, Does.Contain("별까지 물어온다"));
            Assert.That(definition.guaranteedRooms, Has.Count.GreaterThanOrEqualTo(7));
        }

        [Test]
        public void WholeJourney_RestoresAllFiveGatesWithoutLosingPrologueFacts()
        {
            StarNightRunState run = CreatePrologueRun();
            run.SetFlag("PROLOGUE_USED_RETURN_CAKE");
            run.SetFlag("PROLOGUE_MARU_RESCUE_SEEN");
            run.SetFlag("PROLOGUE_GUIDE_STAR_TAKEN");
            run.SetFlag("TICKET_MAP_UNLOCKED");
            run.Chapter.AddDepartureProgress(1, "test-guide-star");
            Assert.That(run.CompleteCurrentChapter(), Is.Not.Null);

            StarChapterDefinition[] chapters =
            {
                CreateGateLoopDefinition(),
                CreateMagpieGateLoopDefinition(),
                CreateCloudGateLoopDefinition(),
                CreatePostGateLoopDefinition(),
                CreateSunGateLoopDefinition()
            };
            foreach (StarChapterDefinition chapter in chapters)
            {
                run.BeginChapter(chapter);
                run.ChapterLoop.OpenRoutes();
                GateRouteDefinition first = chapter.gateRoutes[0];
                GateRouteDefinition second = chapter.gateRoutes[1];
                Assert.That(run.ChapterLoop.CompleteRoute(first.id), Is.True);
                Assert.That(run.ChapterLoop.CompleteRoute(second.id), Is.True);
                Assert.That(run.ChapterLoop.TryContribute(first.id), Is.True);
                Assert.That(run.ChapterLoop.TryContribute(second.id), Is.True);
                Assert.That(run.ChapterLoop.TryActivateGate(), Is.True);
                Assert.That(run.CompleteCurrentChapter(), Is.Not.Null);
            }

            Assert.That(run.RouteMap.RestoredGateCount, Is.EqualTo(5));
            Assert.That(run.RouteMap.PlayerStationIndex, Is.EqualTo(5));
            Assert.That(run.RouteMap.MaruStationIndex, Is.EqualTo(5));
            Assert.That(run.ChapterReports, Has.Count.EqualTo(6));
            Assert.That(run.GetFlag("PROLOGUE_USED_RETURN_CAKE"), Is.True);
            Assert.That(run.GetFlag("TICKET_MAP_UNLOCKED"), Is.True);
            Assert.That(run.RouteMap.BuildTicketText(), Does.Contain("되찾은 별문 5/5"));
        }

        [Test]
        public void MoonMillBeginChapter_PreservesUnlockedTicketFromPrologue()
        {
            StarNightRunState run = CreatePrologueRun();
            run.SetFlag("TICKET_MAP_UNLOCKED");
            int originalSeed = run.Seed;

            run.BeginChapter(CreateGateLoopDefinition());

            Assert.That(run.Seed, Is.EqualTo(originalSeed));
            Assert.That(run.GetFlag("TICKET_MAP_UNLOCKED"), Is.True);
            Assert.That(run.CurrentChapter, Is.EqualTo(StarChapterId.MoonRabbitMill));
        }

        [Test]
        public void PolarisDefinition_UsesCenterStarGoalWithoutGateLoop()
        {
            StarChapterDefinition definition = PolarisChapterBootstrap.CreateDefinition();

            Assert.That(definition.chapter, Is.EqualTo(StarChapterId.PolarisObservatory));
            Assert.That(definition.useGateLoop, Is.False);
            Assert.That(definition.oneSentenceRule, Does.Contain("마루보다 먼저 중심별"));
            Assert.That(definition.guaranteedRooms, Has.Count.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void PolarisAccess_RequiresAllFiveRestoredGates()
        {
            (StarNightRunState run, PolarisFinaleState finale) = CreatePolarisRun(false);

            Assert.That(run.RouteMap.RestoredGateCount, Is.Zero);
            Assert.That(finale.Phase, Is.EqualTo(PolarisFinalePhase.Locked));
            Assert.That(run.GetFlag("POLARIS_ACCESS_GRANTED"), Is.False);
        }

        [Test]
        public void PolarisRecords_AllFiveUnlockObservatory()
        {
            (_, PolarisFinaleState finale) = CreatePolarisRun();

            RegisterAllPolarisRecords(finale);

            Assert.That(finale.RecordCount, Is.EqualTo(5));
            Assert.That(finale.Phase, Is.EqualTo(PolarisFinalePhase.Observatory));
            Assert.That(StarNightRunState.Instance.GetFlag("POLARIS_ALL_RECORDS_SEEN"), Is.True);
        }

        [Test]
        public void PolarisRecords_DuplicateDoesNotAdvanceCount()
        {
            (_, PolarisFinaleState finale) = CreatePolarisRun();

            Assert.That(finale.RegisterRecord(StarChapterId.MoonRabbitMill), Is.True);
            Assert.That(finale.RegisterRecord(StarChapterId.MoonRabbitMill), Is.False);

            Assert.That(finale.RecordCount, Is.EqualTo(1));
        }

        [Test]
        public void PolarisTruth_StartsCenterStarCountdown()
        {
            (_, PolarisFinaleState finale) = CreatePolarisRun();
            RegisterAllPolarisRecords(finale);

            Assert.That(finale.InspectObservatory(), Is.True);

            Assert.That(finale.Phase, Is.EqualTo(PolarisFinalePhase.Pursuit));
            Assert.That(finale.CountdownActive, Is.True);
            Assert.That(finale.TimeRemaining, Is.EqualTo(finale.PursuitDuration));
            Assert.That(StarNightRunState.Instance.GetFlag("POLARIS_TRUTH_SEEN"), Is.True);
        }

        [Test]
        public void PolarisRestoration_RejectsWrongToolOrder()
        {
            (_, PolarisFinaleState finale) = CreateReadyPolarisPursuit();

            Assert.That(finale.TryRestore(FableVerb.Link), Is.False);
            Assert.That(finale.TryRestore(FableVerb.Resize), Is.True);
            Assert.That(finale.TryRestore(FableVerb.Awaken), Is.False);

            Assert.That(finale.RestorationStep, Is.EqualTo(1));
            Assert.That(finale.ExpectedVerb, Is.EqualTo(FableVerb.Link));
        }

        [Test]
        public void PolarisRestoration_AllFiveToolsReachFinalChoice()
        {
            (_, PolarisFinaleState finale) = CreateReadyPolarisPursuit();

            RestoreAllPolarisTools(finale);

            Assert.That(finale.RestorationStep, Is.EqualTo(5));
            Assert.That(finale.Phase, Is.EqualTo(PolarisFinalePhase.FinalChoice));
            Assert.That(finale.CountdownActive, Is.False);
            Assert.That(StarNightRunState.Instance.GetFlag("POLARIS_CENTER_STAR_REACHED"), Is.True);
        }

        [Test]
        public void PolarisTimer_StableGardenAndFinalLightAddTime()
        {
            StarNightRunState run = CreateBarePolarisRun();
            run.SetFlag("CH5_HAOREUM_NATURAL_WAKE");
            run.SetFlag("CH5_GARDEN_RESTORED");
            run.SetFlag("CH5_FINAL_LIGHT_SUPPORT");
            run.SetFlag("CH5_STAR_PATH_TREE_STABLE");
            PolarisFinaleState finale = AddAndBeginFinale(run);

            Assert.That(finale.PursuitDuration, Is.EqualTo(215f));
        }

        [Test]
        public void PolarisTimer_BurnedOverheatedRouteReducesTime()
        {
            StarNightRunState run = CreateBarePolarisRun();
            run.SetFlag("CH5_SUN_AWAKENED_FORCEFULLY");
            run.SetFlag("CH5_GARDEN_FIRE");
            run.SetFlag("CH5_STAR_PATH_TREE_OVERGROWN");
            run.SetFlag("CH5_STAR_PATH_TREE_BURNED");
            PolarisFinaleState finale = AddAndBeginFinale(run);

            Assert.That(finale.PursuitDuration, Is.EqualTo(78f));
        }

        [Test]
        public void PolarisTimeout_ProducesClosedUniverseEnding()
        {
            (StarNightRunState run, PolarisFinaleState finale) = CreateReadyPolarisPursuit();

            finale.AdvanceTime(finale.PursuitDuration + 1f);

            Assert.That(finale.Ending, Is.EqualTo(PolarisEndingType.ClosedUniverse));
            Assert.That(finale.Phase, Is.EqualTo(PolarisFinalePhase.Complete));
            Assert.That(run.GetFlag("ENDING_CLOSEDUNIVERSE"), Is.True);
            Assert.That(run.RunActive, Is.False);
            Assert.That(run.EndReason, Is.EqualTo(StarRunEndReason.JourneyComplete));
        }

        [Test]
        public void PolarisPathCutter_IsAlwaysAvailableAfterCenterStar()
        {
            (StarNightRunState run, PolarisFinaleState finale) = CreateReadyPolarisFinalChoice();

            Assert.That(finale.TryChooseEnding(PolarisEndingType.PathCutter), Is.True);

            Assert.That(finale.Ending, Is.EqualTo(PolarisEndingType.PathCutter));
            Assert.That(run.GetFlag("ENDING_PATHCUTTER"), Is.True);
        }

        [Test]
        public void PolarisNewLeash_UsesUnlockedRedThread()
        {
            (StarNightRunState run, PolarisFinaleState finale) = CreateReadyPolarisFinalChoice();

            Assert.That(run.IsToolUnlocked(FableVerb.Link), Is.True);
            Assert.That(finale.TryChooseEnding(PolarisEndingType.NewLeash), Is.True);

            Assert.That(run.GetFlag("ENDING_NEWLEASH"), Is.True);
        }

        [Test]
        public void PolarisStarRoad_IsBlockedWhenOptionalCluesAreMissing()
        {
            (_, PolarisFinaleState finale) = CreateReadyPolarisFinalChoice();

            Assert.That(finale.StarRoadAvailable, Is.False);
            Assert.That(finale.TryChooseEnding(PolarisEndingType.StarRoad), Is.False);
            Assert.That(finale.BuildStarRoadRequirements(), Does.Contain("◇"));
        }

        [Test]
        public void PolarisStarRoad_RequiresMemoryConnectionDeliveryAndLight()
        {
            (StarNightRunState run, PolarisFinaleState finale) = CreateReadyPolarisFinalChoice();
            GrantStarRoadClues(run);

            Assert.That(finale.StarRoadAvailable, Is.True);
            Assert.That(finale.TryChooseEnding(PolarisEndingType.StarRoad), Is.True);

            Assert.That(finale.Ending, Is.EqualTo(PolarisEndingType.StarRoad));
            Assert.That(run.GetFlag("ENDING_STARROAD"), Is.True);
            Assert.That(run.GetFlag("POLARIS_RANI_DELIVERED"), Is.True);
            Assert.That(run.GetFlag("POLARIS_MARU_RELEASED"), Is.True);
            Assert.That(run.Actions.Records.Any(record =>
                record.actionType == StarActionType.RaniCommandWithdrawn), Is.True);
        }

        [Test]
        public void PolarisStarRoad_AcceptsRestoredMemoryAfterLetterDestroyed()
        {
            (StarNightRunState run, PolarisFinaleState finale) = CreateReadyPolarisFinalChoice();
            run.SetFlag("STARPATH_LETTER_DESTROYED");
            run.SetFlag("STARPATH_RANI_COMMAND_CONTEXT_KNOWN");
            run.SetFlag("CH5_RANI_PRESERVED_POT_FOUND");
            run.SetFlag("STARPATH_MARU_ORIGINAL_COMMAND_KNOWN");
            run.SetFlag("STARPATH_RANI_CAN_BE_DELIVERED");
            run.SetFlag("STARPATH_POLARIS_ROUTE_REGISTERED");
            run.SetFlag("CH5_FINAL_LIGHT_SUPPORT");

            Assert.That(PolarisFinaleState.HasStarRoadMemory(run), Is.True);
            Assert.That(finale.StarRoadAvailable, Is.True);
        }

        [Test]
        public void PolarisCounterVerb_UsesMostFrequentRecordedTool()
        {
            StarNightRunState run = CreateBarePolarisRun();
            run.Actions.Record(new StarActionContext { actionType = StarActionType.ToolApplied, tool = FableVerb.Link });
            run.Actions.Record(new StarActionContext { actionType = StarActionType.ToolApplied, tool = FableVerb.Link });
            run.Actions.Record(new StarActionContext { actionType = StarActionType.ToolApplied, tool = FableVerb.Float });
            PolarisFinaleState finale = AddAndBeginFinale(run);

            Assert.That(finale.CounterVerb, Is.EqualTo(FableVerb.Link));
        }

        [Test]
        public void PolarisEvaluation_ContainsPlayerRebuttal()
        {
            (_, PolarisFinaleState finale) = CreatePolarisRun();

            string evaluation = finale.BuildEvaluationAndRebuttal();

            Assert.That(evaluation, Does.Contain("왜 붙잡았는지는 이해"));
            Assert.That(evaluation, Does.Contain("놓아주는 말은 당신이 직접"));
        }

        [Test]
        public void PolarisTicket_RemainsFiveOfFiveAtFinalStation()
        {
            (StarNightRunState run, PolarisFinaleState finale) = CreatePolarisRun();

            Assert.That(finale.AccessGranted, Is.True);
            Assert.That(run.RouteMap.RestoredGateCount, Is.EqualTo(5));
            Assert.That(run.RouteMap.PlayerStationIndex, Is.EqualTo(5));
            Assert.That(run.RouteMap.MaruStationIndex, Is.EqualTo(5));
            Assert.That(run.RouteMap.BuildTicketText(), Does.Contain("되찾은 별문 5/5"));
        }

        [Test]
        public void M6BalanceProfile_ContainsSevenChapterTimeBudgets()
        {
            Assert.That(StarNightBalanceProfile.ChapterTargets, Has.Count.EqualTo(7));
            Assert.That(StarNightBalanceProfile.GetTarget(StarChapterId.Prologue).minimumMinutes,
                Is.EqualTo(5f));
            Assert.That(StarNightBalanceProfile.GetTarget(StarChapterId.StarPostOffice).maximumMinutes,
                Is.EqualTo(12f));
            Assert.That(StarNightBalanceProfile.GetTarget(StarChapterId.PolarisObservatory).maximumMinutes,
                Is.EqualTo(18f));
        }

        [TestCase(44f, RunPaceBand.TooFast)]
        [TestCase(45f, RunPaceBand.Target)]
        [TestCase(60f, RunPaceBand.Target)]
        [TestCase(61f, RunPaceBand.TooSlow)]
        public void M6BalanceProfile_EvaluatesGeneralEndingPace(float minutes, RunPaceBand expected)
        {
            Assert.That(StarNightBalanceProfile.EvaluateRunPace(minutes * 60f, false),
                Is.EqualTo(expected));
        }

        [TestCase(59f, RunPaceBand.TooFast)]
        [TestCase(60f, RunPaceBand.Target)]
        [TestCase(80f, RunPaceBand.Target)]
        [TestCase(81f, RunPaceBand.TooSlow)]
        public void M6BalanceProfile_EvaluatesStarRoadPace(float minutes, RunPaceBand expected)
        {
            Assert.That(StarNightBalanceProfile.EvaluateRunPace(minutes * 60f, true),
                Is.EqualTo(expected));
        }

        [Test]
        public void M6BalanceProfile_FiveGateChaptersExposeThreeDistinctRoutes()
        {
            List<ChapterBalanceTarget> gateTargets = StarNightBalanceProfile.ChapterTargets
                .Where(target => target.gateLoopChapter)
                .ToList();

            Assert.That(gateTargets, Has.Count.EqualTo(5));
            Assert.That(gateTargets.All(target =>
                target.routeIds.Count == 3 && target.routeIds.Distinct().Count() == 3), Is.True);
        }

        [Test]
        public void M6AlertClock_ReachesSecondBellAtNinetySeconds()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            Assert.That(run.ChapterLoop.TryActivateGate(), Is.True);

            run.ChapterLoop.AdvanceAlertClock(89f);
            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.First));

            run.ChapterLoop.AdvanceAlertClock(1f);
            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.Second));
            Assert.That(run.Chapter.PostGateAlert,
                Is.EqualTo(StarGateAlertRules.SecondBellThreshold).Within(0.01f));
        }

        [Test]
        public void M6AlertClock_ReachesThirdBellAtOneHundredEightySeconds()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            run.ChapterLoop.TryActivateGate();

            run.ChapterLoop.AdvanceAlertClock(180f);

            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.Third));
            Assert.That(run.Chapter.GateClosing, Is.True);
            Assert.That(run.Chapter.PostGateAlert,
                Is.EqualTo(StarGateAlertRules.ThirdBellThreshold).Within(0.01f));
        }

        [Test]
        public void M6AlertClock_DoesNotRunBeforeManualGateActivation()
        {
            StarNightRunState run = CreateReadyGateLoopRun();

            run.ChapterLoop.AdvanceAlertClock(600f);

            Assert.That(run.Chapter.GateActivated, Is.False);
            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.None));
            Assert.That(run.Chapter.PostGateAlert, Is.Zero);
        }

        [Test]
        public void M6AlertMitigation_BuysTimeWithoutReversingRungBell()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            run.ChapterLoop.TryActivateGate();
            run.Chapter.AddGateAlert(25f);
            run.Chapter.AddScent(-10f, "미끼로 시간을 벌었다", "M6Bait");

            Assert.That(run.Chapter.PostGateAlert, Is.EqualTo(15f).Within(0.01f));
            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.First));

            run.Chapter.AddGateAlert(20f);
            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.Second));
            run.Chapter.AddScent(-20f, "두 번째 미끼", "M6Bait2");

            Assert.That(run.Chapter.PostGateAlert,
                Is.EqualTo(StarGateAlertRules.SecondBellThreshold).Within(0.01f));
            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.Second));
        }

        [Test]
        public void M6AlertMitigation_CannotCancelThirdBell()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            run.ChapterLoop.TryActivateGate();
            run.Chapter.AddGateAlert(StarGateAlertRules.ThirdBellThreshold);

            run.Chapter.AddScent(-50f, "세 번째 방울 뒤 미끼", "LateBait");

            Assert.That(run.Chapter.BellPhase, Is.EqualTo(StarBellPhase.Third));
            Assert.That(run.Chapter.PostGateAlert,
                Is.EqualTo(StarGateAlertRules.ThirdBellThreshold).Within(0.01f));
            Assert.That(run.Chapter.GateClosing, Is.True);
        }

        [Test]
        public void M6AccidentReport_IncludesGateAndBellContext()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            run.ChapterLoop.TryActivateGate();
            run.ChapterLoop.AdvanceAlertClock(90f);

            run.AccidentReport.Add("붉은 실", "닻을 잡아당겨", "다리를 흔들었다");

            AccidentStep step = run.AccidentReport.Steps.Single();
            Assert.That(step.gateActivated, Is.True);
            Assert.That(step.bellPhase, Is.EqualTo(2));
            Assert.That(run.AccidentReport.BuildReport(), Does.Contain("[별문 가동 · 방울 2]"));
        }

        [Test]
        public void M6AccidentReport_PreGateAccidentRemainsReadableWithoutFalseBell()
        {
            StarNightRunState run = CreateGateLoopRun();

            run.AccidentReport.Add("달떡", "너무 커져", "굴뚝을 막았다");

            AccidentStep step = run.AccidentReport.Steps.Single();
            Assert.That(step.gateActivated, Is.False);
            Assert.That(step.bellPhase, Is.Zero);
            Assert.That(run.AccidentReport.BuildReport(), Does.Not.Contain("별문 가동"));
        }

        [Test]
        public void M6Aggregate_CalculatesTemptationAndRouteSelectionRates()
        {
            StarNightBalanceAggregate aggregate = new();
            aggregate.Add(new RunBalanceSnapshot
            {
                endReason = StarRunEndReason.JourneyComplete,
                ending = PolarisEndingType.PathCutter,
                durationSeconds = 50f * 60f,
                chapters =
                {
                    new ChapterBalanceSample
                    {
                        chapter = StarChapterId.MoonRabbitMill,
                        temptationEntered = true,
                        contributedRoutes = { "CH1_ROUTE_MILL", "CH1_ROUTE_MINE" }
                    },
                    new ChapterBalanceSample
                    {
                        chapter = StarChapterId.MagpieBridge,
                        temptationEntered = false,
                        contributedRoutes = { "CH2_ROUTE_NEW_ANCHOR", "CH2_ROUTE_OLD_BRIDGE" }
                    }
                }
            });

            Assert.That(aggregate.totalRuns, Is.EqualTo(1));
            Assert.That(aggregate.TemptationRate, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(aggregate.GetRouteShare(StarChapterId.MoonRabbitMill, "CH1_ROUTE_MILL"),
                Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void M6Aggregate_SeparatesGeneralAndStarRoadTimeAndInformation()
        {
            StarNightBalanceAggregate aggregate = new();
            aggregate.Add(new RunBalanceSnapshot
            {
                ending = PolarisEndingType.PathCutter,
                durationSeconds = 50f * 60f,
                informationUnits = 3
            });
            aggregate.Add(new RunBalanceSnapshot
            {
                ending = PolarisEndingType.StarRoad,
                durationSeconds = 70f * 60f,
                informationUnits = 7
            });

            Assert.That(aggregate.GeneralAverageMinutes, Is.EqualTo(50f));
            Assert.That(aggregate.StarRoadAverageMinutes, Is.EqualTo(70f));
            Assert.That(aggregate.GeneralAverageInformation, Is.EqualTo(3f));
            Assert.That(aggregate.StarRoadAverageInformation, Is.EqualTo(7f));
        }

        [Test]
        public void M6RunTelemetry_TracksRoutesTemptationAndEndingAcrossRun()
        {
            StarNightRunState run = CreateReadyGateLoopRun();
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.EnteredTemptationRoom,
                routeId = "CH1_TEMPTATION"
            });
            run.SetFlag("ENDING_PATHCUTTER");

            run.EndRun(StarRunEndReason.JourneyComplete);

            RunBalanceSnapshot snapshot = run.Telemetry.Latest;
            Assert.That(snapshot.endReason, Is.EqualTo(StarRunEndReason.JourneyComplete));
            Assert.That(snapshot.ending, Is.EqualTo(PolarisEndingType.PathCutter));
            Assert.That(snapshot.chapters, Has.Count.EqualTo(1));
            Assert.That(snapshot.chapters[0].contributedRoutes, Has.Count.EqualTo(2));
            Assert.That(snapshot.chapters[0].temptationEntered, Is.True);
        }

        [Test]
        public void M6InformationCount_DistinguishesGeneralAndStarRoadRequirements()
        {
            StarNightRunState run = CreateBarePolarisRun();
            run.SetFlag("TICKET_MAP_UNLOCKED");
            run.SetFlag("POLARIS_ALL_RECORDS_SEEN");
            run.SetFlag("POLARIS_TRUTH_SEEN");

            Assert.That(StarNightBalanceProfile.CountInformationUnits(run),
                Is.EqualTo(StarNightBalanceProfile.GeneralEndingInformationUnits));

            GrantStarRoadClues(run);

            Assert.That(StarNightBalanceProfile.CountInformationUnits(run),
                Is.EqualTo(StarNightBalanceProfile.StarRoadInformationUnits));
        }

        [TestCase(0.54f, false)]
        [TestCase(0.55f, true)]
        [TestCase(0.70f, true)]
        [TestCase(0.71f, false)]
        public void M6TemptationRate_UsesDesignTargetBand(float rate, bool expected)
        {
            Assert.That(StarNightBalanceProfile.IsTemptationRateOnTarget(rate), Is.EqualTo(expected));
        }

        [Test]
        public void M6PreviousChapterModifiers_StillCoverEveryTransition()
        {
            StarNightRunState run = CreateRun(StarChapterId.MoonRabbitMill, FableVerb.Resize);
            run.SetFlag("moonmill.mill.repaired");
            run.ConsequenceResolver.ResolveMoonMill();
            run.SetFlag("CH2_OLD_BRIDGE_CUT");
            run.ConsequenceResolver.ResolveMagpieBridge();
            run.SetFlag("CH3_GURU_RELEASED");
            run.ConsequenceResolver.ResolveCloudWhaleRanch();
            run.SetFlag("CH4_LETTER_STATE_OPENED");
            run.ConsequenceResolver.ResolveStarPostOffice();
            run.SetFlag("CH5_HAOREUM_NATURAL_WAKE");
            run.ConsequenceResolver.ResolveSleepingSunGarden();

            string[] expected =
            {
                "moonmill.support",
                "magpie.supply_shortage",
                "cloud.drought",
                "post.rani_argument",
                "garden.stable_sun"
            };
            Assert.That(expected.All(id => run.Consequences.Any(modifier => modifier.id == id)), Is.True);
        }

        private StarNightRunState CreateRun(StarChapterId chapter, FableVerb coreVerb)
        {
            runObject = new GameObject("@StarNightRun Test");
            StarNightRunState run = runObject.AddComponent<StarNightRunState>();
            run.BeginNewRun(173);
            run.BeginChapter(CreateDefinition(chapter, coreVerb));
            return run;
        }

        private (StarNightRunState run, PolarisFinaleState finale) CreatePolarisRun(bool grantGates = true)
        {
            StarNightRunState run = CreateBarePolarisRun(grantGates);
            return (run, AddAndBeginFinale(run));
        }

        private StarNightRunState CreateBarePolarisRun(bool grantGates = true)
        {
            runObject = new GameObject("@StarNightRun Polaris Test");
            StarNightRunState run = runObject.AddComponent<StarNightRunState>();
            run.BeginNewRun(6174);
            if (grantGates)
            {
                StarChapterId[] gates =
                {
                    StarChapterId.MoonRabbitMill,
                    StarChapterId.MagpieBridge,
                    StarChapterId.CloudWhaleRanch,
                    StarChapterId.StarPostOffice,
                    StarChapterId.SleepingSunGarden
                };
                foreach (StarChapterId gate in gates)
                {
                    run.RouteMap.RegisterGateRestored(gate);
                }
            }
            run.BeginChapter(PolarisChapterBootstrap.CreateDefinition());
            run.UnlockTool(FableVerb.Resize);
            run.UnlockTool(FableVerb.Link);
            run.UnlockTool(FableVerb.Float);
            run.UnlockTool(FableVerb.Deliver);
            run.UnlockTool(FableVerb.Awaken);
            return run;
        }

        private static PolarisFinaleState AddAndBeginFinale(StarNightRunState run)
        {
            PolarisFinaleState finale = run.gameObject.AddComponent<PolarisFinaleState>();
            finale.Begin(run);
            return finale;
        }

        private (StarNightRunState run, PolarisFinaleState finale) CreateReadyPolarisPursuit()
        {
            (StarNightRunState run, PolarisFinaleState finale) = CreatePolarisRun();
            RegisterAllPolarisRecords(finale);
            Assert.That(finale.InspectObservatory(), Is.True);
            return (run, finale);
        }

        private (StarNightRunState run, PolarisFinaleState finale) CreateReadyPolarisFinalChoice()
        {
            (StarNightRunState run, PolarisFinaleState finale) = CreateReadyPolarisPursuit();
            RestoreAllPolarisTools(finale);
            return (run, finale);
        }

        private static void RegisterAllPolarisRecords(PolarisFinaleState finale)
        {
            Assert.That(finale.RegisterRecord(StarChapterId.MoonRabbitMill), Is.True);
            Assert.That(finale.RegisterRecord(StarChapterId.MagpieBridge), Is.True);
            Assert.That(finale.RegisterRecord(StarChapterId.CloudWhaleRanch), Is.True);
            Assert.That(finale.RegisterRecord(StarChapterId.StarPostOffice), Is.True);
            Assert.That(finale.RegisterRecord(StarChapterId.SleepingSunGarden), Is.True);
        }

        private static void RestoreAllPolarisTools(PolarisFinaleState finale)
        {
            Assert.That(finale.TryRestore(FableVerb.Resize), Is.True);
            Assert.That(finale.TryRestore(FableVerb.Link), Is.True);
            Assert.That(finale.TryRestore(FableVerb.Float), Is.True);
            Assert.That(finale.TryRestore(FableVerb.Deliver), Is.True);
            Assert.That(finale.TryRestore(FableVerb.Awaken), Is.True);
        }

        private static void GrantStarRoadClues(StarNightRunState run)
        {
            run.SetFlag("STARPATH_LETTER_PRESERVED");
            run.SetFlag("STARPATH_MARU_ORIGINAL_COMMAND_KNOWN");
            run.SetFlag("STARPATH_RANI_CAN_BE_DELIVERED");
            run.SetFlag("STARPATH_POLARIS_ROUTE_REGISTERED");
            run.SetFlag("CH5_FINAL_LIGHT_SUPPORT");
        }

        private StarNightRunState CreatePrologueRun()
        {
            runObject = new GameObject("@StarNightRun Prologue Test");
            StarNightRunState run = runObject.AddComponent<StarNightRunState>();
            run.BeginNewRun(173);
            run.BeginChapter(StarNightPrologueBootstrap.CreateDefinition());
            return run;
        }

        private PrologueJourneyBeat CreatePrologueBeat(PrologueBeatMode mode)
        {
            GameObject beatObject = new($"Prologue Beat {mode}");
            spawnedObjects.Add(beatObject);
            PrologueJourneyBeat beat = beatObject.AddComponent<PrologueJourneyBeat>();
            beat.Configure(mode);
            return beat;
        }

        private StarNightRunState CreateGateLoopRun()
        {
            runObject = new GameObject("@StarNightRun Gate Loop Test");
            StarNightRunState run = runObject.AddComponent<StarNightRunState>();
            run.BeginNewRun(173);
            run.BeginChapter(CreateGateLoopDefinition());
            return run;
        }

        private StarNightRunState CreateReadyGateLoopRun()
        {
            StarNightRunState run = CreateGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            run.ChapterLoop.CompleteRoute("CH1_ROUTE_MILL");
            run.ChapterLoop.CompleteRoute("CH1_ROUTE_MINE");
            run.ChapterLoop.TryContribute("CH1_ROUTE_MILL");
            run.ChapterLoop.TryContribute("CH1_ROUTE_MINE");
            return run;
        }

        private StarNightRunState CreateMagpieGateLoopRun()
        {
            runObject = new GameObject("@StarNightRun Magpie Gate Loop Test");
            StarNightRunState run = runObject.AddComponent<StarNightRunState>();
            run.BeginNewRun(2718);
            run.BeginChapter(CreateMagpieGateLoopDefinition());
            return run;
        }

        private StarNightRunState CreatePostGateLoopRun()
        {
            runObject = new GameObject("@StarNightRun Star Post Gate Loop Test");
            StarNightRunState run = runObject.AddComponent<StarNightRunState>();
            run.BeginNewRun(31415);
            run.BeginChapter(CreatePostGateLoopDefinition());
            return run;
        }

        private StarNightRunState CreateCloudGateLoopRun()
        {
            runObject = new GameObject("@StarNightRun Cloud Gate Loop Test");
            StarNightRunState run = runObject.AddComponent<StarNightRunState>();
            run.BeginNewRun(16180);
            run.BeginChapter(CreateCloudGateLoopDefinition());
            return run;
        }

        private StarNightRunState CreateSunGateLoopRun()
        {
            runObject = new GameObject("@StarNightRun Sun Garden Gate Loop Test");
            StarNightRunState run = runObject.AddComponent<StarNightRunState>();
            run.BeginNewRun(51515);
            run.BeginChapter(CreateSunGateLoopDefinition());
            return run;
        }

        private StarNightRunState CreateReadySunGateLoopRun()
        {
            StarNightRunState run = CreateSunGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            run.ChapterLoop.CompleteRoute("CH5_ROUTE_STORED_SUNLIGHT");
            run.ChapterLoop.CompleteRoute("CH5_ROUTE_GREENHOUSE_TOP");
            run.ChapterLoop.TryContribute("CH5_ROUTE_STORED_SUNLIGHT");
            run.ChapterLoop.TryContribute("CH5_ROUTE_GREENHOUSE_TOP");
            return run;
        }

        private StarNightRunState CreateReadyCloudGateLoopRun()
        {
            StarNightRunState run = CreateCloudGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            run.ChapterLoop.CompleteRoute("CH3_ROUTE_RANCH_WHEEL");
            run.ChapterLoop.CompleteRoute("CH3_ROUTE_STORM_RIDGE");
            run.ChapterLoop.TryContribute("CH3_ROUTE_RANCH_WHEEL");
            run.ChapterLoop.TryContribute("CH3_ROUTE_STORM_RIDGE");
            return run;
        }

        private StarNightRunState CreateReadyPostGateLoopRun()
        {
            StarNightRunState run = CreatePostGateLoopRun();
            run.ChapterLoop.OpenRoutes();
            run.ChapterLoop.CompleteRoute("CH4_ROUTE_REGULAR_POST");
            run.ChapterLoop.CompleteRoute("CH4_ROUTE_DEAD_LETTER");
            run.ChapterLoop.TryContribute("CH4_ROUTE_REGULAR_POST");
            run.ChapterLoop.TryContribute("CH4_ROUTE_DEAD_LETTER");
            return run;
        }

        private static StarChapterDefinition CreateGateLoopDefinition()
        {
            return new StarChapterDefinition
            {
                chapter = StarChapterId.MoonRabbitMill,
                displayName = "제1장 · 달토끼 방앗간 v0.2",
                coreVerb = FableVerb.Resize,
                useGateLoop = true,
                gateContributionRequired = 2,
                requiredDepartureItems = 3,
                gateRoutes =
                {
                    new GateRouteDefinition
                    {
                        id = "CH1_ROUTE_MILL",
                        displayName = "방앗간 수리",
                        archetype = GateRouteArchetype.Cooperation,
                        contributionId = "CH1_PATH_CAKE_MILL",
                        contributionDisplayName = "새 길떡"
                    },
                    new GateRouteDefinition
                    {
                        id = "CH1_ROUTE_MINE",
                        displayName = "달광산 탐색",
                        archetype = GateRouteArchetype.Exploration,
                        contributionId = "CH1_PATH_CAKE_MINE",
                        contributionDisplayName = "광산 길떡"
                    },
                    new GateRouteDefinition
                    {
                        id = "CH1_ROUTE_STORAGE",
                        displayName = "겨울 저장고",
                        archetype = GateRouteArchetype.Appropriation,
                        contributionId = "CH1_PATH_CAKE_STORAGE",
                        contributionDisplayName = "저장 길떡"
                    }
                }
            };
        }

        private static StarChapterDefinition CreatePostGateLoopDefinition()
        {
            return new StarChapterDefinition
            {
                chapter = StarChapterId.StarPostOffice,
                displayName = "제4장 · 별 우체국 v0.2",
                coreVerb = FableVerb.Deliver,
                useGateLoop = true,
                gateContributionRequired = 2,
                requiredDepartureItems = 2,
                gateRoutes =
                {
                    new GateRouteDefinition
                    {
                        id = "CH4_ROUTE_REGULAR_POST",
                        displayName = "정규 우편 분류",
                        archetype = GateRouteArchetype.Cooperation,
                        contributionId = "CH4_REGULAR_ADDRESS_FRAGMENT",
                        contributionDisplayName = "정규 주소 조각"
                    },
                    new GateRouteDefinition
                    {
                        id = "CH4_ROUTE_DEAD_LETTER",
                        displayName = "반송 불가 보관소",
                        archetype = GateRouteArchetype.Exploration,
                        contributionId = "CH4_DEAD_ADDRESS_FRAGMENT",
                        contributionDisplayName = "폐기 주소 조각"
                    },
                    new GateRouteDefinition
                    {
                        id = "CH4_ROUTE_SEALED_LETTER",
                        displayName = "마지막 편지의 봉인",
                        archetype = GateRouteArchetype.Appropriation,
                        contributionId = "CH4_SEALED_ADDRESS_IMPRINT",
                        contributionDisplayName = "봉인 주소 인장"
                    }
                }
            };
        }

        private static StarChapterDefinition CreateCloudGateLoopDefinition()
        {
            return new StarChapterDefinition
            {
                chapter = StarChapterId.CloudWhaleRanch,
                displayName = "제3장 · 구름고래 목장 v0.2",
                coreVerb = FableVerb.Float,
                useGateLoop = true,
                gateContributionRequired = 2,
                requiredDepartureItems = 2,
                gateRoutes =
                {
                    new GateRouteDefinition
                    {
                        id = "CH3_ROUTE_RANCH_WHEEL",
                        displayName = "목장 수차 복구",
                        archetype = GateRouteArchetype.Cooperation,
                        contributionId = "CH3_CLEAR_WIND",
                        contributionDisplayName = "맑은 바람"
                    },
                    new GateRouteDefinition
                    {
                        id = "CH3_ROUTE_STORM_RIDGE",
                        displayName = "폭풍 능선 탐사",
                        archetype = GateRouteArchetype.Exploration,
                        contributionId = "CH3_GALE_WIND",
                        contributionDisplayName = "거센 바람"
                    },
                    new GateRouteDefinition
                    {
                        id = "CH3_ROUTE_GURU_BREATH",
                        displayName = "구루 강제 기상",
                        archetype = GateRouteArchetype.Appropriation,
                        contributionId = "CH3_GURU_BREATH",
                        contributionDisplayName = "구루의 숨결"
                    }
                }
            };
        }

        private static StarChapterDefinition CreateMagpieGateLoopDefinition()
        {
            return new StarChapterDefinition
            {
                chapter = StarChapterId.MagpieBridge,
                displayName = "제2장 · 까치다리 정거장 v0.2",
                coreVerb = FableVerb.Link,
                useGateLoop = true,
                gateContributionRequired = 2,
                requiredDepartureItems = 2,
                gateRoutes =
                {
                    new GateRouteDefinition
                    {
                        id = "CH2_ROUTE_NEW_ANCHOR",
                        displayName = "까치들과 새 닻 설치",
                        archetype = GateRouteArchetype.Cooperation,
                        contributionId = "CH2_NEW_ANCHOR",
                        contributionDisplayName = "새 닻"
                    },
                    new GateRouteDefinition
                    {
                        id = "CH2_ROUTE_STORM_ANCHOR",
                        displayName = "폭풍탑 예비 닻",
                        archetype = GateRouteArchetype.Exploration,
                        contributionId = "CH2_STORM_ANCHOR",
                        contributionDisplayName = "예비 닻"
                    },
                    new GateRouteDefinition
                    {
                        id = "CH2_ROUTE_OLD_BRIDGE",
                        displayName = "옛 물류 다리 전용",
                        archetype = GateRouteArchetype.Appropriation,
                        contributionId = "CH2_OLD_ANCHOR",
                        contributionDisplayName = "낡은 닻"
                    }
                }
            };
        }

        private static StarChapterDefinition CreateSunGateLoopDefinition()
        {
            return new StarChapterDefinition
            {
                chapter = StarChapterId.SleepingSunGarden,
                displayName = "제5장 · 잠든 해님의 정원 v0.2",
                coreVerb = FableVerb.Awaken,
                useGateLoop = true,
                gateContributionRequired = 2,
                requiredDepartureItems = 2,
                gateRoutes =
                {
                    new GateRouteDefinition
                    {
                        id = "CH5_ROUTE_STORED_SUNLIGHT",
                        displayName = "저장 햇빛 모으기",
                        archetype = GateRouteArchetype.Cooperation,
                        contributionId = "CH5_EVEN_LIGHT",
                        contributionDisplayName = "고른 빛"
                    },
                    new GateRouteDefinition
                    {
                        id = "CH5_ROUTE_GREENHOUSE_TOP",
                        displayName = "온실 꼭대기 탐사",
                        archetype = GateRouteArchetype.Exploration,
                        contributionId = "CH5_HIGH_LIGHT",
                        contributionDisplayName = "높은 빛"
                    },
                    new GateRouteDefinition
                    {
                        id = "CH5_ROUTE_HAOREUM_WAKE",
                        displayName = "해오름 강제 기상",
                        archetype = GateRouteArchetype.Appropriation,
                        contributionId = "CH5_HAOREUM_LIGHT",
                        contributionDisplayName = "해오름 빛"
                    }
                }
            };
        }

        private static StarChapterDefinition CreateDefinition(StarChapterId chapter, FableVerb coreVerb)
        {
            return new StarChapterDefinition
            {
                chapter = chapter,
                displayName = chapter.ToString(),
                coreVerb = coreVerb,
                requiredDepartureItems = 3
            };
        }

        private static GameObject CreateLinkable(string id, Vector2 position)
        {
            GameObject gameObject = new(id);
            gameObject.transform.position = position;
            gameObject.AddComponent<SpriteRenderer>();
            Rigidbody2D body = gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            FableObject target = gameObject.AddComponent<FableObject>();
            target.Configure(id, id, StarItemKind.General, FableTraits.Linkable, 1f);
            return gameObject;
        }

        private static GameObject CreateWeighted(string id, float mass, float gravity,
            FableTraits extraTraits = FableTraits.None)
        {
            GameObject gameObject = new(id);
            gameObject.AddComponent<SpriteRenderer>();
            Rigidbody2D body = gameObject.AddComponent<Rigidbody2D>();
            body.mass = mass;
            body.gravityScale = gravity;
            FableObject target = gameObject.AddComponent<FableObject>();
            target.Configure(id, id, StarItemKind.General,
                FableTraits.Floatable | FableTraits.Linkable | extraTraits, 1f);
            gameObject.AddComponent<CloudWeightState>();
            return gameObject;
        }

        private static GameObject CreatePostalParcel(string id, Vector2 position,
            FableTraits extraTraits = FableTraits.None)
        {
            GameObject gameObject = new(id);
            gameObject.transform.position = position;
            gameObject.AddComponent<SpriteRenderer>();
            Rigidbody2D body = gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            FableObject parcel = gameObject.AddComponent<FableObject>();
            parcel.Configure(id, id, StarItemKind.General,
                FableTraits.Deliverable | FableTraits.PostalParcel | extraTraits, 1f);
            return gameObject;
        }

        private static GameObject CreateAddress(string id, Vector2 position)
        {
            GameObject gameObject = new($"Address-{id}");
            gameObject.transform.position = position;
            gameObject.AddComponent<SpriteRenderer>();
            gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
            FableObject target = gameObject.AddComponent<FableObject>();
            target.Configure($"address-{id}", id, StarItemKind.General, FableTraits.PostalAddress, 0f);
            gameObject.AddComponent<StarPostalAddress>().Configure(id, id);
            return gameObject;
        }

        private static GameObject CreateSunGrowthTarget(string id, SunGrowthKind kind,
            int bloomAt = 2, int burnAt = 5)
        {
            GameObject gameObject = new(id);
            gameObject.AddComponent<SpriteRenderer>();
            gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            FableTraits traits = FableTraits.LightReactive | FableTraits.Living |
                                 FableTraits.GrowthNode;
            traits |= kind == SunGrowthKind.StarPathTree
                ? FableTraits.StarPathTree
                : FableTraits.GardenPlant;
            FableObject fable = gameObject.AddComponent<FableObject>();
            fable.Configure(id, id, StarItemKind.General, traits, 1f);
            SunGrowthState growth = gameObject.AddComponent<SunGrowthState>();
            growth.Configure(id, id, kind, bloomAt, burnAt);
            return gameObject;
        }
    }
}
