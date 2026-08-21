#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using StarNight.Stage.Rooms;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace StarNight.MapAuthoring.Editor
{
    public static class StageSeedBatchValidator
    {
        public const int DefaultSeedCount = 500;
        public const int FixedRegressionSeedCount = 10;
        public const int RandomSeedCount = 490;
        public const int FailureDetailLimit = 20;
        public const string ReportFolder = "Assets/_Game/Editor/MapAuthoring/Reports/StageSeedValidation";
        private static readonly int[] FixedRegressionSeeds =
        {
            0,
            1,
            -1,
            10801,
            10802,
            314159,
            271828,
            8675309,
            int.MaxValue,
            int.MinValue + 1,
        };

        [MenuItem("Tools/Star Night/Map E11/Validate 500 Seeds", priority = 113)]
        public static void ValidateSampleProfileMenu()
        {
            StageMapProfile profile = StageMapProfileSampleFactory.EnsureSample();
            IReadOnlyList<RoomTemplate> templates = RoomTemplateSampleFactory.EnsureSamples();
            StageSeedValidationReport report = RunApproval(profile, templates, 10801, true);
            Selection.activeObject = report;
            Debug.Log($"[GCORE-09] {report.CreateSummary()}");
        }

        public static StageSeedValidationReport RunApproval(
            StageMapProfile profile,
            IReadOnlyList<RoomTemplate> templates,
            int randomSeed,
            bool showProgress = false)
        {
            return RunSeedSet(profile, templates, CreateApprovalSeedSet(randomSeed), randomSeed, showProgress);
        }

        public static IReadOnlyList<int> CreateApprovalSeedSet(int randomSeed)
        {
            var result = new List<int>(DefaultSeedCount);
            var used = new HashSet<int>();
            for (int index = 0; index < FixedRegressionSeeds.Length; index++)
            {
                if (used.Add(FixedRegressionSeeds[index])) result.Add(FixedRegressionSeeds[index]);
            }

            uint state = randomSeed == 0 ? 0x9E3779B9u : unchecked((uint)randomSeed);
            while (result.Count < DefaultSeedCount)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                int candidate = unchecked((int)state);
                if (used.Add(candidate)) result.Add(candidate);
            }
            return result;
        }

        public static StageSeedValidationReport Run(
            StageMapProfile profile,
            IReadOnlyList<RoomTemplate> templates,
            int startSeed,
            int seedCount = DefaultSeedCount,
            bool captureFailureScreenshots = true,
            bool showProgress = false)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (templates == null || templates.Count == 0) throw new ArgumentException("At least one RoomTemplate is required.", nameof(templates));
            if (seedCount <= 0) throw new ArgumentOutOfRangeException(nameof(seedCount));

            var seeds = new List<int>(seedCount);
            for (int index = 0; index < seedCount; index++) seeds.Add(startSeed + index);
            return RunSeedSet(profile, templates, seeds, startSeed, showProgress);
        }

        private static StageSeedValidationReport RunSeedSet(
            StageMapProfile profile,
            IReadOnlyList<RoomTemplate> templates,
            IReadOnlyList<int> seeds,
            int randomSeed,
            bool showProgress)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (templates == null || templates.Count == 0) throw new ArgumentException("At least one RoomTemplate is required.", nameof(templates));
            if (seeds == null || seeds.Count <= 0) throw new ArgumentOutOfRangeException(nameof(seeds));

            int seedCount = seeds.Count;
            bool approvalSet = seedCount == DefaultSeedCount;
            EnsureFolder(ReportFolder);
            string reportName = approvalSet
                ? $"{StageLayoutSnapshotBaker.SanitizeFileName(profile.StageId)}_GCORE09_{randomSeed}_{seedCount}"
                : $"{StageLayoutSnapshotBaker.SanitizeFileName(profile.StageId)}_{randomSeed}_{seedCount}";
            string assetPath = $"{ReportFolder}/{reportName}.asset";
            StageSeedValidationReport report = AssetDatabase.LoadAssetAtPath<StageSeedValidationReport>(assetPath);
            if (report == null)
            {
                report = ScriptableObject.CreateInstance<StageSeedValidationReport>();
                AssetDatabase.CreateAsset(report, assetPath);
            }
            ResetReport(report, profile.StageId, randomSeed, seedCount, approvalSet);

            var familyCounts = new Dictionary<LayoutFamily, int>();
            var validationHashes = new HashSet<string>(StringComparer.Ordinal);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                for (int index = 0; index < seedCount; index++)
                {
                    if (showProgress && index % 25 == 0)
                        EditorUtility.DisplayProgressBar("GCORE-09 · Seed Approval", $"Seed {seeds[index]} ({index}/{seedCount})", index / (float)seedCount);

                    int seed = seeds[index];
                    StageGeneratedLayout layout = StageMapGenerator.Generate(profile, templates, seed);
                    RoomInteriorLayout interior = RoomInteriorGenerator.GenerateCommonTestRoom(seed);
                    report.TotalRooms += layout.Rooms.Count;
                    report.TotalConnections += layout.Connections.Count;
                    validationHashes.Add(layout.ValidationHash ?? string.Empty);
                    familyCounts.TryGetValue(layout.Family, out int familyCount);
                    familyCounts[layout.Family] = familyCount + 1;

                    List<string> stack = ValidateLayout(profile, layout);
                    for (int errorIndex = 0; errorIndex < interior.ValidationErrors.Count; errorIndex++)
                    {
                        string interiorError = interior.ValidationErrors[errorIndex];
                        Add(stack, "INTERIOR_" + ExtractFailureCode(interiorError), interiorError);
                    }
                    if (stack.Count == 0)
                    {
                        report.PassedSeedCount++;
                        continue;
                    }

                    report.FailedSeedCount++;
                    CountFailureCategories(report, stack);
                    if (report.Failures.Count < FailureDetailLimit)
                        report.Failures.Add(CreateFailureReport(layout, interior, stack));
                }
            }
            finally
            {
                if (showProgress) EditorUtility.ClearProgressBar();
            }

            stopwatch.Stop();
            report.UniqueValidationHashCount = validationHashes.Count;
            report.DurationMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            report.GeneratedAtUtc = DateTime.UtcNow.ToString("O");
            report.FamilyCounts = familyCounts
                .OrderBy(pair => pair.Key)
                .Select(pair => new StageSeedFamilyCount { Family = pair.Key, Count = pair.Value })
                .ToList();
            WriteReports(report, reportName);
            EditorUtility.SetDirty(report);
            AssetDatabase.SaveAssets();
            return report;
        }

        public static string GetReportAssetPath(string stageId, int startSeed, int seedCount = DefaultSeedCount)
        {
            string suffix = seedCount == DefaultSeedCount
                ? $"GCORE09_{startSeed}_{seedCount}"
                : $"{startSeed}_{seedCount}";
            return $"{ReportFolder}/{StageLayoutSnapshotBaker.SanitizeFileName(stageId)}_{suffix}.asset";
        }

        private static List<string> ValidateLayout(StageMapProfile profile, StageGeneratedLayout layout)
        {
            var failures = new List<string>();
            if (layout == null)
            {
                Add(failures, "GENERATOR", "Generator returned no layout.");
                return failures;
            }
            if (layout.ErrorCount > 0) Add(failures, "GENERATOR", $"Generator reported {layout.ErrorCount} errors.");
            if (layout.Rooms.Count < profile.MinRooms || layout.Rooms.Count > profile.MaxRooms)
                Add(failures, "GENERATOR", $"Room count {layout.Rooms.Count} is outside {profile.MinRooms}..{profile.MaxRooms}.");

            var rooms = new Dictionary<string, StageGeneratedRoom>(StringComparer.Ordinal);
            for (int index = 0; index < layout.Rooms.Count; index++)
            {
                StageGeneratedRoom room = layout.Rooms[index];
                if (room == null || room.Template == null)
                {
                    Add(failures, "OUTER_ESCAPE", $"Room {index} has no template.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(room.NodeGuid) || rooms.ContainsKey(room.NodeGuid))
                    Add(failures, "GENERATOR", $"Room {index} has a missing or duplicate GUID.");
                else
                    rooms.Add(room.NodeGuid, room);
                ValidateRoomGeometry(room, failures);
            }

            for (int first = 0; first < layout.Rooms.Count; first++)
            {
                StageGeneratedRoom a = layout.Rooms[first];
                if (a?.Template == null) continue;
                for (int second = first + 1; second < layout.Rooms.Count; second++)
                {
                    StageGeneratedRoom b = layout.Rooms[second];
                    if (b?.Template != null && StageLayoutGraphUtility.RoomsOverlap(a.PositionCells, a.Template.SizeCells, b.PositionCells, b.Template.SizeCells))
                        Add(failures, "OUTER_ESCAPE", $"Room rectangles overlap: {a.NodeGuid} / {b.NodeGuid}.");
                }
            }

            for (int index = 0; index < layout.Connections.Count; index++)
                ValidateConnection(layout.Connections[index], rooms, failures);

            ValidateRequiredRoles(profile, layout, failures);
            ValidateRoutes(layout, rooms, failures);
            ValidateGuaranteedEvents(profile, layout, failures);
            return failures.Distinct(StringComparer.Ordinal).ToList();
        }

        private static void ValidateRoomGeometry(StageGeneratedRoom room, ICollection<string> failures)
        {
            RoomTemplate template = room.Template;
            if (template.SizeCells.x <= 0 || template.SizeCells.y <= 0)
                Add(failures, "OUTER_ESCAPE", $"{room.NodeGuid} has invalid bounds {template.SizeCells}.");
            if (room.PositionCells != StageLayoutGraphUtility.SnapToPlacementGrid(room.PositionCells))
                Add(failures, "OUTER_ESCAPE", $"{room.NodeGuid} is outside the 2-cell placement grid.");
            if (template.GeometryHash == null || string.IsNullOrWhiteSpace(template.GeometryHash.Value))
                Add(failures, "OUTER_ESCAPE", $"{template.RoomId} has no geometry contract hash.");
            if (template.Sockets == null || template.Sockets.Count == 0)
            {
                Add(failures, "PORTAL_GAP", $"{template.RoomId} has no portal sockets.");
                return;
            }

            var socketIds = new HashSet<string>(StringComparer.Ordinal);
            var sides = new HashSet<CardinalDirection>();
            for (int index = 0; index < template.Sockets.Count; index++)
            {
                RoomSocketDefinition socket = template.Sockets[index];
                if (socket == null || string.IsNullOrWhiteSpace(socket.SocketGuid) || !socketIds.Add(socket.SocketGuid))
                {
                    Add(failures, "PORTAL_GAP", $"{template.RoomId} has a missing or duplicate socket GUID.");
                    continue;
                }
                sides.Add(socket.Side);
                if (!StageLayoutGraphUtility.IsSocketOnBoundary(socket, template.SizeCells))
                    Add(failures, "OUTER_ESCAPE", $"{template.RoomId}/{socket.SocketGuid} is not on its declared boundary.");
                if (socket.OpeningSizeCells != Vector2Int.one)
                    Add(failures, "PORTAL_GAP", $"{template.RoomId}/{socket.SocketGuid} must use an exact 1x1 opening.");
                if (socket.FloorHeightCell < 0 || socket.FloorHeightCell >= template.SizeCells.y)
                    Add(failures, "FLOOR_GAP", $"{template.RoomId}/{socket.SocketGuid} floor height is outside room bounds.");
            }
            if (sides.Count < 4)
                Add(failures, "OUTER_ESCAPE", $"{template.RoomId} does not declare all four sealed boundary sides.");

            for (int index = 0; index < room.ElementSlots.Count; index++)
            {
                GeneratedElementSlot slot = room.ElementSlots[index];
                if (slot.LocalCell.x <= 0 || slot.LocalCell.x >= template.SizeCells.x ||
                    slot.LocalCell.y <= 0 || slot.LocalCell.y >= template.SizeCells.y)
                    Add(failures, "ELEMENT_SLOT", $"{slot.SlotGuid} leaves the safe interior of {room.NodeGuid}.");
            }
        }

        private static void ValidateConnection(
            StageGeneratedConnection connection,
            IReadOnlyDictionary<string, StageGeneratedRoom> rooms,
            ICollection<string> failures)
        {
            if (connection == null || !rooms.TryGetValue(connection.SourceNodeGuid ?? string.Empty, out StageGeneratedRoom sourceRoom) ||
                !rooms.TryGetValue(connection.TargetNodeGuid ?? string.Empty, out StageGeneratedRoom targetRoom))
            {
                Add(failures, "PORTAL_GAP", "Connection references a missing room.");
                return;
            }
            RoomSocketDefinition source = FindSocket(sourceRoom.Template, connection.SourceSocketGuid);
            RoomSocketDefinition target = FindSocket(targetRoom.Template, connection.TargetSocketGuid);
            SocketCompatibility compatibility = StageLayoutGraphUtility.GetCompatibility(source, target, sourceRoom == targetRoom);
            if (compatibility != SocketCompatibility.Compatible)
            {
                string category = compatibility == SocketCompatibility.FloorHeightMismatch ? "FLOOR_GAP" : "PORTAL_GAP";
                Add(failures, category, $"{connection.ConnectionGuid} socket compatibility is {compatibility}.");
                return;
            }
            if (connection.RouteKind == GeneratedRouteKind.MainRoute && (!source.MainRouteAllowed || !target.MainRouteAllowed))
                Add(failures, "PORTAL_GAP", $"{connection.ConnectionGuid} main route uses a blocked socket.");
            if (!connection.Bidirectional)
                Add(failures, "PORTAL_GAP", $"{connection.ConnectionGuid} must be bidirectional by default.");
            if (connection.RequiresCorridor)
                Add(failures, "PORTAL_GAP", $"{connection.ConnectionGuid} cannot create a physical corridor between rooms.");
        }

        private static void ValidateRequiredRoles(StageMapProfile profile, StageGeneratedLayout layout, ICollection<string> failures)
        {
            if (profile.RequiredRoles == null) return;
            for (int index = 0; index < profile.RequiredRoles.Count; index++)
            {
                RoomRoleRequirement required = profile.RequiredRoles[index];
                if (required == null) continue;
                int count = layout.Rooms.Count(room => room != null && room.Role == required.Role);
                if (count < required.MinCount || count > required.MaxCount)
                    Add(failures, "GENERATOR", $"Role {required.Role} count {count} is outside {required.MinCount}..{required.MaxCount}.");
            }
        }

        private static void ValidateRoutes(
            StageGeneratedLayout layout,
            IReadOnlyDictionary<string, StageGeneratedRoom> rooms,
            ICollection<string> failures)
        {
            StageGeneratedRoom start = layout.Rooms.FirstOrDefault(room => room != null && room.Role == RoomRole.Start);
            StageGeneratedRoom exit = layout.Rooms.FirstOrDefault(room => room != null && room.Role == RoomRole.Exit);
            if (start == null || exit == null)
            {
                Add(failures, "MAIN_ROUTE", "Start or Exit room is missing.");
                return;
            }

            HashSet<string> mainReachable = Traverse(layout, start.NodeGuid, true);
            if (!mainReachable.Contains(exit.NodeGuid) || !layout.HasValidMainRoute)
                Add(failures, "MAIN_ROUTE", "Start cannot reach Exit through basic Main Route movement.");
            foreach (StageGeneratedRoom room in rooms.Values.Where(room => room.MainRoute))
                if (!mainReachable.Contains(room.NodeGuid))
                    Add(failures, "MARU_ROUTE", $"Maru cannot reach Main Route room {room.NodeGuid}.");

            HashSet<string> allReachable = Traverse(layout, start.NodeGuid, false);
            foreach (StageGeneratedRoom room in rooms.Values)
                if (!allReachable.Contains(room.NodeGuid))
                    Add(failures, "MAIN_ROUTE", $"Room {room.NodeGuid} is disconnected from Start.");
        }

        private static void ValidateGuaranteedEvents(StageMapProfile profile, StageGeneratedLayout layout, ICollection<string> failures)
        {
            if (profile.GuaranteedEvents == null) return;
            for (int index = 0; index < profile.GuaranteedEvents.Count; index++)
            {
                GuaranteedEventRule rule = profile.GuaranteedEvents[index];
                if (rule == null || rule.MinimumCount <= 0) continue;
                int count = layout.Rooms
                    .Where(room => room != null && room.Role == rule.TargetRole)
                    .SelectMany(room => room.ElementSlots)
                    .Count(slot => string.Equals(slot.ContentId, rule.EventId, StringComparison.Ordinal));
                if (count < rule.MinimumCount)
                    Add(failures, "ELEMENT_SLOT", $"Guaranteed event {rule.EventId} count {count} is below {rule.MinimumCount}.");
            }
        }

        private static HashSet<string> Traverse(StageGeneratedLayout layout, string start, bool mainOnly)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(start)) return visited;
            var queue = new Queue<string>();
            visited.Add(start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                for (int index = 0; index < layout.Connections.Count; index++)
                {
                    StageGeneratedConnection edge = layout.Connections[index];
                    if (edge == null || mainOnly && edge.RouteKind != GeneratedRouteKind.MainRoute) continue;
                    string next = string.Equals(edge.SourceNodeGuid, current, StringComparison.Ordinal) ? edge.TargetNodeGuid :
                        string.Equals(edge.TargetNodeGuid, current, StringComparison.Ordinal) ? edge.SourceNodeGuid : null;
                    if (!string.IsNullOrWhiteSpace(next) && visited.Add(next)) queue.Enqueue(next);
                }
            }
            return visited;
        }

        private static StageSeedFailureReport CreateFailureReport(
            StageGeneratedLayout layout,
            RoomInteriorLayout interior,
            IReadOnlyList<string> stack)
        {
            string first = stack.Count > 0 ? stack[0] : "[UNKNOWN] Validation failed.";
            string failureCode = ExtractFailureCode(first);
            bool interiorFailure = failureCode.StartsWith("INTERIOR_", StringComparison.Ordinal);
            StageGeneratedRoom failedRoom = layout.Rooms.FirstOrDefault(room =>
                room != null && !string.IsNullOrWhiteSpace(room.NodeGuid) && first.Contains(room.NodeGuid));
            failedRoom ??= layout.Rooms.FirstOrDefault(room => room != null && room.Role == RoomRole.Start);
            return new StageSeedFailureReport
            {
                Seed = layout.Seed,
                RoomNodeStableId = interiorFailure ? interior.RoomId : failedRoom?.NodeGuid ?? string.Empty,
                FailureCode = failureCode,
                FirstFailedCell = interiorFailure ? interior.EntryWorldCell : failedRoom?.PositionCells ?? Vector2Int.zero,
                InventoryState = "NotInstantiated",
                RoomStreamingState = "GraphOnly",
            };
        }

        private static string CaptureFailureScreenshot(StageGeneratedLayout layout, string reportName)
        {
            StageLayoutPreviewApplier.Apply(layout, false);
            StageLayoutSimulationController controller = UnityEngine.Object.FindFirstObjectByType<StageLayoutSimulationController>(FindObjectsInactive.Include);
            controller?.BeginSimulation(false);
            Camera camera = controller != null ? controller.PreviewCamera : Camera.main;
            if (camera == null) return string.Empty;

            const int width = 960;
            const int height = 540;
            string folder = $"{ReportFolder}/{reportName}_Failures";
            EnsureFolder(folder);
            string assetPath = $"{folder}/Seed_{layout.Seed}.png";
            string absolutePath = AssetPathUtility.ToAbsolutePath(assetPath);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var renderTexture = new RenderTexture(width, height, 24);
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return assetPath;
        }

        private static void CountFailureCategories(StageSeedValidationReport report, IReadOnlyCollection<string> stack)
        {
            if (stack.Any(item => item.StartsWith("[OUTER_ESCAPE]", StringComparison.Ordinal))) report.OuterEscapeFailureCount++;
            if (stack.Any(item => item.StartsWith("[FLOOR_GAP]", StringComparison.Ordinal))) report.FloorGapFailureCount++;
            if (stack.Any(item => item.StartsWith("[PORTAL_GAP]", StringComparison.Ordinal))) report.PortalGapFailureCount++;
            if (stack.Any(item => item.StartsWith("[MAIN_ROUTE]", StringComparison.Ordinal))) report.MainRouteFailureCount++;
            if (stack.Any(item => item.StartsWith("[MARU_ROUTE]", StringComparison.Ordinal))) report.MaruRouteFailureCount++;
            if (stack.Any(item => item.StartsWith("[GENERATOR]", StringComparison.Ordinal)
                                  || item.StartsWith("[ELEMENT_SLOT]", StringComparison.Ordinal)
                                  || item.StartsWith("[INTERIOR_", StringComparison.Ordinal))) report.OtherFailureCount++;
        }

        private static void WriteReports(StageSeedValidationReport report, string reportName)
        {
            report.JsonReportPath = $"{ReportFolder}/{reportName}.json";
            report.CsvReportPath = $"{ReportFolder}/{reportName}.csv";
            File.WriteAllText(AssetPathUtility.ToAbsolutePath(report.JsonReportPath), JsonUtility.ToJson(report, true), new UTF8Encoding(true));

            var csv = new StringBuilder();
            csv.AppendLine("Seed,RoomNodeStableId,FailureCode,FirstFailedCell,InventoryState,RoomStreamingState");
            for (int index = 0; index < report.Failures.Count; index++)
            {
                StageSeedFailureReport failure = report.Failures[index];
                csv.Append(failure.Seed).Append(',')
                    .Append(Escape(failure.RoomNodeStableId)).Append(',')
                    .Append(Escape(failure.FailureCode)).Append(',')
                    .Append(Escape(failure.FirstFailedCell.ToString())).Append(',')
                    .Append(Escape(failure.InventoryState)).Append(',')
                    .Append(Escape(failure.RoomStreamingState)).AppendLine();
            }
            File.WriteAllText(AssetPathUtility.ToAbsolutePath(report.CsvReportPath), csv.ToString(), new UTF8Encoding(true));
            AssetDatabase.ImportAsset(report.JsonReportPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(report.CsvReportPath, ImportAssetOptions.ForceUpdate);
        }

        private static void ResetReport(
            StageSeedValidationReport report,
            string stageId,
            int startSeed,
            int seedCount,
            bool approvalSet)
        {
            report.StageId = stageId;
            report.StartSeed = startSeed;
            report.SeedCount = seedCount;
            report.FixedRegressionSeedCount = approvalSet ? FixedRegressionSeedCount : 0;
            report.RandomSeedCount = approvalSet ? RandomSeedCount : seedCount;
            report.PassedSeedCount = 0;
            report.FailedSeedCount = 0;
            report.TotalRooms = 0;
            report.TotalConnections = 0;
            report.OuterEscapeFailureCount = 0;
            report.FloorGapFailureCount = 0;
            report.PortalGapFailureCount = 0;
            report.MainRouteFailureCount = 0;
            report.MaruRouteFailureCount = 0;
            report.OtherFailureCount = 0;
            report.UniqueValidationHashCount = 0;
            report.DurationMilliseconds = 0d;
            report.GeneratedAtUtc = string.Empty;
            report.JsonReportPath = string.Empty;
            report.CsvReportPath = string.Empty;
            report.FamilyCounts.Clear();
            report.Failures.Clear();
        }

        private static RoomSocketDefinition FindSocket(RoomTemplate template, string guid)
        {
            if (template?.Sockets == null) return null;
            return template.Sockets.FirstOrDefault(socket => socket != null && string.Equals(socket.SocketGuid, guid, StringComparison.Ordinal));
        }

        private static void Add(ICollection<string> failures, string category, string message)
        {
            failures.Add($"[{category}] {message}");
        }

        private static string ExtractFailureCode(string failure)
        {
            if (string.IsNullOrWhiteSpace(failure) || failure[0] != '[') return "UNKNOWN";
            int end = failure.IndexOf(']');
            return end > 1 ? failure.Substring(1, end - 1) : "UNKNOWN";
        }

        private static string Escape(string value)
        {
            return $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}

#endif
