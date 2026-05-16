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
        [SerializeField] private Color backgroundColor = new Color(0.06f,0.07f,0.09f,1f);
        [Header("Tune")]
        [SerializeField] private Vector2 playAreaSize = new Vector2(8f,5f);
        [SerializeField] private float   cameraZ = -10f;

        private void Awake()
        {
            if (!autoCreateScene) return;
            EnsureCamera(); EnsureLighting();
            var root = new GameObject("StickLab Systems");

            // GameObjects
            var cursorGO        = C(root,"Cursor");
            var gameLoopGO      = C(root,"Game Loop");
            var sessionGO       = C(root,"Training Session");
            var dashboardGO     = C(root,"Training Dashboard");
            var heatmapGO       = C(root,"Heatmap Recorder");
            var replayGO        = C(root,"Heatmap Replay");
            var replayPathGO    = C(root,"Replay Path");
            var replayCursorGO  = C(root,"Replay Cursor");
            var analyticsGO     = C(root,"Analytics Dashboard");

            // Drill GOs + path GOs
            var circleGO   = C(root,"Circle Drill");   var circlePathGO   = C(root,"Circle Path");
            var lineGO     = C(root,"Line Drill");     var linePathGO     = C(root,"Line Path");
            var shapeGO    = C(root,"Shape Drill");    var shapePathGO    = C(root,"Shape Path");
            var infGO      = C(root,"Infinity Drill"); var infPathGO      = C(root,"Infinity Path");
            var spiralGO   = C(root,"Spiral Drill");   var spiralPathGO   = C(root,"Spiral Path");
            var zigzagGO   = C(root,"ZigZag Drill");   var zigzagPathGO   = C(root,"ZigZag Path");
            var waveGO     = C(root,"Wave Drill");     var wavePathGO     = C(root,"Wave Path");

            // Components
            var inputHandler    = gameLoopGO.AddComponent<InputHandler>();
            var cursorCtrl      = cursorGO.AddComponent<CursorController>();
            var cursorInd       = cursorGO.AddComponent<CursorIndicator>();

            var circleDrill     = circleGO.AddComponent<CircleDrill>();
            var lineDrill       = lineGO.AddComponent<LineDrill>();
            var shapeDrill      = shapeGO.AddComponent<ShapeDrill>();
            var infDrill        = infGO.AddComponent<InfinityLoopDrill>();
            var spiralDrill     = spiralGO.AddComponent<SpiralDrill>();
            var zigzagDrill     = zigzagGO.AddComponent<ZigZagDrill>();
            var waveDrill       = waveGO.AddComponent<WaveRiderDrill>();

            var circleRend      = circlePathGO.AddComponent<PathRenderer>();
            var lineRend        = linePathGO.AddComponent<PathRenderer>();
            var shapeRend       = shapePathGO.AddComponent<PathRenderer>();
            var infRend         = infPathGO.AddComponent<PathRenderer>();
            var spiralRend      = spiralPathGO.AddComponent<PathRenderer>();
            var zigzagRend      = zigzagPathGO.AddComponent<PathRenderer>();
            var waveRend        = wavePathGO.AddComponent<PathRenderer>();

            var gameLoop        = gameLoopGO.AddComponent<GameLoop>();
            var sessionMgr      = sessionGO.AddComponent<TrainingSessionManager>();
            var dashboard       = dashboardGO.AddComponent<TrainingDashboardUI>();
            var heatmapRec      = heatmapGO.AddComponent<HeatmapRecorder>();
            var heatmapReplay   = replayGO.AddComponent<HeatmapReplayController>();
            var analyticsUI     = analyticsGO.AddComponent<AnalyticsDashboardUI>();

            // FIX: PathRenderer [RequireComponent(LineRenderer)] - get LineRenderer after AddComponent
            var replayPathRend  = replayPathGO.AddComponent<PathRenderer>();
            var replayLineRend  = replayPathGO.GetComponent<LineRenderer>();
            var replayCursorInd = replayCursorGO.AddComponent<CursorIndicator>();

            // Wire paths
            circleDrill.SetPathRenderer(circleRend);
            lineDrill.SetPathRenderer(lineRend);
            shapeDrill.SetPathRenderer(shapeRend);
            infDrill.SetPathRenderer(infRend);
            spiralDrill.SetPathRenderer(spiralRend);
            zigzagDrill.SetPathRenderer(zigzagRend);
            waveDrill.SetPathRenderer(waveRend);

            // Wire session
            sessionMgr.SetReferences(circleDrill, lineDrill, shapeDrill, cursorCtrl);
            sessionMgr.SetExtendedReferences(infDrill, spiralDrill);
            sessionMgr.SetExtendedReferences2(zigzagDrill, waveDrill);

            // Wire game loop
            gameLoop.SetReferences(inputHandler, cursorCtrl, sessionMgr);
            gameLoop.SetHeatmapRecorder(heatmapRec);

            // Wire dashboard + analytics
            dashboard.SetReferences(inputHandler, cursorCtrl, sessionMgr);
            analyticsUI.SetReferences(heatmapRec, heatmapReplay);

            // Play area
            var rect = new Rect(-playAreaSize.x*.5f,-playAreaSize.y*.5f,playAreaSize.x,playAreaSize.y);
            cursorCtrl.SetPlayArea(rect);
            heatmapRec.SetPlayArea(rect);

            // Replay
            heatmapReplay.SetSource(heatmapRec);
            replayPathRend.SetThickness(0.05f);
            replayPathRend.SetColor(new Color(0.57f,0.42f,1f,0.8f));
            heatmapReplay.SetReplayCursor(replayCursorGO.transform);
            heatmapReplay.SetReplayPathRenderer(replayLineRend);

            // Visuals
            cursorInd.Configure(new Color(0.95f,0.95f,1f,1f),0.12f,24);
            replayCursorInd.Configure(new Color(1f,0.86f,0.25f,1f),0.10f,20);

            // All drills off at start
            foreach (var go in new[]{circleGO,lineGO,shapeGO,infGO,spiralGO,zigzagGO,waveGO,replayCursorGO})
                go.SetActive(false);

            cursorCtrl.SetPosition(Vector2.zero);
        }

        static GameObject C(GameObject p,string n){var g=new GameObject(n);g.transform.SetParent(p.transform,false);return g;}

        void EnsureCamera(){
            var cam=Camera.main;
            if(cam==null){var g=new GameObject("Main Camera");cam=g.AddComponent<Camera>();cam.tag="MainCamera";}
            cam.orthographic=true; cam.backgroundColor=backgroundColor;
            cam.transform.position=new Vector3(0,0,cameraZ); cam.clearFlags=CameraClearFlags.SolidColor;
        }
        static void EnsureLighting()=>RenderSettings.ambientLight=Color.white;
    }
}
