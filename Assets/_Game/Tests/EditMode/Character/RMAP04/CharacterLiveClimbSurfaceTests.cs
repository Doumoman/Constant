#if UNITY_EDITOR
using NUnit.Framework;
using StarNight.Character.Live.Movement;
using UnityEngine;

namespace StarNight.Character.Tests.EditMode.Rmap04
{
    [Category("RMAP04")]
    public sealed class CharacterLiveClimbSurfaceTests
    {
        [Test]
        public void LadderPoleAndOneWayMarkers_KeepTheirSeparateResponsibilities()
        {
            var ladder = new GameObject("RMAP04_Ladder", typeof(BoxCollider2D),
                typeof(CharacterLiveClimbSurface));
            var pole = new GameObject("RMAP04_Pole", typeof(BoxCollider2D),
                typeof(CharacterLiveClimbSurface));
            var oneWay = new GameObject("RMAP04_OneWay", typeof(BoxCollider2D),
                typeof(CharacterLiveOneWayPlatform), typeof(CharacterLiveGrabSurface));

            ladder.GetComponent<CharacterLiveClimbSurface>().Configure(
                CharacterLiveClimbSurface.SurfaceKind.Ladder, ladder.GetComponent<BoxCollider2D>());
            pole.GetComponent<CharacterLiveClimbSurface>().Configure(
                CharacterLiveClimbSurface.SurfaceKind.Pole, pole.GetComponent<BoxCollider2D>());
            oneWay.GetComponent<CharacterLiveOneWayPlatform>().Configure(oneWay.GetComponent<BoxCollider2D>());
            oneWay.GetComponent<CharacterLiveGrabSurface>().Configure(
                CharacterLiveGrabSurface.SurfaceKind.OneWay);

            Assert.IsTrue(ladder.GetComponent<CharacterLiveClimbSurface>().IsUsable);
            Assert.IsTrue(pole.GetComponent<CharacterLiveClimbSurface>().IsUsable);
            Assert.IsTrue(ladder.GetComponent<BoxCollider2D>().isTrigger);
            Assert.IsTrue(pole.GetComponent<BoxCollider2D>().isTrigger);
            Assert.IsFalse(oneWay.GetComponent<CharacterLiveGrabSurface>().IsGrabAllowed,
                "RMAP04 one-way traversal must retain RMAP03's Grab prohibition.");
            Assert.IsNotNull(oneWay.GetComponent<CharacterLiveOneWayPlatform>().PlatformCollider);

            Object.DestroyImmediate(ladder);
            Object.DestroyImmediate(pole);
            Object.DestroyImmediate(oneWay);
        }
    }
}
#endif
