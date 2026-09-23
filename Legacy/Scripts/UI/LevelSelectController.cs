using System;
using UnityEngine;
using FunkyThursday.Core;

namespace FunkyThursday.UI
{
    /// <summary>
    /// Lists every LevelData found under Resources/Levels as a clickable
    /// entry; picking one hands off to GameFlowManager.StartLevel.
    /// </summary>
    public class LevelSelectController : MonoBehaviour
    {
        private LevelData[] levels;

        private void Awake()
        {
            GameFlowManager.EnsureExists();
        }

        private void Start()
        {
            levels = Resources.LoadAll<LevelData>("Levels");
            Array.Sort(levels, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        }

        private void OnGUI()
        {
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(Screen.width / 2f - 300f, 40f, 600f, 60f), "Select a Level", titleStyle);

            if (levels == null || levels.Length == 0)
            {
                GUIStyle warn = new GUIStyle(GUI.skin.label) { fontSize = 20 };
                warn.normal.textColor = Color.red;
                GUI.Label(new Rect(40f, 140f, 600f, 40f), "No levels found in Resources/Levels.", warn);
                return;
            }

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 24 };
            const float startY = 140f;
            const float buttonHeight = 70f;
            const float buttonWidth = 420f;
            float x = Screen.width / 2f - buttonWidth / 2f;

            for (int i = 0; i < levels.Length; i++)
            {
                LevelData level = levels[i];
                Rect rect = new Rect(x, startY + i * (buttonHeight + 15f), buttonWidth, buttonHeight);
                string label = string.IsNullOrEmpty(level.levelName) ? level.name : level.levelName;

                if (GUI.Button(rect, label, buttonStyle))
                {
                    GameFlowManager.Instance.StartLevel(level);
                }
            }

            if (GUI.Button(new Rect(20f, Screen.height - 60f, 140f, 40f), "Back"))
            {
                GameFlowManager.Instance.GoToMainMenu();
            }
        }
    }
}
