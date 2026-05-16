using System;
using StickLab.Drills;
using UnityEngine;

namespace StickLab.Core
{
    public class TrainingSessionManager : MonoBehaviour
    {
        public enum SessionState { Ready, Running, Paused, Results }
        public enum DrillKind    { Circle, Line, Shape, InfinityLoop, Spiral, ZigZag, WaveRider }

        [Serializable]
        public struct TrainingStage
        {
            public string   stageName;
            public DrillKind drillKind;
            public LineDrill.LineOrientation lineOrientation;
            public ShapeDrill.ShapeType      shapeType;
            public Vector2  center;
            public float    radius;
            public float    lineLength;
            public float    thickness;
            public float    tolerance;
            public float    passScore;
            public float    holdSeconds;
            public float    spiralTurns;
            public float    spiralInnerRadius;
            public float    zigZagAmplitude;
            public int      zigZagPeaks;
            public float    waveAmplitude;
            public int      waveCount;
        }

        [Header("Core Drills")]
        [SerializeField] private CircleDrill    circleDrill;
        [SerializeField] private LineDrill      lineDrill;
        [SerializeField] private ShapeDrill     shapeDrill;
        [SerializeField] private CursorController cursorController;

        [Header("Extended Drills")]
        [SerializeField] private InfinityLoopDrill infinityDrill;
        [SerializeField] private SpiralDrill       spiralDrill;
        [SerializeField] private ZigZagDrill       zigZagDrill;
        [SerializeField] private WaveRiderDrill    waveRiderDrill;

        [Header("Stages")]
        [SerializeField] private TrainingStage[] stages;
        [SerializeField, Min(0f)] private float difficultyThicknessMultiplier = 0.92f;
        [SerializeField, Min(0f)] private float difficultyToleranceMultiplier = 0.90f;
        [SerializeField, Min(0f)] private float difficultySizeMultiplier      = 0.97f;

        // ── Public state ──────────────────────────────────────────────────────
        public string       CurrentStageName        { get; private set; } = string.Empty;
        public DrillKind    CurrentDrillKind        { get; private set; }
        public int          CurrentStageIndex       { get; private set; }
        public int          DifficultyTier          { get; private set; }
        public int          StageCount              => stages != null ? stages.Length : 0;
        public float        CurrentAccuracy         { get; private set; }
        public float        CurrentSmoothness       { get; private set; }
        public float        CurrentSpeedConsistency { get; private set; }
        public float        CurrentTotalScore       { get; private set; }
        public float        CurrentDeviation        { get; private set; }
        public float        AverageScore            { get; private set; }
        public float        PeakScore               { get; private set; }
        public float        ElapsedSeconds          { get; private set; }
        public int          CompletedStages         { get; private set; }
        public SessionState State                   { get; private set; } = SessionState.Ready;
        public bool         IsPaused                => State == SessionState.Paused;
        public bool         IsRunning               => State == SessionState.Running;
        public bool         HasStarted              => State != SessionState.Ready;
        public float        HoldProgress01          => activeStage.holdSeconds <= Mathf.Epsilon ? 0f : Mathf.Clamp01(successTimer / activeStage.holdSeconds);
        public int          InfinityLoopCrossings   => infinityDrill != null ? infinityDrill.CrossingCount : 0;
        public string       ControllerHintText      => "Start=begin  Retry/R=stop  LT=precision  RT=fire";

        private TrainingStage activeStage;
        private float         successTimer;
        private bool          initialized;

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake() => EnsureStages();

        // ── Wiring ────────────────────────────────────────────────────────────
        public void SetReferences(CircleDrill cd, LineDrill ld, ShapeDrill sd, CursorController cc)
        { circleDrill = cd; lineDrill = ld; shapeDrill = sd; cursorController = cc; }

        public void SetExtendedReferences(InfinityLoopDrill id, SpiralDrill sd2)
        { infinityDrill = id; spiralDrill = sd2; }

        public void SetExtendedReferences2(ZigZagDrill zd, WaveRiderDrill wd)
        { zigZagDrill = zd; waveRiderDrill = wd; }

        // ── Session control ───────────────────────────────────────────────────
        public void BeginSession()
        {
            EnsureStages();
            initialized = true; State = SessionState.Running;
            DifficultyTier = CurrentStageIndex = CompletedStages = 0;
            successTimer = ElapsedSeconds = PeakScore = AverageScore = 0f;
            ActivateStage(0);
        }

        public void StopSession()    { State = SessionState.Paused;  SetAllInactive(); ResetLive(); }
        public void EndSession()     { State = SessionState.Results; SetAllInactive(); }
        public void RestartSession() { State = SessionState.Ready;   SetAllInactive(); ResetLive(); BeginSession(); }

