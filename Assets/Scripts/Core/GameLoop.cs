using UnityEngine;

namespace StickLab.Core
{
    public class GameLoop : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private CursorController cursorController;
        [SerializeField] private TrainingSessionManager sessionManager;

        public void SetReferences(InputHandler newInputHandler, CursorController newCursorController, TrainingSessionManager newSessionManager)
        {
            inputHandler = newInputHandler;
            cursorController = newCursorController;
            sessionManager = newSessionManager;
        }

        private void Update()
        {
            if (inputHandler == null || cursorController == null || sessionManager == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            inputHandler.Sample();
            if (!sessionManager.IsRunning)
            {
                return;
            }

            Vector2 stickInput = inputHandler.RightStick;

            cursorController.Tick(stickInput, deltaTime, inputHandler.PrecisionMultiplier);
            sessionManager.Tick(cursorController.Position2D, deltaTime);
        }
    }
}
