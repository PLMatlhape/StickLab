using UnityEngine;
using UnityEngine.UI;

namespace StickLab.Analytics
{
    public class AnalyticsDashboardUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HeatmapRecorder heatmapRecorder;
        [SerializeField] private HeatmapReplayController replayController;

        [Header("Layout")]
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F2;

        [Header("Visuals")]
        [SerializeField] private Color backgroundColor = new Color(0.04f, 0.05f, 0.07f, 0.96f);
        [SerializeField] private Color panelColor = new Color(0.10f, 0.12f, 0.17f, 0.98f);
        [SerializeField] private Color accentColor = new Color(0.57f, 0.42f, 1f, 1f);
        [SerializeField] private Color textColor = new Color(0.95f, 0.96f, 1f, 1f);
        [SerializeField] private Color mutedTextColor = new Color(0.73f, 0.78f, 0.88f, 1f);
        [SerializeField] private Font uiFont;

        private Canvas canvas;
        private Text summaryText;
        private Text replayStatusText;
        private RawImage heatmapPreview;
        private Text sampleCountText;
        private Text averageScoreText;
        private Text peakScoreText;
        private Text averageVelocityText;
        private Text durationText;
        private Button renderButton;
        private Button replayButton;
        private Button clearButton;
        private bool visible = true;

        private void Awake()
        {
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (buildOnAwake)
            {
                BuildUi();
            }
        }

        private void Start()
        {
            RefreshUi();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                visible = !visible;
                if (canvas != null)
                {
                    canvas.enabled = visible;
                }
            }

            if (!visible)
            {
                return;
            }

