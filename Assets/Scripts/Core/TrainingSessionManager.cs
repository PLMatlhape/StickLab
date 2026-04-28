using System;
using StickLab.Drills;
using UnityEngine;

namespace StickLab.Core
{
    public class TrainingSessionManager : MonoBehaviour
    {
        public enum DrillKind
        {
            Circle,
            Line,
            Shape
        }

        [Serializable]
        public struct TrainingStage
        {
            public string stageName;
            public DrillKind drillKind;
            public LineDrill.LineOrientation lineOrientation;
            public ShapeDrill.ShapeType shapeType;
            public Vector2 center;
            public float radius;
            public float lineLength;
            public float thickness;
            public float tolerance;
            public float passScore;
            public float holdSeconds;
        }

        [Header("Drills")]
        [SerializeField] private CircleDrill circleDrill;
        [SerializeField] private LineDrill lineDrill;
        [SerializeField] private ShapeDrill shapeDrill;
        [SerializeField] private CursorController cursorController;

        [Header("Stages")]
        [SerializeField] private TrainingStage[] stages;
        [SerializeField, Min(0f)] private float difficultyThicknessMultiplier = 0.92f;
        [SerializeField, Min(0f)] private float difficultyToleranceMultiplier = 0.90f;
        [SerializeField, Min(0f)] private float difficultySizeMultiplier = 0.97f;

        public string CurrentStageName { get; private set; } = string.Empty;
        public DrillKind CurrentDrillKind { get; private set; }
        public int CurrentStageIndex { get; private set; }
        public int DifficultyTier { get; private set; }
        public int StageCount => stages != null ? stages.Length : 0;
        public float CurrentAccuracy { get; private set; }
        public float CurrentSmoothness { get; private set; }
        public float CurrentSpeedConsistency { get; private set; }
        public float CurrentTotalScore { get; private set; }
        public float CurrentDeviation { get; private set; }
        public float HoldProgress01 => activeStage.holdSeconds <= Mathf.Epsilon ? 0f : Mathf.Clamp01(successTimer / activeStage.holdSeconds);
        public bool IsRunning { get; private set; }
        public bool HasStarted { get; private set; }
        public string ControllerHintText => "Controller: Start = begin/restart, Retry/R = stop";

        private TrainingStage activeStage;
        private float successTimer;
        private bool initialized;

        private void Awake()
        {
            EnsureStages();
        }

        private void Start()
        {
        }

        public void SetReferences(CircleDrill newCircleDrill, LineDrill newLineDrill, ShapeDrill newShapeDrill, CursorController newCursorController)
        {
            circleDrill = newCircleDrill;
            lineDrill = newLineDrill;
            shapeDrill = newShapeDrill;
            cursorController = newCursorController;
        }

        public void BeginSession()
        {
            EnsureStages();
            initialized = true;
            HasStarted = true;
            DifficultyTier = 0;
            CurrentStageIndex = 0;
            successTimer = 0f;
            IsRunning = true;
            ActivateStage(CurrentStageIndex);
        }

        public void StopSession()
        {
            IsRunning = false;
            SetAllDrillsInactive();
            ResetLiveMetrics();
        }

        public void RestartSession()
        {
            StopSession();
            BeginSession();
        }

        public void SetDifficultyProfile(float sizeMultiplier, float thicknessMultiplier, float toleranceMultiplier)
        {
            difficultySizeMultiplier = Mathf.Clamp(sizeMultiplier, 0.6f, 1f);
            difficultyThicknessMultiplier = Mathf.Clamp(thicknessMultiplier, 0.6f, 1f);
            difficultyToleranceMultiplier = Mathf.Clamp(toleranceMultiplier, 0.6f, 1f);
        }

