#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Core.Flow;
using StarNight.Core.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StarNight.Core.Tests
{
    public sealed class GameBootstrapPlayModeTests
    {
        [UnityTest]
        public IEnumerator TenDuplicateBootstrapObjectsLeaveOneServiceRoot()
        {
            yield return null;

            for (int index = 0; index < 10; index++)
            {
                var duplicate = new GameObject("Duplicate Core Services " + index);
                duplicate.AddComponent<GameBootstrap>();
            }

            yield return null;

            GameBootstrap[] bootstraps = Object.FindObjectsByType<GameBootstrap>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert.That(bootstraps, Has.Length.EqualTo(1));
            Assert.That(GameBootstrap.Instance, Is.SameAs(bootstraps[0]));
            Assert.That(GameBootstrap.Instance.Services.GetRequired<SettingsRepository>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator BootToTitleTenTimesKeepsOneBootstrapAndOneSettingsService()
        {
            for (int index = 0; index < 10; index++)
            {
                AsyncOperation bootLoad = SceneManager.LoadSceneAsync(GameBootstrap.BootSceneName, LoadSceneMode.Single);
                Assert.That(bootLoad, Is.Not.Null);
                yield return bootLoad;

                float timeoutAt = Time.realtimeSinceStartup + 5f;
                while (SceneManager.GetActiveScene().name != GameBootstrap.TitleSceneName
                    && Time.realtimeSinceStartup < timeoutAt)
                {
                    yield return null;
                }

                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(GameBootstrap.TitleSceneName));

                GameBootstrap[] bootstraps = Object.FindObjectsByType<GameBootstrap>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

                Assert.That(bootstraps, Has.Length.EqualTo(1));
                Assert.That(bootstraps[0].Services.GetRequired<SettingsRepository>(), Is.Not.Null);
                Assert.That(bootstraps[0].Services.GetRequired<RunRecordRepository>(), Is.Not.Null);
                Assert.That(bootstraps[0].Services.Count, Is.EqualTo(5));
            }
        }
    }
}

#endif
