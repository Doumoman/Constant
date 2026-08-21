#if LEGACY_DISABLED
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace StarNight.Interaction.Tests
{
    public sealed class InputActionContractTests
    {
        private const string AssetPath = "Assets/_Game/Interaction/Data/Resources/Input/StarNightControls.inputactions";

        private InputActionAsset actions;

        [SetUp]
        public void SetUp()
        {
            actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.That(actions, Is.Not.Null, $"Input Action asset missing at {AssetPath}");
        }

        [Test]
        public void RequiredActionMapsAndActionsExist()
        {
            Assert.That(actions.FindActionMap("Gameplay"), Is.Not.Null);
            Assert.That(actions.FindActionMap("Dialogue"), Is.Not.Null);
            Assert.That(actions.FindActionMap("Menu"), Is.Not.Null);
            Assert.That(actions.FindActionMap("Debug"), Is.Not.Null);

            InputActionMap gameplay = actions.FindActionMap("Gameplay", true);
            string[] required =
            {
                "MoveHorizontal", "LookVertical", "Jump", "PrimaryAction",
                "PlaceBomb", "PlaceRope", "SelectNextItem", "SelectPreviousItem", "OpenMap", "Pause",
            };

            foreach (string actionName in required)
            {
                Assert.That(gameplay.FindAction(actionName), Is.Not.Null, actionName);
            }

            InputActionMap dialogue = actions.FindActionMap("Dialogue", true);
            Assert.That(dialogue.FindAction("AdvanceDialogue"), Is.Not.Null);
            Assert.That(dialogue.FindAction("Navigate"), Is.Not.Null);
            Assert.That(dialogue.FindAction("Pause"), Is.Not.Null);
        }

        [Test]
        public void DefaultMovementUsesArrowKeysAndNeverWasd()
        {
            InputAction move = actions.FindAction("Gameplay/MoveHorizontal", true);
            List<string> keyboardPaths = move.bindings
                .Where(binding => binding.path.StartsWith("<Keyboard>"))
                .Select(binding => binding.path)
                .ToList();

            CollectionAssert.Contains(keyboardPaths, "<Keyboard>/leftArrow");
            CollectionAssert.Contains(keyboardPaths, "<Keyboard>/rightArrow");
            CollectionAssert.DoesNotContain(keyboardPaths, "<Keyboard>/a");
            CollectionAssert.DoesNotContain(keyboardPaths, "<Keyboard>/d");

            IEnumerable<string> everyGameplayKeyboardPath = actions.FindActionMap("Gameplay", true).bindings
                .Where(binding => binding.path.StartsWith("<Keyboard>"))
                .Select(binding => binding.path);
            CollectionAssert.DoesNotContain(everyGameplayKeyboardPath, "<Keyboard>/w");
            CollectionAssert.DoesNotContain(everyGameplayKeyboardPath, "<Keyboard>/a");
            CollectionAssert.DoesNotContain(everyGameplayKeyboardPath, "<Keyboard>/s");
            CollectionAssert.DoesNotContain(everyGameplayKeyboardPath, "<Keyboard>/d");
        }

        [Test]
        public void GameplayActionsSupportKeyboardAndGamepadTogether()
        {
            AssertBinding("Gameplay/MoveHorizontal", "<Keyboard>/leftArrow", "<Gamepad>/leftStick/x");
            AssertBinding("Gameplay/LookVertical", "<Keyboard>/downArrow", "<Gamepad>/leftStick/y");
            AssertBinding("Gameplay/Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            AssertBinding("Gameplay/PrimaryAction", "<Keyboard>/x", "<Gamepad>/buttonWest");
            AssertBinding("Gameplay/PlaceBomb", "<Keyboard>/z", "<Gamepad>/buttonEast");
            AssertBinding("Gameplay/PlaceRope", "<Keyboard>/c", "<Gamepad>/leftShoulder");
            AssertBinding("Gameplay/SelectNextItem", "<Keyboard>/tab", "<Gamepad>/rightStickPress");
            AssertBinding("Gameplay/OpenMap", "<Keyboard>/m", "<Gamepad>/select");
        }

        [Test]
        public void DialogueUsesXAndHasNoCancelBinding()
        {
            AssertBinding("Dialogue/AdvanceDialogue", "<Keyboard>/x", "<Gamepad>/buttonWest");
            InputActionMap dialogue = actions.FindActionMap("Dialogue", true);
            IEnumerable<string> keyboardPaths = dialogue.bindings
                .Where(binding => binding.path.StartsWith("<Keyboard>"))
                .Select(binding => binding.path);
            CollectionAssert.DoesNotContain(keyboardPaths, "<Keyboard>/z");
        }

        private void AssertBinding(string actionPath, string keyboardPath, string gamepadPath)
        {
            InputAction action = actions.FindAction(actionPath, true);
            List<string> paths = action.bindings.Select(binding => binding.path).ToList();
            CollectionAssert.Contains(paths, keyboardPath, actionPath);
            CollectionAssert.Contains(paths, gamepadPath, actionPath);
        }
    }
}

#endif
