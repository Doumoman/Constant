#if LEGACY_DISABLED
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.Core.Flow
{
    public sealed class SceneTransitionService
    {
        public bool IsTransitioning { get; private set; }
        public bool LastOperationSucceeded { get; private set; }

        public IEnumerator LoadSingle(string sceneName)
        {
            yield return Load(sceneName, LoadSceneMode.Single);
        }

        public IEnumerator LoadAdditive(string sceneName)
        {
            yield return Load(sceneName, LoadSceneMode.Additive);
        }

        public IEnumerator Unload(string sceneName)
        {
            LastOperationSucceeded = false;
            if (IsTransitioning || string.IsNullOrWhiteSpace(sceneName))
            {
                yield break;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                yield break;
            }

            IsTransitioning = true;
            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            if (operation != null)
            {
                yield return operation;
                LastOperationSucceeded = operation.isDone;
            }

            IsTransitioning = false;
        }

        private IEnumerator Load(string sceneName, LoadSceneMode mode)
        {
            LastOperationSucceeded = false;
            if (IsTransitioning || string.IsNullOrWhiteSpace(sceneName))
            {
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                yield break;
            }

            IsTransitioning = true;
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);
            if (operation != null)
            {
                yield return operation;
                LastOperationSucceeded = operation.isDone;
            }

            IsTransitioning = false;
        }
    }
}

#endif
