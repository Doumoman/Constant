#if LEGACY_DISABLED
using System;
using StarNight.Interaction.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarNight.UI.Menus
{
    [DisallowMultipleComponent]
    public sealed class InputRebindController : MonoBehaviour
    {
        private InputActionRebindingExtensions.RebindingOperation operation;

        public bool IsWaiting => operation != null;
        public string Status { get; private set; } = string.Empty;

        public event Action BindingChanged;

        private void OnDisable()
        {
            Cancel();
        }

        public bool Begin(
            GameplayInputReader reader,
            string mapName,
            string actionName,
            int bindingIndex,
            string bindingGroup)
        {
            if (IsWaiting || reader?.RuntimeActions == null)
            {
                return false;
            }

            InputAction action = reader.RuntimeActions.FindActionMap(mapName, false)?.FindAction(actionName, false);
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                Status = "리바인딩 대상을 찾지 못했습니다.";
                return false;
            }

            string previousOverride = action.bindings[bindingIndex].overridePath;
            Status = "새 키를 누르세요 · 같은 키를 누르면 취소";
            operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>")
                .WithTimeout(8f);

            if (string.Equals(bindingGroup, "Keyboard", StringComparison.OrdinalIgnoreCase))
            {
                operation.WithControlsHavingToMatchPath("<Keyboard>");
            }
            else if (string.Equals(bindingGroup, "Gamepad", StringComparison.OrdinalIgnoreCase))
            {
                operation.WithControlsHavingToMatchPath("<Gamepad>");
            }

            operation.OnCancel(_ =>
            {
                RestoreOverride(action, bindingIndex, previousOverride);
                Finish("키 변경을 취소했습니다.", false);
            });
            operation.OnComplete(_ =>
            {
                string selectedPath = action.bindings[bindingIndex].overridePath;
                RestoreOverride(action, bindingIndex, previousOverride);
                if (reader.TryApplyBindingOverride(mapName, actionName, bindingIndex, selectedPath, out string error))
                {
                    Finish("키를 변경했습니다.", true);
                }
                else
                {
                    Finish(error, false);
                }
            });
            operation.Start();
            return true;
        }

        public void Cancel()
        {
            if (operation != null)
            {
                operation.Cancel();
            }
        }

        private void Finish(string status, bool changed)
        {
            InputActionRebindingExtensions.RebindingOperation completed = operation;
            operation = null;
            completed?.Dispose();
            Status = status;
            if (changed)
            {
                BindingChanged?.Invoke();
            }
        }

        private static void RestoreOverride(InputAction action, int bindingIndex, string overridePath)
        {
            if (string.IsNullOrEmpty(overridePath))
            {
                action.RemoveBindingOverride(bindingIndex);
            }
            else
            {
                action.ApplyBindingOverride(bindingIndex, overridePath);
            }
        }
    }
}

#endif
