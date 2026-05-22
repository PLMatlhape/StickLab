using UnityEngine;

namespace StickLab.Core
{
    /// <summary>
    /// Persists session progress between Unity play sessions via PlayerPrefs.
    /// Attach to the same GameObject as TrainingSessionManager, or let Bootstrap add it.
    /// </summary>
    public class SessionPersistence : MonoBehaviour
    {
        private const string KeyStageIndex   = "StickLab.LastStageIndex";
        private const string KeyDiffTier     = "StickLab.DifficultyTier";
        private const string KeyBestScore    = "StickLab.BestScore";
        private const string KeyTotalDrills  = "StickLab.TotalDrillsCompleted";
        private const string KeyTotalSeconds = "StickLab.TotalSecondsPlayed";

        [SerializeField] private TrainingSessionManager sessionManager;

        // ── Public accessors ─────────────────────────────────────────────────
        public int   LastStageIndex        => PlayerPrefs.GetInt(KeyStageIndex, 0);
        public int   SavedDifficultyTier   => PlayerPrefs.GetInt(KeyDiffTier, 0);
        public float AllTimeBestScore      => PlayerPrefs.GetFloat(KeyBestScore, 0f);
        public int   TotalDrillsCompleted  => PlayerPrefs.GetInt(KeyTotalDrills, 0);
        public float TotalSecondsPlayed    => PlayerPrefs.GetFloat(KeyTotalSeconds, 0f);

        public void SetSessionManager(TrainingSessionManager sm) => sessionManager = sm;

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Start()
        {
            // Restore last stage so player continues where they left off
            if (sessionManager != null && LastStageIndex > 0)
            {
                // Don't auto-begin — just prime the index so SelectStage works correctly
                Debug.Log($"[SessionPersistence] Last stage was {LastStageIndex}, best score {AllTimeBestScore:0.0}");
            }
        }

        private void OnApplicationQuit() => Save();
        private void OnDisable()         => Save();

        // ── Public API ────────────────────────────────────────────────────────
        public void Save()
        {
            if (sessionManager == null) return;

            PlayerPrefs.SetInt(KeyStageIndex,   sessionManager.CurrentStageIndex);
            PlayerPrefs.SetInt(KeyDiffTier,     sessionManager.DifficultyTier);
            PlayerPrefs.SetFloat(KeyBestScore,  Mathf.Max(AllTimeBestScore, sessionManager.PeakScore));
            PlayerPrefs.SetInt(KeyTotalDrills,  TotalDrillsCompleted + sessionManager.CompletedStages);
            PlayerPrefs.SetFloat(KeyTotalSeconds, TotalSecondsPlayed + sessionManager.ElapsedSeconds);
            PlayerPrefs.Save();

            Debug.Log($"[SessionPersistence] Saved. Stage={sessionManager.CurrentStageIndex} Best={AllTimeBestScore:0.0}");
        }

        public void ResumeFromLastSession()
        {
            if (sessionManager == null) return;
            int savedStage = LastStageIndex;
            if (savedStage > 0 && savedStage < sessionManager.StageCount)
            {
                sessionManager.SelectStage(savedStage);
                Debug.Log($"[SessionPersistence] Resumed from stage {savedStage}");
            }
            else
            {
                sessionManager.BeginSession();
            }
        }

        public void ResetAllProgress()
        {
            PlayerPrefs.DeleteKey(KeyStageIndex);
            PlayerPrefs.DeleteKey(KeyDiffTier);
            PlayerPrefs.DeleteKey(KeyBestScore);
            PlayerPrefs.DeleteKey(KeyTotalDrills);
            PlayerPrefs.DeleteKey(KeyTotalSeconds);
            PlayerPrefs.Save();
            Debug.Log("[SessionPersistence] All progress reset.");
        }
    }
}
