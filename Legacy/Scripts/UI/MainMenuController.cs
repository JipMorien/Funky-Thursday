using UnityEngine;
using FunkyThursday.Core;

namespace FunkyThursday.UI
{
    /// <summary>
    /// The game's title screen: a "Play" button hands off to the level-select
    /// screen via GameFlowManager.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public string gameTitle = "Funky Thursday";

        private void Awake()
        {
            GameFlowManager.EnsureExists();
        }

        private void OnGUI()
        {
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 56,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(centerX - 300f, centerY - 160f, 600f, 100f), gameTitle, titleStyle);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 28 };

            if (GUI.Button(new Rect(centerX - 100f, centerY, 200f, 60f), "Play", buttonStyle))
            {
                GameFlowManager.Instance.GoToLevelSelect();
            }

            if (GUI.Button(new Rect(centerX - 100f, centerY + 80f, 200f, 60f), "Quit", buttonStyle))
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
        }
    }
}
