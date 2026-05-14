using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StickLab.Core
{
    public class InputHandler : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Gameplay";
        [SerializeField] private string rightStickActionName = "RightStick";
        [SerializeField] private string aimActionName = "Aim";
        [SerializeField] private string fireActionName = "Fire";
        [SerializeField] private string confirmActionName = "Confirm";
        [SerializeField] private string cancelActionName = "Cancel";
        [SerializeField] private InputActionReference rightStickAction;
        [SerializeField, Range(0f, 0.5f)] private float deadzone = 0.18f;
        [SerializeField] private bool useFallbackGamepad = true;

        public Vector2 RightStick { get; private set; }
        public bool ControllerConnected => Gamepad.current != null;
        public bool AimHeld => resolvedAimAction != null ? resolvedAimAction.IsPressed() : Gamepad.current != null && Gamepad.current.leftTrigger.ReadValue() > 0.5f;
        public bool FireHeld => resolvedFireAction != null ? resolvedFireAction.IsPressed() : Gamepad.current != null && Gamepad.current.rightTrigger.ReadValue() > 0.5f;
        public bool ConfirmPressedThisFrame => ReadButtonPressedThisFrame(resolvedConfirmAction, Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame, Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame));
        public bool RetryPressedThisFrame => ReadButtonPressedThisFrame(resolvedCancelAction, Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame, Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame);
        public float PrecisionMultiplier => AimHeld ? 0.55f : 1f;
        public string BindingOverridesKey => "StickLab.InputBindingOverrides";

        public event Action<Vector2> RightStickChanged;

        private InputAction resolvedRightStickAction;
        private InputAction resolvedAimAction;
        private InputAction resolvedFireAction;
        private InputAction resolvedConfirmAction;
        private InputAction resolvedCancelAction;

        private void OnEnable()
        {
            LoadBindingOverrides();
            resolvedRightStickAction = ResolveRightStickAction();
            resolvedAimAction = ResolveActionByName(aimActionName);
            resolvedFireAction = ResolveActionByName(fireActionName);
            resolvedConfirmAction = ResolveActionByName(confirmActionName);
            resolvedCancelAction = ResolveActionByName(cancelActionName);

            if (resolvedRightStickAction != null)
            {
                resolvedRightStickAction.Enable();
            }

            resolvedAimAction?.Enable();
            resolvedFireAction?.Enable();
            resolvedConfirmAction?.Enable();
            resolvedCancelAction?.Enable();
        }

        private void OnDisable()
        {
            if (resolvedRightStickAction != null)
            {
                resolvedRightStickAction.Disable();
            }

            resolvedAimAction?.Disable();
            resolvedFireAction?.Disable();
            resolvedConfirmAction?.Disable();
            resolvedCancelAction?.Disable();

            SaveBindingOverrides();
        }

        public void Sample()
        {
            Vector2 processed = ApplyRadialDeadzone(ReadRawRightStick(), deadzone);

            if (processed != RightStick)
            {
                RightStick = processed;
                RightStickChanged?.Invoke(RightStick);
            }
        }

        public void SetDeadzone(float newDeadzone)
        {
            deadzone = Mathf.Clamp(newDeadzone, 0f, 0.5f);
        }

        public float GetDeadzone()
        {
            return deadzone;
        }

        public InputActionAsset GetInputActionsAsset()
        {
            return inputActions;
        }

        public InputAction FindAction(string actionName)
        {
            return ResolveActionByName(actionName);
        }

        public bool HasAction(string actionName)
        {
            return FindAction(actionName) != null;
        }

        public void SaveBindingOverrides()
        {
            if (inputActions == null)
            {
                return;
            }

            string json = inputActions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(BindingOverridesKey, json);
            PlayerPrefs.Save();
        }

        public void LoadBindingOverrides()
        {
            if (inputActions == null || !PlayerPrefs.HasKey(BindingOverridesKey))
            {
                return;
            }

            string json = PlayerPrefs.GetString(BindingOverridesKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                inputActions.LoadBindingOverridesFromJson(json);
            }
        }

        private Vector2 ReadRawRightStick()
        {
            if (resolvedRightStickAction != null)
            {
                return resolvedRightStickAction.ReadValue<Vector2>();
            }

            if (useFallbackGamepad && Gamepad.current != null)
            {
                return Gamepad.current.rightStick.ReadValue();
            }

            return Vector2.zero;
        }

        private InputAction ResolveRightStickAction()
        {
            if (rightStickAction != null && rightStickAction.action != null)
            {
                return rightStickAction.action;
            }

            return ResolveActionByName(rightStickActionName);
        }

        private InputAction ResolveActionByName(string actionName)
        {
            if (inputActions != null)
            {
                InputActionMap map = inputActions.FindActionMap(actionMapName, false);
                if (map != null)
                {
                    return map.FindAction(actionName, false);
                }
            }

            return null;
        }

        private bool ReadButtonPressedThisFrame(InputAction action, bool fallbackGamepadValue, bool fallbackKeyboardValue)
        {
            if (action != null)
            {
                return action.WasPressedThisFrame();
            }

            return fallbackGamepadValue || fallbackKeyboardValue;
        }

        public static Vector2 ApplyRadialDeadzone(Vector2 input, float deadzoneAmount)
        {
            float magnitude = input.magnitude;

            if (magnitude <= deadzoneAmount)
            {
                return Vector2.zero;
            }

            float normalizedMagnitude = Mathf.InverseLerp(deadzoneAmount, 1f, Mathf.Clamp01(magnitude));
            return input.normalized * normalizedMagnitude;
        }
    }
}
