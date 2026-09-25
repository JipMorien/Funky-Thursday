using System.IO;
using UnityEditor;
using UnityEngine;

namespace FunkyThursday.EditorTools
{
    /// <summary>
    /// FMOD spike helpers. FMOD can't play Unity AudioClip assets, so the FMOD engine streams the
    /// same .ogg files from StreamingAssets/FmodSpike/&lt;song id&gt;/. This copies them there.
    /// </summary>
    public static class FmodSpikeTools
    {
        const string MusicFolder = "Assets/_FunkyThursday/Audio/Music";
        const string TargetFolder = "Assets/StreamingAssets/FmodSpike";

        [MenuItem("Funky Thursday/FMOD Spike/Copy Songs To StreamingAssets")]
        static void CopySongs()
        {
            if (!Directory.Exists(MusicFolder))
            {
                Debug.LogError($"{MusicFolder} not found.");
                return;
            }

            int copied = 0;
            foreach (string songFolder in Directory.GetDirectories(MusicFolder))
            {
                string slug = Path.GetFileName(songFolder);
                string target = Path.Combine(TargetFolder, slug);
                Directory.CreateDirectory(target);

                foreach (string track in new[] { "Inst.ogg", "Voices.ogg" })
                {
                    string source = Path.Combine(songFolder, track);
                    if (!File.Exists(source)) continue;
                    File.Copy(source, Path.Combine(target, track), overwrite: true);
                    copied++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"Copied {copied} track(s) to {TargetFolder}.");
        }

        [MenuItem("Funky Thursday/FMOD Spike/Open Timing Log Folder")]
        static void OpenLogFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
