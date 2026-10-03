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
        private readonly List<Patch> patches = new List<Patch>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private readonly System.Random random = new System.Random();
        public IReadOnlyList<Patch> Patches => patches;

        public sealed class Patch
        {
            public GameObject root;
            public MeshRenderer renderer;
            public Texture2D support;
            public MaterialPropertyBlock properties;
            public float height;
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
            // Raycast the current pointer position rather than last frame's cached UI state.
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
            foreach (var existing in patches)
                if ((existing.root.transform.position - hit.point).sqrMagnitude < .25f) return false;
            Place(hit);
            return true;
        }

        private void Place(RaycastHit hit)
        {
            if (quad == null)
            {
                quad = new Mesh { name = "Puddle surface", hideFlags = HideFlags.DontSave };
                quad.vertices = new[] { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(1, 0, 1), new Vector3(-1, 0, 1) };
                quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                quad.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                quad.bounds = new Bounds(Vector3.zero, new Vector3(2, .1f, 2));
            }
            float radius = Mathf.Lerp(radiusRange.x, radiusRange.y, (float)random.NextDouble());
            var root = new GameObject("Puddle / wet stone") { layer = 4 };
            // The courtyard's rendered pavers sit up to 3 cm above their simplified collider.
            root.transform.SetPositionAndRotation(hit.point + Vector3.up * .035f, Quaternion.Euler(0, (float)random.NextDouble() * 360, 0));
            root.transform.localScale = new Vector3(radius, 1, radius * Mathf.Lerp(.65f, 1.05f, (float)random.NextDouble()));
            root.AddComponent<MeshFilter>().sharedMesh = quad;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = waterMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            // Clip at platform/step boundaries; the visible water remains level.
            const int size = 48;
            var support = new Texture2D(size, size, TextureFormat.R8, false, true)
            { name = "Puddle ground support", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var pixels = new byte[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Vector3 p = root.transform.TransformPoint(new Vector3((x + .5f) * 2 / size - 1, 0, (y + .5f) * 2 / size - 1));
                var ray = new Ray(p + Vector3.up * .2f, Vector3.down);
                if (hit.collider.Raycast(ray, out var floor, .4f) && floor.normal.y > .98f && Mathf.Abs(floor.point.y - hit.point.y) < .025f)
                    pixels[y * size + x] = 255;
            }
            support.SetPixelData(pixels, 0); support.Apply(false, true);
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_SupportMap", support);
            properties.SetFloat("_Seed", (float)random.NextDouble() * 1024);
            renderer.SetPropertyBlock(properties);
            patches.Add(new Patch { root = root, renderer = renderer, support = support, properties = properties, height = root.transform.position.y });
            while (patches.Count > maximumPuddles) RemoveFirst();
        }

        private void RemoveFirst()
        {
            var patch = patches[0];
            if (patch.root != null) { patch.root.SetActive(false); Destroy(patch.root); }
            if (patch.support != null) Destroy(patch.support);
            patches.RemoveAt(0);
        }

        public void Clear()
        {
            while (patches.Count > 0) RemoveFirst();
            GetComponent<PuddleReflections>()?.Clear();
        }

        private void OnDisable()
        {
            Clear();
            if (quad != null) { Destroy(quad); quad = null; }
        }
    }
}