        public void Tick(Vector2 cursorPosition, float deltaTime)
        {
            if (!initialized || !IsRunning || deltaTime <= 0f)
            {
                return;
            }

            switch (CurrentDrillKind)
            {
                case DrillKind.Circle:
                    if (circleDrill != null)
                    {
                        circleDrill.Tick(cursorPosition, deltaTime);
                        CurrentAccuracy = circleDrill.AccuracyScore;
                        CurrentSmoothness = circleDrill.SmoothnessScore;
                        CurrentSpeedConsistency = circleDrill.SpeedConsistencyScore;
                        CurrentTotalScore = circleDrill.TotalScore;
                        CurrentDeviation = circleDrill.CurrentDeviation;
                    }
                    break;
                case DrillKind.Line:
                    if (lineDrill != null)
                    {
                        lineDrill.Tick(cursorPosition, deltaTime);
                        CurrentAccuracy = lineDrill.AccuracyScore;
                        CurrentSmoothness = lineDrill.SmoothnessScore;
                        CurrentSpeedConsistency = lineDrill.SpeedConsistencyScore;
                        CurrentTotalScore = lineDrill.TotalScore;
                        CurrentDeviation = lineDrill.CurrentDeviation;
                    }
                    break;
                case DrillKind.Shape:
                    if (shapeDrill != null)
                    {
                        shapeDrill.Tick(cursorPosition, deltaTime);
                        CurrentAccuracy = shapeDrill.AccuracyScore;
                        CurrentSmoothness = shapeDrill.SmoothnessScore;
                        CurrentSpeedConsistency = shapeDrill.SpeedConsistencyScore;
                        CurrentTotalScore = shapeDrill.TotalScore;
                        CurrentDeviation = shapeDrill.CurrentDeviation;
                    }
                    break;
            }

            UpdateProgress(deltaTime);
        }

        public string GetDrillInstructions()
        {
            if (!HasStarted)
            {
                return "Press Start to begin training.";
            }

            if (!IsRunning)
            {
                return "Session stopped. Press Start or Retry to continue.";
            }

            return CurrentDrillKind switch
            {
                DrillKind.Circle => "Trace the circle smoothly.",
                DrillKind.Line => "Hold the stick on the line with constant speed.",
                DrillKind.Shape => "Follow the full shape without leaving the path.",
                _ => string.Empty
            };
        }

        private void UpdateProgress(float deltaTime)
        {
            if (CurrentTotalScore >= activeStage.passScore)
            {
                successTimer += deltaTime;
                if (successTimer >= activeStage.holdSeconds)
                {
                    AdvanceStage();
                }
            }
            else
            {
                successTimer = 0f;
            }
        }

        private void AdvanceStage()
        {
            successTimer = 0f;
            CurrentStageIndex++;

            if (CurrentStageIndex >= StageCount)
            {
                CurrentStageIndex = 0;
                DifficultyTier++;
            }

            ActivateStage(CurrentStageIndex);
        }

        private void ActivateStage(int stageIndex)
        {
            if (StageCount == 0)
            {
                return;
            }

            activeStage = stages[stageIndex];
            CurrentStageName = activeStage.stageName;
            CurrentDrillKind = activeStage.drillKind;

            SetActiveDrillObjects(false);

            switch (activeStage.drillKind)
            {
                case DrillKind.Circle:
                    if (circleDrill != null)
                    {
                        float radius = ScaleByDifficulty(activeStage.radius, difficultySizeMultiplier);
                        float thickness = ScaleByDifficulty(activeStage.thickness, difficultyThicknessMultiplier);
                        float tolerance = ScaleByDifficulty(activeStage.tolerance, difficultyToleranceMultiplier);
                        circleDrill.SetParameters(activeStage.center, radius, thickness, tolerance);
                        circleDrill.BeginDrill();
                        circleDrill.gameObject.SetActive(true);
                        SetCursorTo(circleDrill.StartPosition);
                    }
                    break;
                case DrillKind.Line:
                    if (lineDrill != null)
                    {
                        float length = ScaleByDifficulty(activeStage.lineLength, difficultySizeMultiplier);
                        float thickness = ScaleByDifficulty(activeStage.thickness, difficultyThicknessMultiplier);
                        float tolerance = ScaleByDifficulty(activeStage.tolerance, difficultyToleranceMultiplier);
                        lineDrill.SetParameters(activeStage.lineOrientation, length, thickness, tolerance, activeStage.center);
                        lineDrill.BeginDrill();
                        lineDrill.gameObject.SetActive(true);
                        SetCursorTo(lineDrill.StartPosition);
                    }
                    break;
                case DrillKind.Shape:
                    if (shapeDrill != null)
                    {
                        float radius = ScaleByDifficulty(activeStage.radius, difficultySizeMultiplier);
                        float thickness = ScaleByDifficulty(activeStage.thickness, difficultyThicknessMultiplier);
                        float tolerance = ScaleByDifficulty(activeStage.tolerance, difficultyToleranceMultiplier);
                        shapeDrill.SetParameters(activeStage.shapeType, radius, thickness, tolerance, activeStage.center);
                        shapeDrill.BeginDrill();
                        shapeDrill.gameObject.SetActive(true);
                        SetCursorTo(shapeDrill.StartPosition);
                    }
                    break;
            }
        }

