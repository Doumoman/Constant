#if LEGACY_DISABLED
using System.IO;
using StarNight.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StarNight.Stage.CameraSystem;
using UnityEngine.SceneManagement;

namespace StarNight.MapAuthoring.Editor
{
    public static class MapElementLabBuilder
    {
        public static void OpenOrCreateLab()
        {
            MapElementDefinitionPresetFactory.EnsureLabSamples(out var spike, out _);
            var absolutePath = Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty,
                EditorSceneBuildGuard.MapElementLabPath);
            if (!File.Exists(absolutePath))
            {
                RebuildLabScene();
                return;
            }

            if (!string.Equals(
                    SceneManager.GetActiveScene().path,
                    EditorSceneBuildGuard.MapElementLabPath,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                EditorSceneManager.OpenScene(EditorSceneBuildGuard.MapElementLabPath, OpenSceneMode.Single);
            }

            var rig = Object.FindFirstObjectByType<MapElementLabTestRig>();
            if (rig != null && rig.ActiveDefinition == null)
            {
                rig.SetDefinition(spike);
            }

            MapElementAuthoringSession.SelectedDefinition = rig != null && rig.ActiveDefinition != null
                ? rig.ActiveDefinition
                : spike;
            MapElementWorkbenchWindow.OpenWindow();
            FrameLab();
        }

        public static void RebuildLabScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            MapElementDefinitionPresetFactory.EnsureLabSamples(out var spike, out _);
            EnsureSceneFolder();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "00_MapElementLab";

            var markerRoot = Create("__EDITOR_SCENE_MARKER");
            markerRoot.tag = "EditorOnly";
            var marker = Create("EditorOnlySceneMarker", markerRoot.transform);
            marker.AddComponent<EditorOnlySceneMarker>();

            var labRoot = Create("LabRoot");
            var grid = Create("Grid_1x1", labRoot.transform);
            grid.AddComponent<MapElementLabGridGuide>();
            Create("CellGuideRoot", labRoot.transform);
            Create("PreviewBounds_32x18", labRoot.transform);

            var elementAnchor = Create("ElementAnchor", labRoot.transform);
            var activeElement = Create("ActiveAuthoringElement", elementAnchor.transform);

            var terrainRoot = Create("TestTerrainRoot", labRoot.transform);
            CreateQuad(
                "SolidFloor",
                terrainRoot.transform,
                new Vector3(0f, -3f, 0f),
                new Vector2(15f, 0.8f),
                new Color(0.18f, 0.34f, 0.48f),
                true,
                false);
            CreateQuad(
                "OneWayFloor",
                terrainRoot.transform,
                new Vector3(5.5f, -1.35f, 0f),
                new Vector2(4f, 0.35f),
                new Color(0.25f, 0.55f, 0.65f),
                true,
                false);
            CreateQuad(
                "BreakableWall",
                terrainRoot.transform,
                new Vector3(-7.2f, -0.6f, 0f),
                new Vector2(0.6f, 4f),
                new Color(0.55f, 0.34f, 0.22f),
                true,
                false);
            CreateQuad(
                "UnbreakableBoundary",
                terrainRoot.transform,
                new Vector3(8.2f, 0f, 0f),
                new Vector2(0.5f, 6f),
                new Color(0.18f, 0.22f, 0.34f),
                true,
                false);

            var objectRoot = Create("TestObjectRoot", labRoot.transform);
            CreateTestObject("LightCrate", objectRoot.transform, new Vector3(-5.5f, -2.1f, 0f), new Vector2(0.8f, 0.8f), new Color(0.65f, 0.48f, 0.25f));
            CreateTestObject("HeavyBlock", objectRoot.transform, new Vector3(-4.2f, -2f, 0f), Vector2.one, new Color(0.42f, 0.45f, 0.52f));
            CreateTestObject("DummyEnemy", objectRoot.transform, new Vector3(4f, -2f, 0f), new Vector2(0.8f, 1.2f), new Color(0.8f, 0.28f, 0.35f));
            CreateTestObject("ProjectileDummy", objectRoot.transform, new Vector3(6.5f, 1.8f, 0f), new Vector2(0.35f, 0.35f), new Color(0.95f, 0.72f, 0.25f));

