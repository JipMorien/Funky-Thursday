using UnityEditor;
using UnityEngine;

namespace FunkyThursday.EditorTools
{
    /// <summary>
    /// Applies rhythm-safe import settings to everything under Audio/Music: fully decoded in memory
    /// and preloaded, so PlayScheduled starts on the exact DSP sample with no streaming hiccup.
    /// </summary>
    public sealed class MusicImportPostprocessor : AssetPostprocessor
    {
        const string MusicFolder = "/_FunkyThursday/Audio/Music/";

        void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').Contains(MusicFolder)) return;

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = false;
            importer.loadInBackground = false;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
        }
    }
}
