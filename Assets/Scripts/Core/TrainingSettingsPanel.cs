using UnityEngine;

namespace StickLab.Core
{
    public class TrainingSettingsPanel : MonoBehaviour
    {
        private enum DifficultyPreset
        {
            Easy,
            Normal,
            Hard
        }

        [Header("References")]
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private CursorController cursorController;
        [SerializeField] private TrainingSessionManager sessionManager;

        [Header("Panel")]
        [SerializeField] private Rect panelRect = new Rect(12f, 420f, 360f, 260f);
        [SerializeField] private int fontSize = 18;
        [SerializeField] private bool visible = true;

        [Header("Tuning")]
        [SerializeField, Range(0f, 0.5f)] private float deadzone = 0.18f;
        [SerializeField, Range(2f, 16f)] private float sensitivity = 8f;
        [SerializeField] private DifficultyPreset difficultyPreset = DifficultyPreset.Normal;

        public void SetReferences(InputHandler newInputHandler, CursorController newCursorController, TrainingSessionManager newSessionManager)
        {
            inputHandler = newInputHandler;
            cursorController = newCursorController;
            sessionManager = newSessionManager;
        }

        private void Start()
        {
            ApplySettings();
        }

        private void Update()
        {
            if (inputHandler == null || cursorController == null || sessionManager == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible || inputHandler == null || cursorController == null || sessionManager == null)
            {
                return;
            }

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(14, fontSize - 2),
                normal = { textColor = new Color(0.9f, 0.93f, 1f, 1f) }
            };

            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(panelRect);
            GUILayout.Space(8f);
            GUILayout.Label("Settings", titleStyle);
            GUILayout.Label("F1 to hide/show", bodyStyle);

            GUILayout.Label($"Deadzone: {deadzone:0.00}", bodyStyle);
            deadzone = GUILayout.HorizontalSlider(deadzone, 0f, 0.5f);

            GUILayout.Label($"Sensitivity: {sensitivity:0.0}", bodyStyle);
            sensitivity = GUILayout.HorizontalSlider(sensitivity, 2f, 16f);

            GUILayout.Space(6f);
            GUILayout.Label("Difficulty", bodyStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(difficultyPreset == DifficultyPreset.Easy, "Easy", GUI.skin.button))
            {
                difficultyPreset = DifficultyPreset.Easy;
            }
            if (GUILayout.Toggle(difficultyPreset == DifficultyPreset.Normal, "Normal", GUI.skin.button))
            {
                difficultyPreset = DifficultyPreset.Normal;
            }
            if (GUILayout.Toggle(difficultyPreset == DifficultyPreset.Hard, "Hard", GUI.skin.button))
            {
                difficultyPreset = DifficultyPreset.Hard;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label(sessionManager.ControllerHintText, bodyStyle);

            if (GUILayout.Button("Apply Settings"))
            {
                ApplySettings();
            }

            GUILayout.EndArea();
        }

        private void ApplySettings()
        {
            if (inputHandler != null)
            {
                inputHandler.SetDeadzone(deadzone);
            }

            if (cursorController != null)
            {
                cursorController.SetSensitivity(sensitivity);
            }

            if (sessionManager != null)
            {
                sessionManager.SetDifficultyProfile(GetSizeMultiplier(), GetThicknessMultiplier(), GetToleranceMultiplier());
            }
        }

        private float GetSizeMultiplier()
        {
            return difficultyPreset switch
            {
                DifficultyPreset.Easy => 1f,
                DifficultyPreset.Normal => 0.97f,
                DifficultyPreset.Hard => 0.92f,
                _ => 0.97f
            };
        }

        private float GetThicknessMultiplier()
        {
            return difficultyPreset switch
            {
                DifficultyPreset.Easy => 1f,
                DifficultyPreset.Normal => 0.92f,
                DifficultyPreset.Hard => 0.84f,
                _ => 0.92f
            };
        }

        private float GetToleranceMultiplier()
        {
            return difficultyPreset switch
            {
                DifficultyPreset.Easy => 1f,
                DifficultyPreset.Normal => 0.90f,
                DifficultyPreset.Hard => 0.80f,
                _ => 0.90f
            };
        }
    }
}