        public void PauseSession()
        { if (State == SessionState.Running) { State = SessionState.Paused; SetAllInactive(); } }

        public void ResumeSession()
        { if (State == SessionState.Paused) { State = SessionState.Running; ActivateStage(CurrentStageIndex); } }

        public void SelectStage(int index)
        {
            EnsureStages();
            if (StageCount == 0) return;
            CurrentStageIndex = Mathf.Clamp(index, 0, StageCount - 1);
            State = SessionState.Running; successTimer = 0f; initialized = true;
            ActivateStage(CurrentStageIndex);
        }

        public void SetDifficultyProfile(float size, float thick, float tol)
        {
            difficultySizeMultiplier      = Mathf.Clamp(size,  0.6f, 1f);
            difficultyThicknessMultiplier = Mathf.Clamp(thick, 0.6f, 1f);
            difficultyToleranceMultiplier = Mathf.Clamp(tol,   0.6f, 1f);
        }

        // ── Tick ──────────────────────────────────────────────────────────────
        public void Tick(Vector2 pos, float dt)
        {
            if (!initialized || !IsRunning || dt <= 0f) return;
            ElapsedSeconds += dt;

            switch (CurrentDrillKind)
            {
                case DrillKind.Circle when circleDrill != null:
                    circleDrill.Tick(pos, dt);
                    Read(circleDrill.AccuracyScore, circleDrill.SmoothnessScore,
                         circleDrill.SpeedConsistencyScore, circleDrill.TotalScore, circleDrill.CurrentDeviation);
                    break;
                case DrillKind.Line when lineDrill != null:
                    lineDrill.Tick(pos, dt);
                    Read(lineDrill.AccuracyScore, lineDrill.SmoothnessScore,
                         lineDrill.SpeedConsistencyScore, lineDrill.TotalScore, lineDrill.CurrentDeviation);
                    break;
                case DrillKind.Shape when shapeDrill != null:
                    shapeDrill.Tick(pos, dt);
                    Read(shapeDrill.AccuracyScore, shapeDrill.SmoothnessScore,
                         shapeDrill.SpeedConsistencyScore, shapeDrill.TotalScore, shapeDrill.CurrentDeviation);
                    break;
                case DrillKind.InfinityLoop when infinityDrill != null:
                    infinityDrill.Tick(pos, dt);
                    Read(infinityDrill.AccuracyScore, infinityDrill.SmoothnessScore,
                         infinityDrill.SpeedConsistencyScore, infinityDrill.TotalScore, infinityDrill.CurrentDeviation);
                    break;
                case DrillKind.Spiral when spiralDrill != null:
                    spiralDrill.Tick(pos, dt);
                    Read(spiralDrill.AccuracyScore, spiralDrill.SmoothnessScore,
                         spiralDrill.SpeedConsistencyScore, spiralDrill.TotalScore, spiralDrill.CurrentDeviation);
                    break;
                case DrillKind.ZigZag when zigZagDrill != null:
                    zigZagDrill.Tick(pos, dt);
                    Read(zigZagDrill.AccuracyScore, zigZagDrill.SmoothnessScore,
                         zigZagDrill.SpeedConsistencyScore, zigZagDrill.TotalScore, zigZagDrill.CurrentDeviation);
                    break;
                case DrillKind.WaveRider when waveRiderDrill != null:
                    waveRiderDrill.Tick(pos, dt);
                    Read(waveRiderDrill.AccuracyScore, waveRiderDrill.SmoothnessScore,
                         waveRiderDrill.SpeedConsistencyScore, waveRiderDrill.TotalScore, waveRiderDrill.CurrentDeviation);
                    break;
            }

            AverageScore = Mathf.Lerp(AverageScore, CurrentTotalScore, 0.08f);
            PeakScore    = Mathf.Max(PeakScore, CurrentTotalScore);
            TickProgress(dt);
        }

        // ── Queries ───────────────────────────────────────────────────────────
        public string GetDrillInstructions()
        {
            if (!HasStarted) return "Press Start to begin training.";
            if (!IsRunning)  return "Session stopped. Press Start or Retry to continue.";
            return CurrentDrillKind switch
            {
                DrillKind.Circle       => "Trace the circle smoothly.",
                DrillKind.Line         => "Hold the stick on the line with constant speed.",
                DrillKind.Shape        => "Follow the full shape without leaving the path.",
                DrillKind.InfinityLoop => "Trace the infinity loop — clean crossings earn bonus points.",
                DrillKind.Spiral       => "Follow the spiral inward with steady speed.",
                DrillKind.ZigZag       => "Follow the zig-zag — sharp corners need quick direction changes.",
                DrillKind.WaveRider    => "Ride the wave — smooth oscillations, constant speed.",
                _                      => string.Empty
            };
        }

