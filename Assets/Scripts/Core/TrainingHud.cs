using UnityEngine;

namespace StickLab.Core
{
    public class TrainingHud : MonoBehaviour
    {
        [SerializeField] private TrainingSessionManager sessionManager;
        [SerializeField] private Rect panelRect = new Rect(12f, 12f, 360f, 220f);
        [SerializeField] private int fontSize = 18;

        public void SetSessionManager(TrainingSessionManager newSessionManager)
        {
            sessionManager = newSessionManager;
        }

        private void OnGUI()
        {
            if (sessionManager == null)
            {
                return;
            }

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(14, fontSize - 2),
                normal = { textColor = new Color(0.92f, 0.94f, 1f, 1f) }
            };

            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(panelRect);
            GUILayout.Space(8f);
            GUILayout.Label("StickLab Training", headerStyle);
            GUILayout.Label($"Stage: {sessionManager.CurrentStageIndex + 1}/{sessionManager.StageCount}  Tier: {sessionManager.DifficultyTier}", bodyStyle);
            GUILayout.Label(sessionManager.CurrentStageName, bodyStyle);
            GUILayout.Label(sessionManager.GetDrillInstructions(), bodyStyle);
            GUILayout.Label($"Accuracy: {sessionManager.CurrentAccuracy:0.0}", bodyStyle);
            GUILayout.Label($"Smoothness: {sessionManager.CurrentSmoothness:0.0}", bodyStyle);
            GUILayout.Label($"Speed: {sessionManager.CurrentSpeedConsistency:0.0}", bodyStyle);
            GUILayout.Label($"Total: {sessionManager.CurrentTotalScore:0.0}", bodyStyle);
            GUILayout.Label($"Hold: {(sessionManager.HoldProgress01 * 100f):0}%", bodyStyle);
            GUILayout.EndArea();
        }
    }
}
