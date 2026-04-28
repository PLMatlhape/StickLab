using UnityEngine;
using StickLab.Analytics;

namespace StickLab.Core
{
    public class GameLoop : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private CursorController cursorController;
        [SerializeField] private TrainingSessionManager sessionManager;
        [SerializeField] private HeatmapRecorder heatmapRecorder;

        private bool recordingActive;

        public void SetReferences(InputHandler newInputHandler, CursorController newCursorController, TrainingSessionManager newSessionManager)
        {
            inputHandler = newInputHandler;
            cursorController = newCursorController;
            sessionManager = newSessionManager;
        }

        public void SetHeatmapRecorder(HeatmapRecorder newHeatmapRecorder)
        {
            heatmapRecorder = newHeatmapRecorder;
        }

        private void Update()
        {
            if (inputHandler == null || cursorController == null || sessionManager == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            HandleHeatmapRecordingState();

            inputHandler.Sample();
            if (!sessionManager.IsRunning)
            {
                return;
            }

            Vector2 stickInput = inputHandler.RightStick;

            cursorController.Tick(stickInput, deltaTime, inputHandler.PrecisionMultiplier);
            sessionManager.Tick(cursorController.Position2D, deltaTime);

            if (heatmapRecorder != null)
            {
                heatmapRecorder.CaptureSample(cursorController.Position2D, sessionManager.CurrentTotalScore, deltaTime);
            }
        }

        private void HandleHeatmapRecordingState()
        {
            if (heatmapRecorder == null || sessionManager == null)
            {
                return;
            }

            if (sessionManager.IsRunning && !recordingActive)
            {
                recordingActive = true;
                heatmapRecorder.BeginRecording();
            }
            else if (!sessionManager.IsRunning && recordingActive)
            {
                recordingActive = false;
                heatmapRecorder.EndRecording();
            }
        }
    }
}
