using System.IO;
using FunkyThursday.Data;
using FunkyThursday.Flow;
using FunkyThursday.UI;
using FunkyThursday.UI.Menus;
using FunkyThursday.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FunkyThursday.EditorTools
{
    /// <summary>
    /// Builds Scenes/Menu.unity: camera, animated backdrop, a dimmed Canvas for the Title, Level Select
    /// and Options screens, the menu music source, the persistent SongManager and an EventSystem.
    /// </summary>
    public static class MenuSceneBuilder
    {
        public const string ScenePath = "Assets/_FunkyThursday/Scenes/Menu.unity";

        [MenuItem("Funky Thursday/Build Menu Scene")]
        static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SongLibrary library = SongLibraryAssets.EnsureLibrary();
            HiggsfieldAssetLinker.Link(library);
            Build(library);
            SceneSetup.RegisterScenes();
        }

        public static void Build(SongLibrary library)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SceneSetup.CreateCamera(5f);
            SceneSetup.CreateGlobalLight2D();

            var backdropObject = new GameObject("Backdrop");
            var backdrop = backdropObject.AddComponent<SpriteRenderer>();
            backdrop.sortingOrder = 0;
            var backdropAnimator = backdropObject.AddComponent<SpriteAnimator>();

            var music = new GameObject("Menu Music").AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;

            var manager = new GameObject("Song Manager").AddComponent<SongManager>();
            GameplaySceneBuilder.Set(manager, "library", library);

            SceneSetup.CreateEventSystem();
            Canvas canvas = SceneSetup.CreateCanvas("Menu Canvas", 0);
            UIFactory.Panel("Dim", canvas.transform, MenuPalette.Dim);
            RectTransform screens = UIFactory.Stretch("Screens", canvas.transform);

            var controller = new GameObject("Menu Controller").AddComponent<MenuController>();
            GameplaySceneBuilder.Set(controller, "library", library);
            GameplaySceneBuilder.Set(controller, "screensRoot", screens);
            GameplaySceneBuilder.Set(controller, "backdrop", backdrop);
            GameplaySceneBuilder.Set(controller, "backdropAnimator", backdropAnimator);
            GameplaySceneBuilder.Set(controller, "music", music);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = controller.gameObject;
        }
    }
}
