using UnityEngine;
using UnityEngine.Video;

namespace FunkyThursday.Visuals
{
    /// <summary>
    /// Optional backdrop that plays a looping MP4 from StreamingAssets through a VideoPlayer. It always
    /// uses a URL source, the only source WebGL supports, so the same setup works on desktop and in the
    /// browser. The clip renders into a small point-filtered RenderTexture, keeping pixels crisp,
    /// shown on a quad sized to the camera view.
    /// </summary>
    public sealed class VideoLoopBackdrop : MonoBehaviour
    {
        [SerializeField, Min(16)] int pixelWidth = 320;
        [SerializeField, Min(16)] int pixelHeight = 180;
        [SerializeField, Min(1f)] float worldHeight = 10f;
        [SerializeField] int sortingOrder = -1;

        VideoPlayer _player;
        RenderTexture _texture;
        Material _material;
        MeshRenderer _quad;

        public bool IsPlaying => _player != null && _player.isPlaying;

        /// <summary>Plays a file relative to StreamingAssets, e.g. "Video/bg_crypt-keeper.mp4".</summary>
        public void Play(string streamingAssetsPath)
        {
            if (string.IsNullOrEmpty(streamingAssetsPath)) return;
            EnsureBuilt();

            _player.Stop();
            _player.url = $"{Application.streamingAssetsPath}/{streamingAssetsPath}";
            _player.Prepare();
            _quad.enabled = true;
        }

        public void Stop()
        {
            if (_player != null) _player.Stop();
            if (_quad != null) _quad.enabled = false;
        }

        void EnsureBuilt()
        {
            if (_player != null) return;

            _texture = new RenderTexture(pixelWidth, pixelHeight, 0) { filterMode = FilterMode.Point, name = "Backdrop Video" };

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _material = new Material(shader) { mainTexture = _texture };

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Video Quad";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);
            quad.transform.localScale = new Vector3(worldHeight * pixelWidth / pixelHeight, worldHeight, 1f);
            _quad = quad.GetComponent<MeshRenderer>();
            _quad.sharedMaterial = _material;
            _quad.sortingOrder = sortingOrder;
            _quad.enabled = false;

            _player = gameObject.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.isLooping = true;
            _player.skipOnDrop = true;
            _player.source = VideoSource.Url;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.targetTexture = _texture;
            _player.audioOutputMode = VideoAudioOutputMode.None;
            _player.prepareCompleted += p => p.Play();
            _player.errorReceived += (p, message) => Debug.LogWarning($"Backdrop video failed: {message}", this);
        }

        void OnDestroy()
        {
            if (_texture != null) _texture.Release();
            if (_texture != null) Destroy(_texture);
            if (_material != null) Destroy(_material);
        }
    }
}
