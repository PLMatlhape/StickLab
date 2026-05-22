using UnityEngine;
using UnityEngine.UI;

namespace StickLab.Analytics
{
    public class AnalyticsDashboardUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HeatmapRecorder        heatmapRecorder;
        [SerializeField] private HeatmapReplayController replayController;

        [Header("Layout")]
        [SerializeField] private bool    buildOnAwake = true;
        [SerializeField] private KeyCode toggleKey    = KeyCode.F2;

        [Header("Visuals")]
        [SerializeField] private Color backgroundColor = new Color(0.04f, 0.05f, 0.07f, 0.96f);
        [SerializeField] private Color panelColor      = new Color(0.10f, 0.12f, 0.17f, 0.98f);
        [SerializeField] private Color accentColor     = new Color(0.57f, 0.42f, 1f,   1f);
        [SerializeField] private Color textColor       = new Color(0.95f, 0.96f, 1f,   1f);
        [SerializeField] private Color mutedColor      = new Color(0.73f, 0.78f, 0.88f,1f);
        [SerializeField] private Font  uiFont;

        // ── UI refs ───────────────────────────────────────────────────────────
        private Canvas   canvas;
        private Text     sampleCountText, avgScoreText, peakScoreText, avgVelocityText, durationText;
        private Text     replayStatusText;
        private RawImage heatmapPreview;
        private bool     visible = false;  // FIX: hidden by default, F2 to toggle

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (buildOnAwake) BuildUi();
        }

        private void Start()
        {
            // FIX: hide on start — press F2 to show analytics
            if (canvas != null) canvas.enabled = false;
            RefreshUi();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                visible = !visible;
                if (canvas != null) canvas.enabled = visible;
            }
            if (visible) RefreshUi();
        }

        public void SetReferences(HeatmapRecorder rec, HeatmapReplayController replay)
        {
            heatmapRecorder  = rec;
            replayController = replay;
        }

        // ── UI construction ───────────────────────────────────────────────────
        private void BuildUi()
        {
            var go = new GameObject("Analytics Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;  // above game, below nothing critical
            go.AddComponent<GraphicRaycaster>();

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;

            var root = canvas.GetComponent<RectTransform>();

            // Background (non-raycast)
            var bg = MakePanel(root, "BG", new Vector2(0.28f,0.12f), new Vector2(0.88f,0.82f), Vector2.zero, Vector2.zero, backgroundColor);
            bg.GetComponent<Image>().raycastTarget = false;

            // Main panel
            var panel = MakePanel(root, "Panel", new Vector2(0.28f,0.12f), new Vector2(0.88f,0.82f), Vector2.zero, Vector2.zero, panelColor);

            MakeLabel(panel, "Title", "ANALYTICS / HEATMAP", 26, FontStyle.Bold,
                new Vector2(0,1), new Vector2(0,1), new Vector2(24,-22), new Vector2(400,32), textColor, TextAnchor.MiddleLeft);

            // Stats column
            var stats = MakePanel(panel,"Stats",new Vector2(0,1),new Vector2(0.38f,0),new Vector2(24,-80),new Vector2(-24,-24),new Color(0.07f,0.08f,0.11f,.95f));
            stats.GetComponent<Image>().raycastTarget = false; // FIX: was blocking clicks

            sampleCountText  = MakeStatRow(stats, "Samples",      0);
            avgScoreText     = MakeStatRow(stats, "Avg Score",     1);
            peakScoreText    = MakeStatRow(stats, "Peak Score",    2);
            avgVelocityText  = MakeStatRow(stats, "Avg Velocity",  3);
            durationText     = MakeStatRow(stats, "Duration",      4);

            // Heatmap preview
            var preview = MakePanel(panel,"Preview",new Vector2(0.42f,0.12f),new Vector2(0.95f,0.88f),new Vector2(24,24),new Vector2(-24,-80),new Color(0.07f,0.08f,0.11f,.98f));
            preview.GetComponent<Image>().raycastTarget = false; // FIX

            MakeLabel(preview,"PreviewLbl","HEATMAP PREVIEW",15,FontStyle.Bold,
                new Vector2(0,1),new Vector2(0,1),new Vector2(12,-12),new Vector2(180,20),textColor,TextAnchor.MiddleLeft);

            var imgGO = new GameObject("HeatmapImg", typeof(RectTransform));
            imgGO.transform.SetParent(preview, false);
            var imgRT = imgGO.GetComponent<RectTransform>();
            imgRT.anchorMin = Vector2.zero; imgRT.anchorMax = Vector2.one;
            imgRT.offsetMin = new Vector2(12,12); imgRT.offsetMax = new Vector2(-12,-42);
            heatmapPreview = imgGO.AddComponent<RawImage>();
            heatmapPreview.color = Color.white;

            replayStatusText = MakeLabel(preview,"ReplayStatus","Replay ready",13,FontStyle.Normal,
                new Vector2(0,0),new Vector2(1,0),new Vector2(12,10),new Vector2(-12,22),mutedColor,TextAnchor.MiddleLeft);

            // Buttons row
            var btnRow = MakePanel(panel,"Buttons",new Vector2(0,0),new Vector2(1,0),new Vector2(24,16),new Vector2(-24,60),new Color(0,0,0,0));
            btnRow.GetComponent<Image>().raycastTarget = false; // FIX

            MakeButton(btnRow, "Render",  "Render Heatmap", new Vector2(0,0),   new Vector2(170,40), BuildHeatmapPreview);
            MakeButton(btnRow, "Replay",  "Replay",         new Vector2(178,0), new Vector2(120,40), StartReplay);
            MakeButton(btnRow, "Clear",   "Clear",          new Vector2(306,0), new Vector2(120,40), ClearAnalytics);
        }

        // ── Refresh ───────────────────────────────────────────────────────────
        private void RefreshUi()
        {
            if (heatmapRecorder == null) return;

            var s = heatmapRecorder.GetSummary();
            if (sampleCountText != null) sampleCountText.text = $"Samples: {s.SampleCount}";
            if (avgScoreText    != null) avgScoreText.text    = $"Avg Score: {s.AverageScore:0.0}";
            if (peakScoreText   != null) peakScoreText.text   = $"Peak Score: {s.PeakScore:0.0}";
            if (avgVelocityText != null) avgVelocityText.text = $"Avg Velocity: {s.AverageVelocity:0.00}";
            if (durationText    != null) durationText.text    = $"Duration: {s.DurationSeconds:0.0}s";

            if (replayStatusText != null && replayController != null)
                replayStatusText.text = replayController.GetStatusText();

            // Auto-render heatmap when samples arrive
            if (heatmapPreview != null && heatmapPreview.texture == null && s.SampleCount > 0)
                BuildHeatmapPreview();
        }

        private void BuildHeatmapPreview()
        {
            if (heatmapRecorder == null || heatmapPreview == null) return;
            heatmapPreview.texture = heatmapRecorder.BuildHeatmapTexture();
        }

        private void StartReplay()
        {
            if (replayController == null) return;
            replayController.LoadFromRecorder();
            replayController.Play();
        }

        private void ClearAnalytics()
        {
            heatmapRecorder?.Clear();
            replayController?.Stop();
            if (heatmapPreview != null) heatmapPreview.texture = null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private Text MakeStatRow(RectTransform parent, string label, int row)
        {
            float y = -16f - row * 28f;
            return MakeLabel(parent, label, $"{label}: —", 15, FontStyle.Normal,
                new Vector2(0,1), new Vector2(0,1), new Vector2(12, y), new Vector2(300, 22),
                textColor, TextAnchor.MiddleLeft);
        }

        private RectTransform MakePanel(RectTransform parent, string name,
            Vector2 ancMin, Vector2 ancMax, Vector2 oMin, Vector2 oMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = oMin;   rt.offsetMax = oMax;
            go.AddComponent<Image>().color = color;
            return rt;
        }

        private Text MakeLabel(RectTransform parent, string name, string text,
            int size, FontStyle style, Vector2 ancMin, Vector2 ancMax,
            Vector2 oMin, Vector2 oMax, Color color, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = oMin;   rt.offsetMax = oMax;
            var t = go.AddComponent<Text>();
            t.font = uiFont; t.text = text; t.fontSize = size; t.fontStyle = style;
            t.color = color; t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            return t;
        }

        private void MakeButton(RectTransform parent, string name, string label,
            Vector2 oMin, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var r = MakePanel(parent, name,
                new Vector2(0,0), new Vector2(0,0), oMin, oMin + size, accentColor);
            var btn = r.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            MakeLabel(r, "T", label, 13, FontStyle.Bold,
                new Vector2(.5f,.5f), new Vector2(.5f,.5f),
                Vector2.zero, new Vector2(size.x - 8f, 20f), textColor, TextAnchor.MiddleCenter);
        }
    }
}
