#if LEGACY_DISABLED
using StarNight.Interaction.Input;
using UnityEngine.InputSystem;

namespace StarNight.UI.HUD
{
    public static class InputGlyphResolver
    {
        public static string Resolve(GameplayInputReader reader, string actionName, InputDisplayDevice device)
        {
            if (device == InputDisplayDevice.Gamepad)
            {
                return GamepadGlyph(actionName, Gamepad.current?.displayName);
            }

            InputActionAsset actions = reader?.RuntimeActions != null ? reader.RuntimeActions : reader?.SourceAsset;
            InputAction action = actions?.FindAction("Gameplay/" + actionName, false);
            string display = action?.GetBindingDisplayString(group: "Keyboard");
            if (!string.IsNullOrWhiteSpace(display))
            {
                return display.ToUpperInvariant();
            }

            return actionName == "OpenMap" ? "TAB" : "X";
        }

        public static string GamepadGlyph(string actionName, string displayName)
        {
            if (actionName == "OpenMap")
            {
                return "PAD VIEW";
            }

            string device = displayName ?? string.Empty;
            if (device.Contains("DualShock") || device.Contains("DualSense") || device.Contains("PlayStation"))
            {
                return "PAD □";
            }

            if (device.Contains("Switch") || device.Contains("Nintendo"))
            {
                return "PAD Y";
            }

            return "PAD X";
        }
    }
}

#endif