            var playerRoot = Create("TestPlayerRoot", labRoot.transform);
            var player = Create("PlayerMapTestRig", playerRoot.transform);
            player.transform.position = new Vector3(-2.5f, 0.15f, 0f);
            var playerBody = player.AddComponent<Rigidbody2D>();
            playerBody.gravityScale = 2.2f;
            playerBody.freezeRotation = true;
            playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var playerCollider = player.AddComponent<BoxCollider2D>();
            playerCollider.size = new Vector2(0.55f, 0.95f);
            var playerRig = player.AddComponent<MapElementLabPlayerRig>();
            var playerVisual = CreateQuad(
                "PlayerVisual",
                player.transform,
                Vector3.zero,
                new Vector2(0.55f, 0.95f),
                new Color(0.95f, 0.86f, 0.48f),
                false,
                false);
            playerVisual.transform.localPosition = Vector3.zero;

            var cameraRig = Create("CameraRig", labRoot.transform);
            var sceneCamera = CreateCamera("ScenePreviewCamera", cameraRig.transform, false);
            sceneCamera.transform.position = new Vector3(0f, 0f, -10f);
            var gameCamera = CreateCamera("GamePreviewCamera", cameraRig.transform, true);
            gameCamera.tag = "MainCamera";
            gameCamera.transform.position = new Vector3(0f, 0f, -10f);
            gameCamera.GetComponent<Camera>().orthographicSize = CameraTileProfile.DefaultVisibleHeightTiles * 0.5f;
            gameCamera.AddComponent<CameraCriticalFrame>().Configure(new CameraTileProfile());

            var lightingRoot = Create("LightingRoot", labRoot.transform);
            var mainLight = Create("Main Light", lightingRoot.transform);
            var light = mainLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.65f;
            mainLight.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            Create("AudioPreviewRoot", labRoot.transform);
            Create("ValidationMarkerRoot", labRoot.transform);

            var canvas = Create("LabCanvas");
            Create("StateLabel", canvas.transform);
            Create("TestInstruction", canvas.transform);
            Create("RuntimeMetricPanel", canvas.transform);

            var labRig = labRoot.AddComponent<MapElementLabTestRig>();
            labRig.Configure(spike, activeElement.transform, playerRig);
            EditorUtility.SetDirty(labRig);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EditorSceneBuildGuard.MapElementLabPath);

