#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#define FT_NEW_INPUT
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FunkyThursday.Data;
using FunkyThursday.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FunkyThursday.EditorTools
{
    /// <summary>
    /// Shared scene-building helpers and the one-click entry point:
    /// Funky Thursday → Build All Scenes (song library, art links, Menu and Gameplay scenes, Build Settings).
    /// </summary>
    public static class SceneSetup
    {
        [MenuItem("Funky Thursday/Build All Scenes", priority = 0)]
        static void BuildAll()
        {
            if (!SongLibraryAssets.NotInPlayMode()) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            SongLibrary library = SongLibraryAssets.EnsureLibrary();
            if (library == null) return;
            int art = HiggsfieldAssetLinker.Link(library);
            AssetDatabase.SaveAssets();

            GameplaySceneBuilder.Build(SongLibraryAssets.Load());
            MenuSceneBuilder.Build(SongLibraryAssets.Load());
            RegisterScenes();

            Debug.Log($"Built Menu and Gameplay scenes ({SongLibraryAssets.Load().Count} songs, {art} Higgsfield asset(s) linked). Menu is open: press Play.");
        }

        /// <summary>Menu first (it loads when the game starts), Gameplay second, anything else after.</summary>
        public static void RegisterScenes()
        {
            var ordered = new List<EditorBuildSettingsScene>();
            foreach (string path in new[] { MenuSceneBuilder.ScenePath, GameplaySceneBuilder.ScenePath })
            {
                if (System.IO.File.Exists(path)) ordered.Add(new EditorBuildSettingsScene(path, true));
            }
            // Keep other scenes that still exist, but drop the Phase 2 test scene: its controller predates SongData.
            ordered.AddRange(EditorBuildSettings.scenes.Where(s =>
                ordered.All(o => o.path != s.path)
                && System.IO.File.Exists(s.path)
                && !s.path.EndsWith("Gameplay_Phase2.unity")));
            EditorBuildSettings.scenes = ordered.ToArray();
        }

        public static Camera CreateCamera(float size)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = GothicPalette.Void;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        /// <summary>
        /// URP's 2D Renderer lights sprites only through 2D lights, so each scene gets one white global
        /// light. Added by reflection so the builders still compile in projects without URP.
        /// </summary>
        public static void CreateGlobalLight2D()
        {
            Type lightType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (lightType == null) return;

            var go = new GameObject("Global Light 2D");
            Component light = go.AddComponent(lightType);
            PropertyInfo kind = lightType.GetProperty("lightType");
            if (kind != null && kind.CanWrite) kind.SetValue(light, Enum.Parse(kind.PropertyType, "Global"));
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Mouse support for the menus, using whichever input backend the project runs.</summary>
        public static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if FT_NEW_INPUT
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
