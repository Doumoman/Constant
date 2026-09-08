#if UNITY_EDITOR
using NUnit.Framework;
using StarNight.Character.Live.Movement;
using UnityEngine;

namespace StarNight.Character.Tests.EditMode.Rmap03
{
    [Category("RMAP03")]
    public sealed class CharacterLiveGrabSurfaceTests
    {
        [Test]
        public void SurfaceClassification_OnlySafeSolidKindsAllowGrab()
        {
            var owner = new GameObject("RMAP03_SurfaceClassification");
            var surface = owner.AddComponent<CharacterLiveGrabSurface>();

            surface.Configure(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            Assert.IsTrue(surface.IsGrabAllowed);
            surface.Configure(CharacterLiveGrabSurface.SurfaceKind.DestructibleSafe);
            Assert.IsTrue(surface.IsGrabAllowed);
            surface.Configure(CharacterLiveGrabSurface.SurfaceKind.MovingSafe);
            Assert.IsTrue(surface.IsGrabAllowed);
            Assert.IsTrue(surface.IsMovingSafeSolid);

            foreach (CharacterLiveGrabSurface.SurfaceKind unsafeKind in new[]
                     {
                         CharacterLiveGrabSurface.SurfaceKind.OneWay,
                         CharacterLiveGrabSurface.SurfaceKind.Hazard,
                         CharacterLiveGrabSurface.SurfaceKind.Crushing,
                         CharacterLiveGrabSurface.SurfaceKind.Decoration
                     })
            {
                surface.Configure(unsafeKind);
                Assert.IsFalse(surface.IsGrabAllowed, unsafeKind.ToString());
            }

            surface.Configure(CharacterLiveGrabSurface.SurfaceKind.MovingSafe);
            surface.SetDangerous(true);
            Assert.IsFalse(surface.IsGrabAllowed,
                "A current danger transition immediately removes a moving anchor.");
            Object.DestroyImmediate(owner);
        }
    }
}
#endif
