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
        [SerializeField] private InputActionReference rightStickAction;
        [SerializeField, Range(0f, 0.5f)] private float deadzone = 0.18f;
        [SerializeField] private bool useFallbackGamepad = true;

        public Vector2 RightStick { get; private set; }
        public bool ControllerConnected => Gamepad.current != null;
        public bool ConfirmPressedThisFrame => (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            || (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame));
        public bool RetryPressedThisFrame => (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame)
            || (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame);

        public event Action<Vector2> RightStickChanged;

        private InputAction resolvedRightStickAction;

        private void OnEnable()
        {
            resolvedRightStickAction = ResolveRightStickAction();

            if (resolvedRightStickAction != null)
            {
                resolvedRightStickAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (resolvedRightStickAction != null)
            {
                resolvedRightStickAction.Disable();
            }
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

            if (inputActions != null)
            {
                InputActionMap map = inputActions.FindActionMap(actionMapName, false);
                if (map != null)
                {
                    return map.FindAction(rightStickActionName, false);
                }
            }

            return null;
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
