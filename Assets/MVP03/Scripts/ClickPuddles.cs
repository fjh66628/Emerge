using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
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
        private const int SupportSize = 48, SampleCount = SupportSize * SupportSize, MaxHits = 4;
        private static readonly ProfilerMarker PlacementMarker = new ProfilerMarker("Puddles.Enqueue");
        private static readonly ProfilerMarker UploadMarker = new ProfilerMarker("Puddles.FinishSupportJob");
        private static readonly ProfilerMarker MergeMarker = new ProfilerMarker("Puddles.GPUMaxUnion");
        private static readonly ProfilerMarker ScheduleMarker = new ProfilerMarker("Puddles.ScheduleSupportJob");
        private Camera view;
        private Mesh quad;
        private Material coverageMaterial;
        private CommandBuffer commands;
        private MaterialPropertyBlock drawProperties;
        private Vector4 atlas;
        private int atlasWidth, atlasHeight;
        private Work work;
        private Stamp active;
        private Patch spare;
        private Patch warmingPatch;
        private int warmFrame;
        private readonly List<Stamp> stamps = new List<Stamp>(48);
        private readonly List<Patch> patches = new List<Patch>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private readonly Queue<int> dirtyHeights = new Queue<int>();
        private readonly HashSet<int> dirtySet = new HashSet<int>();
        private readonly System.Random random = new System.Random();
        public IReadOnlyList<Patch> Patches => patches;
        public int PuddleCount => stamps.Count;
        public int PendingCount { get { int n = 0; foreach (var s in stamps) if (s.support == null) n++; return n; } }
        public bool IsBusy => active != null || PendingCount != 0 || dirtyHeights.Count != 0;
        internal float LowestWaterHeight
        {
            get
            {
                float height = float.PositiveInfinity;
                if (groundSurfaces != null) foreach (var floor in groundSurfaces)
                    if (floor != null) height = Mathf.Min(height, floor.bounds.max.y + .035f);
                return float.IsPositiveInfinity(height) ? .035f : height;
            }
        }

        // Each elevation has ONE surface. Max blending cannot double the reflection at overlaps.
        public sealed class Patch
        {
            public GameObject root;
            public MeshRenderer renderer;
            public RenderTexture support;
            public MaterialPropertyBlock properties;
            public float height;
        }

        private sealed class Stamp
        {
            public Vector3 position;
            public Vector2 radius;
            public float sine, cosine;
            public Texture2D support;
            public int HeightKey => Mathf.RoundToInt(position.y * 1000);
            public Vector2 Extents => new Vector2(Mathf.Abs(cosine) * radius.x + Mathf.Abs(sine) * radius.y,
                Mathf.Abs(sine) * radius.x + Mathf.Abs(cosine) * radius.y);
        }

        // Reused persistent buffers: no per-click megapixel arrays or thousands of managed raycasts.
        private sealed class Work
        {
            public NativeArray<RaycastCommand> rays = new NativeArray<RaycastCommand>(SampleCount, Allocator.Persistent);
            public NativeArray<RaycastHit> hits = new NativeArray<RaycastHit>(SampleCount * MaxHits, Allocator.Persistent);
            public NativeArray<byte> mask = new NativeArray<byte>(SampleCount, Allocator.Persistent);
            public NativeArray<int> groundIds;
            public JobHandle fence;
            public Work(Collider[] surfaces)
            {
                groundIds = new NativeArray<int>(surfaces.Length, Allocator.Persistent);
                for (int i = 0; i < surfaces.Length; i++) groundIds[i] = surfaces[i] != null ? surfaces[i].GetInstanceID() : 0;
            }
            public void Dispose()
            {
                // Disabling / clearing never waits on the physics worker. Disposal is also a job.
                rays.Dispose(fence); hits.Dispose(fence); mask.Dispose(fence); groundIds.Dispose(fence);
            }
        }

        private struct BuildRays : IJobParallelFor
        {
            [WriteOnly] public NativeArray<RaycastCommand> rays;
            public Vector3 centre;
            public Vector2 radius;
            public float sine, cosine;
            public void Execute(int index)
            {
                float u = ((index % SupportSize + .5f) / (SupportSize * .5f) - 1) * radius.x;
                float v = ((index / SupportSize + .5f) / (SupportSize * .5f) - 1) * radius.y;
                var point = centre + new Vector3(cosine * u + sine * v, .2f, -sine * u + cosine * v);
                rays[index] = new RaycastCommand(point, Vector3.down,
                    new QueryParameters(~(1 << 4), false, QueryTriggerInteraction.Ignore, false), .4f);
            }
        }

        private struct ReadSupport : IJobParallelFor
        {
            [ReadOnly] public NativeArray<RaycastHit> hits;
            [ReadOnly] public NativeArray<int> groundIds;
            [WriteOnly] public NativeArray<byte> mask;
            public float floorHeight;
            public void Execute(int index)
            {
                byte value = 0;
                for (int h = 0; h < MaxHits; h++)
                {
                    var hit = hits[index * MaxHits + h];
                    int id = hit.colliderInstanceID; // No UnityEngine.Object access on a worker.
                    if (id == 0) break;
                    if (hit.normal.y < .98f || Mathf.Abs(hit.point.y - floorHeight) >= .025f) continue;
                    for (int g = 0; g < groundIds.Length; g++)
                        if (groundIds[g] == id) { value = 255; break; }
                    if (value != 0) break;
                }
                mask[index] = value;
            }
        }

        public void Configure(Material material, Collider[] surfaces)
        {
            Clear(); ReleaseResources(); waterMaterial = material; groundSurfaces = surfaces;
            if (Application.isPlaying && isActiveAndEnabled) EnsureResources();
        }
        private void Awake() => view = GetComponent<Camera>();
        private void Start() => EnsureResources();

        private void Update()
        {
            if (warmingPatch != null && Time.frameCount > warmFrame)
            {
                if (warmingPatch == spare) warmingPatch.root.SetActive(false);
                warmingPatch = null;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                TryPlaceAtScreen(Mouse.current.position.ReadValue());
            if (!EnsureResources()) return;
            if (active != null && work.fence.IsCompleted)
            {
                using (UploadMarker.Auto())
                {
                    work.fence.Complete(); // Already finished: never wait for unfinished work.
                    if (stamps.Contains(active))
                    {
                        active.support = new Texture2D(SupportSize, SupportSize, TextureFormat.R8, false, true)
                        { name = "Puddle floor support", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                        active.support.SetPixelData(work.mask, 0);
                        active.support.Apply(false, true); // Only 2.25 KB; no full-atlas CPU upload.
                        Dirty(active.HeightKey);
                    }
                    active = null;
                }
            }
            // At most one atlas update per frame, regardless of click burst / number of heights.
            if (dirtyHeights.Count != 0)
            {
                int key = dirtyHeights.Dequeue(); dirtySet.Remove(key);
                using (MergeMarker.Auto()) RebuildHeight(key);
            }
            if (active == null)
                foreach (var stamp in stamps)
                {
                    if (stamp.support != null) continue;
                    using (ScheduleMarker.Auto())
                    {
                        active = stamp;
                        var build = new BuildRays { rays = work.rays, centre = stamp.position, radius = stamp.radius, sine = stamp.sine, cosine = stamp.cosine }.Schedule(SampleCount, 64);
                        var trace = RaycastCommand.ScheduleBatch(work.rays, work.hits, 64, MaxHits, build);
                        work.fence = new ReadSupport { hits = work.hits, groundIds = work.groundIds, mask = work.mask, floorHeight = stamp.position.y - .035f }.Schedule(SampleCount, 64, trace);
                        JobHandle.ScheduleBatchedJobs();
                    }
                    break;
                }
        }

        public bool TryPlaceAtScreen(Vector2 position)
        {
            using (PlacementMarker.Auto())
            {
                if (!Application.isPlaying || !isActiveAndEnabled || waterMaterial == null || waterMaterial.GetTexture("_StainMap") == null) return false;
                if (view == null) view = GetComponent<Camera>();
                if (!view.pixelRect.Contains(position)) return false;
                if (EventSystem.current != null)
                {
                    uiHits.Clear();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
                    if (uiHits.Count != 0) return false;
                }
                if (!Physics.Raycast(view.ScreenPointToRay(position), out var hit, view.farClipPlane, ~(1 << 4), QueryTriggerInteraction.Ignore)) return false;
                if (hit.normal.y < .98f || groundSurfaces == null || System.Array.IndexOf(groundSurfaces, hit.collider) < 0) return false;
                float radius = Mathf.Lerp(radiusRange.x, radiusRange.y, (float)random.NextDouble());
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                stamps.Add(new Stamp { position = hit.point + Vector3.up * .035f,
                    radius = new Vector2(radius, radius * Mathf.Lerp(.65f, 1.05f, (float)random.NextDouble())), sine = Mathf.Sin(angle), cosine = Mathf.Cos(angle) });
                while (stamps.Count > maximumPuddles)
                {
                    var oldest = stamps[0]; stamps.RemoveAt(0); Dirty(oldest.HeightKey);
                    if (oldest.support != null) Destroy(oldest.support);
                }
                return true;
            }
        }

        private void Dirty(int key) { if (dirtySet.Add(key)) dirtyHeights.Enqueue(key); }
        public void RebuildSurfaces()
        {
            foreach (var patch in patches) Dirty(Mathf.RoundToInt(patch.height * 1000));
            foreach (var stamp in stamps) if (stamp.support != null) Dirty(stamp.HeightKey);
        }

        private void RebuildHeight(int key)
        {
            Patch patch = patches.Find(p => Mathf.RoundToInt(p.height * 1000) == key);
            bool ready = stamps.Exists(s => s.HeightKey == key && s.support != null);
            if (!ready)
            {
                if (patch != null) { patches.Remove(patch); Recycle(patch); }
                return;
            }
            if (patch == null)
            {
                patch = spare ?? NewPatch(); spare = null;
                patch.height = key / 1000f; patch.root.name = "Merged water stains / " + key;
                patch.properties.Clear(); patch.properties.SetTexture("_SupportMap", patch.support);
                patch.properties.SetVector("_SupportAtlas", atlas);
                patch.renderer.SetPropertyBlock(patch.properties);
                patches.Add(patch);
            }
            commands.Clear();
            commands.SetRenderTarget(patch.support);
            commands.ClearRenderTarget(false, true, Color.clear);
            Vector2 min = Vector2.one * float.PositiveInfinity, max = Vector2.one * float.NegativeInfinity;
            foreach (var stamp in stamps)
            {
                if (stamp.HeightKey != key || stamp.support == null) continue;
                var centre = new Vector2(stamp.position.x, stamp.position.z);
                min = Vector2.Min(min, centre - stamp.Extents); max = Vector2.Max(max, centre + stamp.Extents);
                drawProperties.Clear();
                drawProperties.SetTexture("_StainMap", waterMaterial.GetTexture("_StainMap"));
                drawProperties.SetTexture("_SupportMap", stamp.support);
                drawProperties.SetVector("_Atlas", atlas);
                drawProperties.SetVector("_Stamp", new Vector4(stamp.position.x, stamp.position.z, stamp.radius.x, stamp.radius.y));
                drawProperties.SetVector("_Rotation", new Vector4(stamp.cosine, stamp.sine, 0, 0));
                commands.DrawMesh(quad, Matrix4x4.identity, coverageMaterial, 0, 0, drawProperties);
            }
            Graphics.ExecuteCommandBuffer(commands);
            // Keep geometry/culling tight even though the backing atlas has fixed world bounds.
            patch.root.transform.position = new Vector3((min.x + max.x) * .5f, patch.height, (min.y + max.y) * .5f);
            patch.root.transform.localScale = new Vector3((max.x - min.x) * .5f, 1, (max.y - min.y) * .5f);
            patch.root.SetActive(true);
        }

        private bool EnsureResources()
        {
            if (coverageMaterial != null) return true;
            if (!Application.isPlaying || waterMaterial == null || groundSurfaces == null || groundSurfaces.Length == 0) return false;
            var shader = Shader.Find("Hidden/MVP03/Puddle Coverage");
            if (shader == null) return false;
            coverageMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            commands = new CommandBuffer { name = "Water stain GPU max union" };
            drawProperties = new MaterialPropertyBlock();
            work = new Work(groundSurfaces);
            // Fixed world texels: growing a pool never reallocates or resamples its whole atlas.
            Bounds bounds = default; bool first = true;
            foreach (var surface in groundSurfaces)
            {
                if (surface == null) continue;
                if (first) { bounds = surface.bounds; first = false; } else bounds.Encapsulate(surface.bounds);
            }
            var min = bounds.min; var max = bounds.max;
            float x = Mathf.Floor(min.x - radiusRange.y * 2), z = Mathf.Floor(min.z - radiusRange.y * 2);
            atlas = new Vector4(x, z, Mathf.Ceil(max.x + radiusRange.y * 2) - x, Mathf.Ceil(max.z + radiusRange.y * 2) - z);
            atlasWidth = Mathf.Clamp(Mathf.CeilToInt(atlas.z * 64), 64, 2048);
            atlasHeight = Mathf.Clamp(Mathf.CeilToInt(atlas.w * 64), 64, 2048);
            quad = new Mesh { name = "Merged water surface", hideFlags = HideFlags.DontSave };
            quad.vertices = new[] { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(1, 0, 1), new Vector3(-1, 0, 1) };
            quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            quad.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quad.bounds = new Bounds(Vector3.zero, new Vector3(2, .1f, 2));
            // Prepare the first atlas and shader when entering Play, not when clicking.
            spare = NewPatch();
            drawProperties.SetTexture("_StainMap", Texture2D.blackTexture);
            drawProperties.SetTexture("_SupportMap", Texture2D.blackTexture);
            drawProperties.SetVector("_Atlas", atlas);
            drawProperties.SetVector("_Stamp", new Vector4(atlas.x + 1, atlas.y + 1, 1, 1));
            drawProperties.SetVector("_Rotation", new Vector4(1, 0, 0, 0));
            commands.SetRenderTarget(spare.support); commands.ClearRenderTarget(false, true, Color.clear);
            commands.DrawMesh(quad, Matrix4x4.identity, coverageMaterial, 0, 0, drawProperties);
            Graphics.ExecuteCommandBuffer(commands); commands.Clear();
            // Render one completely clipped surface at startup to prepare the forward pass/PSO.
            // It has zero coverage and is never included in Patches or the placement count.
            spare.properties.SetTexture("_SupportMap", spare.support);
            spare.properties.SetVector("_SupportAtlas", atlas);
            spare.renderer.SetPropertyBlock(spare.properties);
            spare.root.transform.position = new Vector3(atlas.x + atlas.z * .5f, LowestWaterHeight, atlas.y + atlas.w * .5f);
            spare.root.transform.localScale = new Vector3(atlas.z * .5f, 1, atlas.w * .5f);
            spare.root.SetActive(true); warmingPatch = spare; warmFrame = Time.frameCount;
            return true;
        }

        private Patch NewPatch()
        {
            var root = new GameObject("Water coverage reserve") { layer = 4, hideFlags = HideFlags.DontSave };
            root.SetActive(false);
            root.AddComponent<MeshFilter>().sharedMesh = quad;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = waterMaterial; renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true; renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
            var texture = new RenderTexture(atlasWidth, atlasHeight, 0, format, RenderTextureReadWrite.Linear)
            { name = "GPU merged water coverage", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.Create();
            return new Patch { root = root, renderer = renderer, support = texture, properties = new MaterialPropertyBlock() };
        }

        private void Recycle(Patch patch)
        {
            patch.root.SetActive(false);
            if (spare == null) spare = patch; else Release(patch);
        }
        private static void Release(Patch patch)
        {
            if (patch == null) return;
            if (patch.root != null) { patch.root.SetActive(false); Destroy(patch.root); }
            if (patch.support != null) { patch.support.Release(); Destroy(patch.support); }
        }
        public void Clear()
        {
            foreach (var patch in patches) Recycle(patch);
            foreach (var stamp in stamps) if (stamp.support != null) Destroy(stamp.support);
            patches.Clear(); stamps.Clear(); dirtyHeights.Clear(); dirtySet.Clear();
            // The active job may finish, but its stamp no longer belongs to this collection.
            GetComponent<PuddleReflections>()?.Clear();
        }
        private void ReleaseResources()
        {
            work?.Dispose(); work = null; active = null;
            Release(spare); spare = null; warmingPatch = null;
            commands?.Release(); commands = null;
            if (coverageMaterial != null) Destroy(coverageMaterial);
            if (quad != null) Destroy(quad);
            coverageMaterial = null; quad = null;
        }
        private void OnDisable() { Clear(); ReleaseResources(); }
    }
}
