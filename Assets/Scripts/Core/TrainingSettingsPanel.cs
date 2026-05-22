using UnityEngine;

namespace StickLab.Core
{
    /// <summary>
    /// Lightweight IMGUI settings overlay (F1 to toggle).
    /// Adjusts deadzone, sensitivity, and difficulty without touching the main dashboard.
    /// </summary>
    public class TrainingSettingsPanel : MonoBehaviour
    {
        private enum DifficultyPreset { Easy, Normal, Hard }

        [Header("References")]
        [SerializeField] private InputHandler          inputHandler;
        [SerializeField] private CursorController      cursorController;
        [SerializeField] private TrainingSessionManager sessionManager;

        [Header("Panel")]
        [SerializeField] private Rect  panelRect        = new Rect(12f, 420f, 340f, 240f);
        [SerializeField] private int   fontSize         = 18;
        [SerializeField] private bool  visible          = true;

        [Header("Tuning")]
        [SerializeField, Range(0f, 0.5f)] private float deadzone         = 0.18f;
        [SerializeField, Range(2f, 16f)]  private float sensitivity      = 8f;
        [SerializeField] private DifficultyPreset       difficultyPreset = DifficultyPreset.Normal;

        // ── Public wiring ─────────────────────────────────────────────────────
        public void SetReferences(InputHandler ih, CursorController cc, TrainingSessionManager sm)
        {
            inputHandler   = ih;
            cursorController = cc;
            sessionManager = sm;
        }

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Start() => ApplySettings();

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || inputHandler == null || cursorController == null || sessionManager == null) return;

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = fontSize,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };
            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(14, fontSize - 2),
                normal   = { textColor = new Color(0.9f, 0.93f, 1f) }
            };

            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(panelRect);
            GUILayout.Space(8f);
            GUILayout.Label("Settings  (F1 to hide)", titleStyle);

            // Deadzone
            GUILayout.Label($"Deadzone: {deadzone:0.00}", bodyStyle);
            float newDz = GUILayout.HorizontalSlider(deadzone, 0f, 0.5f);
            if (!Mathf.Approximately(newDz, deadzone))
            {
                deadzone = newDz;
                inputHandler.SetDeadzone(deadzone);
            }

            // Sensitivity
            GUILayout.Label($"Sensitivity: {sensitivity:0.0}", bodyStyle);
            float newSens = GUILayout.HorizontalSlider(sensitivity, 2f, 16f);
            if (!Mathf.Approximately(newSens, sensitivity))
            {
                sensitivity = newSens;
                cursorController.SetSensitivity(sensitivity);
            }

            // Difficulty
            GUILayout.Space(6f);
            GUILayout.Label("Difficulty", bodyStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(difficultyPreset == DifficultyPreset.Easy,   "Easy",   GUI.skin.button)) difficultyPreset = DifficultyPreset.Easy;
            if (GUILayout.Toggle(difficultyPreset == DifficultyPreset.Normal, "Normal", GUI.skin.button)) difficultyPreset = DifficultyPreset.Normal;
            if (GUILayout.Toggle(difficultyPreset == DifficultyPreset.Hard,   "Hard",   GUI.skin.button)) difficultyPreset = DifficultyPreset.Hard;
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label(sessionManager.ControllerHintText, bodyStyle);

            if (GUILayout.Button("Apply")) ApplySettings();

            GUILayout.EndArea();
        }

        // ── Single definition — no duplicates ─────────────────────────────────
        private void ApplySettings()
        {
            inputHandler?.SetDeadzone(deadzone);
            cursorController?.SetSensitivity(sensitivity);
            sessionManager?.SetDifficultyProfile(SizeMultiplier(), ThicknessMultiplier(), ToleranceMultiplier());
        }

        private float SizeMultiplier() => difficultyPreset switch
        {
            DifficultyPreset.Easy => 1f,
            DifficultyPreset.Hard => 0.92f,
            _                     => 0.97f
        };

        private float ThicknessMultiplier() => difficultyPreset switch
        {
            DifficultyPreset.Easy => 1f,
            DifficultyPreset.Hard => 0.84f,
            _                     => 0.92f
        };

        private float ToleranceMultiplier() => difficultyPreset switch
        {
            DifficultyPreset.Easy => 1f,
            DifficultyPreset.Hard => 0.80f,
            _                     => 0.90f
        };
    }
}
