using StickLab.Drills;
using StickLab.Analytics;
using StickLab.Rendering;
using UnityEngine;

namespace StickLab.Core
{
    public class TrainingSceneBootstrap : MonoBehaviour
    {
        [Header("Bootstrap")]
        [SerializeField] private bool  autoCreateScene  = true;
        [SerializeField] private Color backgroundColor  = new Color(0.06f, 0.07f, 0.09f, 1f);

        [Header("Tune")]
        [SerializeField] private Vector2 playAreaSize = new Vector2(8f, 5f);
        [SerializeField] private float   cameraZ      = -10f;

        private void Awake()
        {
            if (!autoCreateScene) return;

            // ── Camera ────────────────────────────────────────────────────────
            Camera cam = EnsureCamera();
            EnsureLighting();

            // ── Root ──────────────────────────────────────────────────────────
            var root = new GameObject("StickLab Systems");

            // ── Create GameObjects ────────────────────────────────────────────
            var cursorGO        = Child(root, "Cursor");
            var circlePathGO    = Child(root, "Circle Path");
            var linePathGO      = Child(root, "Line Path");
            var shapePathGO     = Child(root, "Shape Path");
            var infinityPathGO  = Child(root, "Infinity Path");
            var spiralPathGO    = Child(root, "Spiral Path");
            var circleDrillGO   = Child(root, "Circle Drill");
            var lineDrillGO     = Child(root, "Line Drill");
            var shapeDrillGO    = Child(root, "Shape Drill");
            var infinityDrillGO = Child(root, "Infinity Drill");
            var spiralDrillGO   = Child(root, "Spiral Drill");
            var gameLoopGO      = Child(root, "Game Loop");
            var sessionGO       = Child(root, "Training Session");
            var dashboardGO     = Child(root, "Training Dashboard");
            var heatmapGO       = Child(root, "Heatmap Recorder");
            var replayGO        = Child(root, "Heatmap Replay");
            var replayPathGO    = Child(root, "Replay Path");
            var replayCursorGO  = Child(root, "Replay Cursor");
            var analyticsGO     = Child(root, "Analytics Dashboard");

            // ── Add components ─────────────────────────────────────────────────
            var inputHandler     = gameLoopGO.AddComponent<InputHandler>();
            var cursorCtrl       = cursorGO.AddComponent<CursorController>();
            var cursorIndicator  = cursorGO.AddComponent<CursorIndicator>();

            var circleRenderer   = circlePathGO.AddComponent<PathRenderer>();
            var lineRenderer     = linePathGO.AddComponent<PathRenderer>();
            var shapeRenderer    = shapePathGO.AddComponent<PathRenderer>();
            var infinityRenderer = infinityPathGO.AddComponent<PathRenderer>();
            var spiralRenderer   = spiralPathGO.AddComponent<PathRenderer>();

            var circleDrill      = circleDrillGO.AddComponent<CircleDrill>();
            var lineDrill        = lineDrillGO.AddComponent<LineDrill>();
            var shapeDrill       = shapeDrillGO.AddComponent<ShapeDrill>();
            var infinityDrill    = infinityDrillGO.AddComponent<InfinityLoopDrill>();
            var spiralDrill      = spiralDrillGO.AddComponent<SpiralDrill>();

            var gameLoop         = gameLoopGO.AddComponent<GameLoop>();
            var sessionMgr       = sessionGO.AddComponent<TrainingSessionManager>();
            var dashboard        = dashboardGO.AddComponent<TrainingDashboardUI>();

            var heatmapRecorder  = heatmapGO.AddComponent<HeatmapRecorder>();
            var heatmapReplay    = replayGO.AddComponent<HeatmapReplayController>();
            var analyticsUI      = analyticsGO.AddComponent<AnalyticsDashboardUI>();
            var replayPath       = replayPathGO.AddComponent<PathRenderer>();
            var replayCursor     = replayCursorGO.AddComponent<CursorIndicator>();

            // ── Wire path renderers ───────────────────────────────────────────
            circleDrill.SetPathRenderer(circleRenderer);
            lineDrill.SetPathRenderer(lineRenderer);
            shapeDrill.SetPathRenderer(shapeRenderer);
            infinityDrill.SetPathRenderer(infinityRenderer);
            spiralDrill.SetPathRenderer(spiralRenderer);

            // ── Wire session manager ──────────────────────────────────────────
            sessionMgr.SetReferences(circleDrill, lineDrill, shapeDrill, cursorCtrl);
            sessionMgr.SetExtendedReferences(infinityDrill, spiralDrill);

            // ── Wire game loop ─────────────────────────────────────────────────
            gameLoop.SetReferences(inputHandler, cursorCtrl, sessionMgr);
            gameLoop.SetHeatmapRecorder(heatmapRecorder);

            // ── Wire dashboard ────────────────────────────────────────────────
            dashboard.SetReferences(inputHandler, cursorCtrl, sessionMgr);

            // ── Wire analytics ────────────────────────────────────────────────
            analyticsUI.SetReferences(heatmapRecorder, heatmapReplay);

            // ── Configure play area ────────────────────────────────────────────
            var playRect = new Rect(-playAreaSize.x * 0.5f, -playAreaSize.y * 0.5f, playAreaSize.x, playAreaSize.y);
            cursorCtrl.SetPlayArea(playRect);
            heatmapRecorder.SetPlayArea(playRect);

            // ── Configure replay ──────────────────────────────────────────────
            heatmapReplay.SetSource(heatmapRecorder);
            replayPath.SetThickness(0.05f);
            replayPath.SetColor(new Color(0.57f, 0.42f, 1f, 0.8f));
            heatmapReplay.SetReplayCursor(replayCursorGO.transform);
            heatmapReplay.SetReplayPathRenderer(replayPathGO.GetComponent<LineRenderer>());

            // ── Configure cursor visuals ──────────────────────────────────────
            cursorIndicator.Configure(new Color(0.95f, 0.95f, 1f, 1f), 0.12f, 24);
            replayCursor.Configure(new Color(1f, 0.86f, 0.25f, 1f), 0.10f, 20);

            // ── All drills start inactive ─────────────────────────────────────
            circleDrillGO.SetActive(false);
            lineDrillGO.SetActive(false);
            shapeDrillGO.SetActive(false);
            infinityDrillGO.SetActive(false);
            spiralDrillGO.SetActive(false);

            // ── Cursor start position ─────────────────────────────────────────
            cursorCtrl.SetPosition(Vector2.zero);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private Camera EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.orthographic     = true;
            cam.backgroundColor  = backgroundColor;
            cam.transform.position = new Vector3(0f, 0f, cameraZ);
            cam.clearFlags       = CameraClearFlags.SolidColor;
            return cam;
        }

        private static void EnsureLighting() => RenderSettings.ambientLight = Color.white;
    }
}
