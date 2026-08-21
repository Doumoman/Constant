#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Interaction.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarNight.Interaction.Tests
{
    public sealed class BindingOverrideTests
    {
        [Test]
        public void RebindRejectsSameAndDuplicateKeysAndRoundTripsOverrideJson()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap gameplay = asset.AddActionMap("Gameplay");
            gameplay.AddAction("MoveHorizontal", InputActionType.Value).AddBinding("<Keyboard>/leftArrow");
            gameplay.AddAction("LookVertical", InputActionType.Value).AddBinding("<Keyboard>/upArrow");
            gameplay.AddAction("Jump").AddBinding("<Keyboard>/space", groups: "Keyboard");
            gameplay.AddAction("PrimaryAction").AddBinding("<Keyboard>/x", groups: "Keyboard");
            gameplay.AddAction("PlaceBomb").AddBinding("<Keyboard>/z", groups: "Keyboard");
            gameplay.AddAction("PlaceRope").AddBinding("<Keyboard>/c", groups: "Keyboard");
            gameplay.AddAction("OpenMap").AddBinding("<Keyboard>/tab", groups: "Keyboard");
            gameplay.AddAction("Pause").AddBinding("<Keyboard>/escape", groups: "Keyboard");
            InputActionMap dialogue = asset.AddActionMap("Dialogue");
            dialogue.AddAction("AdvanceDialogue").AddBinding("<Keyboard>/x");
            dialogue.AddAction("Navigate", InputActionType.Value).AddBinding("<Keyboard>/upArrow");
            dialogue.AddAction("Pause").AddBinding("<Keyboard>/escape");
            InputActionMap menu = asset.AddActionMap("Menu");
            menu.AddAction("Navigate", InputActionType.Value).AddBinding("<Gamepad>/leftStick");
            menu.AddAction("MenuSubmit").AddBinding("<Keyboard>/enter");
            menu.AddAction("MenuCancel").AddBinding("<Keyboard>/escape");

            var owner = new GameObject("InputReaderTest");
            GameplayInputReader reader = owner.AddComponent<GameplayInputReader>();
            reader.Configure(asset);
            int primary = reader.FindBindingIndex("Gameplay", "PrimaryAction", "Keyboard");
            int bomb = reader.FindBindingIndex("Gameplay", "PlaceBomb", "Keyboard");

            Assert.That(reader.TryApplyBindingOverride("Gameplay", "PrimaryAction", primary, "<Keyboard>/x", out string sameError), Is.False);
            Assert.That(sameError, Does.Contain("취소"));
            Assert.That(reader.TryApplyBindingOverride("Gameplay", "PrimaryAction", primary, "<Keyboard>/v", out _), Is.True);
            Assert.That(reader.TryApplyBindingOverride("Gameplay", "PlaceBomb", bomb, "<Keyboard>/v", out string conflictError), Is.False);
            Assert.That(conflictError, Does.Contain("겹칩니다"));

            string json = reader.SaveBindingOverrides();
            reader.ResetBindingOverrides();
            Assert.That(reader.ApplyBindingOverrides(json), Is.True);
            Assert.That(reader.GetBindingDisplayString("Gameplay", "PrimaryAction", primary), Does.Contain("V"));

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(asset);
        }
    }
}

#endif
