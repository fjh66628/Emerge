using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace MVP03
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class ClickPuddles : MonoBehaviour
    {
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Collider[] groundSurfaces;
        [SerializeField] private Vector2 radiusRange = new Vector2(1.5f, 2.3f);
        [SerializeField, Range(1, 48)] private int maximumPuddles = 24;
        private Camera view;
        private Mesh quad;
        private Texture2D stainTexture;
        private byte[] stainPixels;
        private readonly List<Stamp> stamps = new List<Stamp>();
        private readonly List<Patch> patches = new List<Patch>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private readonly HashSet<int> heights = new HashSet<int>();
        private readonly System.Random random = new System.Random();
        public IReadOnlyList<Patch> Patches => patches;
        public int PuddleCount => stamps.Count;

        // One merged surface per height: overlapping stamps are shaded only once.
        public sealed class Patch
        {
            public GameObject root;
            public MeshRenderer renderer;
            public Texture2D support;
            public MaterialPropertyBlock properties;
            public float height;
        }

        private sealed class Stamp
        {
            public Vector3 position;
            public Vector2 radius;
            public float sine, cosine;
            public byte[] support;
            public int HeightKey => Mathf.RoundToInt(position.y * 1000);
            public Vector2 Extents => new Vector2(Mathf.Abs(cosine) * radius.x + Mathf.Abs(sine) * radius.y,
                Mathf.Abs(sine) * radius.x + Mathf.Abs(cosine) * radius.y);
        }

        public void Configure(Material material, Collider[] surfaces)
        { waterMaterial = material; groundSurfaces = surfaces; }

        private void Awake() => view = GetComponent<Camera>();
        private void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                TryPlaceAtScreen(Mouse.current.position.ReadValue());
        }

        public bool TryPlaceAtScreen(Vector2 position)
        {
            if (!Application.isPlaying || waterMaterial == null) return false;
            if (view == null) view = GetComponent<Camera>();
            if (!view.pixelRect.Contains(position)) return false;
            if (EventSystem.current != null)
            {
                uiHits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
                if (uiHits.Count != 0) return false;
            }
            if (!Physics.Raycast(view.ScreenPointToRay(position), out var hit, view.farClipPlane,
                ~(1 << 4), QueryTriggerInteraction.Ignore)) return false;
            if (hit.normal.y < .98f || groundSurfaces == null || System.Array.IndexOf(groundSurfaces, hit.collider) < 0)
                return false;
            if (!LoadStain()) return false;
            Place(hit);
            return true;
        }

        private bool LoadStain()
        {
            var texture = waterMaterial.GetTexture("_StainMap") as Texture2D;
            if (texture == null || !texture.isReadable) return false;
            if (texture == stainTexture && stainPixels != null) return true;
            stainTexture = texture;
            var pixels = texture.GetPixels32();
            stainPixels = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++) stainPixels[i] = pixels[i].r;
            return true;
        }

        private void Place(RaycastHit hit)
        {
            if (!LoadStain()) return;
            float radius = Mathf.Lerp(radiusRange.x, radiusRange.y, (float)random.NextDouble());
            float angle = (float)random.NextDouble() * Mathf.PI * 2;
            var stamp = new Stamp
            {
                position = hit.point + Vector3.up * .035f,
                radius = new Vector2(radius, radius * Mathf.Lerp(.65f, 1.05f, (float)random.NextDouble())),
                sine = Mathf.Sin(angle), cosine = Mathf.Cos(angle), support = new byte[48 * 48]
            };
            // Horizontal water level, clipped to its supporting floor/tread.
            for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
            {
                float u = ((x + .5f) / 24 - 1) * stamp.radius.x;
                float v = ((y + .5f) / 24 - 1) * stamp.radius.y;
                Vector3 point = stamp.position + new Vector3(stamp.cosine * u + stamp.sine * v, 0, -stamp.sine * u + stamp.cosine * v);
                if (hit.collider.Raycast(new Ray(point + Vector3.up * .2f, Vector3.down), out var floor, .4f) &&
                    floor.normal.y > .98f && Mathf.Abs(floor.point.y - hit.point.y) < .025f) stamp.support[y * 48 + x] = 255;
            }
            stamps.Add(stamp);
            while (stamps.Count > maximumPuddles) stamps.RemoveAt(0);
            RebuildSurfaces();
        }

        public void RebuildSurfaces()
        {
            if (waterMaterial == null || !LoadStain()) return;
            heights.Clear();
            foreach (var stamp in stamps) heights.Add(stamp.HeightKey);
            for (int i = patches.Count - 1; i >= 0; i--)
                if (!heights.Contains(Mathf.RoundToInt(patches[i].height * 1000))) { Release(patches[i]); patches.RemoveAt(i); }
            foreach (int key in heights) RebuildHeight(key);
        }

        private void RebuildHeight(int key)
        {
            Vector2 min = Vector2.one * float.PositiveInfinity, max = Vector2.one * float.NegativeInfinity;
            foreach (var stamp in stamps)
            {
                if (stamp.HeightKey != key) continue;
                Vector2 centre = new Vector2(stamp.position.x, stamp.position.z);
                min = Vector2.Min(min, centre - stamp.Extents); max = Vector2.Max(max, centre + stamp.Extents);
            }
            // Stable world texels keep existing shorelines stationary as the union expands.
            const float density = 64;
            min = new Vector2(Mathf.Floor(min.x * density), Mathf.Floor(min.y * density)) / density;
            max = new Vector2(Mathf.Ceil(max.x * density), Mathf.Ceil(max.y * density)) / density;
            Vector2 size = max - min;
            int width = Mathf.Clamp(Mathf.RoundToInt(size.x * density), 64, 2048);
            int height = Mathf.Clamp(Mathf.RoundToInt(size.y * density), 64, 2048);
            var coverage = new byte[width * height];
            foreach (var stamp in stamps)
            {
                if (stamp.HeightKey != key) continue;
                Vector2 centre = new Vector2(stamp.position.x, stamp.position.z), extent = stamp.Extents;
                int x0 = Mathf.Clamp(Mathf.FloorToInt((centre.x - extent.x - min.x) / size.x * width), 0, width - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt((centre.x + extent.x - min.x) / size.x * width), 0, width - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt((centre.y - extent.y - min.y) / size.y * height), 0, height - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt((centre.y + extent.y - min.y) / size.y * height), 0, height - 1);
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                {
                    float dx = min.x + (x + .5f) / width * size.x - centre.x;
                    float dz = min.y + (y + .5f) / height * size.y - centre.y;
                    float u = (stamp.cosine * dx - stamp.sine * dz) / (2 * stamp.radius.x) + .5f;
                    float v = (stamp.sine * dx + stamp.cosine * dz) / (2 * stamp.radius.y) + .5f;
                    if (u < 0 || u > 1 || v < 0 || v > 1) continue;
                    float shape = Sample(stainPixels, stainTexture.width, stainTexture.height, u, v);
                    float support = Sample(stamp.support, 48, 48, u, v);
                    byte value = (byte)Mathf.RoundToInt(shape * support / 255f);
                    int index = y * width + x;
                    if (value > coverage[index]) coverage[index] = value;
                }
            }
            Patch patch = patches.Find(p => Mathf.RoundToInt(p.height * 1000) == key);
            if (patch == null)
            {
                EnsureMesh();
                var root = new GameObject("Merged water stains / " + key) { layer = 4 };
                root.AddComponent<MeshFilter>().sharedMesh = quad;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = waterMaterial; renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true; renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                patch = new Patch { root = root, renderer = renderer, height = key / 1000f, properties = new MaterialPropertyBlock() };
                patches.Add(patch);
            }
            if (patch.support == null || patch.support.width != width || patch.support.height != height)
            {
                if (patch.support != null) Destroy(patch.support);
                patch.support = new Texture2D(width, height, TextureFormat.R8, false, true)
                { name = "Merged water coverage", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            }
            patch.support.SetPixelData(coverage, 0); patch.support.Apply(false, false);
            patch.root.transform.position = new Vector3((min.x + max.x) * .5f, patch.height, (min.y + max.y) * .5f);
            patch.root.transform.localScale = new Vector3(size.x * .5f, 1, size.y * .5f);
            patch.properties.SetTexture("_SupportMap", patch.support);
            patch.renderer.SetPropertyBlock(patch.properties);
        }

        private static float Sample(byte[] data, int width, int height, float u, float v)
        {
            float x = Mathf.Clamp(u * width - .5f, 0, width - 1), y = Mathf.Clamp(v * height - .5f, 0, height - 1);
            int x0 = (int)x, y0 = (int)y, x1 = Mathf.Min(x0 + 1, width - 1), y1 = Mathf.Min(y0 + 1, height - 1);
            return Mathf.Lerp(Mathf.Lerp(data[y0 * width + x0], data[y0 * width + x1], x - x0),
                Mathf.Lerp(data[y1 * width + x0], data[y1 * width + x1], x - x0), y - y0);
        }

        private void EnsureMesh()
        {
            if (quad != null) return;
            quad = new Mesh { name = "Merged water surface", hideFlags = HideFlags.DontSave };
            quad.vertices = new[] { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(1, 0, 1), new Vector3(-1, 0, 1) };
            quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            quad.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quad.bounds = new Bounds(Vector3.zero, new Vector3(2, .1f, 2));
        }

        private static void Release(Patch patch)
        {
            if (patch.root != null) { patch.root.SetActive(false); Destroy(patch.root); }
            if (patch.support != null) Destroy(patch.support);
        }

        public void Clear()
        {
            foreach (var patch in patches) Release(patch);
            patches.Clear(); stamps.Clear();
            GetComponent<PuddleReflections>()?.Clear();
        }

        private void OnDisable()
        {
            Clear();
            if (quad != null) { Destroy(quad); quad = null; }
        }
    }
}
