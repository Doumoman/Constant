#if LEGACY_DISABLED
using System;
using StarNight.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.MapAuthoring.Editor
{
    public sealed class MapElementBakeResult
    {
        public bool Success { get; internal set; }
        public string Message { get; internal set; }
        public MapElementValidationReport Validation { get; internal set; }
        public MapElementBakePaths Paths { get; internal set; }
        public MapElementDefinition BakedDefinition { get; internal set; }
        public GameObject RuntimePrefab { get; internal set; }
        public MapElementVisualProfileAsset VisualProfile { get; internal set; }
        public string SourceHash { get; internal set; }
    }

    public static class MapElementBakePipeline
    {
        public static MapElementBakeResult Bake(
            MapElementDefinition authoringDefinition,
            GameObject sourceRoot = null)
        {
            var validation = MapElementValidator.ValidateSourceForBake(authoringDefinition, sourceRoot);
            var result = new MapElementBakeResult
            {
                Success = false,
                Validation = validation,
                Message = validation.CreateSummary(),
            };
            if (!validation.IsValid)
            {
                return result;
            }

            var paths = AssetPathUtility.GetMapElementBakePaths(authoringDefinition);
            result.Paths = paths;
            try
            {
                EnsureOutputFolders(paths);
                using (new UndoScope($"Bake {authoringDefinition.ElementId}"))
                {
                    SaveSourcePrefab(authoringDefinition, paths.SourcePrefab);
                    var sourceHash = BakeHashUtility.ComputeAssetFileHash(paths.SourcePrefab);
                    if (string.IsNullOrWhiteSpace(sourceHash))
                    {
                        throw new InvalidOperationException("Source Prefab hash를 계산하지 못했습니다.");
                    }

                    var bakedDefinition = CreateOrUpdateDefinition(authoringDefinition, paths.Definition);
                    var visualProfile = CreateOrUpdateVisualProfile(
                        authoringDefinition,
                        sourceHash,
                        paths.VisualProfile);
                    var runtimePrefab = SaveRuntimePrefab(
                        paths.SourcePrefab,
                        paths.RuntimePrefab,
                        bakedDefinition);
                    var metadata = CreateMetadata(paths, sourceHash);

                    ApplyBakeReferences(bakedDefinition, runtimePrefab, visualProfile, metadata);
                    if (authoringDefinition != bakedDefinition)
                    {
                        ApplyBakeReferences(authoringDefinition, runtimePrefab, visualProfile, metadata);
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    SaveLabSceneIfDirty();

                    // A synchronous refresh can invalidate the native objects returned while
                    // the assets were being created. Always validate and return the reloaded
                    // project assets so repeated bakes are independent of editor object lifetime.
                    bakedDefinition = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(paths.Definition);
                    runtimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths.RuntimePrefab);
                    visualProfile = AssetDatabase.LoadAssetAtPath<MapElementVisualProfileAsset>(paths.VisualProfile);

                    var postValidation = MapElementValidator.ValidateBakedDefinition(bakedDefinition);
                    result.Validation = postValidation;
                    result.BakedDefinition = bakedDefinition;
                    result.RuntimePrefab = runtimePrefab;
                    result.VisualProfile = visualProfile;
                    result.SourceHash = sourceHash;
                    result.Success = postValidation.IsValid;
                    result.Message = postValidation.IsValid
                        ? $"Bake 완료: {authoringDefinition.ElementId} · {sourceHash.Substring(0, 12)}"
                        : $"Bake 후 검증 실패: {postValidation.CreateSummary()}";
                }
            }
            catch (Exception exception)
            {
                result.Success = false;
                result.Message = $"Bake 실패: {exception.Message}";
                Debug.LogException(exception);
            }

            return result;
        }

        private static void EnsureOutputFolders(MapElementBakePaths paths)
        {
            AssetPathUtility.EnsureParentFolder(paths.SourcePrefab);
            AssetPathUtility.EnsureParentFolder(paths.RuntimePrefab);
            AssetPathUtility.EnsureParentFolder(paths.Definition);
            AssetPathUtility.EnsureParentFolder(paths.VisualProfile);
        }

        private static void SaveSourcePrefab(
            MapElementDefinition definition,
            string sourcePath)
        {
            var sourceRoot = MapElementPrefabAssembler.CreateSourceHierarchy(definition);
            try
            {
                var saved = PrefabUtility.SaveAsPrefabAsset(sourceRoot, sourcePath);
                if (saved == null)
                {
                    throw new InvalidOperationException($"Source Prefab 저장 실패: {sourcePath}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceRoot);
            }

            AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static MapElementDefinition CreateOrUpdateDefinition(
            MapElementDefinition authoring,
            string definitionPath)
        {
            var baked = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(definitionPath);
            if (baked == null)
            {
                baked = ScriptableObject.CreateInstance<MapElementDefinition>();
                baked.name = authoring.ElementId;
                Undo.RegisterCreatedObjectUndo(baked, $"Create {authoring.ElementId} Definition");
                AssetDatabase.CreateAsset(baked, definitionPath);
            }
            else if (baked != authoring)
            {
                Undo.RecordObject(baked, $"Update {authoring.ElementId} Definition");
            }

            if (baked != authoring)
            {
                EditorUtility.CopySerialized(authoring, baked);
                baked.name = authoring.ElementId;
            }

            EditorUtility.SetDirty(baked);
            return baked;
        }

        private static MapElementVisualProfileAsset CreateOrUpdateVisualProfile(
            MapElementDefinition authoring,
            string sourceHash,
            string visualPath)
        {
            var visual = AssetDatabase.LoadAssetAtPath<MapElementVisualProfileAsset>(visualPath);
            if (visual == null)
            {
                visual = ScriptableObject.CreateInstance<MapElementVisualProfileAsset>();
                visual.name = $"{authoring.ElementId}_Visual";
                Undo.RegisterCreatedObjectUndo(visual, $"Create {authoring.ElementId} Visual Profile");
                AssetDatabase.CreateAsset(visual, visualPath);
            }
            else
            {
                Undo.RecordObject(visual, $"Update {authoring.ElementId} Visual Profile");
            }

            visual.CopyFrom(authoring.ElementId, sourceHash, authoring.VisualProfile);
            EditorUtility.SetDirty(visual);
            return visual;
        }

        private static GameObject SaveRuntimePrefab(
            string sourcePath,
            string runtimePath,
            MapElementDefinition bakedDefinition)
        {
            var contents = PrefabUtility.LoadPrefabContents(sourcePath);
            try
            {
                MapElementPrefabAssembler.AddRuntimeContract(contents, bakedDefinition);
                var saved = PrefabUtility.SaveAsPrefabAsset(contents, runtimePath);
                if (saved == null)
                {
                    throw new InvalidOperationException($"Runtime Prefab 저장 실패: {runtimePath}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.ImportAsset(runtimePath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(runtimePath);
        }

        private static ElementBakeMetadata CreateMetadata(
            MapElementBakePaths paths,
            string sourceHash)
        {
            return new ElementBakeMetadata
            {
                SchemaVersion = 1,
                SourceGuid = AssetDatabase.AssetPathToGUID(paths.SourcePrefab),
                SourceHash = sourceHash,
                RuntimePrefabGuid = AssetDatabase.AssetPathToGUID(paths.RuntimePrefab),
                LastBakedUnityVersion = Application.unityVersion,
                LastBakedAtUtc = DateTime.UtcNow.ToString("O"),
            };
        }

        private static void ApplyBakeReferences(
            MapElementDefinition definition,
            GameObject runtimePrefab,
            MapElementVisualProfileAsset visualProfile,
            ElementBakeMetadata metadata)
        {
            Undo.RecordObject(definition, $"Update {definition.ElementId} Bake Metadata");
            definition.RuntimePrefab = runtimePrefab;
            definition.BakedVisualProfile = visualProfile;
            definition.BakeMetadata = new ElementBakeMetadata
            {
                SchemaVersion = metadata.SchemaVersion,
                SourceGuid = metadata.SourceGuid,
                SourceHash = metadata.SourceHash,
                RuntimePrefabGuid = metadata.RuntimePrefabGuid,
                LastBakedUnityVersion = metadata.LastBakedUnityVersion,
                LastBakedAtUtc = metadata.LastBakedAtUtc,
            };
            EditorUtility.SetDirty(definition);
        }

        private static void SaveLabSceneIfDirty()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isDirty &&
                string.Equals(
                    scene.path,
                    EditorSceneBuildGuard.MapElementLabPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}

#endif
