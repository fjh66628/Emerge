using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MVP03
{
    // After movement, camera follow, billboards and their culling bounds.
    [DefaultExecutionOrder(300), DisallowMultipleComponent, RequireComponent(typeof(ClickPuddles))]
    public sealed class PuddleReflections : MonoBehaviour
    {
        [SerializeField] private int reflectionRendererIndex;
        [SerializeField, Range(256, 1024)] private int maximumWidth = 768;
        [SerializeField, Range(10, 60)] private int refreshRate = 30;
        private Camera source, reflection;
        private ClickPuddles puddles;
        private readonly Dictionary<int, Capture> captures = new Dictionary<int, Capture>();
        private readonly List<int> obsolete = new List<int>();
        private readonly HashSet<int> used = new HashSet<int>();
        private readonly Plane[] frustum = new Plane[6];
        private readonly List<PortraitPose> portraits = new List<PortraitPose>();
        private readonly UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();
        public int CaptureCount => captures.Count;
        public int RenderCount { get; private set; }

        private sealed class Capture
        {
            public RenderTexture texture;
            public float height, lastTime = -1000;
            public bool visible, ready;
            public Vector3 cameraPosition;
            public Quaternion cameraRotation;
            public Matrix4x4 projection, vp;
        }
        private struct PortraitPose
        {
            public SpriteRenderer renderer;
            public Quaternion rotation;
            public Bounds bounds;
        }

        public void Configure(int rendererIndex) => reflectionRendererIndex = rendererIndex;

        private void Awake() { source = GetComponent<Camera>(); puddles = GetComponent<ClickPuddles>(); }

        private void LateUpdate()
        {
            if (puddles.Patches.Count == 0) return;
            GeometryUtility.CalculateFrustumPlanes(source, frustum);
            used.Clear();
            foreach (var capture in captures.Values) capture.visible = false;
            foreach (var patch in puddles.Patches)
            {
                int key = Mathf.RoundToInt(patch.height * 1000);
                used.Add(key);
                if (!captures.TryGetValue(key, out var capture))
                { capture = new Capture { height = patch.height }; captures.Add(key, capture); }
                if (patch.renderer != null && GeometryUtility.TestPlanesAABB(frustum, patch.renderer.bounds)) capture.visible = true;
            }
            obsolete.Clear();
            foreach (var pair in captures) if (!used.Contains(pair.Key)) obsolete.Add(pair.Key);
            foreach (int key in obsolete) { Release(captures[key]); captures.Remove(key); }
            Capture next = null;
            foreach (var capture in captures.Values)
            {
                if (!capture.visible) continue;
                bool moved = (capture.cameraPosition - source.transform.position).sqrMagnitude > .0001f ||
                    Quaternion.Angle(capture.cameraRotation, source.transform.rotation) > .05f || capture.projection != source.projectionMatrix;
                if (!moved && Time.unscaledTime - capture.lastTime < 1f / refreshRate) continue;
                if (next == null || capture.lastTime < next.lastTime) next = capture;
            }
            // One capture budget per frame, shared by all pools at this elevation.
            // Different visible stair elevations refresh in oldest-first order.
            if (next != null) Render(next);
            foreach (var patch in puddles.Patches)
            {
                var capture = captures[Mathf.RoundToInt(patch.height * 1000)];
                if (!capture.ready) continue;
                patch.properties.SetTexture("_PlanarReflection", capture.texture);
                patch.properties.SetMatrix("_ReflectionVP", capture.vp);
                patch.properties.SetFloat("_HasReflection", 1);
                patch.renderer.SetPropertyBlock(patch.properties);
            }
        }

        private void Render(Capture capture)
        {
            if (reflection == null)
            {
                var go = new GameObject("MVP03 pooled reflection camera") { hideFlags = HideFlags.HideAndDontSave };
                reflection = go.AddComponent<Camera>(); reflection.enabled = false;
            }
            int width = Mathf.Clamp(source.pixelWidth / 2, 256, maximumWidth);
            int height = Mathf.Max(128, Mathf.RoundToInt(width / source.aspect));
            if (capture.texture == null || capture.texture.width != width || capture.texture.height != height)
            {
                Release(capture);
                capture.texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBHalf)
                { name = "Puddle planar reflection", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, useMipMap = true, autoGenerateMips = false };
                capture.texture.Create();
            }
            reflection.CopyFrom(source);
            reflection.enabled = false;
            reflection.cameraType = CameraType.Reflection;
            reflection.allowMSAA = false;
            reflection.allowHDR = true;
            reflection.useOcclusionCulling = false;
            reflection.cullingMask = source.cullingMask & ~(1 << 4) & ~(1 << 5);
            reflection.targetTexture = capture.texture;
            var data = reflection.GetUniversalAdditionalCameraData();
            data.SetRenderer(reflectionRendererIndex);
            data.renderPostProcessing = false;
            data.requiresColorTexture = false;
            data.requiresDepthTexture = false;
            data.renderShadows = true;
            data.volumeLayerMask = 0;
            data.allowXRRendering = false;
            Matrix4x4 mirror = Matrix4x4.identity;
            mirror.m11 = -1; mirror.m13 = 2 * capture.height;
            reflection.transform.SetPositionAndRotation(mirror.MultiplyPoint(source.transform.position),
                Quaternion.LookRotation(Vector3.Reflect(source.transform.forward, Vector3.up), Vector3.up));
            reflection.worldToCameraMatrix = source.worldToCameraMatrix * mirror;
            // Oblique near plane removes the floor and everything below the water plane.
            Vector3 point = reflection.worldToCameraMatrix.MultiplyPoint(new Vector3(0, capture.height + .006f, 0));
            Vector3 normal = reflection.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
            reflection.projectionMatrix = source.CalculateObliqueMatrix(new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(point, normal)));
            reflection.cullingMatrix = reflection.projectionMatrix * reflection.worldToCameraMatrix;
            bool previousInversion = GL.invertCulling;
            portraits.Clear();
            try
            {
                // HD2D cards must also face the reflected view; otherwise a 45-degree
                // main-camera billboard is edge-on in the water. Only the capture sees these poses.
                foreach (var portrait in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                {
                    if (portrait.GetComponentInParent<PixelPilgrim>() == null &&
                        portrait.GetComponentInParent<BouncingSlime>() == null && portrait.GetComponentInParent<PixelBillboard>() == null) continue;
                    portraits.Add(new PortraitPose { renderer = portrait, rotation = portrait.transform.rotation, bounds = portrait.localBounds });
                    bool reversed = Vector3.Dot(portrait.transform.right, source.transform.right) < 0;
                    portrait.transform.rotation = reflection.transform.rotation * (reversed ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);
                    // A conservative local sphere also contains the upright per-light shadow card.
                    var b = portrait.sprite.bounds;
                    float r = Mathf.Max(b.min.magnitude, b.max.magnitude) * 2;
                    portrait.localBounds = new Bounds(Vector3.zero, Vector3.one * r);
                }
                GL.invertCulling = !previousInversion;
                request.destination = capture.texture;
                RenderPipeline.SubmitRenderRequest(reflection, request);
                capture.texture.GenerateMips();
                capture.vp = GL.GetGPUProjectionMatrix(reflection.projectionMatrix, true) * reflection.worldToCameraMatrix;
                capture.lastTime = Time.unscaledTime;
                capture.cameraPosition = source.transform.position;
                capture.cameraRotation = source.transform.rotation;
                capture.projection = source.projectionMatrix;
                capture.ready = true;
                RenderCount++;
            }
            finally
            {
                GL.invertCulling = previousInversion;
                foreach (var pose in portraits)
                    if (pose.renderer != null) { pose.renderer.transform.rotation = pose.rotation; pose.renderer.localBounds = pose.bounds; }
                reflection.targetTexture = null;
            }
        }

        private static void Release(Capture capture)
        {
            if (capture.texture != null) { capture.texture.Release(); Destroy(capture.texture); capture.texture = null; }
            capture.ready = false;
        }

        public void Clear()
        {
            foreach (var capture in captures.Values) Release(capture);
            captures.Clear();
        }

        private void OnDisable()
        {
            Clear();
            if (reflection != null) { Destroy(reflection.gameObject); reflection = null; }
        }
    }
}
