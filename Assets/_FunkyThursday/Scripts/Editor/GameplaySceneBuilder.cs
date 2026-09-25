using System.IO;
using FunkyThursday.AI;
using FunkyThursday.Core;
using FunkyThursday.Data;
using FunkyThursday.Gameplay;
using FunkyThursday.UI;
using FunkyThursday.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FunkyThursday.EditorTools
{
    /// <summary>
    /// Builds Scenes/Gameplay.unity from scratch: camera, conductor, stage, strumlines, systems, HUD
    /// (health bar, countdown, pause menu, round-end screen) and the controller, with every
    /// reference wired. Songs and art come from the SongLibrary, so rebuilding loses nothing.
    /// </summary>
    public static class GameplaySceneBuilder
    {
        public const string ScenePath = "Assets/_FunkyThursday/Scenes/Gameplay.unity";

        const float CameraSize = 5f;
        const float ReceptorY = 3.4f;
        const float StrumlineX = 5.075f;
        const float CharacterX = 3.2f;
        const float GroundY = -5f + 12f / 9f; // top of the backdrop's ground strip

        [MenuItem("Funky Thursday/Build Gameplay Scene")]
        static void BuildMenu()
        {
            if (!SongLibraryAssets.NotInPlayMode()) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SongLibrary library = SongLibraryAssets.EnsureLibrary();
            if (library == null) return;
            HiggsfieldAssetLinker.Link(library);
            AssetDatabase.SaveAssets();
            Build(SongLibraryAssets.Load());
            SceneSetup.RegisterScenes();
        }

        public static void Build(SongLibrary library)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Opening a scene can unload the object passed in; wire the copy that is on disk.
            library = SongLibraryAssets.Load();

            SceneSetup.CreateCamera(CameraSize);
            SceneSetup.CreateGlobalLight2D();

            var conductorObject = new GameObject("Conductor");
            conductorObject.AddComponent<AudioSource>();
            var conductor = conductorObject.AddComponent<Conductor>();
            var vocalsObject = new GameObject("Vocals");
            vocalsObject.transform.SetParent(conductorObject.transform, false);
            var vocalsSource = vocalsObject.AddComponent<AudioSource>();
            vocalsSource.playOnAwake = false;
            Set(conductor, "vocalsSource", vocalsSource);

            // Stage
            var stage = new GameObject("Stage").transform;
            var backdrop = CreateChild("Backdrop", stage, Vector3.zero).AddComponent<SpriteRenderer>();
            backdrop.sortingOrder = 0;
            var backdropAnimator = backdrop.gameObject.AddComponent<SpriteAnimator>();
            Set(backdropAnimator, "conductor", conductor);
            var videoBackdrop = CreateChild("Video Backdrop", stage, Vector3.zero).AddComponent<VideoLoopBackdrop>();

            var opponent = CreatePuppet("Opponent", stage, new Vector3(-CharacterX, GroundY, 0f), conductor);
            var player = CreatePuppet("Player", stage, new Vector3(CharacterX, GroundY, 0f), conductor);

            // Strumlines
            var strumlines = new GameObject("Strumlines").transform;
            var opponentStrum = CreateChild("Opponent Strumline", strumlines, new Vector3(-StrumlineX, ReceptorY, 0f)).AddComponent<Strumline>();
            SetInt(opponentStrum, "side", (int)StrumSide.Opponent);
            var playerStrum = CreateChild("Player Strumline", strumlines, new Vector3(StrumlineX, ReceptorY, 0f)).AddComponent<Strumline>();
            SetInt(playerStrum, "side", (int)StrumSide.Player);

            // Systems
            var systems = new GameObject("Systems").transform;
            var input = CreateChild("Player Input", systems, Vector3.zero).AddComponent<PlayerInput>();

            var spawner = CreateChild("Note Spawner", systems, Vector3.zero).AddComponent<NoteSpawner>();
            Set(spawner, "conductor", conductor);
            Set(spawner, "opponentStrumline", opponentStrum);
            Set(spawner, "playerStrumline", playerStrum);

            var judge = CreateChild("Hit Judge", systems, Vector3.zero).AddComponent<HitJudge>();
            Set(judge, "conductor", conductor);
            Set(judge, "spawner", spawner);
            Set(judge, "strumline", playerStrum);
            Set(judge, "input", input);

            var opponentAI = CreateChild("Opponent AI", systems, Vector3.zero).AddComponent<AIOpponent>();
            Set(opponentAI, "conductor", conductor);
            Set(opponentAI, "spawner", spawner);
            Set(opponentAI, "strumline", opponentStrum);

            var playerBot = CreateChild("Player Bot", systems, Vector3.zero).AddComponent<AIOpponent>();
            Set(playerBot, "conductor", conductor);
            Set(playerBot, "spawner", spawner);
            Set(playerBot, "strumline", playerStrum);
            playerBot.enabled = false;

            var health = CreateChild("Health", systems, Vector3.zero).AddComponent<HealthSystem>();

            // HUD
            SceneSetup.CreateEventSystem();
            Canvas canvas = SceneSetup.CreateCanvas("HUD Canvas", 0);

            var barRect = SceneSetup.CreateRect("Health Bar", canvas.transform);
            barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 0f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = new Vector2(0f, 80f);
            var healthBar = barRect.gameObject.AddComponent<HealthBar>();
            Set(healthBar, "health", health);
            Set(healthBar, "conductor", conductor);

            var countdownRect = SceneSetup.CreateRect("Countdown", canvas.transform);
            countdownRect.anchorMin = countdownRect.anchorMax = countdownRect.pivot = new Vector2(0.5f, 0.5f);
            countdownRect.anchoredPosition = new Vector2(0f, 60f);
            var countdown = countdownRect.gameObject.AddComponent<CountdownDisplay>();
            Set(countdown, "conductor", conductor);

            Canvas menuCanvas = SceneSetup.CreateCanvas("Menu Canvas", 10);
            var pauseMenu = SceneSetup.CreateRect("Pause Menu", menuCanvas.transform).gameObject.AddComponent<PauseMenu>();
            var roundEnd = SceneSetup.CreateRect("Round End", menuCanvas.transform).gameObject.AddComponent<RoundEndScreen>();

            // Controller
            var controller = new GameObject("Gameplay Controller").AddComponent<GameplayController>();
            Set(controller, "library", library);
            Set(controller, "conductor", conductor);
            Set(controller, "spawner", spawner);
            Set(controller, "input", input);
            Set(controller, "judge", judge);
            Set(controller, "opponentAI", opponentAI);
            Set(controller, "playerBot", playerBot);
            Set(controller, "health", health);
            Set(controller, "healthBar", healthBar);
            Set(controller, "opponent", opponent);
            Set(controller, "player", player);
            Set(controller, "backdrop", backdrop);
            Set(controller, "backdropAnimator", backdropAnimator);
            Set(controller, "videoBackdrop", videoBackdrop);
            Set(controller, "pauseMenu", pauseMenu);
            Set(controller, "roundEnd", roundEnd);

            // FMOD spike: per-round timing log for the Unity vs FMOD comparison.
            var logger = CreateChild("Timing Logger", systems, Vector3.zero).AddComponent<TimingLogger>();
            Set(logger, "controller", controller);
            Set(logger, "judge", judge);
            Set(logger, "conductor", conductor);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = controller.gameObject;
        }

        static CharacterPuppet CreatePuppet(string name, Transform parent, Vector3 position, Conductor conductor)
        {
            var go = CreateChild(name, parent, position);
            go.AddComponent<SpriteRenderer>().sortingOrder = 10;
            var puppet = go.AddComponent<CharacterPuppet>();
            Set(puppet, "conductor", conductor);
            return puppet;
        }

        static GameObject CreateChild(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go;
        }

        internal static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"{target.GetType().Name} has no serialized field '{field}'.");
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