        public string GetStageName(int i)
            => (stages != null && i >= 0 && i < stages.Length) ? stages[i].stageName : string.Empty;
        public DrillKind GetStageKind(int i)
            => (stages != null && i >= 0 && i < stages.Length) ? stages[i].drillKind : DrillKind.Circle;

        public string GetStageSummary(int i)
        {
            if (stages == null || i < 0 || i >= stages.Length) return string.Empty;
            var s = stages[i];
            return s.drillKind switch
            {
                DrillKind.Circle       => $"Circle • R={s.radius:0.0}",
                DrillKind.Line         => $"Line • {s.lineOrientation}",
                DrillKind.Shape        => $"Shape • {s.shapeType}",
                DrillKind.InfinityLoop => $"∞ Loop • Scale={s.radius:0.0}",
                DrillKind.Spiral       => $"Spiral • {s.spiralTurns:0.0} turns",
                DrillKind.ZigZag       => $"Zig-Zag • {s.zigZagPeaks} peaks",
                DrillKind.WaveRider    => $"Wave • {s.waveCount} cycles",
                _                      => s.stageName
            };
        }

        // ── Private ───────────────────────────────────────────────────────────
        private void Read(float acc, float sm, float sp, float tot, float dev)
        { CurrentAccuracy=acc; CurrentSmoothness=sm; CurrentSpeedConsistency=sp; CurrentTotalScore=tot; CurrentDeviation=dev; }

        private void TickProgress(float dt)
        {
            if (CurrentTotalScore >= activeStage.passScore)
            { successTimer += dt; if (successTimer >= activeStage.holdSeconds) AdvanceStage(); }
            else successTimer = 0f;
        }

        private void AdvanceStage()
        {
            successTimer = 0f; CompletedStages++; CurrentStageIndex++;
            if (CurrentStageIndex >= StageCount) { CurrentStageIndex=0; DifficultyTier++; EndSession(); return; }
            ActivateStage(CurrentStageIndex);
        }

        private void ActivateStage(int index)
        {
            if (StageCount == 0) return;
            activeStage = stages[index];
            CurrentStageName = activeStage.stageName;
            CurrentDrillKind = activeStage.drillKind;
            SetAllInactive();

            float r   = S(activeStage.radius,    difficultySizeMultiplier);
            float th  = S(activeStage.thickness, difficultyThicknessMultiplier);
            float tol = S(activeStage.tolerance, difficultyToleranceMultiplier);

            switch (activeStage.drillKind)
            {
                case DrillKind.Circle when circleDrill != null:
                    circleDrill.SetParameters(activeStage.center, r, th, tol);
                    circleDrill.BeginDrill(); circleDrill.gameObject.SetActive(true);
                    SetCursor(circleDrill.StartPosition); break;

                case DrillKind.Line when lineDrill != null:
                    float len = S(activeStage.lineLength, difficultySizeMultiplier);
                    lineDrill.SetParameters(activeStage.lineOrientation, len, th, tol, activeStage.center);
                    lineDrill.BeginDrill(); lineDrill.gameObject.SetActive(true);
                    SetCursor(lineDrill.StartPosition); break;

                case DrillKind.Shape when shapeDrill != null:
                    shapeDrill.SetParameters(activeStage.shapeType, r, th, tol, activeStage.center);
                    shapeDrill.BeginDrill(); shapeDrill.gameObject.SetActive(true);
                    SetCursor(shapeDrill.StartPosition); break;

                case DrillKind.InfinityLoop when infinityDrill != null:
                    infinityDrill.SetParameters(activeStage.center, r, th, tol);
                    infinityDrill.BeginDrill(); infinityDrill.gameObject.SetActive(true);
                    SetCursor(infinityDrill.StartPosition); break;

                case DrillKind.Spiral when spiralDrill != null:
                    spiralDrill.SetParameters(activeStage.center, r, th, tol);
                    spiralDrill.BeginDrill(); spiralDrill.gameObject.SetActive(true);
                    SetCursor(spiralDrill.StartPosition); break;

                case DrillKind.ZigZag when zigZagDrill != null:
                    float zLen = S(activeStage.lineLength, difficultySizeMultiplier);
                    float zAmp = S(activeStage.zigZagAmplitude, difficultySizeMultiplier);
                    zigZagDrill.SetParameters(activeStage.center, zLen, zAmp, th, tol);
                    zigZagDrill.BeginDrill(); zigZagDrill.gameObject.SetActive(true);
                    SetCursor(zigZagDrill.StartPosition); break;

                case DrillKind.WaveRider when waveRiderDrill != null:
                    float wLen = S(activeStage.lineLength, difficultySizeMultiplier);
                    float wAmp = S(activeStage.waveAmplitude, difficultySizeMultiplier);
                    waveRiderDrill.SetParameters(activeStage.center, wLen, wAmp, activeStage.waveCount, th, tol);
                    waveRiderDrill.BeginDrill(); waveRiderDrill.gameObject.SetActive(true);
                    SetCursor(waveRiderDrill.StartPosition); break;
            }
        }

