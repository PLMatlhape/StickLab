using System.Collections.Generic;
using StickLab.Drills;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StickLab.Core
{
    public class TrainingDashboardUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private CursorController cursorController;
        [SerializeField] private TrainingSessionManager sessionManager;

        [Header("Visuals")]
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.06f, 0.09f, 1f);
        [SerializeField] private Color panelColor = new Color(0.10f, 0.12f, 0.17f, 0.92f);
        [SerializeField] private Color panelEdgeColor = new Color(0.20f, 0.24f, 0.35f, 1f);
        [SerializeField] private Color accentColor = new Color(0.57f, 0.42f, 1f, 1f);
        [SerializeField] private Color textColor = new Color(0.95f, 0.96f, 1f, 1f);
        [SerializeField] private Color mutedTextColor = new Color(0.75f, 0.79f, 0.88f, 1f);
        [SerializeField] private Font uiFont;

        [Header("Layout")]
        [SerializeField] private bool buildOnAwake = true;

        private Canvas canvas;
        private Text connectionText;
        private Text profileText;
        private Text stageTitleText;
        private Text stageSubtitleText;
        private Text instructionText;
        private Text tipText;
        private Text scoreText;
        private Text accuracyText;
        private Text smoothnessText;
        private Text speedText;
        private Text deviationText;
        private Text precisionText;
        private Text controlsText;
        private Image accuracyFill;
        private Image smoothnessFill;
        private Image speedFill;
        private Image deviationFill;
        private readonly Dictionary<string, Text> bindingLabels = new Dictionary<string, Text>();
        private readonly List<Button> stageButtons = new List<Button>();
        private readonly List<(string actionName, Text label)> rebindRows = new List<(string actionName, Text label)>();
        private bool isRebinding;
        private InputActionRebindingExtensions.RebindingOperation activeRebind;
        private float currentDeadzone = 0.18f;
        private float currentSensitivity = 8f;

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
            LoadBindingOverrides();
            ApplyDeadzone();
            ApplySensitivity();
            RefreshBindingLabels();
        }

        private void Update()
        {
            if (inputHandler == null || sessionManager == null)
            {
                return;
            }

            if (inputHandler.ConfirmPressedThisFrame && !sessionManager.IsRunning)
            {
                sessionManager.BeginSession();
            }
            else if (inputHandler.RetryPressedThisFrame && sessionManager.IsRunning)
            {
                sessionManager.StopSession();
            }
            else if (inputHandler.ConfirmPressedThisFrame && sessionManager.HasStarted && !sessionManager.IsRunning)
            {
                sessionManager.RestartSession();
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (canvas != null)
                {
                    canvas.enabled = !canvas.enabled;
                }
            }
        }

        private void LateUpdate()
        {
            RefreshUi();
        }

        public void SetReferences(InputHandler newInputHandler, CursorController newCursorController, TrainingSessionManager newSessionManager)
        {
            inputHandler = newInputHandler;
            cursorController = newCursorController;
            sessionManager = newSessionManager;
        }

        private void BuildUi()
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject("Training Canvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = canvas.GetComponent<RectTransform>();

            CreatePanel(root, "Background", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, backgroundColor);

            CreateSidebar(root);
            CreateTopBar(root);
            CreateCenterPanel(root);
            CreateRightPanel(root);
            CreateBottomStrip(root);
        }

        private void CreateSidebar(RectTransform root)
        {
            RectTransform sidebar = CreatePanel(root, "Sidebar", new Vector2(0f, 0f), new Vector2(0.18f, 1f), new Vector2(24f, 24f), new Vector2(-24f, -24f), panelColor);
            CreateEdge(sidebar);

            CreateLabel(sidebar, "Title", "AIM LAB", 30, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -28f), new Vector2(160f, 38f), textColor, TextAnchor.MiddleLeft);
            CreateLabel(sidebar, "Subtitle", "FOR CONTROLLER", 18, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -62f), new Vector2(180f, 28f), accentColor, TextAnchor.MiddleLeft);

            string[] items = { "HOME", "DRILLS", "CUSTOM", "RESULTS", "ANALYTICS", "LEADERBOARD", "SETTINGS" };
            float startY = -135f;
            for (int i = 0; i < items.Length; i++)
            {
                float y = startY - (i * 58f);
                bool selected = i == 0;
                CreateNavItem(sidebar, items[i], selected, y);
            }

            RectTransform controllerCard = CreatePanel(sidebar, "ControllerCard", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 16f), new Vector2(-16f, 155f), new Color(0.08f, 0.10f, 0.14f, 1f));
            CreateEdge(controllerCard);
            CreateLabel(controllerCard, "ControllerHeader", "CONTROLLER", 18, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(140f, 26f), textColor, TextAnchor.MiddleLeft);
            CreateLabel(controllerCard, "Connection", "Disconnected", 16, FontStyle.Normal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -54f), new Vector2(160f, 24f), mutedTextColor, TextAnchor.MiddleLeft);
            CreateLabel(controllerCard, "Battery", "LT = ADS • RT = Fire", 14, FontStyle.Normal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -82f), new Vector2(190f, 24f), mutedTextColor, TextAnchor.MiddleLeft);

            RectTransform settingsCard = CreatePanel(sidebar, "QuickSettings", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 16f), new Vector2(-16f, 18f), new Color(0.08f, 0.10f, 0.14f, 1f));
            CreateEdge(settingsCard);
            CreateLabel(settingsCard, "SettingsHeader", "QUICK SETTINGS", 18, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(180f, 24f), textColor, TextAnchor.MiddleLeft);
            CreateDeadzoneRow(settingsCard, new Vector2(16f, -54f));
            CreateSensitivityRow(settingsCard, new Vector2(16f, -96f));
        }

        private void CreateTopBar(RectTransform root)
        {
            RectTransform top = CreatePanel(root, "TopBar", new Vector2(0.18f, 0.92f), new Vector2(1f, 1f), new Vector2(24f, 16f), new Vector2(-24f, -16f), new Color(0.08f, 0.10f, 0.14f, 0.92f));
            CreateEdge(top);
            connectionText = CreateLabel(top, "TopConnection", "Disconnected", 16, FontStyle.Bold, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(240f, 24f), textColor, TextAnchor.MiddleLeft);
            profileText = CreateLabel(top, "TopProfile", "Aimer • Level 12", 16, FontStyle.Normal, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(220f, 24f), mutedTextColor, TextAnchor.MiddleRight);
        }

        private void CreateCenterPanel(RectTransform root)
        {
            RectTransform center = CreatePanel(root, "CenterPanel", new Vector2(0.20f, 0.23f), new Vector2(0.75f, 0.90f), Vector2.zero, Vector2.zero, panelColor);
            CreateEdge(center);

            stageTitleText = CreateLabel(center, "StageTitle", "CIRCLE TRACKING", 26, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(420f, 32f), textColor, TextAnchor.MiddleLeft);
            stageSubtitleText = CreateLabel(center, "StageSubtitle", "Press Start to begin", 16, FontStyle.Normal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -62f), new Vector2(400f, 24f), mutedTextColor, TextAnchor.MiddleLeft);
            instructionText = CreateLabel(center, "Instruction", "Trace the circle smoothly.", 18, FontStyle.Normal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -98f), new Vector2(520f, 24f), textColor, TextAnchor.MiddleLeft);
            precisionText = CreateLabel(center, "Precision", "ADS mode: OFF", 16, FontStyle.Bold, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(180f, 24f), accentColor, TextAnchor.MiddleRight);

            RectTransform startRow = CreateRow(center, new Vector2(28f, -138f), 620f, 44f);
            CreateActionButton(startRow, "Start", "START / RESTART", () => sessionManager?.BeginSession());
            CreateActionButton(startRow, "Stop", "STOP / RETRY", () => sessionManager?.StopSession());
            CreateActionButton(startRow, "Reset", "RESET BINDINGS", ResetAllBindings);

            tipText = CreateLabel(center, "Tip", "Focus on smooth movements over speed. ADS lowers sensitivity for precision.", 16, FontStyle.Italic, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 24f), new Vector2(760f, 28f), mutedTextColor, TextAnchor.MiddleLeft);

            stageButtons.Clear();
            RectTransform stageStrip = CreateRow(center, new Vector2(28f, 64f), 760f, 110f);
            stageButtons.Add(CreateStageCard(stageStrip, "CIRCLE TRACKING", "Track circles with smooth accuracy", 0));
            stageButtons.Add(CreateStageCard(stageStrip, "LINE CONTROL", "Maintain constant speed on straight lines", 1));
            stageButtons.Add(CreateStageCard(stageStrip, "SHAPE TRACE", "Trace complex shapes with precision", 4));
        }

        private void CreateRightPanel(RectTransform root)
        {
            RectTransform right = CreatePanel(root, "RightPanel", new Vector2(0.76f, 0.23f), new Vector2(1f, 0.90f), new Vector2(0f, 0f), new Vector2(-24f, 0f), panelColor);
            CreateEdge(right);

            CreateLabel(right, "PerfTitle", "PERFORMANCE", 22, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -22f), new Vector2(220f, 28f), textColor, TextAnchor.MiddleLeft);
            scoreText = CreateLabel(right, "Score", "87.6", 38, FontStyle.Bold, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(140f, 44f), accentColor, TextAnchor.MiddleCenter);
            CreateLabel(right, "ScoreLabel", "SCORE", 14, FontStyle.Normal, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(100f, 20f), mutedTextColor, TextAnchor.MiddleCenter);

            accuracyText = CreateMetricRow(right, "ACCURACY", 0.94f, new Vector2(22f, -174f), out accuracyFill);
            smoothnessText = CreateMetricRow(right, "SMOOTHNESS", 0.88f, new Vector2(22f, -234f), out smoothnessFill);
            speedText = CreateMetricRow(right, "SPEED CONSISTENCY", 0.86f, new Vector2(22f, -294f), out speedFill);
            deviationText = CreateMetricRow(right, "REACTION CONTROL", 0.82f, new Vector2(22f, -354f), out deviationFill);

            CreateLabel(right, "BindingsHeader", "CUSTOM INPUTS", 18, FontStyle.Bold, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(22f, 148f), new Vector2(180f, 24f), textColor, TextAnchor.MiddleLeft);
            CreateRebindRow(right, "RightStick", "Right Stick", "Rebind", 22f, 118f);
            CreateRebindRow(right, "Aim", "ADS / LT", "Rebind", 22f, 76f);
            CreateRebindRow(right, "Fire", "Fire / RT", "Rebind", 22f, 34f);
            CreateRebindRow(right, "Confirm", "Start / Submit", "Rebind", 22f, -8f);
            CreateRebindRow(right, "Cancel", "Retry / Cancel", "Rebind", 22f, -50f);
        }

        private void CreateBottomStrip(RectTransform root)
        {
            RectTransform bottom = CreatePanel(root, "BottomStrip", new Vector2(0.18f, 0f), new Vector2(1f, 0.19f), new Vector2(24f, 24f), new Vector2(-24f, 24f), new Color(0.08f, 0.10f, 0.14f, 0.92f));
            CreateEdge(bottom);
            controlsText = CreateLabel(bottom, "Controls", "Press Start to begin. Hold LT for precision mode. RT is available as a fire input.", 16, FontStyle.Normal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -20f), new Vector2(820f, 24f), mutedTextColor, TextAnchor.MiddleLeft);

            RectTransform stageInfo = CreatePanel(bottom, "StageInfo", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-380f, 12f), new Vector2(-22f, -12f), new Color(0.07f, 0.08f, 0.11f, 1f));
            CreateEdge(stageInfo);
            CreateLabel(stageInfo, "StageInfoText", "DAILY GOAL", 14, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -12f), new Vector2(120f, 20f), mutedTextColor, TextAnchor.MiddleLeft);
            CreateLabel(stageInfo, "StageInfoValue", "2 / 5 Drills", 18, FontStyle.Bold, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -12f), new Vector2(120f, 20f), accentColor, TextAnchor.MiddleRight);
            CreateProgressBar(stageInfo, new Vector2(14f, 16f), new Vector2(-14f, 10f), accentColor, 0.72f);
        }

        private void CreateNavItem(RectTransform parent, string label, bool selected, float yOffset)
        {
            RectTransform item = CreatePanel(parent, label, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, yOffset - 24f), new Vector2(-16f, yOffset + 18f), selected ? new Color(0.23f, 0.20f, 0.38f, 1f) : new Color(0f, 0f, 0f, 0.08f));
            CreateEdge(item, selected ? accentColor : panelEdgeColor);
            CreateLabel(item, "Text", label, 16, FontStyle.Bold, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(180f, 24f), selected ? textColor : mutedTextColor, TextAnchor.MiddleLeft);
        }

        private Button CreateStageCard(RectTransform parent, string title, string subtitle, int stageIndex)
        {
            RectTransform card = CreatePanel(parent, title, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 8f), new Vector2(236f, -8f), new Color(0.09f, 0.10f, 0.14f, 1f));
            CreateEdge(card);
            Button button = card.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.colors = BuildButtonColors();
            button.onClick.AddListener(() => sessionManager?.SelectStage(stageIndex));

            CreateLabel(card, "Title", title, 14, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(200f, 20f), textColor, TextAnchor.MiddleLeft);
            CreateLabel(card, "Subtitle", subtitle, 12, FontStyle.Normal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -40f), new Vector2(200f, 28f), mutedTextColor, TextAnchor.MiddleLeft);

            stageButtons.Add(button);
            return button;
        }

        private void CreateActionButton(RectTransform parent, string name, string label, UnityAction onClick)
        {
            RectTransform buttonRect = CreatePanel(parent, name, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(196f, -6f), accentColor);
            CreateEdge(buttonRect, accentColor);
            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.colors = BuildButtonColors();
            button.onClick.AddListener(onClick);
            CreateLabel(buttonRect, "Text", label, 16, FontStyle.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 24f), textColor, TextAnchor.MiddleCenter);
        }

        private void CreateRebindRow(RectTransform parent, string actionName, string label, string buttonText, float x, float y)
        {
            RectTransform row = CreatePanel(parent, actionName + "Row", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(x, y - 8f), new Vector2(-22f, y + 26f), new Color(0.07f, 0.08f, 0.11f, 1f));
            CreateLabel(row, "Name", label, 13, FontStyle.Bold, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(120f, 20f), textColor, TextAnchor.MiddleLeft);
            Text bindingLabel = CreateLabel(row, "Binding", string.Empty, 12, FontStyle.Normal, new Vector2(0.48f, 0.5f), new Vector2(0.48f, 0.5f), new Vector2(0f, 0f), new Vector2(110f, 20f), mutedTextColor, TextAnchor.MiddleCenter);
            bindingLabels[actionName] = bindingLabel;

            RectTransform buttonRect = CreatePanel(row, buttonText, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-92f, -14f), new Vector2(-12f, 14f), accentColor);
            CreateEdge(buttonRect, accentColor);
            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.colors = BuildButtonColors();
            button.onClick.AddListener(() => StartRebind(actionName));
            CreateLabel(buttonRect, "Text", buttonText, 12, FontStyle.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 18f), textColor, TextAnchor.MiddleCenter);
        }

        private void CreateDeadzoneRow(RectTransform parent, Vector2 position)
        {
            RectTransform row = CreatePanel(parent, "DeadzoneAdjust", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(position.x, position.y - 8f), new Vector2(-16f, position.y + 28f), new Color(0f, 0f, 0f, 0f));
            CreateLabel(row, "DeadzoneText", "Deadzone", 12, FontStyle.Bold, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(80f, 18f), mutedTextColor, TextAnchor.MiddleLeft);
            Text valueText = CreateLabel(row, "DeadzoneValue", currentDeadzone.ToString("0.00"), 12, FontStyle.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10f, 0f), new Vector2(70f, 18f), textColor, TextAnchor.MiddleCenter);

            RectTransform decBtn = CreatePanel(row, "DeadzoneDec", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-74f, -12f), new Vector2(-50f, 12f), accentColor);
            CreateEdge(decBtn, accentColor);
            Button decButton = decBtn.gameObject.AddComponent<Button>();
            decButton.transition = Selectable.Transition.ColorTint;
            decButton.colors = BuildButtonColors();
            decButton.onClick.AddListener(() =>
            {
                currentDeadzone = Mathf.Clamp(currentDeadzone - 0.02f, 0f, 0.5f);
                ApplyDeadzone();
                valueText.text = currentDeadzone.ToString("0.00");
            });
            CreateLabel(decBtn, "Text", "-", 14, FontStyle.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 18f), textColor, TextAnchor.MiddleCenter);

            RectTransform incBtn = CreatePanel(row, "DeadzoneInc", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-44f, -12f), new Vector2(-20f, 12f), accentColor);
            CreateEdge(incBtn, accentColor);
            Button incButton = incBtn.gameObject.AddComponent<Button>();
            incButton.transition = Selectable.Transition.ColorTint;
            incButton.colors = BuildButtonColors();
            incButton.onClick.AddListener(() =>
            {
                currentDeadzone = Mathf.Clamp(currentDeadzone + 0.02f, 0f, 0.5f);
                ApplyDeadzone();
                valueText.text = currentDeadzone.ToString("0.00");
            });
            CreateLabel(incBtn, "Text", "+", 14, FontStyle.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 18f), textColor, TextAnchor.MiddleCenter);
        }

        private void CreateSensitivityRow(RectTransform parent, Vector2 position)
        {
            RectTransform row = CreatePanel(parent, "SensitivityAdjust", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(position.x, position.y - 8f), new Vector2(-16f, position.y + 28f), new Color(0f, 0f, 0f, 0f));
            CreateLabel(row, "SensitivityText", "Sensitivity", 12, FontStyle.Bold, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(80f, 18f), mutedTextColor, TextAnchor.MiddleLeft);
            Text valueText = CreateLabel(row, "SensitivityValue", currentSensitivity.ToString("0.0"), 12, FontStyle.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10f, 0f), new Vector2(70f, 18f), textColor, TextAnchor.MiddleCenter);

            RectTransform decBtn = CreatePanel(row, "SensitivityDec", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-74f, -12f), new Vector2(-50f, 12f), accentColor);
            CreateEdge(decBtn, accentColor);
            Button decButton = decBtn.gameObject.AddComponent<Button>();
            decButton.transition = Selectable.Transition.ColorTint;
            decButton.colors = BuildButtonColors();
            decButton.onClick.AddListener(() =>
            {
                currentSensitivity = Mathf.Clamp(currentSensitivity - 0.5f, 2f, 16f);
                ApplySensitivity();
                valueText.text = currentSensitivity.ToString("0.0");
            });
            CreateLabel(decBtn, "Text", "-", 14, FontStyle.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 18f), textColor, TextAnchor.MiddleCenter);

            RectTransform incBtn = CreatePanel(row, "SensitivityInc", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-44f, -12f), new Vector2(-20f, 12f), accentColor);
            CreateEdge(incBtn, accentColor);
            Button incButton = incBtn.gameObject.AddComponent<Button>();
            incButton.transition = Selectable.Transition.ColorTint;
            incButton.colors = BuildButtonColors();
            incButton.onClick.AddListener(() =>
            {
                currentSensitivity = Mathf.Clamp(currentSensitivity + 0.5f, 2f, 16f);
                ApplySensitivity();
                valueText.text = currentSensitivity.ToString("0.0");
            });
            CreateLabel(incBtn, "Text", "+", 14, FontStyle.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 18f), textColor, TextAnchor.MiddleCenter);
        }

        private Text CreateMetricRow(RectTransform parent, string label, float initial, Vector2 position, out Image fill)
        {
            CreateLabel(parent, label + "Label", label, 14, FontStyle.Normal, new Vector2(0f, 1f), new Vector2(0f, 1f), position, new Vector2(220f, 20f), mutedTextColor, TextAnchor.MiddleLeft);
            Text valueText = CreateLabel(parent, label + "Value", Mathf.RoundToInt(initial * 100f) + "%", 14, FontStyle.Bold, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, position.y), new Vector2(70f, 20f), textColor, TextAnchor.MiddleRight);
            fill = CreateProgressBar(parent, new Vector2(22f, position.y - 22f), new Vector2(-22f, 8f), accentColor, initial);
            return valueText;
        }

        private Image CreateProgressBar(RectTransform parent, Vector2 position, Vector2 sizeDelta, Color fillColor, float initialValue)
        {
            RectTransform background = CreatePanel(parent, "BarBackground", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(position.x, position.y - sizeDelta.y), new Vector2(sizeDelta.x, position.y), new Color(0.15f, 0.17f, 0.24f, 1f));
            Image bgImage = background.gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.13f, 0.15f, 0.21f, 1f);
            RectTransform fillRect = CreatePanel(background, "Fill", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), fillColor);
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = Mathf.Clamp01(initialValue);
            return fill;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private void RefreshUi()
        {
            if (sessionManager == null)
            {
                return;
            }

            if (connectionText != null)
            {
                connectionText.text = inputHandler != null && inputHandler.ControllerConnected ? "Connected" : "Disconnected";
                connectionText.color = inputHandler != null && inputHandler.ControllerConnected ? new Color(0.55f, 1f, 0.65f, 1f) : mutedTextColor;
            }

            if (profileText != null)
            {
                profileText.text = inputHandler != null ? (inputHandler.AimHeld ? "Aimer • ADS Active" : "Aimer • Ready") : "Aimer";
            }

            if (stageTitleText != null)
            {
                stageTitleText.text = sessionManager.HasStarted ? sessionManager.CurrentStageName : "READY";
            }

            if (stageSubtitleText != null)
            {
                stageSubtitleText.text = sessionManager.GetStageSummary(sessionManager.CurrentStageIndex);
            }

            if (instructionText != null)
            {
                instructionText.text = sessionManager.GetDrillInstructions();
            }

            if (precisionText != null)
            {
                precisionText.text = inputHandler != null && inputHandler.AimHeld ? "ADS MODE: ON" : "ADS MODE: OFF";
                precisionText.color = inputHandler != null && inputHandler.AimHeld ? accentColor : mutedTextColor;
            }

            if (scoreText != null)
            {
                scoreText.text = sessionManager.CurrentTotalScore.ToString("0.0");
            }

            UpdateMetric(accuracyText, accuracyFill, sessionManager.CurrentAccuracy / 100f);
            UpdateMetric(smoothnessText, smoothnessFill, sessionManager.CurrentSmoothness / 100f);
            UpdateMetric(speedText, speedFill, sessionManager.CurrentSpeedConsistency / 100f);
            UpdateMetric(deviationText, deviationFill, 1f - Mathf.Clamp01(sessionManager.CurrentDeviation / 0.5f));

            if (tipText != null)
            {
                tipText.text = sessionManager.IsRunning
                    ? "Focus on smooth movements over speed. Consistency builds precision."
                    : "Press Start to begin a session, or select a drill card below.";
            }

            if (controlsText != null)
            {
                controlsText.text = sessionManager.ControllerHintText;
            }

            currentDeadzone = ReadCurrentDeadzone();
            currentSensitivity = ReadCurrentSensitivity();

            RefreshBindingLabels();
        }

        private void UpdateMetric(Text valueText, Image fill, float normalized)
        {
            if (valueText != null)
            {
                valueText.text = Mathf.Clamp01(normalized).ToString("P0");
            }

            if (fill != null)
            {
                fill.fillAmount = Mathf.Clamp01(normalized);
            }
        }

        private void RefreshBindingLabels()
        {
            if (inputHandler == null)
            {
                return;
            }

            foreach (KeyValuePair<string, Text> pair in bindingLabels)
            {
                InputAction action = inputHandler.FindAction(pair.Key);
                if (action != null)
                {
                    pair.Value.text = action.GetBindingDisplayString();
                }
                else
                {
                    pair.Value.text = "Unbound";
                }
            }
        }

        private void StartRebind(string actionName)
        {
            if (inputHandler == null)
            {
                return;
            }

            InputAction action = inputHandler.FindAction(actionName);
            if (action == null)
            {
                return;
            }

            if (isRebinding)
            {
                return;
            }

            isRebinding = true;
            action.Disable();

            activeRebind = action.PerformInteractiveRebinding(0)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("Mouse")
                .OnCancel(_ => CompleteRebind(action))
                .OnComplete(_ => CompleteRebind(action));

            activeRebind.Start();
        }

        private void CompleteRebind(InputAction action)
        {
            if (activeRebind != null)
            {
                activeRebind.Dispose();
                activeRebind = null;
            }

            action.Enable();
            inputHandler?.SaveBindingOverrides();
            isRebinding = false;
            RefreshBindingLabels();
        }

        private void ResetAllBindings()
        {
            InputActionAsset asset = inputHandler != null ? inputHandler.GetInputActionsAsset() : null;
            if (asset == null)
            {
                return;
            }

            asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(inputHandler.BindingOverridesKey);
            PlayerPrefs.Save();
            RefreshBindingLabels();
        }

        private void LoadBindingOverrides()
        {
            if (inputHandler == null)
            {
                return;
            }

            inputHandler.LoadBindingOverrides();
        }

        private float ReadCurrentDeadzone()
        {
            return currentDeadzone;
        }

        private float ReadCurrentSensitivity()
        {
            return currentSensitivity;
        }

        private void ApplyDeadzone()
        {
            inputHandler?.SetDeadzone(currentDeadzone);
        }

        private void ApplySensitivity()
        {
            cursorController?.SetSensitivity(currentSensitivity);
        }

        private RectTransform CreateRow(RectTransform parent, Vector2 position, float width, float height)
        {
            return CreatePanel(parent, "Row", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(position.x, position.y), new Vector2(position.x + width, position.y + height), new Color(0f, 0f, 0f, 0f));
        }

        private RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);
            RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            Image image = panelObject.AddComponent<Image>();
            image.color = color;
            return rectTransform;
        }

        private Text CreateLabel(RectTransform parent, string name, string value, int size, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color, TextAnchor alignment)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            Text text = labelObject.AddComponent<Text>();
            text.text = value;
            text.font = uiFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void CreateEdge(RectTransform panel, Color? borderColor = null)
        {
            Color edgeColor = borderColor ?? panelEdgeColor;
            GameObject edgeObject = new GameObject("Edge", typeof(RectTransform));
            edgeObject.transform.SetParent(panel, false);
            RectTransform rectTransform = edgeObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            Image image = edgeObject.AddComponent<Image>();
            image.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.04f);
            image.raycastTarget = false;
        }

        private void CreateAdjustRow(RectTransform parent, string label, float value, Vector2 position, System.Func<float> dec, System.Func<float> inc, System.Action<float> apply)
        {
            RectTransform row = CreatePanel(parent, label + "Adjust", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(position.x, position.y - 8f), new Vector2(-16f, position.y + 28f), new Color(0f, 0f, 0f, 0f));
            CreateLabel(row, label + "Text", label, 12, FontStyle.Bold, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(80f, 18f), mutedTextColor, TextAnchor.MiddleLeft);
            Text valueText = CreateLabel(row, label + "Value", value.ToString("0.00"), 12, FontStyle.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10f, 0f), new Vector2(70f, 18f), textColor, TextAnchor.MiddleCenter);

            RectTransform decBtn = CreatePanel(row, label + "Dec", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-74f, -12f), new Vector2(-50f, 12f), accentColor);
            CreateEdge(decBtn, accentColor);
            Button decButton = decBtn.gameObject.AddComponent<Button>();
            decButton.onClick.AddListener(() =>
            {
                float newValue = Mathf.Clamp(dec(), label == "Deadzone" ? 0f : 2f, label == "Deadzone" ? 0.5f : 16f);
                apply(newValue);
                valueText.text = newValue.ToString("0.00");
            });
            CreateLabel(decBtn, "Text", "-", 14, FontStyle.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 18f), textColor, TextAnchor.MiddleCenter);

            RectTransform incBtn = CreatePanel(row, label + "Inc", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-44f, -12f), new Vector2(-20f, 12f), accentColor);
            CreateEdge(incBtn, accentColor);
            Button incButton = incBtn.gameObject.AddComponent<Button>();
            incButton.onClick.AddListener(() =>
            {
                float newValue = Mathf.Clamp(inc(), label == "Deadzone" ? 0f : 2f, label == "Deadzone" ? 0.5f : 16f);
                apply(newValue);
                valueText.text = newValue.ToString("0.00");
            });
            CreateLabel(incBtn, "Text", "+", 14, FontStyle.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 18f), textColor, TextAnchor.MiddleCenter);
        }

        private ColorBlock BuildButtonColors()
        {
            ColorBlock colors = new ColorBlock
            {
                normalColor = new Color(0f, 0f, 0f, 0f),
                highlightedColor = new Color(1f, 1f, 1f, 0.06f),
                pressedColor = new Color(1f, 1f, 1f, 0.12f),
                selectedColor = new Color(1f, 1f, 1f, 0.06f),
                disabledColor = new Color(1f, 1f, 1f, 0.02f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };

            return colors;
        }
    }
}
