#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Debugging;
using StarNight.Population.P7;
using StarNight.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace StarNight.Tests.PlayMode
{
    public sealed class P7PopulationLabPlayModeTests
    {
        private const string ScenePath =
            "Assets/StarNight/Scenes/Labs/"
            + "P6_MoonRoomGraphGeneratorLab.unity";

        [UnityTest]
        public IEnumerator IntegratedP6P7Lab_PopulatesEconomyAndPurchasesInWorld()
        {
            yield return LoadLabSceneAsync();

            Scene scene = SceneManager.GetActiveScene();
            P7PopulationLabContract[] contracts =
                FindSceneComponents<P7PopulationLabContract>(scene);
            Assert.That(contracts.Length, Is.EqualTo(1));
            P7PopulationLabContract contract = contracts[0];

            Assert.That(
                contract.RegionProfile,
                Is.Not.Null);
            Assert.That(
                contract.RegionProfile.Region,
                Is.EqualTo(RoomRegion.MoonPalace));
            Assert.That(
                contract.PopulationFingerprint,
                Is.Not.Null.And.Not.Empty);
            Assert.That(
                contract.RefreshValidation(),
                Is.True,
                contract.LastValidation);
            Assert.That(contract.ValidationPassed, Is.True);
            Assert.That(contract.RoomBudgetsSatisfied, Is.True);
            Assert.That(contract.StageBudgetsSatisfied, Is.True);
            Assert.That(contract.SlotClaimsUnique, Is.True);
            Assert.That(contract.MainPathPurchaseSatisfied, Is.True);
            Assert.That(contract.OptionalRiskRewardSatisfied, Is.True);
            Assert.That(contract.ShopContractSatisfied, Is.True);
            Assert.That(contract.ExitApproachProtected, Is.True);
            Assert.That(
                contract.MainPathGoldAvailableForShop,
                Is.GreaterThanOrEqualTo(3));
            Assert.That(contract.Markers, Is.Not.Empty);
            Assert.That(
                contract.Markers.All(marker =>
                    marker != null
                    && marker.gameObject.scene == scene),
                Is.True);
            Assert.That(
                contract.Markers.Count(marker =>
                    marker.Kind == P7PopulationKind.SmallGold
                    || marker.Kind == P7PopulationKind.BigGold),
                Is.GreaterThan(0));
            Assert.That(
                contract.Markers.Count(marker =>
                    marker.Kind == P7PopulationKind.Enemy),
                Is.GreaterThan(0));
            Assert.That(
                contract.Markers.Count(marker =>
                    marker.Kind == P7PopulationKind.Trap),
                Is.GreaterThan(0));

            Assert.That(contract.MomoShop, Is.Not.Null);
            Assert.That(
                contract.MomoShop.OfferCount,
                Is.EqualTo(P7EconomyRules.RequiredWorldOfferCount));
            P7MomoOffer2D cheapest = contract.MomoShop.Offers
                .OrderBy(offer => offer.Price)
                .First();
            Assert.That(
                contract.MainPathGoldAvailableForShop,
                Is.GreaterThanOrEqualTo(cheapest.Price));
            contract.Wallet.Configure(
                contract.MainPathGoldAvailableForShop);
            int before = contract.Wallet.Gold;
            Assert.That(cheapest.TryPurchase(), Is.True);
            Assert.That(
                contract.Wallet.Gold,
                Is.EqualTo(before - cheapest.Price));
            Assert.That(cheapest.IsSold, Is.True);
            Assert.That(cheapest.TryPurchase(), Is.False);

            yield return null;

            Assert.That(contract, Is.Not.Null);
            Assert.That(contract.ValidationPassed, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        private static T[] FindSceneComponents<T>(Scene scene)
            where T : Component
        {
            var values = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                values.AddRange(
                    roots[index].GetComponentsInChildren<T>(true));
            }

            return values.ToArray();
        }

        private static IEnumerator LoadLabSceneAsync()
        {
            AsyncOperation operation;
#if UNITY_EDITOR
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath),
                Is.Not.Null,
                $"Missing integrated P6/P7 Lab scene: {ScenePath}");
            operation =
                EditorSceneManager.LoadSceneAsyncInPlayMode(
                    ScenePath,
                    new LoadSceneParameters(LoadSceneMode.Single));
#else
            operation = SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
#endif
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }
    }
}

#endif