        private void SetCursor(Vector2 p) { if (cursorController != null) cursorController.SetPosition(p); }

        private void SetAllInactive()
        {
            if (circleDrill    != null) circleDrill.gameObject.SetActive(false);
            if (lineDrill      != null) lineDrill.gameObject.SetActive(false);
            if (shapeDrill     != null) shapeDrill.gameObject.SetActive(false);
            if (infinityDrill  != null) infinityDrill.gameObject.SetActive(false);
            if (spiralDrill    != null) spiralDrill.gameObject.SetActive(false);
            if (zigZagDrill    != null) zigZagDrill.gameObject.SetActive(false);
            if (waveRiderDrill != null) waveRiderDrill.gameObject.SetActive(false);
        }

        private void ResetLive() =>
            CurrentAccuracy = CurrentSmoothness = CurrentSpeedConsistency =
            CurrentTotalScore = CurrentDeviation = AverageScore = 0f;

        private float S(float v, float m) => v * Mathf.Pow(m, DifficultyTier);

        private void EnsureStages()
        {
            if (stages != null && stages.Length > 0) return;
            stages = new[]
            {
                new TrainingStage { stageName="Circle Warmup",   drillKind=DrillKind.Circle,       center=Vector2.zero, radius=2.4f,  thickness=0.36f, tolerance=0.22f, passScore=78f, holdSeconds=2.5f },
                new TrainingStage { stageName="Horizontal Line", drillKind=DrillKind.Line,         lineOrientation=LineDrill.LineOrientation.Horizontal, center=Vector2.zero, lineLength=6.2f, thickness=0.28f, tolerance=0.16f, passScore=80f, holdSeconds=2.5f },
                new TrainingStage { stageName="Vertical Line",   drillKind=DrillKind.Line,         lineOrientation=LineDrill.LineOrientation.Vertical,   center=Vector2.zero, lineLength=6.0f, thickness=0.26f, tolerance=0.15f, passScore=80f, holdSeconds=2.5f },
                new TrainingStage { stageName="Diagonal Line",   drillKind=DrillKind.Line,         lineOrientation=LineDrill.LineOrientation.DiagonalUp, center=Vector2.zero, lineLength=6.0f, thickness=0.24f, tolerance=0.14f, passScore=82f, holdSeconds=2.5f },
                new TrainingStage { stageName="Triangle Trace",  drillKind=DrillKind.Shape,        shapeType=ShapeDrill.ShapeType.Triangle, center=Vector2.zero, radius=2.1f, thickness=0.22f, tolerance=0.18f, passScore=82f, holdSeconds=3.0f },
                new TrainingStage { stageName="Square Trace",    drillKind=DrillKind.Shape,        shapeType=ShapeDrill.ShapeType.Square,   center=Vector2.zero, radius=2.0f, thickness=0.20f, tolerance=0.17f, passScore=84f, holdSeconds=3.0f },
                new TrainingStage { stageName="Pentagon Trace",  drillKind=DrillKind.Shape,        shapeType=ShapeDrill.ShapeType.Pentagon, center=Vector2.zero, radius=2.0f, thickness=0.18f, tolerance=0.16f, passScore=85f, holdSeconds=3.0f },
                new TrainingStage { stageName="Infinity Loop",   drillKind=DrillKind.InfinityLoop, center=Vector2.zero, radius=2.2f,  thickness=0.24f, tolerance=0.20f, passScore=80f, holdSeconds=3.5f },
                new TrainingStage { stageName="Spiral In",       drillKind=DrillKind.Spiral,       center=Vector2.zero, radius=2.4f,  spiralInnerRadius=0.4f, spiralTurns=2.5f, thickness=0.22f, tolerance=0.18f, passScore=80f, holdSeconds=3.5f },
                new TrainingStage { stageName="Zig Zag",         drillKind=DrillKind.ZigZag,       center=Vector2.zero, lineLength=6.0f, zigZagAmplitude=0.8f, zigZagPeaks=4, thickness=0.24f, tolerance=0.18f, passScore=80f, holdSeconds=3.0f },
                new TrainingStage { stageName="Wave Rider",      drillKind=DrillKind.WaveRider,    center=Vector2.zero, lineLength=6.5f, waveAmplitude=0.75f, waveCount=2,   thickness=0.22f, tolerance=0.17f, passScore=82f, holdSeconds=3.0f },
            };
        }
    }
}
