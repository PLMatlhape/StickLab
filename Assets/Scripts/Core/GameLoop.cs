using UnityEngine;
using StickLab.Analytics;

namespace StickLab.Core
{
    public class GameLoop : MonoBehaviour
    {
        [SerializeField] private InputHandler           inputHandler;
        [SerializeField] private CursorController       cursorController;
        [SerializeField] private TrainingSessionManager sessionManager;
        [SerializeField] private HeatmapRecorder        heatmapRecorder;

        private bool recordingActive;

        public void SetReferences(InputHandler ih, CursorController cc, TrainingSessionManager sm)
        { inputHandler=ih; cursorController=cc; sessionManager=sm; }

        public void SetHeatmapRecorder(HeatmapRecorder hr) => heatmapRecorder = hr;

        private void Update()
        {
            if (inputHandler==null||cursorController==null||sessionManager==null) return;
            inputHandler.Sample();
            SyncHeatmap();
            if (!sessionManager.IsRunning) return;
            float dt = Time.deltaTime;
            cursorController.Tick(inputHandler.RightStick, dt, inputHandler.PrecisionMultiplier);
            sessionManager.Tick(cursorController.Position2D, dt);
            if (heatmapRecorder!=null && heatmapRecorder.IsRecording)
                heatmapRecorder.CaptureSample(cursorController.Position2D, sessionManager.CurrentTotalScore, dt);
        }

        // FIX: recording only starts once IsRunning=true; ends exactly once
        private void SyncHeatmap()
        {
            if (heatmapRecorder==null||sessionManager==null) return;
            if (sessionManager.IsRunning && !recordingActive)
            { recordingActive=true; heatmapRecorder.BeginRecording(); }
            else if (!sessionManager.IsRunning && recordingActive)
            { recordingActive=false; heatmapRecorder.EndRecording(); }
        }
    }
}
