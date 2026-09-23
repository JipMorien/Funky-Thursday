using System.Collections;
using FunkyThursday.Core;
using FunkyThursday.Data;
using FunkyThursday.Gameplay;
using FunkyThursday.Visuals;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FunkyThursday.Flow
{
    /// <summary>
    /// Persistent game flow: which song is selected, moving between the Menu and Gameplay scenes
    /// with a fade, and recording results. Lives in the Menu scene and survives scene loads.
    /// Gameplay also runs without it (opened directly in the editor) by falling back to its own library.
    /// </summary>
    public sealed class SongManager : MonoBehaviour
    {
        public const string MenuScene = "Menu";
        public const string GameplayScene = "Gameplay";

        public enum MenuEntry
        {
            Title,
            LevelSelect
        }

        [SerializeField] SongLibrary library;
        [SerializeField, Min(0f)] float fadeSeconds = 0.3f;

        CanvasGroup _fade;
        bool _loading;

        public static SongManager Instance { get; private set; }

        public SongLibrary Library => library;
        public int CurrentIndex { get; private set; }
        public SongData CurrentSong => library != null ? library.Get(CurrentIndex) : null;

        /// <summary>Which menu screen the Menu scene should open on next.</summary>
        public MenuEntry NextMenuEntry { get; private set; } = MenuEntry.Title;

        /// <summary>The last reported round, for the Level Select to highlight a new best.</summary>
        public RoundStats? LastResult { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildFade();
            GameSettings.ApplyAudio();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlaySong(int index)
        {
            if (library == null || library.Get(index) == null) return;
            CurrentIndex = index;
            LoadScene(GameplayScene);
        }

        public void ReturnToLevelSelect()
        {
            NextMenuEntry = MenuEntry.LevelSelect;
            LoadScene(MenuScene);
        }

        public void ReturnToTitle()
        {
            NextMenuEntry = MenuEntry.Title;
            LoadScene(MenuScene);
        }

        /// <summary>Called by the menu once it has opened the requested screen.</summary>
        public void ConsumeMenuEntry() => NextMenuEntry = MenuEntry.Title;

        public void ReportResult(SongData song, RoundStats stats)
        {
            LastResult = stats;
            if (song != null) SaveData.Submit(song.id, stats);
        }

        public void LoadScene(string sceneName)
        {
            if (_loading) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        /// <summary>Scene change that works with or without a SongManager in the scene.</summary>
        public static void GoTo(string sceneName)
        {
            if (Instance != null) Instance.LoadScene(sceneName);
            else if (Application.CanStreamedLevelBeLoaded(sceneName)) SceneManager.LoadScene(sceneName);
            else Debug.LogWarning($"Scene '{sceneName}' is not in Build Settings. Run Funky Thursday → Build All Scenes.");
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            _loading = true;
            _fade.blocksRaycasts = true;
            yield return Fade(0f, 1f);

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            while (!load.isDone) yield return null;

            GameSettings.ApplyAudio();
            yield return Fade(1f, 0f);
            _fade.blocksRaycasts = false;
            _loading = false;
        }

        IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                _fade.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
                yield return null;
            }
            _fade.alpha = to;
        }

        void BuildFade()
        {
            var canvasObject = new GameObject("Fade Canvas", typeof(Canvas), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var image = new GameObject("Black", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(canvasObject.transform, false);
            image.color = GothicPalette.Void;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            _fade = canvasObject.GetComponent<CanvasGroup>();
            _fade.alpha = 0f;
            _fade.blocksRaycasts = false;
            _fade.interactable = false;
        }
    }
}