            MapElementAuthoringSession.SelectedDefinition = spike;
            MapElementAuthoringSession.EditMode = MapElementEditMode.Footprint;
            Selection.activeObject = spike;
            MapElementWorkbenchWindow.OpenWindow();
            FrameLab();
            Debug.Log("[MAP-E03] 00_MapElementLab 생성 완료 · 1×1 Spike / 2×1 Moving Platform 저작 준비");
        }

        public static int RefreshCommonElementGallery()
        {
            var scene = SceneManager.GetActiveScene();
            if (!string.Equals(
                    scene.path,
                    EditorSceneBuildGuard.MapElementLabPath,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                EditorSceneManager.OpenScene(EditorSceneBuildGuard.MapElementLabPath, OpenSceneMode.Single);
                scene = SceneManager.GetActiveScene();
            }

            var labRoot = GameObject.Find("LabRoot");
            if (labRoot == null)
            {
                return 0;
            }

            var previous = labRoot.transform.Find("CommonElementGallery");
            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            var gallery = Create("CommonElementGallery", labRoot.transform);
            gallery.tag = "EditorOnly";
            var definitions = CommonElementCatalogFactory.EnsureCatalog();
            var createdCount = 0;
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null || definition.RuntimePrefab == null)
                {
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(definition.RuntimePrefab, gallery.transform) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                instance.name = $"Gallery_{definition.ElementId}";
                var column = index % 6;
                var row = index / 6;
                instance.transform.localPosition = new Vector3(-7.5f + column * 3f, 6f - row * 2.6f, 0f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                var element = instance.GetComponent<MapElementInstance>();
                if (element != null)
                {
                    element.SetMapRoomState(MapRoomState.Dormant);
                }

                CreateGalleryCard(instance.transform, definition);
                createdCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EditorSceneBuildGuard.MapElementLabPath);
            Debug.Log($"[MAP-E05] Common Element Lab gallery refreshed: {createdCount}/{definitions.Count}");
            return createdCount;
        }

        public static int RefreshMaruElementGallery()
        {
            var scene = SceneManager.GetActiveScene();
            if (!string.Equals(
                    scene.path,
                    EditorSceneBuildGuard.MapElementLabPath,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                EditorSceneManager.OpenScene(EditorSceneBuildGuard.MapElementLabPath, OpenSceneMode.Single);
                scene = SceneManager.GetActiveScene();
            }

            var labRoot = GameObject.Find("LabRoot");
            if (labRoot == null)
            {
                return 0;
            }

            var previous = labRoot.transform.Find("MaruElementGallery");
            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            var gallery = Create("MaruElementGallery", labRoot.transform);
            gallery.tag = "EditorOnly";
            var definitions = MaruElementCatalogFactory.EnsureCatalog();
            var createdCount = 0;
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null || definition.RuntimePrefab == null)
                {
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(definition.RuntimePrefab, gallery.transform) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                instance.name = $"Gallery_{definition.ElementId}";
                var column = index % 2;
                var row = index / 2;
                instance.transform.localPosition = new Vector3(10.2f + column * 3.2f, 5.2f - row * 3f, 0f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                var element = instance.GetComponent<MapElementInstance>();
                if (element != null)
                {
                    element.SetMapRoomState(MapRoomState.Dormant);
                }

                CreateGalleryCard(instance.transform, definition);
                CreateMaruOutcomePreview(instance.transform, definition);
                createdCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EditorSceneBuildGuard.MapElementLabPath);
            Debug.Log($"[MAP-E06] Maru Element Lab gallery refreshed: {createdCount}/{definitions.Count}");
            return createdCount;
        }

        public static int RefreshMoonElementGallery()
        {
            var scene = SceneManager.GetActiveScene();
            if (!string.Equals(
                    scene.path,
                    EditorSceneBuildGuard.MapElementLabPath,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                EditorSceneManager.OpenScene(EditorSceneBuildGuard.MapElementLabPath, OpenSceneMode.Single);
                scene = SceneManager.GetActiveScene();
            }

            var labRoot = GameObject.Find("LabRoot");
            if (labRoot == null)
            {
                return 0;
            }

            var previous = labRoot.transform.Find("MoonElementGallery");
            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            var gallery = Create("MoonElementGallery", labRoot.transform);
            gallery.tag = "EditorOnly";
            var definitions = MoonElementCatalogFactory.EnsureCatalog();
            var createdCount = 0;
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null || definition.RuntimePrefab == null)
                {
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(definition.RuntimePrefab, gallery.transform) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                instance.name = $"Gallery_{definition.ElementId}";
                var column = index % 4;
                var row = index / 4;
                instance.transform.localPosition = new Vector3(-7.8f + column * 4.2f, -5.2f - row * 3.4f, 0f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                var element = instance.GetComponent<MapElementInstance>();
                if (element != null)
                {
                    element.SetMapRoomState(MapRoomState.Dormant);
                }

                CreateGalleryCard(instance.transform, definition);
                CreateMoonContractPreview(instance.transform, definition);
                createdCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EditorSceneBuildGuard.MapElementLabPath);
            Debug.Log($"[MAP-E07/Moon] Moon Element Lab gallery refreshed: {createdCount}/{definitions.Count}");
            return createdCount;
        }

        public static int RefreshBridgeElementGallery()
        {
            var scene = SceneManager.GetActiveScene();
            if (!string.Equals(
                    scene.path,
                    EditorSceneBuildGuard.MapElementLabPath,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                EditorSceneManager.OpenScene(EditorSceneBuildGuard.MapElementLabPath, OpenSceneMode.Single);
                scene = SceneManager.GetActiveScene();
            }

            var labRoot = GameObject.Find("LabRoot");
            if (labRoot == null)
            {
                return 0;
            }

            var previous = labRoot.transform.Find("BridgeElementGallery");
            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            var gallery = Create("BridgeElementGallery", labRoot.transform);
            gallery.tag = "EditorOnly";
            var definitions = BridgeElementCatalogFactory.EnsureCatalog();
            var createdCount = 0;
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null || definition.RuntimePrefab == null)
                {
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(definition.RuntimePrefab, gallery.transform) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                instance.name = $"Gallery_{definition.ElementId}";
                var column = index % 4;
                var row = index / 4;
                instance.transform.localPosition = new Vector3(-7.8f + column * 4.2f, -12.4f - row * 3.6f, 0f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                var element = instance.GetComponent<MapElementInstance>();
                if (element != null)
                {
                    element.SetMapRoomState(MapRoomState.Dormant);
                }

                CreateGalleryCard(instance.transform, definition);
                CreateBridgeContractPreview(instance.transform, definition);
                createdCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EditorSceneBuildGuard.MapElementLabPath);
            Debug.Log($"[MAP-E07/Bridge] Bridge Element Lab gallery refreshed: {createdCount}/{definitions.Count}");
            return createdCount;
        }

        public static int RefreshPalaceElementGallery()
        {
            var scene = SceneManager.GetActiveScene();
            if (!string.Equals(
                    scene.path,
                    EditorSceneBuildGuard.MapElementLabPath,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                EditorSceneManager.OpenScene(EditorSceneBuildGuard.MapElementLabPath, OpenSceneMode.Single);
                scene = SceneManager.GetActiveScene();
            }

            var labRoot = GameObject.Find("LabRoot");
            if (labRoot == null)
            {
                return 0;
            }

            var previous = labRoot.transform.Find("PalaceElementGallery");
            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            var gallery = Create("PalaceElementGallery", labRoot.transform);
            gallery.tag = "EditorOnly";
            var definitions = PalaceElementCatalogFactory.EnsureCatalog();
            var createdCount = 0;
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null || definition.RuntimePrefab == null)
                {
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(definition.RuntimePrefab, gallery.transform) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                instance.name = $"Gallery_{definition.ElementId}";
                var column = index % 4;
                var row = index / 4;
                instance.transform.localPosition = new Vector3(-7.8f + column * 4.2f, -19.6f - row * 3.8f, 0f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                var element = instance.GetComponent<MapElementInstance>();
                if (element != null)
                {
                    element.SetMapRoomState(MapRoomState.Dormant);
                }

                CreateGalleryCard(instance.transform, definition);
                CreatePalaceContractPreview(instance.transform, definition);
                createdCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EditorSceneBuildGuard.MapElementLabPath);
            Debug.Log($"[MAP-E07/Palace] Palace Element Lab gallery refreshed: {createdCount}/{definitions.Count}");
            return createdCount;
        }

        public static int RefreshPostElementGallery()
        {
            var scene = SceneManager.GetActiveScene();
            if (!string.Equals(
                    scene.path,
                    EditorSceneBuildGuard.MapElementLabPath,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                EditorSceneManager.OpenScene(EditorSceneBuildGuard.MapElementLabPath, OpenSceneMode.Single);
                scene = SceneManager.GetActiveScene();
            }

            var labRoot = GameObject.Find("LabRoot");
            if (labRoot == null)
            {
                return 0;
            }

            var previous = labRoot.transform.Find("PostElementGallery");
            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            var gallery = Create("PostElementGallery", labRoot.transform);
            gallery.tag = "EditorOnly";
            var definitions = PostElementCatalogFactory.EnsureCatalog();
            var createdCount = 0;
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null || definition.RuntimePrefab == null)
                {
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(definition.RuntimePrefab, gallery.transform) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                instance.name = $"Gallery_{definition.ElementId}";
                var column = index % 4;
                var row = index / 4;
                instance.transform.localPosition = new Vector3(-7.8f + column * 4.2f, -27.2f - row * 3.8f, 0f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                var element = instance.GetComponent<MapElementInstance>();
                if (element != null)
                {
                    element.SetMapRoomState(MapRoomState.Dormant);
                }

                CreateGalleryCard(instance.transform, definition);
                CreatePostContractPreview(instance.transform, definition);
                createdCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EditorSceneBuildGuard.MapElementLabPath);
            Debug.Log($"[MAP-E07/Post] Post Element Lab gallery refreshed: {createdCount}/{definitions.Count}");
            return createdCount;
        }

        public static int RefreshSunElementGallery()
        {
            var scene = SceneManager.GetActiveScene();
            if (!string.Equals(
                    scene.path,
                    EditorSceneBuildGuard.MapElementLabPath,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                EditorSceneManager.OpenScene(EditorSceneBuildGuard.MapElementLabPath, OpenSceneMode.Single);
                scene = SceneManager.GetActiveScene();
            }

            var labRoot = GameObject.Find("LabRoot");
            if (labRoot == null)
            {
                return 0;
            }

            var previous = labRoot.transform.Find("SunElementGallery");
            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            var gallery = Create("SunElementGallery", labRoot.transform);
            gallery.tag = "EditorOnly";
            var definitions = SunElementCatalogFactory.EnsureCatalog();
            var createdCount = 0;
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null || definition.RuntimePrefab == null)
                {
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(definition.RuntimePrefab, gallery.transform) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                instance.name = $"Gallery_{definition.ElementId}";
                var column = index % 4;
                var row = index / 4;
                instance.transform.localPosition = new Vector3(-7.8f + column * 4.2f, -36.8f - row * 5.2f, 0f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                var element = instance.GetComponent<MapElementInstance>();
                if (element != null)
                {
                    element.SetMapRoomState(MapRoomState.Dormant);
                }

                CreateGalleryCard(instance.transform, definition);
                CreateSunContractPreview(instance.transform, definition);
                createdCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EditorSceneBuildGuard.MapElementLabPath);
            Debug.Log($"[MAP-E07/Sun] Sun Element Lab gallery refreshed: {createdCount}/{definitions.Count}");
            return createdCount;
        }

        public static int RefreshPolarisElementGallery()
        {
            EnsureSceneFolder();
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != EditorSceneBuildGuard.MapElementLabPath)
            {
                scene = EditorSceneManager.OpenScene(
                    EditorSceneBuildGuard.MapElementLabPath, OpenSceneMode.Single);
                scene = SceneManager.GetActiveScene();
            }

            var labRoot = GameObject.Find("LabRoot");
            if (labRoot == null)
            {
                return 0;
            }

            var previous = labRoot.transform.Find("PolarisElementGallery");
            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            var gallery = Create("PolarisElementGallery", labRoot.transform);
            gallery.tag = "EditorOnly";
            var definitions = PolarisElementCatalogFactory.EnsureCatalog();
            var createdCount = 0;
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null || definition.RuntimePrefab == null)
                {
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(definition.RuntimePrefab, gallery.transform) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                instance.name = $"Gallery_{definition.ElementId}";
                var column = index % 4;
                var row = index / 4;
                instance.transform.localPosition = new Vector3(-7.8f + column * 4.2f, -47.2f - row * 5.2f, 0f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                var element = instance.GetComponent<MapElementInstance>();
                if (element != null)
                {
                    element.SetMapRoomState(MapRoomState.Dormant);
                }

                CreateGalleryCard(instance.transform, definition);
                CreatePolarisContractPreview(instance.transform, definition);
                createdCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EditorSceneBuildGuard.MapElementLabPath);
            Debug.Log($"[MAP-E07/Polaris] Polaris Element Lab gallery refreshed: {createdCount}/{definitions.Count}");
            return createdCount;
        }

        private static void CreatePolarisContractPreview(Transform parent, MapElementDefinition definition)
        {
            var profile = definition.PolarisProfile;
            if (profile == null)
            {
                return;
            }

            var footprint = definition.Footprint != null
                ? definition.Footprint.BoundsSize
                : Vector2Int.one;
            var contract = profile.Kind switch
            {
                PolarisElementKind.OrbitPlatform => $"Orbit {profile.OrbitRadiusCells.x:0}x{profile.OrbitRadiusCells.y:0} | {profile.OrbitPeriodSeconds:0.0}s | Dial",
                PolarisElementKind.ObservationBeam => $"Range {profile.BeamRangeCells:0} | Sweep {profile.SweepDegrees:0} | Mirror/Signal",
                PolarisElementKind.ReturnField => $"Field {profile.ReturnFieldSizeCells.x:0}x{profile.ReturnFieldSizeCells.y:0} | {profile.ReturnDelaySeconds:0.0}s | Entry",
                PolarisElementKind.StarWeight => $"{profile.MassTag} {profile.Mass:0} | Pressure {profile.PressureWeight} | Carry/Hook",
                PolarisElementKind.GravityDial => $"Gravity {profile.LowGravityScale:0.00}/{profile.NormalGravityScale:0.00} | Unique",
                PolarisElementKind.ConstellationBridge => $"Nodes {profile.NodeGuids.Count} | Cells {profile.BridgeCellCount} | Artifact",
                PolarisElementKind.MemoryBell => $"Rhythm {profile.RhythmPattern.Count} | Clear {profile.InteractionClearanceCells} cells",
                PolarisElementKind.ImmutableStarBlock => $"Immutable | {profile.VisualVariant} | No Tools",
                _ => string.Empty,
            };
            CreateGalleryText(
                "PolarisContractPreview",
                parent,
                contract,
                new Vector3((footprint.x - 1) * 0.5f, footprint.y + 0.18f, -0.1f),
                new Color(0.58f, 0.86f, 1f),
                0.06f);
        }

        private static void CreateSunContractPreview(Transform parent, MapElementDefinition definition)
        {
            var profile = definition.SunProfile;
            if (profile == null)
            {
                return;
            }

            var footprint = definition.Footprint != null
                ? definition.Footprint.BoundsSize
                : Vector2Int.one;
            var contract = profile.Kind switch
            {
                SunElementKind.RotatingSunbeam => $"Arc {profile.ArcDegrees:0} | {profile.RotationSpeedDegreesPerSecond:0} deg/s | Unblocked",
                SunElementKind.ShadowSeed => $"Shadow 2x2 | Life {profile.ShadowLifetimeSeconds:0}s | Water",
                SunElementKind.SunflowerPlatform => $"Width {profile.PlatformWidthCells} | Light {profile.LightSourceRef} | 90 deg",
                SunElementKind.GrowthVine => $"Length {profile.StartLengthCells}-{profile.MaxLengthCells} | Water/Signal | Boundary",
                SunElementKind.DewDrop => $"Fall {profile.FallIntervalSeconds:0.0}s | Full Refill | Cool",
                SunElementKind.OverheatPlatform => $"Safe {profile.SafeSeconds:0}s | Hot {profile.OverheatSeconds:0}s | Water",
                SunElementKind.SunsetFlower => $"Phase {profile.InitialPhase} | Light/Shadow Signal",
                SunElementKind.CrowPerch => $"Event {profile.EventId} | Letter/Ember",
                _ => string.Empty,
            };
            CreateGalleryText(
                "SunContractPreview",
                parent,
                contract,
                new Vector3((footprint.x - 1) * 0.5f, footprint.y + 0.18f, -0.1f),
                new Color(1f, 0.76f, 0.26f),
                0.06f);
        }

        private static void CreatePostContractPreview(Transform parent, MapElementDefinition definition)
        {
            var profile = definition.PostProfile;
            if (profile == null)
            {
                return;
            }

            var footprint = definition.Footprint != null
                ? definition.Footprint.BoundsSize
                : Vector2Int.one;
            var contract = profile.Kind switch
            {
                PostElementKind.Conveyor => $"{profile.LengthCells}x1 | {profile.SurfaceSpeedCellsPerSecond:0.0} cell/s | Heavy Stop",
                PostElementKind.ParcelLauncher => $"Arc {profile.LaunchArc:0.00} | Power {profile.LaunchPower:0} | Parcel Only",
                PostElementKind.ReturnStamp => $"Warn {profile.WarningDelaySeconds:0.0}s | Hook/Pound | Escape {profile.EscapeSpaceBelowCells}",
                PostElementKind.SortingArm => $"Step {profile.RotationStepDegrees} | Push {profile.PushForceCellsPerSecond:0}",
                PostElementKind.MailTube => $"Pair {profile.PairGuid} | Parcel Context",
                PostElementKind.InkPool => $"Slow {profile.SlowRate:P0} | Water Dilute | Footprints",
                PostElementKind.ParcelStack => $"Boxes {profile.BoxCount} | Pound/Bomb",
                PostElementKind.ExpressTube => $"Pair {profile.PairGuid} | OneWay | Story/Parcel",
                _ => string.Empty,
            };
            CreateGalleryText(
                "PostContractPreview",
                parent,
                contract,
                new Vector3((footprint.x - 1) * 0.5f, footprint.y + 0.18f, -0.1f),
                new Color(1f, 0.78f, 0.3f),
                0.06f);
        }

        private static void CreatePalaceContractPreview(Transform parent, MapElementDefinition definition)
        {
            var profile = definition.PalaceProfile;
            if (profile == null)
            {
                return;
            }

            var footprint = definition.Footprint != null
                ? definition.Footprint.BoundsSize
                : Vector2Int.one;
            var contract = profile.Kind switch
            {
                PalaceElementKind.SluiceGate => $"Gate {profile.WidthCells}x{profile.HeightCells} | Hook | No Lock",
                PalaceElementKind.BubbleCannon => $"Bubble {profile.IntervalSeconds:0.0}s | Umbrella x{profile.UmbrellaPushMultiplier:0.0}",
                PalaceElementKind.CurrentVolume => $"Current {profile.ForceCellsPerSecond:0} | Exit Safe {profile.ExitSafePocketCells}",
                PalaceElementKind.TurtlePlatform => $"Sink {profile.SinkDepthCells:0} cell | Weight {profile.WeightThreshold}",
                PalaceElementKind.ClamBounce => $"Cycle {profile.CycleSeconds:0.0}s | Launch {profile.LaunchHeightCells:0}",
                PalaceElementKind.WaterMirrorWall => $"Normal {profile.NormalDirection} | Yeouiju/Signal",
                PalaceElementKind.DrainGrate => $"Drain {profile.DrainRatePerSecond:0.0}/s | Shovel + Hook",
                PalaceElementKind.DragonGateWaterfall => $"Up {profile.ForceCellsPerSecond:0} | Umbrella/Cloud | Refill",
                _ => string.Empty,
            };
            CreateGalleryText(
                "PalaceContractPreview",
                parent,
                contract,
                new Vector3((footprint.x - 1) * 0.5f, footprint.y + 0.18f, -0.1f),
                new Color(0.52f, 0.9f, 1f),
                0.065f);
        }

        private static void CreateBridgeContractPreview(Transform parent, MapElementDefinition definition)
        {
            var profile = definition.BridgeProfile;
            if (profile == null)
            {
                return;
            }

            var footprint = definition.Footprint != null
                ? definition.Footprint.BoundsSize
                : Vector2Int.one;
            var contract = profile.Kind switch
            {
                BridgeElementKind.ThreadBridge => $"Length {profile.LengthCells} | Sag {profile.SagCells:0.0} | W {profile.MaxWeight}",
                BridgeElementKind.KnotPulley => $"Travel {profile.TravelCells:0} | Ratio {profile.WeightRatio:0.0}",
                BridgeElementKind.WindBanner => $"Dir {profile.Direction} | Wet x{profile.WetForceMultiplier:0.0}",
                BridgeElementKind.ThreadBlade => $"Path {profile.PathSpeedCellsPerSecond:0} cell/s | Warn {profile.WarningSeconds:0.00}s",
                BridgeElementKind.MagpiePlatform => $"Stops {profile.StopCount} | Wait {profile.WaitTimeSeconds:0.00}s",
                BridgeElementKind.FeatherUpdraft => $"Volume {profile.VolumeSizeCells.x:0}x{profile.VolumeSizeCells.y:0} | Umbrella x{profile.UmbrellaLiftMultiplier:0.0}",
                BridgeElementKind.BreakingStarPanel => $"Hits {profile.HitCount} | Dwell {profile.DwellBreakSeconds:0.0}s",
                BridgeElementKind.Nest => $"Threads {profile.RequiredPieces} | Critical",
                _ => string.Empty,
            };
            CreateGalleryText(
                "BridgeContractPreview",
                parent,
                contract,
                new Vector3((footprint.x - 1) * 0.5f, footprint.y + 0.18f, -0.1f),
                new Color(0.86f, 0.78f, 1f),
                0.065f);
        }

        private static void CreateMoonContractPreview(Transform parent, MapElementDefinition definition)
        {
            var profile = definition.MoonProfile;
            if (profile == null)
            {
                return;
            }

            var footprint = definition.Footprint != null
                ? definition.Footprint.BoundsSize
                : Vector2Int.one;
            var contract = profile.Kind switch
            {
                MoonElementKind.MoonIronBall => $"Chain {profile.ChainLengthCells} | ±{profile.SwingArcDegrees:0}° | {profile.SwingPeriodSeconds:0.0}s",
                MoonElementKind.FallingMortar => $"Shadow {profile.ShadowWarningSeconds:0.00}s | Fall {profile.FallHeightCells:0}",
                MoonElementKind.DoughPlatform => $"Compress {profile.CompressionCells:0.0} | Bounce {profile.BounceHeightCells:0}",
                MoonElementKind.CraterSlab => $"Tilt {(int)profile.TiltSide:+0;-0} | Fall {profile.FallDelaySeconds:0.0}s",
                MoonElementKind.CassiaRoot => $"Segments {profile.MinimumSegmentCount}-{profile.SegmentCount}",
                MoonElementKind.MillShaft => $"Step {profile.StepAngleDegrees:0}° | {profile.RotationSpeedDegreesPerSecond:0}°/s",
                MoonElementKind.MedicineMortar => $"Inputs {profile.InputSlots} | {profile.OutputId}",
                MoonElementKind.FlourVent => $"On {profile.CycleOnSeconds:0.0}s | Off {profile.CycleOffSeconds:0.0}s",
                _ => string.Empty,
            };
            CreateGalleryText(
                "MoonContractPreview",
                parent,
                contract,
                new Vector3((footprint.x - 1) * 0.5f, footprint.y + 0.18f, -0.1f),
                new Color(0.78f, 0.88f, 1f),
                0.065f);
        }

        private static void CreateMaruOutcomePreview(Transform parent, MapElementDefinition definition)
        {
            var profile = definition.MaruProfile;
            if (profile == null)
            {
                return;
            }

            var footprint = definition.Footprint != null
                ? definition.Footprint.BoundsSize
                : Vector2Int.one;
            CreateGalleryText(
                "RewardPreview",
                parent,
                profile.PreviewRewardText,
                new Vector3((footprint.x - 1) * 0.5f, footprint.y + 0.18f, -0.1f),
                new Color(0.35f, 1f, 0.48f),
                0.075f);
            CreateGalleryText(
                "PenaltyPreview",
                parent,
                profile.PreviewPenaltyText,
                new Vector3((footprint.x - 1) * 0.5f, footprint.y - 0.12f, -0.1f),
                new Color(1f, 0.34f, 0.30f),
                0.075f);
        }

        private static void CreateGalleryText(
            string objectName,
            Transform parent,
            string value,
            Vector3 localPosition,
            Color color,
            float characterSize)
        {
            var labelObject = new GameObject(objectName);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            var label = labelObject.AddComponent<TextMesh>();
            label.text = value;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = characterSize;
            label.fontSize = 32;
            label.color = color;
        }

        private static void CreateGalleryCard(Transform parent, MapElementDefinition definition)
        {
            var footprint = definition.Footprint != null
                ? definition.Footprint.BoundsSize
                : Vector2Int.one;
            var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
            card.name = "LabCatalogCard";
            card.transform.SetParent(parent, false);
            card.transform.localPosition = new Vector3((footprint.x - 1) * 0.5f, (footprint.y - 1) * 0.5f, 0.15f);
            card.transform.localScale = new Vector3(
                Mathf.Max(0.2f, footprint.x * 0.88f),
                Mathf.Max(0.2f, footprint.y * 0.88f),
                1f);
            var collider = card.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
            card.AddComponent<MapElementLabTint>().SetColor(definition.VisualProfile.Tint);

            var labelObject = new GameObject("ElementIdLabel");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3((footprint.x - 1) * 0.5f, -0.72f, -0.1f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = definition.ElementId
                .Replace("COMMON_", string.Empty)
                .Replace("MARU_", string.Empty)
                .Replace("MOON_", string.Empty)
                .Replace("BRIDGE_", string.Empty);
            label.anchor = TextAnchor.UpperCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.105f;
            label.fontSize = 32;
            label.color = new Color(0.86f, 0.92f, 1f, 1f);
        }

        private static GameObject Create(string objectName, Transform parent = null)
        {
            var gameObject = new GameObject(objectName);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {objectName}");
            return gameObject;
        }

        private static GameObject CreateCamera(string objectName, Transform parent, bool enabled)
        {
            var cameraObject = Create(objectName, parent);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = enabled;
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.045f, 0.085f, 1f);
            return cameraObject;
        }

        private static GameObject CreateTestObject(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector2 size,
            Color color)
        {
            var gameObject = CreateQuad(objectName, parent, position, size, color, true, false);
            var body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            return gameObject;
        }

        private static GameObject CreateQuad(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector2 size,
            Color color,
            bool addCollider,
            bool isTrigger)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            gameObject.name = objectName;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);
            var collider3D = gameObject.GetComponent<Collider>();
            if (collider3D != null)
            {
                Object.DestroyImmediate(collider3D);
            }

            var tint = gameObject.AddComponent<MapElementLabTint>();
            tint.SetColor(color);
            if (addCollider)
            {
                var collider = gameObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = isTrigger;
            }

            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {objectName}");
            return gameObject;
        }

        private static void EnsureSceneFolder()
        {
            const string sceneFolder = "Assets/_Game/Editor/MapAuthoring/Scenes";
            if (!AssetDatabase.IsValidFolder(sceneFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Editor/MapAuthoring", "Scenes");
            }
        }

        private static void FrameLab()
        {
            if (SceneView.lastActiveSceneView == null)
            {
                return;
            }

            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.lastActiveSceneView.LookAt(
                Vector3.zero,
                Quaternion.identity,
                9f,
                true,
                true);
            SceneView.lastActiveSceneView.Repaint();
        }
    }
}

#endif
