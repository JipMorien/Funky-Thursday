using UnityEngine;
using FunkyThursday.Core;

namespace FunkyThursday.UI
{
    /// <summary>
    /// Shows the outcome of the level just played (read from GameFlowManager)
    /// and offers to retry it or go back to level select.
    /// </summary>
    public class ResultsController : MonoBehaviour
    {
        private void Awake()
        {
            GameFlowManager.EnsureExists();
        }

        private void OnGUI()
        {
            GameFlowManager flow = GameFlowManager.Instance;
            float centerX = Screen.width / 2f;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 48,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            titleStyle.normal.textColor = flow.LastRunWon ? Color.green : Color.red;
            GUI.Label(new Rect(centerX - 300f, 80f, 600f, 80f), flow.LastRunWon ? "Cleared!" : "Game Over", titleStyle);

            GUIStyle statStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
            statStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(centerX - 300f, 180f, 600f, 40f), $"Score: {flow.LastRunScore}", statStyle);
            GUI.Label(new Rect(centerX - 300f, 220f, 600f, 40f), $"Max Combo: {flow.LastRunMaxCombo}", statStyle);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 24 };
            if (GUI.Button(new Rect(centerX - 220f, 300f, 200f, 60f), "Retry", buttonStyle))
            {
                flow.RetryLevel();
            }
            if (GUI.Button(new Rect(centerX + 20f, 300f, 200f, 60f), "Level Select", buttonStyle))
            {
                flow.GoToLevelSelect();
            }
        }
    }
}
