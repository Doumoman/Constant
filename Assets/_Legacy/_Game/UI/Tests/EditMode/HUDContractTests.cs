#if LEGACY_DISABLED
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Stage.Data;
using StarNight.UI.HUD;
using UnityEngine;

namespace StarNight.UI.Tests
{
    public sealed class HUDContractTests
    {
        [Test]
        public void FormattingMatchesFourHeartWonBellAndZeroConsumableContracts()
        {
            Assert.That(HUDFormatting.Health(3), Is.EqualTo("♥ ♥ ♥ ♡"));
            Assert.That(HUDFormatting.Money(1260), Is.EqualTo("1,260원"));
            Assert.That(HUDFormatting.MoneyDelta(50), Is.EqualTo("+50원"));
            Assert.That(HUDFormatting.MoneyDelta(-50), Is.EqualTo("-50원"));
            Assert.That(HUDFormatting.Bells(BellPhase.None), Is.EqualTo("○ ○ ○"));
            Assert.That(HUDFormatting.Bells(BellPhase.Maru), Is.EqualTo("● ● ●"));
            Assert.That(HUDFormatting.Consumable("로프", 0), Is.EqualTo("로프 ╱ 0"));
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1920f, 1200f)]
        [TestCase(2560f, 1080f)]
        public void SafeAreaAnchorsStayNormalizedAcrossSupportedAspectRatios(float width, float height)
        {
            var safeArea = new Rect(width * 0.025f, height * 0.035f, width * 0.95f, height * 0.93f);
            SafeAreaFitter.CalculateAnchors(safeArea, new Vector2(width, height), out Vector2 minimum, out Vector2 maximum);

            Assert.That(minimum.x, Is.InRange(0f, 1f));
            Assert.That(minimum.y, Is.InRange(0f, 1f));
            Assert.That(maximum.x, Is.InRange(minimum.x, 1f));
            Assert.That(maximum.y, Is.InRange(minimum.y, 1f));
        }

        [Test]
        public void LayoutRegionsAndDeviceGlyphsMatchTheHudContract()
        {
            Assert.That(HUDLayoutContract.FitsNormalizedSafeArea(HUDLayoutContract.TopLeft), Is.True);
            Assert.That(HUDLayoutContract.FitsNormalizedSafeArea(HUDLayoutContract.TopCenter), Is.True);
            Assert.That(HUDLayoutContract.FitsNormalizedSafeArea(HUDLayoutContract.TopRight), Is.True);
            Assert.That(HUDLayoutContract.FitsNormalizedSafeArea(HUDLayoutContract.BottomLeft), Is.True);
            Assert.That(HUDLayoutContract.FitsNormalizedSafeArea(HUDLayoutContract.BottomRight), Is.True);
            Assert.That(InputGlyphResolver.GamepadGlyph("PrimaryAction", "Xbox Controller"), Is.EqualTo("PAD X"));
            Assert.That(InputGlyphResolver.GamepadGlyph("PrimaryAction", "DualSense Gamepad"), Is.EqualTo("PAD □"));
            Assert.That(InputGlyphResolver.GamepadGlyph("PrimaryAction", "Nintendo Switch Pro"), Is.EqualTo("PAD Y"));
            Assert.That(InputGlyphResolver.GamepadGlyph("OpenMap", "Xbox Controller"), Is.EqualTo("PAD VIEW"));
        }

        [Test]
        public void HudModelExposesNoPublicMutatorsOrMutableMapCollections()
        {
            Assert.That(typeof(HUDModel).GetProperties().Where(property => property.SetMethod?.IsPublic == true), Is.Empty);
            var model = new HUDModel();
            Assert.That(((ICollection<HUDMapRoomModel>)model.Rooms).IsReadOnly, Is.True);
            Assert.That(((ICollection<HUDMapConnectionModel>)model.Connections).IsReadOnly, Is.True);
        }
    }
}

#endif
