#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Debugging;
using StarNight.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace StarNight.Tests.PlayMode
{
    public sealed class P4RoomLibraryPlayModeTests
    {
        private const string ScenePath =
            "Assets/StarNight/Scenes/Labs/P4_MoonRoomPrefabLibrary.unity";

        [UnityTest]
        public IEnumerator MoonRoomPrefabLibrary_HasCompleteContractAndEveryRoomSurvivesOneFrame()
        {
            yield return LoadLibrarySceneAsync();

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(scene.IsValid(), Is.True, "The P4 library scene is invalid after loading.");
            Assert.That(scene.isLoaded, Is.True, "The P4 library scene did not finish loading.");
            Assert.That(scene.path, Is.EqualTo(ScenePath));

            P4RoomLibraryContract[] contracts =
                FindSceneComponents<P4RoomLibraryContract>(scene);
            Assert.That(
                contracts.Length,
                Is.EqualTo(1),
                "The P4 library scene must contain exactly one P4RoomLibraryContract.");

            P4RoomLibraryContract contract = contracts[0];
            RoomTemplate2D[] rooms = contract.RoomInstances;
            Assert.That(rooms, Is.Not.Null);
            Assert.That(
                rooms.Length,
                Is.EqualTo(P4RoomLibraryContract.RequiredRoomCount),
                "RoomInstances must contain exactly the 33 required P4 rooms.");

            RoomTemplate2D[] sceneRooms = FindSceneComponents<RoomTemplate2D>(scene);
            Assert.That(
                sceneRooms.Length,
                Is.EqualTo(P4RoomLibraryContract.RequiredRoomCount),
                "The loaded scene must contain exactly 33 RoomTemplate2D instances.");

            HashSet<RoomTemplate2D> uniqueRooms = new HashSet<RoomTemplate2D>();
            for (int index = 0; index < rooms.Length; index++)
            {
                RoomTemplate2D room = rooms[index];
                Assert.That(room, Is.Not.Null, $"RoomInstances[{index}] is missing.");
                Assert.That(
                    uniqueRooms.Add(room),
                    Is.True,
                    $"RoomInstances contains a duplicate reference at index {index}.");
                Assert.That(
                    room.gameObject.scene,
                    Is.EqualTo(scene),
                    $"{room.RoomId} does not belong to the loaded P4 scene.");

                AssertCanonicalLayer(room, room.Terrain, "Terrain");
                AssertCanonicalLayer(room, room.OneWay, "OneWay");
                AssertCanonicalLayer(room, room.Fixture, "Fixture");
                AssertCanonicalLayer(room, room.Hazard, "Hazard");
                AssertCanonicalLayer(room, room.Decoration, "Decoration");
                AssertCanonicalLayer(room, room.Logic, "Logic");

                HashSet<Tilemap> uniqueLayers = new HashSet<Tilemap>
                {
                    room.Terrain,
                    room.OneWay,
                    room.Fixture,
                    room.Hazard,
                    room.Decoration,
                    room.Logic
                };
                Assert.That(
                    uniqueLayers.Count,
                    Is.EqualTo(6),
                    $"{room.RoomId} must reference six distinct canonical Tilemaps.");

                Assert.That(
                    room.Sockets.Count,
                    Is.GreaterThan(0),
                    $"{room.RoomId} has no socket references.");
                HashSet<RoomSocket2D> uniqueSockets = new HashSet<RoomSocket2D>();
                for (int socketIndex = 0;
                     socketIndex < room.Sockets.Count;
                     socketIndex++)
                {
                    RoomSocket2D socket = room.Sockets[socketIndex];
                    Assert.That(
                        socket,
                        Is.Not.Null,
                        $"{room.RoomId} socket reference {socketIndex} is missing.");
                    Assert.That(
                        uniqueSockets.Add(socket),
                        Is.True,
                        $"{room.RoomId} contains duplicate socket references.");
                    Assert.That(
                        socket.transform.IsChildOf(room.transform),
                        Is.True,
                        $"{room.RoomId}/{socket.SocketId} is outside its room hierarchy.");
                }
            }

            for (int index = 0; index < sceneRooms.Length; index++)
            {
                Assert.That(
                    uniqueRooms.Contains(sceneRooms[index]),
                    Is.True,
                    $"{sceneRooms[index].RoomId} is not registered in RoomInstances.");
            }

            Camera mainCamera = Camera.main;
            Assert.That(mainCamera, Is.Not.Null, "The P4 scene requires a MainCamera.");
            Assert.That(mainCamera.gameObject.scene, Is.EqualTo(scene));

            Light[] lights = FindSceneComponents<Light>(scene);
            int directionalCount = 0;
            for (int index = 0; index < lights.Length; index++)
            {
                if (lights[index].type == LightType.Directional)
                {
                    directionalCount++;
                    Assert.That(lights[index].enabled, Is.True);
                    Assert.That(lights[index].gameObject.activeInHierarchy, Is.True);
                }
            }

            Assert.That(
                directionalCount,
                Is.EqualTo(1),
                "The P4 scene must contain exactly one active Directional Light.");
            Assert.That(
                CountMissingMonoBehaviours(scene),
                Is.EqualTo(0),
                "The P4 scene contains missing MonoBehaviour script references.");

            RoomValidationReport validation =
                RoomPrefabValidator.ValidateLibrary(rooms);
            Assert.That(validation.Passed, Is.True, validation.ToString());

            bool[] originalActiveStates = new bool[rooms.Length];
            for (int index = 0; index < rooms.Length; index++)
            {
                originalActiveStates[index] = rooms[index].gameObject.activeSelf;
                rooms[index].gameObject.SetActive(false);
            }

            yield return null;

            try
            {
                for (int index = 0; index < rooms.Length; index++)
                {
                    RoomTemplate2D room = rooms[index];
                    room.gameObject.SetActive(true);
                    yield return null;
                    Assert.That(
                        room.gameObject.activeInHierarchy,
                        Is.True,
                        $"{room.RoomId} did not remain active for one frame.");
                    Assert.That(
                        room.enabled,
                        Is.True,
                        $"{room.RoomId} disabled its RoomTemplate2D during activation.");
                    room.gameObject.SetActive(false);
                }
            }
            finally
            {
                for (int index = 0; index < rooms.Length; index++)
                {
                    if (rooms[index] != null)
                    {
                        rooms[index].gameObject.SetActive(originalActiveStates[index]);
                    }
                }
            }

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator LoadLibrarySceneAsync()
        {
            AsyncOperation operation;
#if UNITY_EDITOR
            operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            operation = SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
#endif
            Assert.That(operation, Is.Not.Null, $"Unable to load P4 scene: {ScenePath}");
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static void AssertCanonicalLayer(
            RoomTemplate2D room,
            Tilemap tilemap,
            string expectedName)
        {
            Assert.That(
                tilemap,
                Is.Not.Null,
                $"{room.RoomId} is missing its {expectedName} Tilemap reference.");
            Assert.That(
                tilemap.name,
                Is.EqualTo(expectedName),
                $"{room.RoomId} has a non-canonical {expectedName} layer name.");
            Assert.That(
                tilemap.gameObject.scene,
                Is.EqualTo(room.gameObject.scene),
                $"{room.RoomId}/{expectedName} belongs to a different scene.");
            Assert.That(
                tilemap.transform.IsChildOf(room.transform),
                Is.True,
                $"{room.RoomId}/{expectedName} is outside its room hierarchy.");
        }

        private static T[] FindSceneComponents<T>(Scene scene)
            where T : Component
        {
            List<T> components = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                components.AddRange(
                    roots[index].GetComponentsInChildren<T>(true));
            }

            return components.ToArray();
        }

        private static int CountMissingMonoBehaviours(Scene scene)
        {
            int missingCount = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] transforms =
                    roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < transforms.Length; index++)
                {
#if UNITY_EDITOR
                    missingCount +=
                        GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                            transforms[index].gameObject);
#else
                    MonoBehaviour[] behaviours =
                        transforms[index].GetComponents<MonoBehaviour>();
                    for (int behaviourIndex = 0;
                         behaviourIndex < behaviours.Length;
                         behaviourIndex++)
                    {
                        if (behaviours[behaviourIndex] == null)
                        {
                            missingCount++;
                        }
                    }
#endif
                }
            }

            return missingCount;
        }
    }
}

#endif
