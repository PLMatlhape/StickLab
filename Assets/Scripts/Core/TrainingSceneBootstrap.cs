using StickLab.Drills;
using StickLab.Analytics;
using StickLab.Rendering;
using UnityEngine;

namespace StickLab.Core
{
    public class TrainingSceneBootstrap : MonoBehaviour
    {
        [Header("Bootstrap")]
        [SerializeField] private bool autoCreateScene = true;
        [SerializeField] private Color backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);

        [Header("Tune")]
        [SerializeField] private Vector2 playAreaSize = new Vector2(8f, 5f);
        [SerializeField] private float cameraZ = -10f;

        private void Awake()
        {
            if (!autoCreateScene)
            {
                return;
            }

            Camera mainCamera = EnsureCamera();
            EnsureLighting(mainCamera);

            GameObject systemsRoot = new GameObject("StickLab Systems");
            GameObject cursorObject = new GameObject("Cursor");
            GameObject pathObject = new GameObject("Circle Path");
            GameObject lineObject = new GameObject("Line Drill");
            GameObject shapeObject = new GameObject("Shape Drill");
            GameObject drillObject = new GameObject("Circle Drill");
            GameObject gameLoopObject = new GameObject("Game Loop");
            GameObject sessionObject = new GameObject("Training Session");
            GameObject dashboardObject = new GameObject("Training Dashboard");
            GameObject replayPathObject = new GameObject("Replay Path");
            GameObject replayCursorObject = new GameObject("Replay Cursor");
            GameObject analyticsObject = new GameObject("Analytics Dashboard");
            GameObject heatmapObject = new GameObject("Heatmap Recorder");
            GameObject replayObject = new GameObject("Heatmap Replay");

            cursorObject.transform.SetParent(systemsRoot.transform, false);
            pathObject.transform.SetParent(systemsRoot.transform, false);
            lineObject.transform.SetParent(systemsRoot.transform, false);
            shapeObject.transform.SetParent(systemsRoot.transform, false);
            drillObject.transform.SetParent(systemsRoot.transform, false);
            gameLoopObject.transform.SetParent(systemsRoot.transform, false);
            sessionObject.transform.SetParent(systemsRoot.transform, false);
            dashboardObject.transform.SetParent(systemsRoot.transform, false);
            replayPathObject.transform.SetParent(systemsRoot.transform, false);
            replayCursorObject.transform.SetParent(systemsRoot.transform, false);
            analyticsObject.transform.SetParent(systemsRoot.transform, false);
            heatmapObject.transform.SetParent(systemsRoot.transform, false);
            replayObject.transform.SetParent(systemsRoot.transform, false);

            InputHandler inputHandler = gameLoopObject.AddComponent<InputHandler>();
            CursorController cursorController = cursorObject.AddComponent<CursorController>();
            CursorIndicator cursorIndicator = cursorObject.AddComponent<CursorIndicator>();
            PathRenderer pathRenderer = pathObject.AddComponent<PathRenderer>();
            lineObject.AddComponent<PathRenderer>();
            shapeObject.AddComponent<PathRenderer>();
            CircleDrill circleDrill = drillObject.AddComponent<CircleDrill>();
            LineDrill lineDrill = lineObject.AddComponent<LineDrill>();
            ShapeDrill shapeDrill = shapeObject.AddComponent<ShapeDrill>();
            GameLoop gameLoop = gameLoopObject.AddComponent<GameLoop>();
            TrainingSessionManager sessionManager = sessionObject.AddComponent<TrainingSessionManager>();
            TrainingDashboardUI dashboard = dashboardObject.AddComponent<TrainingDashboardUI>();
            HeatmapRecorder heatmapRecorder = heatmapObject.AddComponent<HeatmapRecorder>();
            HeatmapReplayController heatmapReplay = replayObject.AddComponent<HeatmapReplayController>();
            AnalyticsDashboardUI analyticsDashboard = analyticsObject.AddComponent<AnalyticsDashboardUI>();
            PathRenderer replayPathRenderer = replayPathObject.AddComponent<PathRenderer>();
            CursorIndicator replayCursorIndicator = replayCursorObject.AddComponent<CursorIndicator>();

            circleDrill.SetPathRenderer(pathRenderer);
            lineDrill.SetPathRenderer(lineObject.GetComponent<PathRenderer>());
            shapeDrill.SetPathRenderer(shapeObject.GetComponent<PathRenderer>());
            sessionManager.SetReferences(circleDrill, lineDrill, shapeDrill, cursorController);
            gameLoop.SetReferences(inputHandler, cursorController, sessionManager);
            gameLoop.SetHeatmapRecorder(heatmapRecorder);
            dashboard.SetReferences(inputHandler, cursorController, sessionManager);
            analyticsDashboard.SetReferences(heatmapRecorder, heatmapReplay);

            heatmapRecorder.SetPlayArea(new Rect(-playAreaSize.x * 0.5f, -playAreaSize.y * 0.5f, playAreaSize.x, playAreaSize.y));
            heatmapReplay.SetSource(heatmapRecorder);
            replayPathRenderer.SetThickness(0.05f);
            replayPathRenderer.SetColor(new Color(0.57f, 0.42f, 1f, 0.8f));

            replayCursorIndicator.Configure(new Color(1f, 0.86f, 0.25f, 1f), 0.10f, 20);

            cursorController.SetPlayArea(new Rect(-playAreaSize.x * 0.5f, -playAreaSize.y * 0.5f, playAreaSize.x, playAreaSize.y));

            cursorIndicator.Configure(new Color(0.95f, 0.95f, 1f, 1f), 0.12f, 24);

            lineDrill.gameObject.SetActive(false);
            shapeDrill.gameObject.SetActive(false);
            circleDrill.gameObject.SetActive(false);
            cursorController.SetPosition(circleDrill.StartPosition);

            replayPathObject.transform.position = Vector3.zero;
            replayCursorObject.transform.position = circleDrill.StartPosition;
            heatmapReplay.SetReplayCursor(replayCursorObject.transform);
            heatmapReplay.SetReplayPathRenderer(replayPathRenderer.GetComponent<LineRenderer>());
        }

        private Camera EnsureCamera()
        {
            Camera camera = Camera.main;

            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            camera.orthographic = true;
            camera.backgroundColor = backgroundColor;
            camera.transform.position = new Vector3(0f, 0f, cameraZ);
            camera.clearFlags = CameraClearFlags.SolidColor;
            return camera;
        }

        private void EnsureLighting(Camera camera)
        {
            if (camera != null)
            {
                RenderSettings.ambientLight = Color.white;
            }
        }
    }
}
