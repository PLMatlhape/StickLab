using UnityEngine;

namespace StickLab.Core
{
    public class TrainingMenu : MonoBehaviour
    {
        [SerializeField] private TrainingSessionManager sessionManager;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private Rect menuRect = new Rect(12f, 250f, 360f, 150f);
        [SerializeField] private int fontSize = 18;

        public void SetReferences(TrainingSessionManager newSessionManager, InputHandler newInputHandler)
        {
            sessionManager = newSessionManager;
            inputHandler = newInputHandler;
        }

        private void Update()
        {
            if (sessionManager == null || inputHandler == null)
            {
                return;
            }

            if (!sessionManager.HasStarted && inputHandler.ConfirmPressedThisFrame)
            {
                sessionManager.BeginSession();
            }
            else if (sessionManager.HasStarted && !sessionManager.IsRunning)
            {
                if (inputHandler.ConfirmPressedThisFrame || inputHandler.RetryPressedThisFrame)
                {
                    sessionManager.RestartSession();
                }
            }
            else if (sessionManager.IsRunning && inputHandler.RetryPressedThisFrame)
            {
                sessionManager.StopSession();
            }
        }

        private void OnGUI()
        {
            if (sessionManager == null)
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

            GUI.Box(menuRect, GUIContent.none);
            GUILayout.BeginArea(menuRect);
            GUILayout.Space(8f);
            GUILayout.Label(sessionManager.HasStarted ? "Session Controls" : "StickLab Ready", titleStyle);
            GUILayout.Label(sessionManager.GetDrillInstructions(), bodyStyle);
            GUILayout.Label(sessionManager.ControllerHintText, bodyStyle);

            if (!sessionManager.HasStarted)
            {
                GUILayout.Label("Press Start / Enter to begin.", bodyStyle);
            }
            else if (sessionManager.IsRunning)
            {
                GUILayout.Label("Press Retry / R to stop.", bodyStyle);
            }
            else
            {
                GUILayout.Label("Press Start / Retry to restart.", bodyStyle);
            }

            GUILayout.EndArea();
        }
    }
}
