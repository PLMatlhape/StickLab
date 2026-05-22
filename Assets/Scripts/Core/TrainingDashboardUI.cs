using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StickLab.Core
{
    public class TrainingDashboardUI : MonoBehaviour
    {
        private enum NavPage { Dashboard, Drills, Settings }
        private enum DifficultyPreset { Easy, Normal, Hard }

        [Header("References")]
        [SerializeField] private InputHandler           inputHandler;
        [SerializeField] private CursorController       cursorController;
        [SerializeField] private TrainingSessionManager sessionManager;

        [Header("Colors")]
        [SerializeField] private Color colBg      = new Color(0.05f,0.06f,0.09f,1f);
        [SerializeField] private Color colPanel   = new Color(0.09f,0.11f,0.15f,1f);
        [SerializeField] private Color colAccent  = new Color(0.55f,0.40f,1.00f,1f);
        [SerializeField] private Color colText    = new Color(0.95f,0.96f,1.00f,1f);
        [SerializeField] private Color colMuted   = new Color(0.60f,0.65f,0.75f,1f);
        [SerializeField] private Color colSuccess = new Color(0.20f,0.90f,0.50f,1f);
        [SerializeField] private Font  uiFont;

        [Header("Layout")]
        [SerializeField] private bool buildOnAwake = true;

        // Runtime state
        private Canvas   canvas;
        private NavPage  currentPage = NavPage.Dashboard;
        private float    currentDeadzone    = 0.18f;
        private float    currentSensitivity = 8f;
        private DifficultyPreset currentDiff = DifficultyPreset.Normal;
        private int      cardOffset = 0;
        private const int CardsPerPage = 5;

        // UI refs
        private Text   topConn, topProfile;
        private readonly Button[] navBtns  = new Button[7];
        private readonly Image[]  navHi    = new Image[7];
        private Text   stageName, stageSub, stageInstr, tipText, precLabel;
        private Button startBtn;
        private Text   startBtnTxt;
        private RectTransform cardStrip;
        private readonly List<RectTransform> cards = new List<RectTransform>();
        private Text   pageLabel, perfStage;
        private Image  gaugeFill;
        private Text   gaugeText;
        private Text   accTxt,smTxt,spTxt,rkTxt;
        private Image  accBar,smBar,spBar,rkBar;
        private RectTransform overlayPanel;
        private CanvasGroup   overlayGroup;
        private Text   overlayTitle, overlayBody, overlaySummary;
        private SmoothFillDisplay   gaugeDisp,accFD,smFD,spFD,rkFD;
        private SmoothNumberDisplay accND,smND,spND,rkND;
        private SmoothPanelTransition overlayFade;
        private bool lastOverlay;
        private readonly Dictionary<string,Text> bindLabels = new Dictionary<string,Text>();
        private bool isRebinding;
        private InputActionRebindingExtensions.RebindingOperation activeRebind;
        private readonly List<Button> focusOrder = new List<Button>();
        private int focusIdx = -1;
        private float navTimer;
        private const float NavDelay = 0.16f;
        private const string DZKey="StickLab.Settings.Deadzone",SensKey="StickLab.Settings.Sensitivity",DiffKey="StickLab.Settings.Difficulty";

        private void Awake()
        {
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (buildOnAwake) BuildUi();
        }
        private void Start()
        {
            LoadSettings(); ApplyDeadzone(); ApplySensitivity(); ApplyDiff();
            RefreshBindings(); EnsureFocus(); InitSmooth(); RefreshCards();
        }
        private void Update()
        {
            if (sessionManager==null||inputHandler==null) return;
            HandleNav();
            if (inputHandler.ConfirmPressedThisFrame) ActivateFocused();
            if (inputHandler.RetryPressedThisFrame)   TogglePause();
            if (Input.GetKeyDown(KeyCode.F1)&&canvas!=null) canvas.enabled=!canvas.enabled;
        }
        private void LateUpdate() => RefreshUi();
        public void SetReferences(InputHandler ih,CursorController cc,TrainingSessionManager sm)
        { inputHandler=ih; cursorController=cc; sessionManager=sm; }

        // ── BUILD ─────────────────────────────────────────────────────────────
        void BuildUi()
        {
            EnsureES();
            var go=new GameObject("Dashboard Canvas");
            go.transform.SetParent(transform,false);
            canvas=go.AddComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=100;
            go.AddComponent<GraphicRaycaster>();
            var cs=go.AddComponent<CanvasScaler>();
            cs.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution=new Vector2(1920f,1080f); cs.matchWidthOrHeight=0.5f;
            var root=canvas.GetComponent<RectTransform>();
            P(root,"BG",V2(0),V2(1),V2(0),V2(0),colBg).raycastTarget=false;
            BuildTopBar(root); BuildSidebar(root); BuildCenter(root); BuildRight(root); BuildOverlay(root);
        }

        void BuildTopBar(RectTransform r)
        {
            var bar=R(r,"TopBar",new Vector2(0.16f,0.935f),V2(1),V2(4),-V2(4),new Color(0.07f,0.08f,0.12f,1));
            topConn   =L(bar,"Conn","● Disconnected",15,FontStyle.Bold,MH(0),MH(0),new Vector2(16,0),new Vector2(220,22),colMuted,TextAnchor.MiddleLeft);
            topProfile=L(bar,"Prof","Aimer • Ready",14,FontStyle.Normal,MH(1),MH(1),new Vector2(-16,0),new Vector2(220,22),colMuted,TextAnchor.MiddleRight);
        }

        void BuildSidebar(RectTransform r)
        {
            var sb=R(r,"Sidebar",V2(0),new Vector2(0.16f,1),new Vector2(8,8),new Vector2(-4,-8),colPanel);
            L(sb,"Logo","STICK LAB",22,FontStyle.Bold,new Vector2(0,1),new Vector2(1,1),new Vector2(14,-26),new Vector2(-14,28),colText,TextAnchor.MiddleLeft);
            L(sb,"Sub","CONTROLLER TRAINING",10,FontStyle.Normal,new Vector2(0,1),new Vector2(1,1),new Vector2(14,-52),new Vector2(-14,16),colAccent,TextAnchor.MiddleLeft);

            string[] lbls={"⌂  Dashboard","⊕  Drills","✎  Training Plan","▤  Stats","◷  History","♛  Leaderboards","⚙  Settings"};
            NavPage[] pgs={NavPage.Dashboard,NavPage.Drills,NavPage.Drills,NavPage.Dashboard,NavPage.Dashboard,NavPage.Dashboard,NavPage.Settings};
            for(int i=0;i<lbls.Length;i++)
            {
                int ci=i; NavPage cp=pgs[i]; float y=-80f-i*48f;
                var item=R(sb,"Nav"+i,new Vector2(0,1),new Vector2(1,1),new Vector2(6,y-18),new Vector2(-6,y+20),new Color(0,0,0,0));
                navHi[i]=item.GetComponent<Image>();
                var btn=item.gameObject.AddComponent<Button>();
                btn.transition=Selectable.Transition.ColorTint; btn.colors=NavColors();
                btn.onClick.AddListener(()=>SwitchPage(cp));
                navBtns[i]=btn; focusOrder.Add(btn);
                L(item,"T",lbls[i],13,FontStyle.Normal,V2(0),V2(1),new Vector2(14,2),new Vector2(-8,-2),colMuted,TextAnchor.MiddleLeft);
            }

            // Controller card
            var cc=R(sb,"CC",new Vector2(0,0),new Vector2(1,0),new Vector2(8,136),new Vector2(-8,8),new Color(0.06f,0.07f,0.10f,1));
            L(cc,"H","CONTROLLER",11,FontStyle.Bold,new Vector2(0,1),new Vector2(1,1),new Vector2(10,-10),new Vector2(-10,16),colText,TextAnchor.MiddleLeft);
            L(cc,"S","Disconnected",11,FontStyle.Normal,new Vector2(0,1),new Vector2(1,1),new Vector2(10,-28),new Vector2(-10,16),colMuted,TextAnchor.MiddleLeft);
            L(cc,"H2","LT=ADS  RT=Fire",10,FontStyle.Normal,new Vector2(0,1),new Vector2(1,1),new Vector2(10,-46),new Vector2(-10,14),colMuted,TextAnchor.MiddleLeft);
            L(cc,"H3","QUICK SETTINGS",11,FontStyle.Bold,new Vector2(0,1),new Vector2(1,1),new Vector2(10,-66),new Vector2(-10,16),colText,TextAnchor.MiddleLeft);
            DZRow(cc,-88f); SensRow(cc,-110f);
        }

        void BuildCenter(RectTransform r)
        {
            var ctr=R(r,"Center",new Vector2(0.16f,0.07f),new Vector2(0.78f,0.935f),new Vector2(6,6),new Vector2(-6,-4),colPanel);

            stageName =L(ctr,"SN","READY",26,FontStyle.Bold,new Vector2(0,1),new Vector2(1,1),new Vector2(18,-28),new Vector2(-18,32),colText,TextAnchor.MiddleLeft);
            stageSub  =L(ctr,"SS","Press Enter to begin",13,FontStyle.Normal,new Vector2(0,1),new Vector2(1,1),new Vector2(18,-62),new Vector2(-18,22),colMuted,TextAnchor.MiddleLeft);
            stageInstr=L(ctr,"SI","",14,FontStyle.Normal,new Vector2(0,1),new Vector2(1,1),new Vector2(18,-84),new Vector2(-18,22),colText,TextAnchor.MiddleLeft);
            precLabel =L(ctr,"ADS","ADS: OFF",13,FontStyle.Bold,new Vector2(1,1),new Vector2(1,1),new Vector2(-18,-28),new Vector2(110,22),colMuted,TextAnchor.MiddleRight);

            // Large preview area
            var prev=R(ctr,"Prev",new Vector2(0,1),new Vector2(1,1),new Vector2(14,-108),new Vector2(-14,-350),new Color(0.04f,0.05f,0.08f,1));
            prev.GetComponent<Image>().raycastTarget=false;
            L(prev,"PH","● DRILL PREVIEW  —  Paths render here during session",11,FontStyle.Normal,new Vector2(0,1),new Vector2(1,1),new Vector2(12,-12),new Vector2(-12,18),colMuted,TextAnchor.MiddleLeft);
            L(prev,"PH2","Move cursor with  WASD / Right Stick  after pressing START",13,FontStyle.Normal,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(-260,-10),new Vector2(520,20),colMuted,TextAnchor.MiddleCenter);
            L(prev,"PH3","The drill path (circle / line / shape) will appear here in the Game viewport",11,FontStyle.Normal,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(-260,14),new Vector2(520,18),new Color(0.40f,0.35f,0.60f,1f),TextAnchor.MiddleCenter);

            // Action buttons
            var br=R(ctr,"Btns",new Vector2(0,1),new Vector2(1,1),new Vector2(14,-366),new Vector2(-14,-314),new Color(0,0,0,0));
            br.GetComponent<Image>().raycastTarget=false;
            startBtn=MkBtn(br,"Start","▶  START SESSION",new Vector2(0,0),new Vector2(0.32f,1),colAccent,()=>{ sessionManager?.BeginSession(); RefreshCards(); });
            startBtnTxt=startBtn.GetComponentInChildren<Text>();
            MkBtn(br,"Stop","■  STOP",new Vector2(0.34f,0),new Vector2(0.56f,1),new Color(0.18f,0.08f,0.08f,1),()=>sessionManager?.StopSession());
            MkBtn(br,"Reset","↺  Reset Bindings",new Vector2(0.58f,0),new Vector2(1f,1),new Color(0.10f,0.12f,0.17f,1),ResetAllBindings);

            // Tip
            var tip=R(ctr,"Tip",new Vector2(0,1),new Vector2(1,1),new Vector2(14,-374),new Vector2(-14,-342),new Color(0.06f,0.07f,0.10f,1));
            tip.GetComponent<Image>().raycastTarget=false;
            L(tip,"Tag","TIP",10,FontStyle.Bold,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(8,0),new Vector2(28,14),colAccent,TextAnchor.MiddleLeft);
            tipText=L(tip,"T","Smooth and steady. Consistency builds precision.",12,FontStyle.Normal,new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(42,0),new Vector2(-50,14),colMuted,TextAnchor.MiddleLeft);

            // Category header
            L(ctr,"CatH","DRILL CATEGORIES",10,FontStyle.Bold,new Vector2(0,1),new Vector2(1,1),new Vector2(14,-352),new Vector2(-14,16),colMuted,TextAnchor.MiddleLeft);

            // Card strip + pagination
            cardStrip=R(ctr,"Strip",new Vector2(0,0),new Vector2(1,0),new Vector2(54,52),new Vector2(-54,190),new Color(0,0,0,0));
            cardStrip.GetComponent<Image>().raycastTarget=false;

            MkBtn(ctr,"Prev","◀",new Vector2(0,0),new Vector2(0,0),new Color(0.12f,0.14f,0.20f,1),
                ()=>{ cardOffset=Mathf.Max(0,cardOffset-CardsPerPage); RefreshCards(); },
                new Vector2(14,52),new Vector2(50,190));
            MkBtn(ctr,"Next","▶",new Vector2(1,0),new Vector2(1,0),new Color(0.12f,0.14f,0.20f,1),
                ()=>{ if(sessionManager!=null) cardOffset=Mathf.Min(Mathf.Max(0,sessionManager.StageCount-CardsPerPage),cardOffset+CardsPerPage); RefreshCards(); },
                new Vector2(-50,52),new Vector2(-14,190));

            pageLabel=L(ctr,"PgLbl","",10,FontStyle.Normal,new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(-60,12),new Vector2(120,16),colMuted,TextAnchor.MiddleCenter);
        }

        void BuildRight(RectTransform r)
        {
            var rp=R(r,"Right",new Vector2(0.78f,0.07f),V2(1),new Vector2(4,6),new Vector2(-8,-4),colPanel);
            L(rp,"H","SESSION STATS",12,FontStyle.Bold,new Vector2(0,1),new Vector2(1,1),new Vector2(12,-18),new Vector2(-12,18),colText,TextAnchor.MiddleLeft);

            // ── Compact gauge: fixed pixel height so it never pushes metrics off ──
            // Gauge container: 120px tall, centred horizontally
            var ga=R(rp,"Gauge",new Vector2(0.5f,1),new Vector2(0.5f,1),new Vector2(-60,-148),new Vector2(60,-28),new Color(0.07f,0.08f,0.12f,1));
            ga.GetComponent<Image>().raycastTarget=false;
            // Radial fill ring
            var fg=new GameObject("GF",typeof(RectTransform)); fg.transform.SetParent(ga,false);
            var frt=fg.GetComponent<RectTransform>();
            frt.anchorMin=V2(0.05f); frt.anchorMax=V2(0.95f); frt.offsetMin=frt.offsetMax=V2(0);
            gaugeFill=fg.AddComponent<Image>(); gaugeFill.color=colAccent;
            gaugeFill.type=Image.Type.Filled; gaugeFill.fillMethod=Image.FillMethod.Radial360;
            gaugeFill.fillOrigin=2; gaugeFill.fillClockwise=true;
            gaugeFill.fillAmount=0f; gaugeFill.raycastTarget=false;
            // Inner dark circle
            var ig=new GameObject("GI",typeof(RectTransform)); ig.transform.SetParent(ga,false);
            var irt=ig.GetComponent<RectTransform>();
            irt.anchorMin=V2(0.18f); irt.anchorMax=V2(0.82f); irt.offsetMin=irt.offsetMax=V2(0);
            ig.AddComponent<Image>().color=colPanel;
            // Score number centred inside
            gaugeText=L(ga,"GN","0.0",26,FontStyle.Bold,
                new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),
                new Vector2(-36,-14),new Vector2(72,30),colText,TextAnchor.MiddleCenter);
            L(ga,"GL","OVERALL SCORE",8,FontStyle.Normal,
                new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),
                new Vector2(-50,-30),new Vector2(100,14),colMuted,TextAnchor.MiddleCenter);

            // Compact stat rows to the right of gauge
            string[] statNames={"Accuracy","Consistency","Smoothness","Speed"};
            for(int si=0;si<statNames.Length;si++){
                float sy=-42f-si*20f;
                L(rp,statNames[si]+"SL",statNames[si],10,FontStyle.Normal,
                    new Vector2(0.5f,1),new Vector2(0.85f,1),new Vector2(4,sy),new Vector2(-4,sy+18),colMuted,TextAnchor.MiddleLeft);
                L(rp,statNames[si]+"SV","—",10,FontStyle.Bold,
                    new Vector2(0.85f,1),new Vector2(1,1),new Vector2(0,sy),new Vector2(-12,sy+18),colSuccess,TextAnchor.MiddleRight);
            }

            // Stage info — one line, tight
            perfStage=L(rp,"PI","Stage 1/1 • Tier 0 • 0.0s",10,FontStyle.Normal,
                new Vector2(0,1),new Vector2(1,1),new Vector2(12,-156),new Vector2(-12,16),colMuted,TextAnchor.MiddleCenter);

            // ── Metric bars — each 32px tall, explicit pixel offsets ──
            float y=-180f;
            accTxt=MetricBar(rp,"ACCURACY",  y,out accBar); y-=34f;
            smTxt =MetricBar(rp,"SMOOTHNESS",y,out smBar);  y-=34f;
            spTxt =MetricBar(rp,"SPEED",     y,out spBar);  y-=34f;
            rkTxt =MetricBar(rp,"REACTION",  y,out rkBar);

            // Divider
            var dv=R(rp,"Dv",new Vector2(0,1),new Vector2(1,1),new Vector2(12,-320),new Vector2(-12,-317),new Color(0.18f,0.20f,0.28f,1));
            dv.GetComponent<Image>().raycastTarget=false;

            L(rp,"BH","BINDINGS",11,FontStyle.Bold,new Vector2(0,1),new Vector2(1,1),new Vector2(12,-328),new Vector2(-12,16),colText,TextAnchor.MiddleLeft);
            float by=-350f;
            BindRow(rp,"RightStick","Right Stick",by); by-=30f;
            BindRow(rp,"Aim",       "ADS / LT",  by);  by-=30f;
            BindRow(rp,"Fire",      "Fire / RT",  by);  by-=30f;
            BindRow(rp,"Confirm",   "Start",     by);   by-=30f;
            BindRow(rp,"Cancel",    "Retry",     by);
        }

        void BuildOverlay(RectTransform r)
        {
            overlayPanel=R(r,"Overlay",new Vector2(0.25f,0.25f),new Vector2(0.75f,0.75f),V2(0),V2(0),new Color(0.04f,0.05f,0.08f,0.97f));
            overlayGroup=overlayPanel.gameObject.AddComponent<CanvasGroup>();
            overlayGroup.alpha=0f; overlayGroup.interactable=false; overlayGroup.blocksRaycasts=false;
            overlayPanel.gameObject.SetActive(false);
            var inn=R(overlayPanel,"Inn",V2(0),V2(1),new Vector2(28,28),new Vector2(-28,-28),new Color(0,0,0,0));
            inn.GetComponent<Image>().raycastTarget=false;
            overlayTitle  =L(inn,"T","PAUSED",34,FontStyle.Bold,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-10),new Vector2(0,42),colText,TextAnchor.MiddleCenter);
            overlayBody   =L(inn,"B","",16,FontStyle.Normal,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-60),new Vector2(0,26),colMuted,TextAnchor.MiddleCenter);
            overlaySummary=L(inn,"S","",13,FontStyle.Bold,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-98),new Vector2(0,22),colText,TextAnchor.MiddleCenter);
            var brow=R(inn,"BR",new Vector2(0,1),new Vector2(1,1),new Vector2(0,-156),new Vector2(0,-108),new Color(0,0,0,0));
            brow.GetComponent<Image>().raycastTarget=false;
            MkBtn(brow,"Res","RESUME",  new Vector2(0,0),new Vector2(0.47f,1),colAccent,()=>{ sessionManager?.ResumeSession(); });
            MkBtn(brow,"Rst","RESTART", new Vector2(0.53f,0),V2(1),new Color(0.15f,0.12f,0.25f,1),()=>{ sessionManager?.RestartSession(); });
        }

        // ── WIDGET BUILDERS ───────────────────────────────────────────────────
        void DZRow(RectTransform p,float y)
        {
            L(p,"DL","Deadzone",10,FontStyle.Normal,new Vector2(0,1),new Vector2(0,1),new Vector2(10,y),new Vector2(70,14),colMuted,TextAnchor.MiddleLeft);
            var v=L(p,"DV",currentDeadzone.ToString("0.00"),10,FontStyle.Bold,new Vector2(0.5f,1),new Vector2(0.5f,1),new Vector2(-18,y),new Vector2(36,14),colText,TextAnchor.MiddleCenter);
            Mini(p,"DD","-",new Vector2(1,1),new Vector2(1,1),new Vector2(-50,y-1),new Vector2(-30,y+13),()=>{ currentDeadzone=Mathf.Clamp(currentDeadzone-0.02f,0f,0.5f);ApplyDeadzone();v.text=currentDeadzone.ToString("0.00");SaveSettings(); });
            Mini(p,"DI","+",new Vector2(1,1),new Vector2(1,1),new Vector2(-26,y-1),new Vector2(-6,y+13), ()=>{ currentDeadzone=Mathf.Clamp(currentDeadzone+0.02f,0f,0.5f);ApplyDeadzone();v.text=currentDeadzone.ToString("0.00");SaveSettings(); });
        }
        void SensRow(RectTransform p,float y)
        {
            L(p,"SL","Sensitivity",10,FontStyle.Normal,new Vector2(0,1),new Vector2(0,1),new Vector2(10,y),new Vector2(70,14),colMuted,TextAnchor.MiddleLeft);
            var v=L(p,"SV",currentSensitivity.ToString("0.0"),10,FontStyle.Bold,new Vector2(0.5f,1),new Vector2(0.5f,1),new Vector2(-18,y),new Vector2(36,14),colText,TextAnchor.MiddleCenter);
            Mini(p,"SD","-",new Vector2(1,1),new Vector2(1,1),new Vector2(-50,y-1),new Vector2(-30,y+13),()=>{ currentSensitivity=Mathf.Clamp(currentSensitivity-0.5f,2f,16f);ApplySensitivity();v.text=currentSensitivity.ToString("0.0");SaveSettings(); });
            Mini(p,"SI","+",new Vector2(1,1),new Vector2(1,1),new Vector2(-26,y-1),new Vector2(-6,y+13), ()=>{ currentSensitivity=Mathf.Clamp(currentSensitivity+0.5f,2f,16f);ApplySensitivity();v.text=currentSensitivity.ToString("0.0");SaveSettings(); });
        }
        void BindRow(RectTransform p,string action,string lbl,float y)
        {
            var row=R(p,action+"R",new Vector2(0,1),new Vector2(1,1),new Vector2(12,y-13),new Vector2(-12,y+13),new Color(0.06f,0.07f,0.10f,1));
            row.GetComponent<Image>().raycastTarget=false;
            L(row,"N",lbl,10,FontStyle.Normal,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(6,0),new Vector2(90,16),colText,TextAnchor.MiddleLeft);
            var bl=L(row,"B","—",9,FontStyle.Normal,new Vector2(0.4f,.5f),new Vector2(0.4f,.5f),V2(0),new Vector2(80,16),colMuted,TextAnchor.MiddleCenter);
            bindLabels[action]=bl;
            Mini(row,"Rb","Rebind",new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-60,-10),new Vector2(-4,10),()=>StartRebind(action));
        }
        Text MetricBar(RectTransform p,string lbl,float y,out Image fill)
        {
            // Label on left
            L(p,lbl+"L",lbl,11,FontStyle.Normal,new Vector2(0,1),new Vector2(0.6f,1),new Vector2(12,y),new Vector2(-4,16),colMuted,TextAnchor.MiddleLeft);
            // Value % on right
            var val=L(p,lbl+"V","0%",11,FontStyle.Bold,new Vector2(1,1),new Vector2(1,1),new Vector2(-12,y),new Vector2(42,16),colText,TextAnchor.MiddleRight);
            // Bar background — 8px tall, sits below label row
            var bg=R(p,lbl+"BG",new Vector2(0,1),new Vector2(1,1),new Vector2(12,y-20),new Vector2(-12,y-10),new Color(0.08f,0.09f,0.13f,1));
            bg.GetComponent<Image>().raycastTarget=false;
            // Fill — anchored 0→0 width, grows via fillAmount (Horizontal Filled)
            var fg=new GameObject(lbl+"F",typeof(RectTransform));
            fg.transform.SetParent(bg,false);
            var frt=fg.GetComponent<RectTransform>();
            frt.anchorMin=V2(0); frt.anchorMax=V2(1);
            frt.offsetMin=frt.offsetMax=V2(0);
            fill=fg.AddComponent<Image>();
            fill.color=colAccent; fill.type=Image.Type.Filled;
            fill.fillMethod=Image.FillMethod.Horizontal;
            fill.fillOrigin=0; fill.fillAmount=0f; fill.raycastTarget=false;
            return val;
        }

        // ── REFRESH ───────────────────────────────────────────────────────────
        void RefreshCards()
        {
            if (cardStrip==null) return;
            foreach(var c in cards) if(c!=null) Destroy(c.gameObject);
            cards.Clear(); focusOrder.RemoveAll(b=>b==null);
            int total=sessionManager!=null?sessionManager.StageCount:0;
            int end=Mathf.Min(cardOffset+CardsPerPage,total);
            float cw=1f/CardsPerPage;
            for(int i=cardOffset;i<end;i++)
            {
                int idx=i; bool active=sessionManager!=null&&i==sessionManager.CurrentStageIndex&&sessionManager.HasStarted;
                float xMin=(i-cardOffset)*cw, xMax=xMin+cw-0.012f;
                var card=R(cardStrip,"C"+i,new Vector2(xMin,0),new Vector2(xMax,1),new Vector2(3,3),new Vector2(-3,-3),
                    active?new Color(0.18f,0.14f,0.34f,1):new Color(0.07f,0.08f,0.12f,1));
                cards.Add(card);
                if(active){ var ab=R(card,"AB",new Vector2(0,1),new Vector2(1,1),new Vector2(0,-3),V2(0),colAccent); ab.GetComponent<Image>().raycastTarget=false; }
                L(card,"N",sessionManager.GetStageName(i),11,FontStyle.Bold,new Vector2(0,1),new Vector2(1,1),new Vector2(8,-12),new Vector2(-8,18),colText,TextAnchor.MiddleLeft);
                L(card,"S",sessionManager.GetStageSummary(i),9,FontStyle.Normal,new Vector2(0,1),new Vector2(1,1),new Vector2(8,-28),new Vector2(-8,16),colMuted,TextAnchor.MiddleLeft);
                var btn=card.gameObject.AddComponent<Button>(); btn.transition=Selectable.Transition.ColorTint; btn.colors=CardColors();
                btn.onClick.AddListener(()=>{
                    if(sessionManager==null) return;
                    // If session not running, begin it then jump to chosen stage
                    if(!sessionManager.IsRunning)
                        sessionManager.BeginSession();
                    sessionManager.SelectStage(idx);
                    RefreshCards();
                });
                focusOrder.Add(btn);
            }
            if(pageLabel!=null&&total>0) pageLabel.text=$"{cardOffset+1}–{end} of {total}";
        }

        void RefreshUi()
        {
            if(sessionManager==null) return;
            // Nav highlights
            for(int i=0;i<navHi.Length;i++){
                if(navHi[i]==null) continue;
                bool s=(i==0&&currentPage==NavPage.Dashboard)||(i==1&&currentPage==NavPage.Drills)||(i==6&&currentPage==NavPage.Settings);
                navHi[i].color=s?new Color(0.20f,0.14f,0.38f,1):new Color(0,0,0,0);
                var t=navHi[i].GetComponentInChildren<Text>(); if(t!=null) t.color=s?colText:colMuted;
            }
            // Top bar
            if(topConn!=null){ bool c=inputHandler!=null&&inputHandler.ControllerConnected; topConn.text=c?"● Connected":"● Disconnected"; topConn.color=c?colSuccess:colMuted; }
            if(topProfile!=null&&inputHandler!=null) topProfile.text=inputHandler.AimHeld?"ADS Active • Precision Mode":"Aimer • Ready";
            // Center
            if(stageName!=null)  stageName.text=sessionManager.HasStarted?sessionManager.CurrentStageName:"READY";
            if(stageSub!=null)   stageSub.text=sessionManager.GetStageSummary(sessionManager.CurrentStageIndex);
            if(stageInstr!=null) stageInstr.text=sessionManager.GetDrillInstructions();
            if(precLabel!=null&&inputHandler!=null){ bool a=inputHandler.AimHeld; precLabel.text=a?"ADS: ON":"ADS: OFF"; precLabel.color=a?colAccent:colMuted; }
            if(startBtnTxt!=null) startBtnTxt.text=sessionManager.IsRunning?"■  STOP SESSION":"▶  START SESSION";
            if(tipText!=null) tipText.text=sessionManager.State switch{
                TrainingSessionManager.SessionState.Running=>"Smooth and steady. Consistency builds precision.",
                TrainingSessionManager.SessionState.Paused =>"Session paused. Resume or restart.",
                TrainingSessionManager.SessionState.Results=>"Session complete! Check your stats.",
                _=>"Press START or click a drill card to begin."};
            // Metrics
            float tot=sessionManager.CurrentTotalScore;
            gaugeDisp?.SetFill(tot/100f,0.2f);
            if(gaugeText!=null&&gaugeDisp!=null) gaugeText.text=(gaugeDisp.CurrentFill*100f).ToString("0.0");
            accFD?.SetFill(sessionManager.CurrentAccuracy/100f,0.15f);
            smFD?.SetFill(sessionManager.CurrentSmoothness/100f,0.15f);
            spFD?.SetFill(sessionManager.CurrentSpeedConsistency/100f,0.15f);
            float rk=1f-Mathf.Clamp01(sessionManager.CurrentDeviation/0.5f);
            rkFD?.SetFill(rk,0.15f);
            accND?.SetValue(sessionManager.CurrentAccuracy,0.1f);
            smND?.SetValue(sessionManager.CurrentSmoothness,0.1f);
            spND?.SetValue(sessionManager.CurrentSpeedConsistency,0.1f);
            rkND?.SetValue(rk*100f,0.1f);
            if(accTxt!=null&&accND!=null) accTxt.text=accND.CurrentValue.ToString("0")+"%";
            if(smTxt !=null&&smND !=null) smTxt.text =smND.CurrentValue.ToString("0") +"%";
            if(spTxt !=null&&spND !=null) spTxt.text =spND.CurrentValue.ToString("0") +"%";
            if(rkTxt !=null&&rkND !=null) rkTxt.text =rkND.CurrentValue.ToString("0") +"%";
            if(perfStage!=null) perfStage.text=$"Stage {sessionManager.CurrentStageIndex+1}/{sessionManager.StageCount}  •  Tier {sessionManager.DifficultyTier}  •  {sessionManager.ElapsedSeconds:0.0}s";
            RefreshBindings();
            UpdateOverlay();
        }

        void UpdateOverlay()
        {
            if(overlayPanel==null||overlayFade==null) return;
            bool show=sessionManager.State==TrainingSessionManager.SessionState.Paused||sessionManager.State==TrainingSessionManager.SessionState.Results;
            if(show!=lastOverlay){ lastOverlay=show; overlayPanel.gameObject.SetActive(true); if(show) overlayFade.FadeIn(0.25f); else overlayFade.FadeOut(0.2f); }
            if(!show) return;
            if(overlayTitle!=null)   overlayTitle.text=sessionManager.State==TrainingSessionManager.SessionState.Results?"RESULTS":"PAUSED";
            if(overlayBody!=null)    overlayBody.text=sessionManager.State==TrainingSessionManager.SessionState.Results?"Session complete. Restart when ready.":"Session paused. Resume to continue.";
            if(overlaySummary!=null) overlaySummary.text=$"Score {sessionManager.CurrentTotalScore:0.0}  |  Peak {sessionManager.PeakScore:0.0}  |  Avg {sessionManager.AverageScore:0.0}  |  {sessionManager.ElapsedSeconds:0.0}s";
        }

        void InitSmooth()
        {
            gaugeDisp=new SmoothFillDisplay(gaugeFill);
            accFD=new SmoothFillDisplay(accBar); smFD=new SmoothFillDisplay(smBar);
            spFD=new SmoothFillDisplay(spBar);   rkFD=new SmoothFillDisplay(rkBar);
            // FIX: force all bars to 0 immediately so they don't show as full on first frame
            gaugeDisp.SetFillInstant(0f);
            accFD.SetFillInstant(0f); smFD.SetFillInstant(0f);
            spFD.SetFillInstant(0f);  rkFD.SetFillInstant(0f);
            accND=new SmoothNumberDisplay(accTxt,"0"); smND=new SmoothNumberDisplay(smTxt,"0");
            spND=new SmoothNumberDisplay(spTxt,"0");   rkND=new SmoothNumberDisplay(rkTxt,"0");
            if(overlayPanel!=null){
                overlayGroup=overlayPanel.GetComponent<CanvasGroup>()??overlayPanel.gameObject.AddComponent<CanvasGroup>();
                overlayFade=new SmoothPanelTransition(overlayGroup);
                overlayFade.SetAlphaInstant(0f);
            }
        }

        // ── SETTINGS ─────────────────────────────────────────────────────────
        void LoadSettings(){ currentDeadzone=PlayerPrefs.GetFloat(DZKey,currentDeadzone); currentSensitivity=PlayerPrefs.GetFloat(SensKey,currentSensitivity); currentDiff=(DifficultyPreset)PlayerPrefs.GetInt(DiffKey,(int)DifficultyPreset.Normal); }
        void SaveSettings(){ PlayerPrefs.SetFloat(DZKey,currentDeadzone); PlayerPrefs.SetFloat(SensKey,currentSensitivity); PlayerPrefs.SetInt(DiffKey,(int)currentDiff); PlayerPrefs.Save(); }
        void ApplyDeadzone()   => inputHandler?.SetDeadzone(currentDeadzone);
        void ApplySensitivity()=> cursorController?.SetSensitivity(currentSensitivity);
        void ApplyDiff(){ if(sessionManager==null) return; var(sz,th,tol)=currentDiff switch{DifficultyPreset.Easy=>(1f,1f,1f),DifficultyPreset.Hard=>(0.92f,0.84f,0.80f),_=>(0.97f,0.92f,0.90f)}; sessionManager.SetDifficultyProfile(sz,th,tol); }
        void SwitchPage(NavPage p)
        {
            currentPage = p;
            // When switching to Drills page, ensure cards are visible and reset offset
            if (p == NavPage.Drills)
            {
                cardOffset = 0;
                RefreshCards();
            }
        }

        // ── REBINDING ─────────────────────────────────────────────────────────
        void RefreshBindings(){ if(inputHandler==null) return; foreach(var kv in bindLabels){ var a=inputHandler.FindAction(kv.Key); kv.Value.text=a!=null?a.GetBindingDisplayString():"—"; } }
        void StartRebind(string n){ if(inputHandler==null||isRebinding) return; var a=inputHandler.FindAction(n); if(a==null) return; isRebinding=true; a.Disable(); activeRebind=a.PerformInteractiveRebinding(0).WithCancelingThrough("<Keyboard>/escape").WithControlsExcluding("Mouse").OnCancel(_=>EndRebind(a)).OnComplete(_=>EndRebind(a)); activeRebind.Start(); }
        void EndRebind(InputAction a){ activeRebind?.Dispose(); activeRebind=null; a.Enable(); inputHandler?.SaveBindingOverrides(); isRebinding=false; RefreshBindings(); }
        void ResetAllBindings(){ var asset=inputHandler?.GetInputActionsAsset(); if(asset==null) return; asset.RemoveAllBindingOverrides(); PlayerPrefs.DeleteKey(inputHandler.BindingOverridesKey); PlayerPrefs.Save(); RefreshBindings(); }

        // ── CONTROLLER NAV ────────────────────────────────────────────────────
        void HandleNav(){ if(focusOrder.Count==0) return; Vector2 nav=Vector2.zero; if(Gamepad.current!=null){nav+=Gamepad.current.leftStick.ReadValue();nav+=Gamepad.current.dpad.ReadValue();} if(Keyboard.current!=null&&Keyboard.current.tabKey.wasPressedThisFrame) nav.y=-1f; if(nav.sqrMagnitude<0.35f){navTimer=0f;return;} navTimer-=Time.unscaledDeltaTime; if(navTimer>0f) return; int dir=Mathf.Abs(nav.y)>=Mathf.Abs(nav.x)?(nav.y>0f?-1:1):(nav.x>0f?1:-1); focusIdx=Mathf.Clamp(focusIdx+dir,0,focusOrder.Count-1); var b=focusOrder[focusIdx]; if(b!=null) EventSystem.current?.SetSelectedGameObject(b.gameObject); navTimer=NavDelay; }
        void EnsureFocus(){ if(focusOrder.Count==0) return; focusIdx=0; EventSystem.current?.SetSelectedGameObject(focusOrder[0]?.gameObject); }
        void ActivateFocused(){ if(focusIdx<0||focusIdx>=focusOrder.Count) return; var b=focusOrder[focusIdx]; if(b!=null&&b.interactable) b.onClick.Invoke(); }
        void TogglePause(){ if(sessionManager==null) return; switch(sessionManager.State){ case TrainingSessionManager.SessionState.Running: sessionManager.PauseSession(); break; case TrainingSessionManager.SessionState.Paused: sessionManager.ResumeSession(); break; case TrainingSessionManager.SessionState.Results: sessionManager.RestartSession(); break; } }

        // ── PRIMITIVES ────────────────────────────────────────────────────────
        static Vector2 V2(float v)=>new Vector2(v,v);
        static Vector2 MH(float a)=>new Vector2(a,0.5f);
        Image P(RectTransform p,string n,Vector2 a0,Vector2 a1,Vector2 o0,Vector2 o1,Color c){ var go=new GameObject(n,typeof(RectTransform)); go.transform.SetParent(p,false); var rt=go.GetComponent<RectTransform>(); rt.anchorMin=a0;rt.anchorMax=a1;rt.offsetMin=o0;rt.offsetMax=o1; var img=go.AddComponent<Image>(); img.color=c; return img; }
        RectTransform R(RectTransform p,string n,Vector2 a0,Vector2 a1,Vector2 o0,Vector2 o1,Color c)=>P(p,n,a0,a1,o0,o1,c).rectTransform;
        Text L(RectTransform p,string n,string txt,int sz,FontStyle fs,Vector2 a0,Vector2 a1,Vector2 o0,Vector2 o1,Color c,TextAnchor al){ var go=new GameObject(n,typeof(RectTransform)); go.transform.SetParent(p,false); var rt=go.GetComponent<RectTransform>(); rt.anchorMin=a0;rt.anchorMax=a1;rt.offsetMin=o0;rt.offsetMax=o1; var t=go.AddComponent<Text>(); t.font=uiFont;t.text=txt;t.fontSize=sz;t.fontStyle=fs;t.color=c;t.alignment=al;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow; return t; }
        Button MkBtn(RectTransform p,string n,string lbl,Vector2 a0,Vector2 a1,Color bg,UnityAction onClick,Vector2 o0=default,Vector2 o1=default){ var rt=R(p,n,a0,a1,o0,o1,bg); var btn=rt.gameObject.AddComponent<Button>(); btn.transition=Selectable.Transition.ColorTint; btn.colors=CardColors(); if(onClick!=null) btn.onClick.AddListener(onClick); focusOrder.Add(btn); L(rt,"T",lbl,13,FontStyle.Bold,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(-70,-11),new Vector2(70,11),colText,TextAnchor.MiddleCenter); return btn; }
        void Mini(RectTransform p,string n,string lbl,Vector2 a0,Vector2 a1,Vector2 o0,Vector2 o1,UnityAction onClick){ var rt=R(p,n,a0,a1,o0,o1,colAccent); var btn=rt.gameObject.AddComponent<Button>(); btn.transition=Selectable.Transition.ColorTint; btn.colors=CardColors(); btn.onClick.AddListener(onClick); L(rt,"T",lbl,11,FontStyle.Bold,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),V2(-8),V2(8),colText,TextAnchor.MiddleCenter); }
        ColorBlock NavColors()=>new ColorBlock{normalColor=new Color(0,0,0,0),highlightedColor=new Color(0.20f,0.14f,0.38f,1),pressedColor=new Color(0.28f,0.20f,0.48f,1),selectedColor=new Color(0.20f,0.14f,0.38f,1),disabledColor=new Color(0.3f,0.3f,0.3f,0.5f),colorMultiplier=1f,fadeDuration=0.1f};
        ColorBlock CardColors()=>new ColorBlock{normalColor=new Color(0,0,0,0),highlightedColor=new Color(1,1,1,0.08f),pressedColor=new Color(1,1,1,0.15f),selectedColor=new Color(1,1,1,0.08f),disabledColor=new Color(1,1,1,0.02f),colorMultiplier=1f,fadeDuration=0.1f};
        void EnsureES(){ if(EventSystem.current!=null) return; var go=new GameObject("EventSystem"); go.AddComponent<EventSystem>(); go.AddComponent<StandaloneInputModule>(); }
    }
}
