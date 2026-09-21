using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunkyThursday.Core
{
    /// <summary>
    /// Persists across scene loads (DontDestroyOnLoad) and carries the
    /// player's level choice and last run's result from the level-select
    /// screen through gameplay to the results screen, since those scenes
    /// never exist at the same time to pass this directly.
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        public const string MainMenuScene = "MainMenu";
        public const string LevelSelectScene = "LevelSelect";
        public const string GameplayScene = "Gameplay";
        public const string ResultsScene = "Results";

        public LevelData SelectedLevel { get; private set; }
        public bool LastRunWon { get; private set; }
        public int LastRunScore { get; private set; }
        public int LastRunMaxCombo { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Creates a GameFlowManager if none exists yet - lets any scene be opened and played directly in the editor.</summary>
        public static void EnsureExists()
        {
            if (Instance != null)
            {
                return;
            }

            new GameObject("GameFlowManager").AddComponent<GameFlowManager>();
        }

        public void GoToMainMenu() => SceneManager.LoadScene(MainMenuScene);

        public void GoToLevelSelect() => SceneManager.LoadScene(LevelSelectScene);

        public void StartLevel(LevelData level)
        {
            SelectedLevel = level;
            SceneManager.LoadScene(GameplayScene);
        }

        public void FinishLevel(bool won, int score, int maxCombo)
        {
            LastRunWon = won;
            LastRunScore = score;
            LastRunMaxCombo = maxCombo;
            SceneManager.LoadScene(ResultsScene);
        }

        public void RetryLevel()
        {
            if (SelectedLevel != null)
            {
                SceneManager.LoadScene(GameplayScene);
            }
            else
            {
                GoToLevelSelect();
            }
        }
    }
}