            RefreshUi();
        }

        public void SetReferences(HeatmapRecorder recorder, HeatmapReplayController replay)
        {
            heatmapRecorder = recorder;
            replayController = replay;
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("Analytics Canvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = canvas.GetComponent<RectTransform>();
            RectTransform panel = CreatePanel(root, "AnalyticsPanel", new Vector2(0.28f, 0.12f), new Vector2(0.88f, 0.82f), Vector2.zero, Vector2.zero, panelColor);
            CreateEdge(panel, accentColor);
            CreateBackground(root);

            CreateLabel(panel, "Title", "ANALYTICS / HEATMAP", 28, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -26f), new Vector2(420f, 34f), textColor, TextAnchor.MiddleLeft);
            summaryText = CreateLabel(panel, "Summary", "Track session precision, replay aim paths, and inspect weak areas.", 16, FontStyle.Normal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -60f), new Vector2(700f, 24f), mutedTextColor, TextAnchor.MiddleLeft);

            RectTransform stats = CreatePanel(panel, "Stats", new Vector2(0f, 1f), new Vector2(0.38f, 0f), new Vector2(28f, -110f), new Vector2(360f, 28f), new Color(0.07f, 0.08f, 0.11f, 0.95f));
            CreateEdge(stats);
            sampleCountText = CreateStatRow(stats, "Samples", 0f, 12f);
            averageScoreText = CreateStatRow(stats, "Avg Score", 1f, 42f);
            peakScoreText = CreateStatRow(stats, "Peak Score", 2f, 72f);
            averageVelocityText = CreateStatRow(stats, "Avg Velocity", 3f, 102f);
            durationText = CreateStatRow(stats, "Duration", 4f, 132f);

            RectTransform previewPanel = CreatePanel(panel, "HeatmapPreviewPanel", new Vector2(0.42f, 0.12f), new Vector2(0.95f, 0.82f), new Vector2(28f, 28f), new Vector2(-28f, -108f), new Color(0.07f, 0.08f, 0.11f, 0.98f));
            CreateEdge(previewPanel);
            CreateLabel(previewPanel, "PreviewLabel", "HEATMAP PREVIEW", 16, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(180f, 22f), textColor, TextAnchor.MiddleLeft);

            heatmapPreview = CreateRawImage(previewPanel, new Vector2(14f, 14f), new Vector2(-14f, -50f));
            replayStatusText = CreateLabel(previewPanel, "ReplayStatus", "Replay ready", 14, FontStyle.Normal, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 12f), new Vector2(-14f, 24f), mutedTextColor, TextAnchor.MiddleLeft);

            RectTransform buttonRow = CreateRow(panel, new Vector2(28f, 28f), 540f, 44f);
            renderButton = CreateButton(buttonRow, "Render Heatmap", 0f, 170f, () => BuildHeatmapPreview());
            replayButton = CreateButton(buttonRow, "Replay", 186f, 120f, () => StartReplay());
            clearButton = CreateButton(buttonRow, "Clear", 324f, 120f, ClearAnalytics);
        }

        private void CreateBackground(RectTransform root)
        {
            RectTransform background = CreatePanel(root, "AnalyticsBackground", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, backgroundColor);
            background.SetAsFirstSibling();
        }

        private void RefreshUi()
        {
            if (heatmapRecorder == null)
            {
                summaryText.text = "Analytics recorder not connected.";
                return;
            }

            HeatmapSummary summary = heatmapRecorder.GetSummary();
            sampleCountText.text = $"Samples: {summary.SampleCount}";
            averageScoreText.text = $"Avg Score: {summary.AverageScore:0.0}";
            peakScoreText.text = $"Peak Score: {summary.PeakScore:0.0}";
            averageVelocityText.text = $"Avg Velocity: {summary.AverageVelocity:0.00}";
            durationText.text = $"Duration: {summary.DurationSeconds:0.0}s";

            if (replayController != null)
            {
                replayStatusText.text = replayController.GetStatusText();
            }

            if (heatmapPreview != null && heatmapPreview.texture == null && summary.SampleCount > 0)
            {
                BuildHeatmapPreview();
            }
        }

        private void BuildHeatmapPreview()
        {
            if (heatmapRecorder == null || heatmapPreview == null)
            {
                return;
            }

            Texture2D texture = heatmapRecorder.BuildHeatmapTexture();
            heatmapPreview.texture = texture;
        }

        private void StartReplay()
        {
            if (replayController == null)
            {
                return;
            }

            replayController.LoadFromRecorder();
            replayController.Play();
        }

        private void ClearAnalytics()
        {
            if (heatmapRecorder != null)
            {
                heatmapRecorder.Clear();
            }

            if (replayController != null)
            {
                replayController.Stop();
            }

            if (heatmapPreview != null)
            {
                heatmapPreview.texture = null;
            }
        }

        private Text CreateStatRow(RectTransform parent, string label, float index, float offsetY)
        {
            return CreateLabel(parent, label, $"{label}: 0", 16, FontStyle.Normal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -offsetY), new Vector2(320f, 20f), textColor, TextAnchor.MiddleLeft);
        }

        private RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = panelObject.GetComponent<Image>();
            image.color = color;
            return rect;
        }

        private void CreateEdge(RectTransform panel, Color? edgeColor = null)
        {
            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.color = edgeColor ?? panelColor;
            }
        }

        private Text CreateLabel(RectTransform parent, string name, string text, int fontSize, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 sizeDelta, Color color, TextAnchor alignment)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.sizeDelta = sizeDelta;

            Text label = labelObject.GetComponent<Text>();
            label.font = uiFont;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private RawImage CreateRawImage(RectTransform parent, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject imageObject = new GameObject("HeatmapPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            RawImage raw = imageObject.GetComponent<RawImage>();
            raw.color = Color.white;
            return raw;
        }

        private RectTransform CreateRow(RectTransform parent, Vector2 offsetMin, float width, float height)
        {
            RectTransform row = CreatePanel(parent, "ButtonRow", new Vector2(0f, 0f), new Vector2(0f, 0f), offsetMin, offsetMin + new Vector2(width, height), new Color(0f, 0f, 0f, 0f));
            return row;
        }

        private Button CreateButton(RectTransform parent, string label, float xOffset, float width, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform buttonRect = CreatePanel(parent, label.Replace(' ', '_'), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(xOffset, 0f), new Vector2(xOffset + width, 44f), accentColor);
            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(onClick);
            CreateLabel(buttonRect, "Text", label, 14, FontStyle.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width - 8f, 20f), textColor, TextAnchor.MiddleCenter);
            return button;
        }
    }
}
