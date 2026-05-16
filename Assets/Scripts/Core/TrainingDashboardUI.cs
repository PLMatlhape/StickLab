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
        private enum DifficultyPreset { Easy, Normal, Hard }

        [Header("References")]
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private CursorController cursorController;
        [SerializeField] private TrainingSessionManager sessionManager;

        [Header("Visuals")]
        [SerializeField] private Color backgroundColor  = new Color(0.05f, 0.06f, 0.09f, 1f);
        [SerializeField] private Color panelColor       = new Color(0.10f, 0.12f, 0.17f, 0.92f);
        [SerializeField] private Color panelEdgeColor   = new Color(0.20f, 0.24f, 0.35f, 1f);
        [SerializeField] private Color accentColor      = new Color(0.57f, 0.42f, 1f, 1f);
        [SerializeField] private Color textColor        = new Color(0.95f, 0.96f, 1f, 1f);
        [SerializeField] private Color mutedTextColor   = new Color(0.75f, 0.79f, 0.88f, 1f);
        [SerializeField] private Font uiFont;

        [Header("Layout")]
        [SerializeField] private bool buildOnAwake = true;

        // ── UI references ────────────────────────────────────────────────────
        private Canvas canvas;
        private Text connectionText, profileText, stageTitleText, stageSubtitleText;
        private Text instructionText, tipText, precisionText, controlsText;
        private Text scoreGaugeText, rightPanelSummary;
        private Text accuracyText, smoothnessText, speedText, deviationText;
        private Image accuracyFill, smoothnessFill, speedFill, deviationFill, scoreGaugeFill;
        private RectTransform overlayPanel;
        private Text overlayTitleText, overlayBodyText, overlaySummaryText;
        private Button overlayPrimaryButton, overlaySecondaryButton;
        private CanvasGroup overlayCanvasGroup;

        private readonly Dictionary<string, Text> bindingLabels = new Dictionary<string, Text>();
        private readonly List<Button> stageButtons  = new List<Button>();
        private readonly List<Button> focusOrder    = new List<Button>();

        private bool isRebinding;
        private InputActionRebindingExtensions.RebindingOperation activeRebind;

        private float currentDeadzone   = 0.18f;
        private float currentSensitivity = 8f;
        private DifficultyPreset currentDifficulty = DifficultyPreset.Normal;

        private int   focusedButtonIndex = -1;
        private float navRepeatTimer;
        private const float NavRepeatDelay = 0.16f;

        private const string DeadzoneKey    = "StickLab.Settings.Deadzone";
        private const string SensitivityKey = "StickLab.Settings.Sensitivity";
        private const string DifficultyKey  = "StickLab.Settings.DifficultyPreset";

        // ── Smooth displays ──────────────────────────────────────────────────
        private SmoothNumberDisplay scoreDisplay, accuracyDisplay, smoothnessDisplay, speedDisplay, deviationDisplay;
        private SmoothFillDisplay   scoreGaugeFillDisplay, accuracyFillDisplay, smoothnessFillDisplay, speedFillDisplay, deviationFillDisplay;
        private SmoothPanelTransition overlayTransition;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (buildOnAwake) BuildUi();
        }

        private void Start()
        {
            LoadSettings();
            LoadBindingOverrides();
            ApplyDeadzone();
            ApplySensitivity();
            ApplyDifficultyProfile();
            RefreshBindingLabels();
            EnsureFocusSelection();
            InitSmoothDisplays();
        }

        private void Update()
        {
            if (inputHandler == null || sessionManager == null) return;
            HandleControllerNavigation();
            if (inputHandler.ConfirmPressedThisFrame)  ActivateFocusedButton();
            if (inputHandler.RetryPressedThisFrame)    ToggleSessionPauseState();
            if (Input.GetKeyDown(KeyCode.F1) && canvas != null) canvas.enabled = !canvas.enabled;
        }

        private void LateUpdate() => RefreshUi();

        // ── Public API ───────────────────────────────────────────────────────
        public void SetReferences(InputHandler ih, CursorController cc, TrainingSessionManager sm)
        {
            inputHandler   = ih;
            cursorController = cc;
            sessionManager = sm;
        }

        // ── Smooth display init ──────────────────────────────────────────────
        private void InitSmoothDisplays()
        {
            // Guard: text refs might be null if BuildUi wasn't called
            scoreDisplay      = new SmoothNumberDisplay(scoreGaugeText,  "0.0");
            accuracyDisplay   = new SmoothNumberDisplay(accuracyText,    "0.0");
            smoothnessDisplay = new SmoothNumberDisplay(smoothnessText,  "0.0");
            speedDisplay      = new SmoothNumberDisplay(speedText,       "0.0");
            deviationDisplay  = new SmoothNumberDisplay(deviationText,   "0.0");

            scoreGaugeFillDisplay  = new SmoothFillDisplay(scoreGaugeFill);
            accuracyFillDisplay    = new SmoothFillDisplay(accuracyFill);
            smoothnessFillDisplay  = new SmoothFillDisplay(smoothnessFill);
            speedFillDisplay       = new SmoothFillDisplay(speedFill);
            deviationFillDisplay   = new SmoothFillDisplay(deviationFill);

            scoreDisplay?.SetValueInstant(0f);
            scoreGaugeFillDisplay?.SetFillInstant(0f);

            // FIX: guard against null overlayPanel before accessing CanvasGroup
            if (overlayPanel != null)
            {
                overlayCanvasGroup = overlayPanel.GetComponent<CanvasGroup>();
                if (overlayCanvasGroup == null)
                    overlayCanvasGroup = overlayPanel.gameObject.AddComponent<CanvasGroup>();
                overlayTransition = new SmoothPanelTransition(overlayCanvasGroup);
                overlayTransition.SetAlphaInstant(0f);
                overlayPanel.gameObject.SetActive(false);
            }
        }

        // ── UI refresh ───────────────────────────────────────────────────────
        private void RefreshUi()
        {
            if (sessionManager == null) return;

            // Connection
            if (connectionText != null)
            {
                bool connected = inputHandler != null && inputHandler.ControllerConnected;
                connectionText.text  = connected ? "Connected" : "Disconnected";
                connectionText.color = connected ? new Color(0.55f, 1f, 0.65f) : mutedTextColor;
            }
            if (profileText != null)
                profileText.text = inputHandler != null && inputHandler.AimHeld ? "Aimer • ADS Active" : "Aimer • Ready";

            // Stage info
            if (stageTitleText   != null) stageTitleText.text   = sessionManager.HasStarted ? sessionManager.CurrentStageName : "READY";
            if (stageSubtitleText!= null) stageSubtitleText.text = sessionManager.GetStageSummary(sessionManager.CurrentStageIndex);
            if (instructionText  != null) instructionText.text  = sessionManager.GetDrillInstructions();
            if (precisionText    != null)
            {
                bool ads = inputHandler != null && inputHandler.AimHeld;
                precisionText.text  = ads ? "ADS MODE: ON" : "ADS MODE: OFF";
                precisionText.color = ads ? accentColor : mutedTextColor;
            }

            // Right panel summary
            if (rightPanelSummary != null)
                rightPanelSummary.text = sessionManager.State == TrainingSessionManager.SessionState.Results
                    ? $"Completed {sessionManager.CompletedStages} stages\nPeak {sessionManager.PeakScore:0.0}\nAverage {sessionManager.AverageScore:0.0}"
                    : $"Stage {sessionManager.CurrentStageIndex + 1}/{sessionManager.StageCount}\nTier {sessionManager.DifficultyTier}\nElapsed {sessionManager.ElapsedSeconds:0.0}s";

            // Smooth metric animations
            float total = sessionManager.CurrentTotalScore;
            scoreDisplay?.SetValue(total, 0.15f);
            scoreGaugeFillDisplay?.SetFill(total / 100f, 0.20f);
            if (scoreGaugeText != null && scoreDisplay != null)
                scoreGaugeText.text = scoreDisplay.CurrentValue.ToString("0.0");

            accuracyDisplay?.SetValue(sessionManager.CurrentAccuracy, 0.15f);
            accuracyFillDisplay?.SetFill(sessionManager.CurrentAccuracy / 100f, 0.20f);

            smoothnessDisplay?.SetValue(sessionManager.CurrentSmoothness, 0.15f);
            smoothnessFillDisplay?.SetFill(sessionManager.CurrentSmoothness / 100f, 0.20f);

            speedDisplay?.SetValue(sessionManager.CurrentSpeedConsistency, 0.15f);
            speedFillDisplay?.SetFill(sessionManager.CurrentSpeedConsistency / 100f, 0.20f);

            float devFill = 1f - Mathf.Clamp01(sessionManager.CurrentDeviation / 0.5f);
            deviationDisplay?.SetValue(devFill * 100f, 0.15f);
            deviationFillDisplay?.SetFill(devFill, 0.20f);

            // Tip
            if (tipText != null)
                tipText.text = sessionManager.State switch
                {
                    TrainingSessionManager.SessionState.Running => "Focus on smooth movements. Consistency builds precision.",
                    TrainingSessionManager.SessionState.Paused  => "Session paused. Resume from the overlay or press Retry.",
                    TrainingSessionManager.SessionState.Results => "Results ready. Restart to run another round.",
                    _                                           => "Press Start to begin a session, or select a drill card."
                };

            if (controlsText != null)
                controlsText.text = sessionManager.ControllerHintText +
                    (inputHandler != null && inputHandler.FireHeld ? " • RT held" : string.Empty);

            UpdateOverlay();
            RefreshBindingLabels();
        }

        // FIX: track last state so fade is triggered only on CHANGE, not every LateUpdate frame
        private bool lastOverlayVisible = false;
        private bool overlayListenersWired = false;

        private void UpdateOverlay()
        {
            if (overlayPanel == null || overlayTransition == null) return;

            bool show = sessionManager.State == TrainingSessionManager.SessionState.Paused
                     || sessionManager.State == TrainingSessionManager.SessionState.Results;

            // FIX: only call FadeIn/FadeOut when visibility actually changes
            if (show != lastOverlayVisible)
            {
                lastOverlayVisible = show;
                if (show)
                {
                    overlayPanel.gameObject.SetActive(true);
                    overlayTransition.FadeIn(0.25f);
                    SetFocusToFirstOverlayButton();
                    WireOverlayButtons();
                }
                else
                {
                    overlayTransition.FadeOut(0.20f);
                    RestoreGameplayFocus();
                }
            }

            // Always update the summary text while overlay is shown (score changes)
            if (!show) return;

            if (overlayTitleText != null)
                overlayTitleText.text = sessionManager.State == TrainingSessionManager.SessionState.Results
                    ? "RESULTS" : "PAUSED";
            if (overlayBodyText != null)
                overlayBodyText.text = sessionManager.State == TrainingSessionManager.SessionState.Results
                    ? "Session complete. Restart when ready."
                    : "Session paused. Resume to continue.";
            if (overlaySummaryText != null)
                overlaySummaryText.text = $"Score {sessionManager.CurrentTotalScore:0.0}  |  Peak {sessionManager.PeakScore:0.0}  |  Avg {sessionManager.AverageScore:0.0}  |  Time {sessionManager.ElapsedSeconds:0.0}s";
        }

        // FIX: wire button listeners once on show, not every frame
        private void WireOverlayButtons()
        {
            if (overlayPrimaryButton != null)
            {
                overlayPrimaryButton.onClick.RemoveAllListeners();
                overlayPrimaryButton.onClick.AddListener(() =>
                {
                    if (sessionManager.State == TrainingSessionManager.SessionState.Results)
                        sessionManager.RestartSession();
                    else
                        sessionManager.ResumeSession();
                });
            }
            if (overlaySecondaryButton != null)
            {
                overlaySecondaryButton.onClick.RemoveAllListeners();
                overlaySecondaryButton.onClick.AddListener(() => sessionManager.RestartSession());
            }
        }

        // ── Settings ─────────────────────────────────────────────────────────
        private void LoadSettings()
        {
            currentDeadzone    = PlayerPrefs.GetFloat(DeadzoneKey,    currentDeadzone);
            currentSensitivity = PlayerPrefs.GetFloat(SensitivityKey, currentSensitivity);
            currentDifficulty  = (DifficultyPreset)PlayerPrefs.GetInt(DifficultyKey, (int)DifficultyPreset.Normal);
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(DeadzoneKey,    currentDeadzone);
            PlayerPrefs.SetFloat(SensitivityKey, currentSensitivity);
            PlayerPrefs.SetInt(DifficultyKey,    (int)currentDifficulty);
            PlayerPrefs.Save();
        }

        private void ApplyDeadzone()    => inputHandler?.SetDeadzone(currentDeadzone);
        private void ApplySensitivity() => cursorController?.SetSensitivity(currentSensitivity);

        private void ApplyDifficultyProfile()
        {
            if (sessionManager == null) return;
            var (sz, th, tol) = currentDifficulty switch
            {
                DifficultyPreset.Easy   => (1f,    1f,    1f),
                DifficultyPreset.Hard   => (0.92f, 0.84f, 0.80f),
                _                       => (0.97f, 0.92f, 0.90f),
            };
            sessionManager.SetDifficultyProfile(sz, th, tol);
            SaveSettings();
        }

        private void SetDifficultyPreset(DifficultyPreset preset)
        {
            currentDifficulty = preset;
            ApplyDifficultyProfile();
        }

        // ── Rebinding ────────────────────────────────────────────────────────
        private void RefreshBindingLabels()
        {
            if (inputHandler == null) return;
            foreach (var kv in bindingLabels)
            {
                var action = inputHandler.FindAction(kv.Key);
                kv.Value.text = action != null ? action.GetBindingDisplayString() : "Unbound";
            }
        }

        private void LoadBindingOverrides() => inputHandler?.LoadBindingOverrides();

        private void StartRebind(string actionName)
        {
            if (inputHandler == null || isRebinding) return;
            var action = inputHandler.FindAction(actionName);
            if (action == null) return;
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
            activeRebind?.Dispose();
            activeRebind = null;
            action.Enable();
            inputHandler?.SaveBindingOverrides();
            isRebinding = false;
            RefreshBindingLabels();
        }

        private void ResetAllBindings()
        {
            var asset = inputHandler?.GetInputActionsAsset();
            if (asset == null) return;
            asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(inputHandler.BindingOverridesKey);
            PlayerPrefs.Save();
            RefreshBindingLabels();
        }

        // ── Controller navigation ────────────────────────────────────────────
        private void HandleControllerNavigation()
        {
            if (focusOrder.Count == 0) return;

            Vector2 nav = Vector2.zero;
            if (Gamepad.current != null)
            {
                nav += Gamepad.current.leftStick.ReadValue();
                nav += Gamepad.current.dpad.ReadValue();
            }
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.isPressed   || Keyboard.current.wKey.isPressed) nav.y += 1f;
                if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) nav.y -= 1f;
                if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) nav.x -= 1f;
                if (Keyboard.current.rightArrowKey.isPressed|| Keyboard.current.dKey.isPressed) nav.x += 1f;
            }
            if (nav.sqrMagnitude < 0.35f) { navRepeatTimer = 0f; return; }
            navRepeatTimer -= Time.unscaledDeltaTime;
            if (navRepeatTimer > 0f) return;
            int dir = Mathf.Abs(nav.y) >= Mathf.Abs(nav.x) ? (nav.y > 0f ? -1 : 1) : (nav.x > 0f ? 1 : -1);
            MoveFocus(dir);
            navRepeatTimer = NavRepeatDelay;
        }

        private void MoveFocus(int step)
        {
            if (focusOrder.Count == 0) return;
            focusedButtonIndex = Mathf.Clamp(focusedButtonIndex + step, 0, focusOrder.Count - 1);
            SelectFocusedButton();
        }

        private void SelectFocusedButton()
        {
            if (focusedButtonIndex < 0 || focusedButtonIndex >= focusOrder.Count) return;
            var btn = focusOrder[focusedButtonIndex];
            if (btn != null) EventSystem.current?.SetSelectedGameObject(btn.gameObject);
        }

        private void SetFocusToFirstOverlayButton()
        {
            if (overlayPrimaryButton == null) return;
            int idx = focusOrder.IndexOf(overlayPrimaryButton);
            if (idx >= 0) focusedButtonIndex = idx;
            SelectFocusedButton();
        }

        private void EnsureFocusSelection()
        {
            if (focusOrder.Count == 0) return;
            if (focusedButtonIndex < 0 || focusedButtonIndex >= focusOrder.Count) focusedButtonIndex = 0;
            SelectFocusedButton();
        }

        private void RestoreGameplayFocus()
        {
            if (focusOrder.Count == 0) return;
            focusedButtonIndex = Mathf.Clamp(focusedButtonIndex, 0, focusOrder.Count - 1);
            SelectFocusedButton();
        }

        private void ActivateFocusedButton()
        {
            if (focusedButtonIndex < 0 || focusedButtonIndex >= focusOrder.Count) return;
            var btn = focusOrder[focusedButtonIndex];
            if (btn != null && btn.interactable) btn.onClick.Invoke();
        }

        private void ToggleSessionPauseState()
        {
            if (sessionManager == null) return;
            switch (sessionManager.State)
            {
                case TrainingSessionManager.SessionState.Running: sessionManager.PauseSession();   break;
                case TrainingSessionManager.SessionState.Paused:  sessionManager.ResumeSession();  break;
                case TrainingSessionManager.SessionState.Results: sessionManager.RestartSession();  break;
            }
        }

        private void RegisterFocusable(Button btn) { if (btn != null) focusOrder.Add(btn); }

        // ── UI construction ──────────────────────────────────────────────────
        private void BuildUi()
        {
            EnsureEventSystem();

            var go = new GameObject("Training Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            go.AddComponent<GraphicRaycaster>();

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;

            var root = canvas.GetComponent<RectTransform>();
            CreatePanel(root, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, backgroundColor);
            CreateSidebar(root);
            CreateTopBar(root);
            CreateCenterPanel(root);
            CreateRightPanel(root);
            CreateBottomStrip(root);
            CreateOverlay(root);
        }

        // ── Sidebar ──────────────────────────────────────────────────────────
        private void CreateSidebar(RectTransform root)
        {
            var sb = CreatePanel(root, "Sidebar", new Vector2(0,0), new Vector2(0.18f,1), new Vector2(24,24), new Vector2(-24,-24), panelColor);
            AddEdge(sb);
            CreateLabel(sb,"Title","AIM LAB",30,FontStyle.Bold,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-28),new Vector2(160,38),textColor,TextAnchor.MiddleLeft);
            CreateLabel(sb,"Subtitle","FOR CONTROLLER",18,FontStyle.Bold,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-62),new Vector2(180,28),accentColor,TextAnchor.MiddleLeft);

            string[] items={"HOME","DRILLS","CUSTOM","RESULTS","ANALYTICS","LEADERBOARD","SETTINGS"};
            for(int i=0;i<items.Length;i++) CreateNavItem(sb,items[i],i==0,-135f-(i*58f));

            // Controller card
            var cc = CreatePanel(sb,"ControllerCard",new Vector2(0,0),new Vector2(1,0),new Vector2(16,16),new Vector2(-16,155),new Color(0.08f,0.10f,0.14f,1));
            AddEdge(cc);
            CreateLabel(cc,"Header","CONTROLLER",18,FontStyle.Bold,new Vector2(0,1),new Vector2(0,1),new Vector2(16,-16),new Vector2(140,26),textColor,TextAnchor.MiddleLeft);
            connectionText = CreateLabel(cc,"Conn","Disconnected",16,FontStyle.Normal,new Vector2(0,1),new Vector2(0,1),new Vector2(16,-54),new Vector2(160,24),mutedTextColor,TextAnchor.MiddleLeft);
            CreateLabel(cc,"Hint","LT = ADS • RT = Fire",14,FontStyle.Normal,new Vector2(0,1),new Vector2(0,1),new Vector2(16,-82),new Vector2(190,24),mutedTextColor,TextAnchor.MiddleLeft);

            // Quick settings
            var qs = CreatePanel(sb,"QuickSettings",new Vector2(0,0),new Vector2(1,0),new Vector2(16,16),new Vector2(-16,18),new Color(0.08f,0.10f,0.14f,1));
            AddEdge(qs);
            CreateLabel(qs,"Header","QUICK SETTINGS",18,FontStyle.Bold,new Vector2(0,1),new Vector2(0,1),new Vector2(16,-16),new Vector2(180,24),textColor,TextAnchor.MiddleLeft);
            BuildDeadzoneRow(qs, new Vector2(16,-54));
            BuildSensitivityRow(qs, new Vector2(16,-96));
            BuildDifficultyButtons(qs, new Vector2(16,-138));
        }

        private void CreateNavItem(RectTransform p, string label, bool sel, float y)
        {
            var item = CreatePanel(p,label,new Vector2(0,1),new Vector2(1,1),new Vector2(16,y-24),new Vector2(-16,y+18),
                sel?new Color(0.23f,0.20f,0.38f,1):new Color(0,0,0,.08f));
            AddEdge(item, sel?accentColor:panelEdgeColor);
            CreateLabel(item,"Text",label,16,FontStyle.Bold,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(18,0),new Vector2(180,24),sel?textColor:mutedTextColor,TextAnchor.MiddleLeft);
        }

        // ── Top bar ──────────────────────────────────────────────────────────
        private void CreateTopBar(RectTransform root)
        {
            var top = CreatePanel(root,"TopBar",new Vector2(0.18f,0.92f),new Vector2(1,1),new Vector2(24,16),new Vector2(-24,-16),new Color(0.08f,0.10f,0.14f,.92f));
            AddEdge(top);
            connectionText = CreateLabel(top,"TopConn","Disconnected",16,FontStyle.Bold,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(24,0),new Vector2(240,24),textColor,TextAnchor.MiddleLeft);
            profileText    = CreateLabel(top,"Profile","Aimer • Ready",16,FontStyle.Normal,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-24,0),new Vector2(220,24),mutedTextColor,TextAnchor.MiddleRight);
        }

        // ── Center panel ─────────────────────────────────────────────────────
        private void CreateCenterPanel(RectTransform root)
        {
            var ctr = CreatePanel(root,"Center",new Vector2(0.20f,0.23f),new Vector2(0.75f,0.90f),Vector2.zero,Vector2.zero,panelColor);
            AddEdge(ctr);
            stageTitleText    = CreateLabel(ctr,"StageTitle","CIRCLE TRACKING",26,FontStyle.Bold,new Vector2(0,1),new Vector2(0,1),new Vector2(28,-28),new Vector2(420,32),textColor,TextAnchor.MiddleLeft);
            stageSubtitleText = CreateLabel(ctr,"StageSub","Press Start to begin",16,FontStyle.Normal,new Vector2(0,1),new Vector2(0,1),new Vector2(28,-62),new Vector2(400,24),mutedTextColor,TextAnchor.MiddleLeft);
            instructionText   = CreateLabel(ctr,"Instruction","Trace the circle smoothly.",18,FontStyle.Normal,new Vector2(0,1),new Vector2(0,1),new Vector2(28,-98),new Vector2(520,24),textColor,TextAnchor.MiddleLeft);
            precisionText     = CreateLabel(ctr,"Precision","ADS mode: OFF",16,FontStyle.Bold,new Vector2(1,1),new Vector2(1,1),new Vector2(-28,-28),new Vector2(180,24),accentColor,TextAnchor.MiddleRight);

            var row = CreateRow(ctr, new Vector2(28,-138), 620, 44);
            CreateActionButton(row,"Start","START / RESTART",()=>sessionManager?.BeginSession());
            CreateActionButton(row,"Stop","STOP / RETRY",()=>sessionManager?.StopSession());
            CreateActionButton(row,"Reset","RESET BINDINGS",ResetAllBindings);

            tipText = CreateLabel(ctr,"Tip","Focus on smooth movements. Consistency builds precision.",16,FontStyle.Italic,new Vector2(0,0),new Vector2(0,0),new Vector2(28,24),new Vector2(760,28),mutedTextColor,TextAnchor.MiddleLeft);

            stageButtons.Clear();
            var strip = CreateRow(ctr, new Vector2(28,64), 760, 110);
            stageButtons.Add(CreateStageCard(strip,"CIRCLE TRACKING","Track circles with accuracy",0));
            stageButtons.Add(CreateStageCard(strip,"LINE CONTROL","Constant speed on lines",1));
            stageButtons.Add(CreateStageCard(strip,"SHAPE TRACE","Trace complex shapes",4));
        }

        // ── Right panel ──────────────────────────────────────────────────────
        private void CreateRightPanel(RectTransform root)
        {
            var rp = CreatePanel(root,"Right",new Vector2(0.76f,0.23f),new Vector2(1,0.90f),Vector2.zero,new Vector2(-24,0),panelColor);
            AddEdge(rp);
            CreateLabel(rp,"PerfTitle","PERFORMANCE",22,FontStyle.Bold,new Vector2(0,1),new Vector2(0,1),new Vector2(22,-22),new Vector2(220,28),textColor,TextAnchor.MiddleLeft);

            // Radial gauge
            var gauge = CreatePanel(rp,"Gauge",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-80,-286),new Vector2(80,-126),new Color(0.07f,0.08f,0.12f,1));
            AddEdge(gauge);
            var gaugeBg   = CreateFilledImage(gauge,"GaugeBg",  new Color(0.18f,0.20f,0.28f,1), Image.FillMethod.Radial360);
            gaugeBg.fillAmount=1f; gaugeBg.fillOrigin=2; gaugeBg.fillClockwise=true;
            scoreGaugeFill= CreateFilledImage(gauge,"GaugeFill",accentColor,Image.FillMethod.Radial360);
            scoreGaugeFill.fillOrigin=2; scoreGaugeFill.fillClockwise=true; scoreGaugeFill.fillAmount=0f;
            var inner = CreatePanel(gauge,"Inner",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-58,-58),new Vector2(58,58),new Color(0.06f,0.07f,0.10f,.98f));
            AddEdge(inner,new Color(0.3f,0.32f,0.45f,.4f));
            scoreGaugeText = CreateLabel(inner,"Score","0.0",40,FontStyle.Bold,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(120,44),textColor,TextAnchor.MiddleCenter);
            CreateLabel(inner,"Lbl","SCORE",14,FontStyle.Normal,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-38),new Vector2(100,20),mutedTextColor,TextAnchor.MiddleCenter);
            rightPanelSummary = CreateLabel(rp,"Summary","Stage 1/1\nTier 0",14,FontStyle.Normal,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-90,-124),new Vector2(180,66),mutedTextColor,TextAnchor.MiddleCenter);

            accuracyText  = CreateMetricRow(rp,"ACCURACY",  0, new Vector2(22,-174), out accuracyFill);
            smoothnessText= CreateMetricRow(rp,"SMOOTHNESS",0, new Vector2(22,-234), out smoothnessFill);
            speedText     = CreateMetricRow(rp,"SPEED",     0, new Vector2(22,-294), out speedFill);
            deviationText = CreateMetricRow(rp,"REACTION",  0, new Vector2(22,-354), out deviationFill);

            CreateLabel(rp,"BindHdr","CUSTOM INPUTS",18,FontStyle.Bold,new Vector2(0,0),new Vector2(0,0),new Vector2(22,148),new Vector2(180,24),textColor,TextAnchor.MiddleLeft);
            CreateRebindRow(rp,"RightStick","Right Stick",22,118);
            CreateRebindRow(rp,"Aim",       "ADS / LT",  22, 76);
            CreateRebindRow(rp,"Fire",      "Fire / RT",  22, 34);
            CreateRebindRow(rp,"Confirm",   "Start/Submit",22,-8);
            CreateRebindRow(rp,"Cancel",    "Retry/Cancel",22,-50);
        }

        // ── Bottom strip ─────────────────────────────────────────────────────
        private void CreateBottomStrip(RectTransform root)
        {
            var bot = CreatePanel(root,"Bottom",new Vector2(0.18f,0),new Vector2(1,0.19f),new Vector2(24,24),new Vector2(-24,24),new Color(0.08f,0.10f,0.14f,.92f));
            AddEdge(bot);
            controlsText = CreateLabel(bot,"Controls","Press Start to begin. Hold LT for precision mode.",16,FontStyle.Normal,new Vector2(0,1),new Vector2(0,1),new Vector2(22,-20),new Vector2(820,24),mutedTextColor,TextAnchor.MiddleLeft);
        }

        // ── Overlay ──────────────────────────────────────────────────────────
        private void CreateOverlay(RectTransform root)
        {
            overlayPanel = CreatePanel(root,"Overlay",new Vector2(0.28f,0.28f),new Vector2(0.72f,0.72f),Vector2.zero,Vector2.zero,new Color(0.04f,0.05f,0.07f,.95f));
            AddEdge(overlayPanel,accentColor);
            overlayPanel.gameObject.SetActive(false);

            var content = CreatePanel(overlayPanel,"Content",Vector2.zero,Vector2.one,new Vector2(30,30),new Vector2(-30,-30),new Color(0,0,0,0));
            overlayTitleText   = CreateLabel(content,"Title","PAUSED",34,FontStyle.Bold,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-8),new Vector2(0,40),textColor,TextAnchor.MiddleCenter);
            overlayBodyText    = CreateLabel(content,"Body","Session paused.",18,FontStyle.Normal,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-58),new Vector2(0,26),mutedTextColor,TextAnchor.MiddleCenter);
            overlaySummaryText = CreateLabel(content,"Summary","Score 0  |  Peak 0  |  Avg 0  |  Time 0s",16,FontStyle.Bold,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-100),new Vector2(0,24),textColor,TextAnchor.MiddleCenter);

            var btns = CreateRow(content, new Vector2(0,-150), 420, 44);
            overlayPrimaryButton   = CreateOverlayButton(btns,"Primary",  "RESUME",  new Vector2(0,0),   new Vector2(200,44),null);
            overlaySecondaryButton = CreateOverlayButton(btns,"Secondary","RESTART", new Vector2(220,0), new Vector2(200,44),null);
        }

        // ── Widget builders ──────────────────────────────────────────────────
        // FIX: single definition of BuildDeadzoneRow / BuildSensitivityRow (removed duplicates)
        private void BuildDeadzoneRow(RectTransform parent, Vector2 pos)
        {
            var row = CreatePanel(parent,"DZRow",new Vector2(0,1),new Vector2(1,1),new Vector2(pos.x,pos.y-8),new Vector2(-16,pos.y+28),new Color(0,0,0,0));
            CreateLabel(row,"Lbl","Deadzone",12,FontStyle.Bold,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(0,0),new Vector2(80,18),mutedTextColor,TextAnchor.MiddleLeft);
            var val = CreateLabel(row,"Val",currentDeadzone.ToString("0.00"),12,FontStyle.Bold,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-10,0),new Vector2(70,18),textColor,TextAnchor.MiddleCenter);
            MakeAdjustButton(row,"DZDec",new Vector2(-74,-12),new Vector2(-50,12),"-",()=>{
                currentDeadzone=Mathf.Clamp(currentDeadzone-0.02f,0f,0.5f); ApplyDeadzone(); val.text=currentDeadzone.ToString("0.00"); SaveSettings();
            });
            MakeAdjustButton(row,"DZInc",new Vector2(-44,-12),new Vector2(-20,12),"+",()=>{
                currentDeadzone=Mathf.Clamp(currentDeadzone+0.02f,0f,0.5f); ApplyDeadzone(); val.text=currentDeadzone.ToString("0.00"); SaveSettings();
            });
        }

        private void BuildSensitivityRow(RectTransform parent, Vector2 pos)
        {
            var row = CreatePanel(parent,"SensRow",new Vector2(0,1),new Vector2(1,1),new Vector2(pos.x,pos.y-8),new Vector2(-16,pos.y+28),new Color(0,0,0,0));
            CreateLabel(row,"Lbl","Sensitivity",12,FontStyle.Bold,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(0,0),new Vector2(80,18),mutedTextColor,TextAnchor.MiddleLeft);
            var val = CreateLabel(row,"Val",currentSensitivity.ToString("0.0"),12,FontStyle.Bold,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-10,0),new Vector2(70,18),textColor,TextAnchor.MiddleCenter);
            MakeAdjustButton(row,"SDec",new Vector2(-74,-12),new Vector2(-50,12),"-",()=>{
                currentSensitivity=Mathf.Clamp(currentSensitivity-0.5f,2f,16f); ApplySensitivity(); val.text=currentSensitivity.ToString("0.0"); SaveSettings();
            });
            MakeAdjustButton(row,"SInc",new Vector2(-44,-12),new Vector2(-20,12),"+",()=>{
                currentSensitivity=Mathf.Clamp(currentSensitivity+0.5f,2f,16f); ApplySensitivity(); val.text=currentSensitivity.ToString("0.0"); SaveSettings();
            });
        }

        private void MakeAdjustButton(RectTransform parent, string name, Vector2 oMin, Vector2 oMax, string label, UnityAction onClick)
        {
            var r = CreatePanel(parent,name,new Vector2(1,.5f),new Vector2(1,.5f),oMin,oMax,accentColor);
            AddEdge(r,accentColor);
            var btn = r.gameObject.AddComponent<Button>();
            btn.transition=Selectable.Transition.ColorTint; btn.colors=BuildButtonColors();
            btn.onClick.AddListener(onClick);
            CreateLabel(r,"T",label,14,FontStyle.Bold,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(20,18),textColor,TextAnchor.MiddleCenter);
        }

        private void BuildDifficultyButtons(RectTransform parent, Vector2 pos)
        {
            var row = CreatePanel(parent,"DiffRow",new Vector2(0,1),new Vector2(1,1),new Vector2(pos.x,pos.y-8),new Vector2(-16,pos.y+40),new Color(0,0,0,0));
            CreateLabel(row,"Lbl","Difficulty",12,FontStyle.Bold,new Vector2(0,1),new Vector2(0,1),new Vector2(0,-2),new Vector2(90,18),mutedTextColor,TextAnchor.MiddleLeft);
            CreateDiffButton(row,"Easy",  DifficultyPreset.Easy,  new Vector2(0,0),  new Vector2(76,28));
            CreateDiffButton(row,"Normal",DifficultyPreset.Normal,new Vector2(82,0), new Vector2(76,28));
            CreateDiffButton(row,"Hard",  DifficultyPreset.Hard,  new Vector2(164,0),new Vector2(76,28));
        }

        private void CreateDiffButton(RectTransform parent, string label, DifficultyPreset preset, Vector2 oMin, Vector2 size)
        {
            var r = CreatePanel(parent,label,new Vector2(0,0),new Vector2(0,0),oMin,oMin+size,accentColor);
            AddEdge(r,accentColor);
            var btn = r.gameObject.AddComponent<Button>(); btn.transition=Selectable.Transition.ColorTint; btn.colors=BuildButtonColors();
            btn.onClick.AddListener(()=>SetDifficultyPreset(preset));
            RegisterFocusable(btn);
            CreateLabel(r,"T",label,12,FontStyle.Bold,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(70,18),textColor,TextAnchor.MiddleCenter);
        }

        private Text CreateMetricRow(RectTransform parent, string label, float initial, Vector2 pos, out Image fill)
        {
            CreateLabel(parent,label+"Lbl",label,14,FontStyle.Normal,new Vector2(0,1),new Vector2(0,1),pos,new Vector2(220,20),mutedTextColor,TextAnchor.MiddleLeft);
            var txt = CreateLabel(parent,label+"Val","0%",14,FontStyle.Bold,new Vector2(1,1),new Vector2(1,1),new Vector2(-22,pos.y),new Vector2(70,20),textColor,TextAnchor.MiddleRight);
            fill = CreateProgressBar(parent, new Vector2(22,pos.y-22), new Vector2(-22,8), accentColor, initial);
            return txt;
        }

        private Image CreateProgressBar(RectTransform parent, Vector2 pos, Vector2 size, Color fillColor, float initial)
        {
            var bg = CreatePanel(parent,"Bar",new Vector2(0,1),new Vector2(1,1),new Vector2(pos.x,pos.y-size.y),new Vector2(size.x,pos.y),new Color(0.15f,0.17f,0.24f,1));
            var fillRect = CreatePanel(bg,"Fill",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,fillColor);
            var img = fillRect.gameObject.AddComponent<Image>();
            img.color=fillColor; img.type=Image.Type.Filled; img.fillMethod=Image.FillMethod.Horizontal; img.fillOrigin=0; img.fillAmount=Mathf.Clamp01(initial);
            return img;
        }

        private void CreateRebindRow(RectTransform parent, string actionName, string label, float x, float y)
        {
            var row = CreatePanel(parent,actionName+"Row",new Vector2(0,0),new Vector2(1,0),new Vector2(x,y-8),new Vector2(-22,y+26),new Color(0.07f,0.08f,0.11f,1));
            CreateLabel(row,"Name",label,13,FontStyle.Bold,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(12,0),new Vector2(120,20),textColor,TextAnchor.MiddleLeft);
            var bindLbl = CreateLabel(row,"Binding","",12,FontStyle.Normal,new Vector2(.48f,.5f),new Vector2(.48f,.5f),Vector2.zero,new Vector2(110,20),mutedTextColor,TextAnchor.MiddleCenter);
            bindingLabels[actionName] = bindLbl;
            var r = CreatePanel(row,"RebindBtn",new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-92,-14),new Vector2(-12,14),accentColor);
            AddEdge(r,accentColor);
            var btn = r.gameObject.AddComponent<Button>(); btn.transition=Selectable.Transition.ColorTint; btn.colors=BuildButtonColors();
            btn.onClick.AddListener(()=>StartRebind(actionName)); RegisterFocusable(btn);
            CreateLabel(r,"T","Rebind",12,FontStyle.Bold,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(72,18),textColor,TextAnchor.MiddleCenter);
        }

        private void CreateActionButton(RectTransform parent, string name, string label, UnityAction onClick)
        {
            var r = CreatePanel(parent,name,new Vector2(0,0),new Vector2(0,1),Vector2.zero,new Vector2(196,-6),accentColor);
            AddEdge(r,accentColor);
            var btn = r.gameObject.AddComponent<Button>(); btn.transition=Selectable.Transition.ColorTint; btn.colors=BuildButtonColors();
            btn.onClick.AddListener(onClick); RegisterFocusable(btn);
            CreateLabel(r,"T",label,16,FontStyle.Bold,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(180,24),textColor,TextAnchor.MiddleCenter);
        }

        private Button CreateStageCard(RectTransform parent, string title, string subtitle, int stageIndex)
        {
            var card = CreatePanel(parent,title,new Vector2(0,0),new Vector2(0,1),new Vector2(0,8),new Vector2(236,-8),new Color(0.09f,0.10f,0.14f,1));
            AddEdge(card);
            var btn = card.gameObject.AddComponent<Button>(); btn.transition=Selectable.Transition.ColorTint; btn.colors=BuildButtonColors();
            btn.onClick.AddListener(()=>sessionManager?.SelectStage(stageIndex)); RegisterFocusable(btn);
            CreateLabel(card,"T",title,14,FontStyle.Bold,new Vector2(0,1),new Vector2(0,1),new Vector2(16,-16),new Vector2(200,20),textColor,TextAnchor.MiddleLeft);
            CreateLabel(card,"S",subtitle,12,FontStyle.Normal,new Vector2(0,1),new Vector2(0,1),new Vector2(16,-40),new Vector2(200,28),mutedTextColor,TextAnchor.MiddleLeft);
            return btn;
        }

        private Button CreateOverlayButton(RectTransform parent, string name, string label, Vector2 oMin, Vector2 size, UnityAction onClick)
        {
            var r = CreatePanel(parent,name,new Vector2(0,0),new Vector2(0,0),oMin,oMin+size,accentColor);
            AddEdge(r,accentColor);
            var btn = r.gameObject.AddComponent<Button>(); btn.transition=Selectable.Transition.ColorTint; btn.colors=BuildButtonColors();
            if (onClick!=null) btn.onClick.AddListener(onClick); RegisterFocusable(btn);
            CreateLabel(r,"T",label,16,FontStyle.Bold,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(180,24),textColor,TextAnchor.MiddleCenter);
            return btn;
        }

        // ── Low-level helpers ─────────────────────────────────────────────────
        private RectTransform CreatePanel(RectTransform parent, string name, Vector2 ancMin, Vector2 ancMax, Vector2 oMin, Vector2 oMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin=ancMin; rt.anchorMax=ancMax; rt.offsetMin=oMin; rt.offsetMax=oMax;
            go.AddComponent<Image>().color=color;
            return rt;
        }

        private RectTransform CreateRow(RectTransform parent, Vector2 pos, float w, float h) =>
            CreatePanel(parent,"Row",new Vector2(0,0),new Vector2(0,0),pos,new Vector2(pos.x+w,pos.y+h),new Color(0,0,0,0));

        private Image CreateFilledImage(RectTransform parent, string name, Color color, Image.FillMethod fill)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite=Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            img.type=Image.Type.Filled; img.fillMethod=fill; img.fillOrigin=0; img.color=color; img.raycastTarget=false;
            return img;
        }

        private Text CreateLabel(RectTransform parent, string name, string value, int size, FontStyle style,
            Vector2 ancMin, Vector2 ancMax, Vector2 oMin, Vector2 oMax, Color color, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin=ancMin; rt.anchorMax=ancMax; rt.offsetMin=oMin; rt.offsetMax=oMax;
            var t = go.AddComponent<Text>();
            t.text=value; t.font=uiFont; t.fontSize=size; t.fontStyle=style;
            t.color=color; t.alignment=align;
            t.horizontalOverflow=HorizontalWrapMode.Overflow; t.verticalOverflow=VerticalWrapMode.Overflow;
            return t;
        }

        private void AddEdge(RectTransform panel, Color? color=null)
        {
            var go = new GameObject("Edge", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero;
            var img = go.AddComponent<Image>();
            Color c = color ?? panelEdgeColor;
            img.color = new Color(c.r,c.g,c.b,0.04f); img.raycastTarget=false;
        }

        private ColorBlock BuildButtonColors() => new ColorBlock
        {
            normalColor      = new Color(0,0,0,0),
            highlightedColor = new Color(1,1,1,0.06f),
            pressedColor     = new Color(1,1,1,0.12f),
            selectedColor    = new Color(1,1,1,0.06f),
            disabledColor    = new Color(1,1,1,0.02f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f
        };

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