        private void SetCursorTo(Vector2 position)
        {
            if (cursorController != null)
            {
                cursorController.SetPosition(position);
            }
        }

        private void SetActiveDrillObjects(bool isActive)
        {
            if (circleDrill != null)
            {
                circleDrill.gameObject.SetActive(isActive && activeStage.drillKind == DrillKind.Circle);
            }

            if (lineDrill != null)
            {
                lineDrill.gameObject.SetActive(isActive && activeStage.drillKind == DrillKind.Line);
            }

            if (shapeDrill != null)
            {
                shapeDrill.gameObject.SetActive(isActive && activeStage.drillKind == DrillKind.Shape);
            }
        }

        private void SetAllDrillsInactive()
        {
            if (circleDrill != null)
            {
                circleDrill.gameObject.SetActive(false);
            }

            if (lineDrill != null)
            {
                lineDrill.gameObject.SetActive(false);
            }

            if (shapeDrill != null)
            {
                shapeDrill.gameObject.SetActive(false);
            }
        }

        private void ResetLiveMetrics()
        {
            CurrentAccuracy = 0f;
            CurrentSmoothness = 0f;
            CurrentSpeedConsistency = 0f;
            CurrentTotalScore = 0f;
            CurrentDeviation = 0f;
        }

        private float ScaleByDifficulty(float value, float multiplier)
        {
            return value * Mathf.Pow(multiplier, DifficultyTier);
        }

        private void EnsureStages()
        {
            if (stages != null && stages.Length > 0)
            {
                return;
            }

            stages = new[]
            {
                new TrainingStage
                {
                    stageName = "Circle Warmup",
                    drillKind = DrillKind.Circle,
                    center = Vector2.zero,
                    radius = 2.4f,
                    thickness = 0.36f,
                    tolerance = 0.22f,
                    passScore = 78f,
                    holdSeconds = 2.5f
                },
                new TrainingStage
                {
                    stageName = "Horizontal Line",
                    drillKind = DrillKind.Line,
                    lineOrientation = LineDrill.LineOrientation.Horizontal,
                    center = Vector2.zero,
                    lineLength = 6.2f,
                    thickness = 0.28f,
                    tolerance = 0.16f,
                    passScore = 80f,
                    holdSeconds = 2.5f
                },
                new TrainingStage
                {
                    stageName = "Vertical Line",
                    drillKind = DrillKind.Line,
                    lineOrientation = LineDrill.LineOrientation.Vertical,
                    center = Vector2.zero,
                    lineLength = 6.0f,
                    thickness = 0.26f,
                    tolerance = 0.15f,
                    passScore = 80f,
                    holdSeconds = 2.5f
                },
                new TrainingStage
                {
                    stageName = "Diagonal Line",
                    drillKind = DrillKind.Line,
                    lineOrientation = LineDrill.LineOrientation.DiagonalUp,
                    center = Vector2.zero,
                    lineLength = 6.0f,
                    thickness = 0.24f,
                    tolerance = 0.14f,
                    passScore = 82f,
                    holdSeconds = 2.5f
                },
                new TrainingStage
                {
                    stageName = "Triangle Trace",
                    drillKind = DrillKind.Shape,
                    shapeType = ShapeDrill.ShapeType.Triangle,
                    center = Vector2.zero,
                    radius = 2.1f,
                    thickness = 0.22f,
                    tolerance = 0.18f,
                    passScore = 82f,
                    holdSeconds = 3.0f
                },
                new TrainingStage
                {
                    stageName = "Square Trace",
                    drillKind = DrillKind.Shape,
                    shapeType = ShapeDrill.ShapeType.Square,
                    center = Vector2.zero,
                    radius = 2.0f,
                    thickness = 0.20f,
                    tolerance = 0.17f,
                    passScore = 84f,
                    holdSeconds = 3.0f
                },
                new TrainingStage
                {
                    stageName = "Pentagon Trace",
                    drillKind = DrillKind.Shape,
                    shapeType = ShapeDrill.ShapeType.Pentagon,
                    center = Vector2.zero,
                    radius = 2.0f,
                    thickness = 0.18f,
                    tolerance = 0.16f,
                    passScore = 85f,
                    holdSeconds = 3.0f
                }
            };
        }
    }
}
