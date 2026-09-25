using System.Collections;
using FunkyThursday.Core;
using FunkyThursday.Data;
using FunkyThursday.Flow;
using FunkyThursday.Placeholders;
using FunkyThursday.Visuals;
using UnityEngine;

namespace FunkyThursday.UI.Menus
{
    /// <summary>
    /// Runs the Menu scene: builds the Title, Level Select and Options screens, switches between them,
    /// and owns the menu music (title theme, per-song previews) and the backdrop behind the UI.
    /// </summary>
    public sealed class MenuController : MonoBehaviour
    {
        [SerializeField] SongLibrary library;
        [SerializeField] RectTransform screensRoot;
        [SerializeField] SpriteRenderer backdrop;
        [SerializeField] SpriteAnimator backdropAnimator;
        [SerializeField] AudioSource music;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.7f;
        [SerializeField, Min(0.05f)] float crossfadeSeconds = 0.35f;

        MainMenuScreen _main;
        LevelSelectScreen _levelSelect;
        OptionsScreen _options;
        MenuScreen _current;
        AudioClip _pendingClip;
        float _pendingStart;
        float _musicBpm = 100f;
        Coroutine _crossfade;

        public SongLibrary Library =>
            SongManager.Instance != null && SongManager.Instance.Library != null ? SongManager.Instance.Library : library;

        /// <summary>0 on each beat of the menu music, rising to 1 just before the next.</summary>
        public float BeatFraction
        {
            get
            {
                if (music == null || !music.isPlaying || _musicBpm <= 0f) return 1f;
                return Mathf.Repeat(music.time * _musicBpm / 60f, 1f);
            }
        }

        void Start()
        {
            GameSettings.ApplyAudio();
            if (Library == null || Library.Count == 0)
            {
                Debug.LogError("MenuController has no Song Library. Run Funky Thursday → Build All Scenes.", this);
                enabled = false;
                return;
            }

            _main = CreateScreen<MainMenuScreen>("Title Screen");
            _levelSelect = CreateScreen<LevelSelectScreen>("Level Select Screen");
            _options = CreateScreen<OptionsScreen>("Options Screen");

            ShowBackdrop(Library.Get(0), true);

            SongManager manager = SongManager.Instance;
            if (manager != null && manager.NextMenuEntry == SongManager.MenuEntry.LevelSelect)
            {
                _levelSelect.SelectIndex(manager.CurrentIndex);
                Switch(_levelSelect);
                manager.ConsumeMenuEntry();
            }
            else
            {
                Switch(_main);
            }
        }

        public void OpenMain() => Switch(_main);
        public void OpenLevelSelect() => Switch(_levelSelect);
        public void OpenOptions() => Switch(_options);

        /// <summary>Opens the Level Select on the secret night (after the title-screen code).</summary>
        public void OpenSecret()
        {
            int index = Library.SecretIndex;
            if (index >= 0) _levelSelect.SelectIndex(index);
            Switch(_levelSelect);
        }

        public void StartSong(int index)
        {
            if (SongManager.Instance != null)
            {
                StartCoroutine(FadeMusicOut());
                SongManager.Instance.PlaySong(index);
            }
            else
            {
                Debug.LogWarning("No SongManager in the scene; add one or run Funky Thursday → Build All Scenes.", this);
            }
        }

        /// <summary>Title theme: the first song's instrumental from the top.</summary>
        public void PlayTitleMusic()
        {
            SongData first = Library.Get(0);
            if (first != null) PlayMusic(first.instrumental, 0f, first.Bpm);
            ShowBackdrop(first, true);
        }

        /// <summary>Level Select preview: backdrop and music follow the highlighted song.</summary>
        public void Preview(SongData song, bool unlocked)
        {
            ShowBackdrop(song, unlocked);
            if (unlocked) PlayMusic(song.instrumental, song.previewStart, song.Bpm);
        }

        void Switch(MenuScreen next)
        {
            if (_current == next) return;
            if (_current != null) _current.Close();
            _current = next;
            _current.Open();
        }

        T CreateScreen<T>(string name) where T : MenuScreen
        {
            RectTransform rect = UIFactory.Stretch(name, screensRoot);
            var screen = rect.gameObject.AddComponent<T>();
            screen.Initialize(this);
            return screen;
        }

        void ShowBackdrop(SongData song, bool unlocked)
        {
            if (backdrop == null || song == null) return;

            if (backdropAnimator != null && SpriteAnimation.IsUsable(song.backdrop))
            {
                backdropAnimator.Play(song.backdrop, restart: false);
                backdrop.color = unlocked ? Color.white : new Color(0.35f, 0.3f, 0.4f);
            }
            else
            {
                if (backdropAnimator != null) backdropAnimator.Stop();
                backdrop.sprite = PixelArtFactory.GraveyardBackdrop;
                backdrop.color = unlocked ? song.backdropTint : song.backdropTint * new Color(0.35f, 0.3f, 0.4f);
            }
        }

        void PlayMusic(AudioClip clip, float startSeconds, float bpm)
        {
            if (music == null || clip == null) return;
            if (music.clip == clip && music.isPlaying) return;

            _pendingClip = clip;
            _pendingStart = Mathf.Clamp(startSeconds, 0f, Mathf.Max(0f, clip.length - 1f));
            _musicBpm = bpm > 0f ? bpm : 100f;
            if (_crossfade != null) StopCoroutine(_crossfade);
            _crossfade = StartCoroutine(Crossfade());
        }

        IEnumerator Crossfade()
        {
            float start = music.volume;
            for (float t = 0f; t < crossfadeSeconds && music.isPlaying; t += Time.unscaledDeltaTime)
            {
                music.volume = Mathf.Lerp(start, 0f, t / crossfadeSeconds);
                yield return null;
            }

            music.clip = _pendingClip;
            music.loop = true;
            music.time = _pendingStart;
            music.Play();

            for (float t = 0f; t < crossfadeSeconds; t += Time.unscaledDeltaTime)
            {
                music.volume = Mathf.Lerp(0f, musicVolume, t / crossfadeSeconds);
                yield return null;
            }
            music.volume = musicVolume;
            _crossfade = null;
        }

        IEnumerator FadeMusicOut()
        {
            if (_crossfade != null) StopCoroutine(_crossfade);
            float start = music != null ? music.volume : 0f;
            for (float t = 0f; t < 0.3f && music != null; t += Time.unscaledDeltaTime)
            {
                music.volume = Mathf.Lerp(start, 0f, t / 0.3f);
                yield return null;
            }
        }
    }
}
