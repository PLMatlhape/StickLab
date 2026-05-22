using StickLab.Drills;
using StickLab.Analytics;
using StickLab.Rendering;
using UnityEngine;

namespace StickLab.Core
{
    public class TrainingSceneBootstrap : MonoBehaviour
    {
        [Header("Bootstrap")]
        [SerializeField] private bool  autoCreateScene = true;
        [SerializeField] private Color backgroundColor = new Color(0.05f,0.06f,0.09f,1f);

        [Header("Input")]
        [SerializeField] private UnityEngine.InputSystem.InputActionAsset inputActionsAsset;

        [Header("Tune")]
        [SerializeField] private Vector2 playAreaSize = new Vector2(8f,5f);
        [SerializeField] private float   cameraZ      = -10f;

        // Drill path colors matching target UI
        private static readonly Color ColCircle   = new Color(0.55f, 0.40f, 1.00f, 1f); // purple
        private static readonly Color ColLine     = new Color(0.30f, 0.70f, 1.00f, 1f); // blue
        private static readonly Color ColShape    = new Color(0.40f, 0.90f, 0.65f, 1f); // teal
        private static readonly Color ColInfinity = new Color(0.55f, 0.40f, 1.00f, 1f); // purple
        private static readonly Color ColSpiral   = new Color(1.00f, 0.60f, 0.20f, 1f); // orange
        private static readonly Color ColZigZag   = new Color(0.90f, 0.30f, 0.50f, 1f); // pink
        private static readonly Color ColWave     = new Color(0.30f, 0.85f, 0.85f, 1f); // cyan

