using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Live.Rmap09.Editor
{
    /// <summary>Builds the isolated physical evidence fixture for RMAP09 only.</summary>
    public static class CharacterLiveComposerLabSceneBuilder
    {
        public const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP09/MoonPalaceComposerLab_RMAP09.unity";

        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string TerrainTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Terrain.asset";
        private const string OverlayTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Affordance.asset";
        private const string GeneratedDirectory = "MapDesign/MCP/GENERATED/RMAP09";
        private const string DerivedAuthoringDirectory =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RMAP09";

        [MenuItem("Tools/MoonPalace/RMAP09/Build Composer Lab")]
        public static void BuildFromMenu() => Build();

        public static void Build()
        {
            RmapComposerResult result = RmapComposer.Compose(RmapComposer.CreateFixtureRequest());
            if (!result.Success)
                throw new InvalidOperationException("RMAP09 fixture composition failed: " + result.FailureSummary);
            RmapComposerComposition composition = result.Composition;
            if (composition.BaseCells.Count != RmapComposer.Width * RmapComposer.Height ||
                composition.Selections.Count != RmapComposer.SlotCount)
                throw new InvalidOperationException("RMAP09 requires a complete 12x8 / six-slot composition.");

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Tile terrainTile = LoadTile(TerrainTilePath, "terrain");
            Tile overlayTile = LoadTile(OverlayTilePath, "overlay");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MoonPalace_ComposerLab_RMAP09", typeof(Grid));
            Tilemap terrain = CreateTerrain(root.transform);
            BakeBase(terrain, terrainTile, composition);
            CreateLadderOverlay(root.transform, overlayTile, composition);
            CharacterLivePlayerRig player = CreatePlayer(scene);
            CreateCamera(root.transform, player.transform);
            CreateLabels(root.transform, composition, result);

            Physics2D.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            WriteArtifacts(composition, result);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static Tile LoadTile(string path, string kind)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) throw new InvalidOperationException("RMAP09 " + kind + " tile is missing: " + path);
            return tile;
        }

        private static Tilemap CreateTerrain(Transform parent)
        {
            var target = new GameObject("RMAP09_BaseTerrain", typeof(Tilemap), typeof(TilemapRenderer),
                typeof(TilemapCollider2D), typeof(Rigidbody2D));
            target.transform.SetParent(parent, false);
            target.GetComponent<TilemapRenderer>().sortingOrder = 1;
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            return target.GetComponent<Tilemap>();
        }

        private static void BakeBase(Tilemap target, Tile terrainTile, RmapComposerComposition composition)
        {
            var positions = new List<Vector3Int>();
            for (var y = 0; y < RmapComposer.Height; y++)
            for (var x = 0; x < RmapComposer.Width; x++)
            {
                if (composition.GetBaseCell(x, y) == RmapPatternBaseCell.Solid)
                    positions.Add(new Vector3Int(x, y, 0));
            }
            target.SetTiles(positions.ToArray(), Enumerable.Repeat<TileBase>(terrainTile, positions.Count).ToArray());
            if (positions.Count == 0 || target.GetTile(positions[0]) == null)
                throw new InvalidOperationException("RMAP09 Base Tilemap did not retain the composed physical terrain.");
            EditorUtility.SetDirty(target);
        }

        private static void CreateLadderOverlay(Transform parent, Tile overlayTile,
            RmapComposerComposition composition)
        {
            var target = new GameObject("RMAP09_LadderOverlay", typeof(Tilemap), typeof(TilemapRenderer),
                typeof(BoxCollider2D), typeof(CharacterLiveClimbSurface));
            target.transform.SetParent(parent, false);
            target.GetComponent<TilemapRenderer>().sortingOrder = 2;
            Tilemap tilemap = target.GetComponent<Tilemap>();
            Vector3Int[] positions = composition.Overlays.Select(value => new Vector3Int(
                value.Cell.X, value.Cell.Y, 0)).ToArray();
            tilemap.SetTiles(positions, Enumerable.Repeat<TileBase>(overlayTile, positions.Length).ToArray());
            if (positions.Length != 7 || positions.Any(position => tilemap.GetTile(position) == null))
                throw new InvalidOperationException("RMAP09 ladder overlay must be a separate seven-cell Tilemap layer.");

            BoxCollider2D trigger = target.GetComponent<BoxCollider2D>();
            trigger.offset = new Vector2(5.5f, 4.5f);
            trigger.size = new Vector2(0.7f, 7f);
            trigger.isTrigger = true;
            target.GetComponent<CharacterLiveClimbSurface>().Configure(
                CharacterLiveClimbSurface.SurfaceKind.Ladder, trigger);
            EditorUtility.SetDirty(tilemap);
        }

        private static CharacterLivePlayerRig CreatePlayer(Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null) throw new InvalidOperationException("RMAP09 Player prefab is missing: " + PlayerPrefabPath);
            GameObject player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            player.name = "RMAP09_Player";
            CharacterLivePlayerRig rig = player.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            if (rig == null || movement == null) throw new InvalidOperationException("RMAP09 Player rig/movement is missing.");
            rig.BodyCollider.size = new Vector2(0.4f, 0.8f);
            rig.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1 << LayerMask.NameToLayer("Default"));
            rig.Body.position = new Vector2(0.6f, 1.01f);
            movement.ResetMotion();
            return rig;
        }

        private static void CreateCamera(Transform parent, Transform target)
        {
            var cameraObject = new GameObject("RMAP09_FollowCamera", typeof(Camera),
                typeof(CharacterLiveCameraFollowDriver));
            cameraObject.transform.SetParent(parent, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.GetComponent<CharacterLiveCameraFollowDriver>().Configure(camera, target,
                new Rect(0f, 0f, RmapComposer.Width, RmapComposer.Height + 1f), 5.5f, 3.5f, 0.08f);
        }

        private static void CreateLabels(Transform parent, RmapComposerComposition composition,
            RmapComposerResult result)
        {
            var label = new GameObject("RMAP09_Controls", typeof(TextMesh));
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(0.2f, 8.7f, -1f);
            TextMesh text = label.GetComponent<TextMesh>();
            text.text = "RMAP09 | 12x8 composed Type3 L->U | A/D: route spine | W/S: ladder overlay\n" +
                "six RMAP07 selections; attempt " + result.AttemptCount.ToString(CultureInfo.InvariantCulture) +
                " accepted; base and overlay remain separate.";
            text.color = new Color(0.9f, 0.94f, 1f, 1f);
            text.characterSize = 0.20f;
            text.fontSize = 28;
            text.anchor = TextAnchor.UpperLeft;

            foreach (RmapComposerSelection selection in composition.Selections)
            {
                var slot = new GameObject("RMAP09_Slot_" + selection.SlotX + "_" + selection.SlotY,
                    typeof(TextMesh));
                slot.transform.SetParent(parent, false);
                slot.transform.position = new Vector3(selection.SlotX + 0.1f, selection.SlotY + 3.8f, -1f);
                TextMesh slotText = slot.GetComponent<TextMesh>();
                slotText.text = selection.CandidateId.Substring("RMAP07_".Length) + "\n" + selection.Transform;
                slotText.color = new Color(0.55f, 0.9f, 1f, 1f);
                slotText.characterSize = 0.12f;
                slotText.fontSize = 20;
                slotText.anchor = TextAnchor.UpperLeft;
            }
        }

        private static void WriteArtifacts(RmapComposerComposition composition, RmapComposerResult result)
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string generated = Path.Combine(root, GeneratedDirectory.Replace('/', Path.DirectorySeparatorChar));
            string authoring = Path.Combine(root, DerivedAuthoringDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(generated);
            Directory.CreateDirectory(authoring);
            WriteDerived(generated, authoring, "rmap09_attempt_trace.csv", AttemptCsv(result), 4, 3);
            WriteDerived(generated, authoring, "rmap09_composition_slots.csv", SlotCsv(result), 7, 18);
            WriteDerived(generated, authoring, "rmap09_base_cells.csv", BaseCsv(composition), 4, 96);
            WriteDerived(generated, authoring, "rmap09_overlay.csv", OverlayCsv(composition), 4, 7);
            string manifest = "{\n" +
                "  \"scene_path\": \"" + ScenePath + "\",\n" +
                "  \"target_chunk\": \"" + composition.PortChunk.ChunkId + "\",\n" +
                "  \"ports\": \"" + string.Join(";", composition.RequiredPortIds) + "\",\n" +
                "  \"profile_digest\": \"" + RmapPortCatalog.BuildFixture().ProfileDigest + "\",\n" +
                "  \"base_digest\": \"" + composition.BaseDigest + "\",\n" +
                "  \"composition_digest\": \"" + composition.CompositionDigest + "\",\n" +
                "  \"attempt_count\": " + result.AttemptCount.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"physical_path\": \"RMAP09_BaseTerrain TilemapCollider2D + RMAP09_LadderOverlay trigger + RMAP09_Player\",\n" +
                "  \"optional_route\": \"" + composition.OptionalRouteEvidence + "\",\n" +
                "  \"derived_authoring_csv\": \"" + DerivedAuthoringDirectory + "; builder-owned\"\n" +
                "}\n";
            File.WriteAllText(Path.Combine(generated, "rmap09_fixture_manifest.json"), manifest,
                new UTF8Encoding(false));
        }

        private static string AttemptCsv(RmapComposerResult result)
        {
            var text = new StringBuilder("AttemptNumber,AttemptId,Accepted,Rejections\n");
            foreach (RmapComposerAttemptTrace trace in result.AttemptTraces)
                text.Append(trace.AttemptNumber.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(Csv(trace.AttemptId)).Append(',').Append(trace.Accepted ? "true" : "false").Append(',')
                    .Append(Csv(string.Join(";", trace.Rejections))).Append('\n');
            return text.ToString();
        }

        private static string SlotCsv(RmapComposerResult result)
        {
            var text = new StringBuilder("AttemptNumber,Accepted,SlotX,SlotY,CandidateId,Transform,FinalCells16\n");
            foreach (RmapComposerAttemptTrace trace in result.AttemptTraces)
            foreach (RmapComposerSelection selection in trace.Selections)
                text.Append(trace.AttemptNumber.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(trace.Accepted ? "true" : "false").Append(',')
                    .Append(selection.SlotX.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(selection.SlotY.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(Csv(selection.CandidateId)).Append(',').Append(Csv(selection.Transform.ToString())).Append(',')
                    .Append(Csv(selection.FinalCells16)).Append('\n');
            return text.ToString();
        }

        private static string BaseCsv(RmapComposerComposition composition)
        {
            var protectedCells = new HashSet<RmapComposerCell>(composition.ProtectedSpine);
            var text = new StringBuilder("X,Y,BaseCell,Protected\n");
            for (var y = 0; y < RmapComposer.Height; y++)
            for (var x = 0; x < RmapComposer.Width; x++)
                text.Append(x.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(y.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(composition.GetBaseCell(x, y)).Append(',')
                    .Append(protectedCells.Contains(new RmapComposerCell(x, y)) ? "true" : "false").Append('\n');
            return text.ToString();
        }

        private static string OverlayCsv(RmapComposerComposition composition)
        {
            var text = new StringBuilder("X,Y,Kind,BaseCellIsAir\n");
            foreach (RmapComposerOverlayCell overlay in composition.Overlays)
                text.Append(overlay.Cell.X.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(overlay.Cell.Y.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(overlay.Kind).Append(',')
                    .Append(composition.GetBaseCell(overlay.Cell.X, overlay.Cell.Y) == RmapPatternBaseCell.Air
                        ? "true" : "false").Append('\n');
            return text.ToString();
        }

        private static void WriteDerived(string generated, string authoring, string name, string text,
            int expectedColumns, int expectedDataRows)
        {
            ValidateCsv(name, text, expectedColumns, expectedDataRows);
            File.WriteAllText(Path.Combine(generated, name), text, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(authoring, name), text, new UTF8Encoding(false));
        }

        private static string Csv(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

        private static void ValidateCsv(string name, string text, int expectedColumns, int expectedDataRows)
        {
            string[] lines = (text ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length != expectedDataRows + 1)
                throw new InvalidOperationException(name + " has an unexpected data-row count.");
            foreach (string line in lines)
                if (ParseCsv(line).Count != expectedColumns)
                    throw new InvalidOperationException(name + " has an invalid CSV column count.");
        }

        private static List<string> ParseCsv(string line)
        {
            var values = new List<string>();
            var value = new StringBuilder();
            var quoted = false;
            for (var index = 0; index < (line ?? string.Empty).Length; index++)
            {
                char character = line[index];
                if (character == '\"')
                {
                    if (quoted && index + 1 < line.Length && line[index + 1] == '\"')
                    {
                        value.Append(character);
                        index++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (character == ',' && !quoted)
                {
                    values.Add(value.ToString());
                    value.Length = 0;
                }
                else
                {
                    value.Append(character);
                }
            }
            if (quoted) throw new InvalidOperationException("Unterminated RMAP09 CSV quoted value.");
            values.Add(value.ToString());
            return values;
        }
    }
}