        private void Awake()
        {
            if (!autoCreateScene) return;
            EnsureCamera(); EnsureLighting();
            var root = new GameObject("StickLab Systems");

            // ── GameObjects ────────────────────────────────────────────────────
            var cursorGO       = C(root,"Cursor");
            var gameLoopGO     = C(root,"Game Loop");
            var sessionGO      = C(root,"Training Session");
            var dashboardGO    = C(root,"Training Dashboard");
            var hudGO          = C(root,"Training HUD");        // NEW: HUD added
            var heatmapGO      = C(root,"Heatmap Recorder");
            var replayGO       = C(root,"Heatmap Replay");
            var replayPathGO   = C(root,"Replay Path");
            var replayCursorGO = C(root,"Replay Cursor");
            var analyticsGO    = C(root,"Analytics Dashboard");

            // Drill pairs
            var circleGO      = C(root,"Circle Drill");   var circlePathGO    = C(root,"Circle Path");
            var lineGO        = C(root,"Line Drill");     var linePathGO      = C(root,"Line Path");
            var shapeGO       = C(root,"Shape Drill");    var shapePathGO     = C(root,"Shape Path");
            var infGO         = C(root,"Infinity Drill"); var infPathGO       = C(root,"Infinity Path");
            var spiralGO      = C(root,"Spiral Drill");   var spiralPathGO    = C(root,"Spiral Path");
            var zigzagGO      = C(root,"ZigZag Drill");   var zigzagPathGO    = C(root,"ZigZag Path");
            var waveGO        = C(root,"Wave Drill");     var wavePathGO      = C(root,"Wave Path");

            // ── Components ─────────────────────────────────────────────────────
            var inputHandler   = gameLoopGO.AddComponent<InputHandler>();
            var cursorCtrl     = cursorGO.AddComponent<CursorController>();
            var cursorInd      = cursorGO.AddComponent<CursorIndicator>();

            // Wire InputActionAsset if assigned in Inspector
            if (inputActionsAsset != null)
            {
                var field = typeof(InputHandler).GetField("inputActions",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(inputHandler, inputActionsAsset);
            }

            // Drills
            var circleDrill  = circleGO.AddComponent<CircleDrill>();
            var lineDrill    = lineGO.AddComponent<LineDrill>();
            var shapeDrill   = shapeGO.AddComponent<ShapeDrill>();
            var infDrill     = infGO.AddComponent<InfinityLoopDrill>();
            var spiralDrill  = spiralGO.AddComponent<SpiralDrill>();
            var zigzagDrill  = zigzagGO.AddComponent<ZigZagDrill>();
            var waveDrill    = waveGO.AddComponent<WaveRiderDrill>();

            // Path renderers with distinct colors per drill type
            var circleRend   = MakePath(circlePathGO,  ColCircle,   0.28f);
            var lineRend     = MakePath(linePathGO,    ColLine,     0.22f);
            var shapeRend    = MakePath(shapePathGO,   ColShape,    0.22f);
            var infRend      = MakePath(infPathGO,     ColInfinity, 0.24f);
            var spiralRend   = MakePath(spiralPathGO,  ColSpiral,   0.20f);
            var zigzagRend   = MakePath(zigzagPathGO,  ColZigZag,   0.22f);
            var waveRend     = MakePath(wavePathGO,    ColWave,     0.20f);

            var gameLoop     = gameLoopGO.AddComponent<GameLoop>();
            var sessionMgr   = sessionGO.AddComponent<TrainingSessionManager>();
            var persistence  = sessionGO.AddComponent<SessionPersistence>();
            var dashboard    = dashboardGO.AddComponent<TrainingDashboardUI>();

            // NEW: TrainingHud wired here so live metrics show during play
            var hud          = hudGO.AddComponent<TrainingHud>();
            hud.SetSessionManager(sessionMgr);

            var heatmapRec   = heatmapGO.AddComponent<HeatmapRecorder>();
            var heatmapReplay= replayGO.AddComponent<HeatmapReplayController>();
            var analyticsUI  = analyticsGO.AddComponent<AnalyticsDashboardUI>();

            // FIX: PathRenderer [RequireComponent(LineRenderer)] auto-adds LineRenderer
            var replayPathRend = replayPathGO.AddComponent<PathRenderer>();
            var replayLineRend = replayPathGO.GetComponent<LineRenderer>(); // guaranteed non-null
            var replayCursorInd= replayCursorGO.AddComponent<CursorIndicator>();

            // ── Wire paths ─────────────────────────────────────────────────────
            circleDrill.SetPathRenderer(circleRend);
            lineDrill.SetPathRenderer(lineRend);
            shapeDrill.SetPathRenderer(shapeRend);
            infDrill.SetPathRenderer(infRend);
            spiralDrill.SetPathRenderer(spiralRend);
            zigzagDrill.SetPathRenderer(zigzagRend);
            waveDrill.SetPathRenderer(waveRend);

            // ── Wire session ───────────────────────────────────────────────────
            sessionMgr.SetReferences(circleDrill, lineDrill, shapeDrill, cursorCtrl);
            sessionMgr.SetExtendedReferences(infDrill, spiralDrill);
            sessionMgr.SetExtendedReferences2(zigzagDrill, waveDrill);
            persistence.SetSessionManager(sessionMgr);

            // ── Wire game loop ─────────────────────────────────────────────────
            gameLoop.SetReferences(inputHandler, cursorCtrl, sessionMgr);
            gameLoop.SetHeatmapRecorder(heatmapRec);

            // ── Wire dashboard + analytics ─────────────────────────────────────
            dashboard.SetReferences(inputHandler, cursorCtrl, sessionMgr);
            analyticsUI.SetReferences(heatmapRec, heatmapReplay);

            // ── Play area ──────────────────────────────────────────────────────
            var rect = new Rect(
                -playAreaSize.x * 0.5f, -playAreaSize.y * 0.5f,
                 playAreaSize.x,         playAreaSize.y);
            cursorCtrl.SetPlayArea(rect);
            heatmapRec.SetPlayArea(rect);

            // ── Replay ─────────────────────────────────────────────────────────
            heatmapReplay.SetSource(heatmapRec);
            replayPathRend.SetThickness(0.04f);
            replayPathRend.SetColor(new Color(0.57f, 0.42f, 1f, 0.7f));
            heatmapReplay.SetReplayCursor(replayCursorGO.transform);
            heatmapReplay.SetReplayPathRenderer(replayLineRend);

            // ── Visuals ────────────────────────────────────────────────────────
            cursorInd.Configure(new Color(1f, 1f, 1f, 0.9f), 0.12f, 24);
            replayCursorInd.Configure(new Color(1f, 0.86f, 0.25f, 1f), 0.10f, 20);

            // ── All drills inactive at start ───────────────────────────────────
            foreach (var go in new[]{ circleGO, lineGO, shapeGO, infGO,
                                       spiralGO, zigzagGO, waveGO, replayCursorGO })
                go.SetActive(false);

            cursorCtrl.SetPosition(Vector2.zero);
        }

        // Creates a PathRenderer with correct material and color so paths are VISIBLE
        private static PathRenderer MakePath(GameObject go, Color color, float thickness)
        {
            var pr = go.AddComponent<PathRenderer>();
            pr.SetColor(color);
            pr.SetThickness(thickness);
            return pr;
        }

        private static GameObject C(GameObject p, string n)
        {
            var g = new GameObject(n);
            g.transform.SetParent(p.transform, false);
            return g;
        }

        private void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.orthographic       = true;
            cam.orthographicSize   = 3f;
            cam.backgroundColor    = backgroundColor;
            cam.transform.position = new Vector3(0f, 0f, cameraZ);
            cam.clearFlags         = CameraClearFlags.SolidColor;
        }

        private static void EnsureLighting() => RenderSettings.ambientLight = Color.white;
    }
}
