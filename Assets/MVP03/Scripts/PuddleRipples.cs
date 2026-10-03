using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace MVP03
{
    // GPU height/previous-height fields share the fixed world UVs of the puddle coverage atlas.
    // No water colliders, per-ring objects, CPU pixel loops or GPU readbacks are needed.
    internal sealed class PuddleRipples : System.IDisposable
    {
        private const float Step = 1f / 30;
        private const int MaximumFields = 4, MaximumImpulses = 64;
        private static readonly ProfilerMarker TickMarker = new ProfilerMarker("Puddles.ContactRipples");
        private readonly Material water, simulation;
        private readonly Mesh quad;
        private readonly Vector4 atlas, texel;
        private readonly int width, height;
        private readonly CommandBuffer commands = new CommandBuffer { name = "Puddle contact height field" };
        private readonly MaterialPropertyBlock draw = new MaterialPropertyBlock();
        private readonly Collider[] overlaps = new Collider[128];
        private readonly HashSet<int> owners = new HashSet<int>();
        private readonly Dictionary<long, Contact> contacts = new Dictionary<long, Contact>();
        private readonly List<long> expiredContacts = new List<long>();
        private readonly List<Field> fields = new List<Field>(MaximumFields);
        private Pair spare;
        private int tick;
        private float clock, accumulator, nextCleanup;
        public int ActiveCount => fields.Count;
        public int ImpulseCount { get; private set; }

        private sealed class Pair { public RenderTexture current, previous; }
        private sealed class Field
        {
            public ClickPuddles.Patch patch;
            public Pair textures;
            public float lastImpulse;
            public readonly List<Vector4> impulses = new List<Vector4>(MaximumImpulses);
        }
        private sealed class Contact
        {
            public Vector2 lastImpulse;
            public float impulseTime, seenTime;
            public int seenTick;
        }

        public PuddleRipples(Material material, Vector4 worldAtlas, Mesh mesh)
        {
            water = material; atlas = worldAtlas; quad = mesh;
            width = Mathf.Clamp(Mathf.CeilToInt(atlas.z * 16), 64, 512);
            height = Mathf.Clamp(Mathf.CeilToInt(atlas.w * 16), 64, 512);
            texel = new Vector4(1f / width, 1f / height, atlas.z / width, atlas.w / height);
            simulation = new Material(Shader.Find("Hidden/MVP03/Puddle Ripples")) { hideFlags = HideFlags.HideAndDontSave };
            spare = NewPair();
            ClearPair(spare);
            Bind(Texture2D.blackTexture);
            draw.SetTexture("_State", spare.current);
            draw.SetVector("_Wave", Vector4.zero);
            commands.SetRenderTarget(spare.previous);
            commands.DrawMesh(quad, Matrix4x4.identity, simulation, 0, 0, draw);
            draw.SetVector("_Impact", new Vector4(atlas.x + 1, atlas.y + 1, .2f, 0));
            commands.DrawMesh(quad, Matrix4x4.identity, simulation, 0, 1, draw);
            Graphics.ExecuteCommandBuffer(commands); commands.Clear();
        }

        public void Tick(IReadOnlyList<ClickPuddles.Patch> patches, float delta)
        {
            if (delta <= 0) return;
            using (TickMarker.Auto())
            {
                if (water.GetFloat("_ContactRippleStrength") <= .001f)
                {
                    if (fields.Count != 0 || contacts.Count != 0) Clear();
                    return;
                }
                clock += delta; tick++;
                commands.Clear();
                DetectContacts(patches);
                float decay = Mathf.Clamp(water.GetFloat("_ContactRippleDecay"), .4f, 4);
                float lifetime = Mathf.Clamp(8 / decay, 3, 12);
                for (int i = fields.Count - 1; i >= 0; i--)
                    if (clock - fields[i].lastImpulse > lifetime) Retire(i);
                if (fields.Count != 0)
                {
                    // Stable fixed steps and a bounded catch-up budget after a slow frame.
                    accumulator = Mathf.Min(accumulator + delta, Step * 2);
                    float requestedSpeed = Mathf.Clamp(water.GetFloat("_ContactRippleSpeed"), .3f, 2);
                    float invX = 1 / (texel.z * texel.z), invZ = 1 / (texel.w * texel.w);
                    float speed = Mathf.Min(requestedSpeed, .9f / (Step * Mathf.Sqrt(invX + invZ)));
                    float c2 = speed * speed * Step * Step;
                    while (accumulator >= Step)
                    {
                        foreach (var field in fields)
                        {
                            Bind(field.patch.support);
                            commands.SetRenderTarget(field.textures.current);
                            foreach (var impulse in field.impulses)
                            {
                                draw.SetVector("_Impact", impulse);
                                commands.DrawMesh(quad, Matrix4x4.identity, simulation, 0, 1, draw);
                            }
                            field.impulses.Clear();
                            draw.SetTexture("_State", field.textures.current);
                            draw.SetVector("_Wave", new Vector4(c2 * invX, c2 * invZ, Mathf.Exp(-decay * Step), Mathf.Exp(-.15f * Step)));
                            commands.SetRenderTarget(field.textures.previous);
                            commands.DrawMesh(quad, Matrix4x4.identity, simulation, 0, 0, draw);
                            var old = field.textures.current; field.textures.current = field.textures.previous; field.textures.previous = old;
                        }
                        accumulator -= Step;
                    }
                    Graphics.ExecuteCommandBuffer(commands);
                    foreach (var field in fields)
                    {
                        field.patch.properties.SetTexture("_RippleMap", field.textures.current);
                        field.patch.properties.SetFloat("_HasContactRipples", 1);
                        field.patch.properties.SetVector("_RippleTexel", texel);
                        field.patch.properties.SetFloat("_RippleBlend", accumulator / Step);
                        field.patch.renderer.SetPropertyBlock(field.patch.properties);
                    }
                }
                else accumulator = 0;
                // Prune without LINQ allocations and without retaining destroyed colliders.
                if (clock >= nextCleanup)
                {
                    nextCleanup = clock + 2; expiredContacts.Clear();
                    foreach (var pair in contacts) if (clock - pair.Value.seenTime > 2) expiredContacts.Add(pair.Key);
                    foreach (long key in expiredContacts) contacts.Remove(key);
                }
            }
        }

        private void DetectContacts(IReadOnlyList<ClickPuddles.Patch> patches)
        {
            foreach (var patch in patches)
            {
                Bounds region = patch.renderer.bounds;
                var centre = new Vector3(region.center.x, patch.height, region.center.z);
                // Controller query capsules shrink near their rounded feet. Broad-phase with
                // a taller slab, then test the actual sole (including controller skin) below.
                var extents = new Vector3(region.extents.x, .4f, region.extents.z);
                int count = Physics.OverlapBoxNonAlloc(centre, extents, overlaps, Quaternion.identity, ~(1 << 4), QueryTriggerInteraction.Ignore);
                owners.Clear();
                for (int i = 0; i < count; i++)
                {
                    var collider = overlaps[i]; overlaps[i] = null;
                    if (collider == null || !collider.enabled) continue;
                    var body = collider.attachedRigidbody;
                    var character = collider as CharacterController;
                    if (body == null && character == null) continue; // Ignore the static architecture.
                    var b = collider.bounds;
                    float sole = b.min.y - (character != null ? character.skinWidth : collider.contactOffset);
                    if (sole > patch.height + .065f || sole < patch.height - .12f) continue;
                    int owner = body != null ? body.GetInstanceID() : collider.GetInstanceID();
                    if (!owners.Add(owner)) continue; // Compound rigidbodies emit once per surface.
                    long key = ((long)(uint)owner << 32) | (uint)Mathf.RoundToInt(patch.height * 1000);
                    if (!contacts.TryGetValue(key, out var contact)) { contact = new Contact { seenTick = -1 }; contacts.Add(key, contact); }
                    var point = new Vector2(b.center.x, b.center.z);
                    bool entered = contact.seenTick != tick - 1;
                    if (entered || (point - contact.lastImpulse).sqrMagnitude > .18f * .18f && clock - contact.impulseTime >= .10f)
                    {
                        float radius = Mathf.Clamp(Mathf.Min(b.extents.x, b.extents.z) * .7f, .17f, .55f);
                        float downward = body != null ? Mathf.Max(0, -body.linearVelocity.y) : Mathf.Max(0, -character.velocity.y);
                        float amplitude = -.022f * Mathf.Clamp(radius / .25f, .7f, 1.7f) * (entered ? 1.25f + Mathf.Min(downward * .12f, .6f) : .8f);
                        Enqueue(patch, new Vector4(point.x, point.y, radius, amplitude));
                        contact.lastImpulse = point; contact.impulseTime = clock;
                    }
                    contact.seenTick = tick; contact.seenTime = clock;
                }
            }
        }

        private void Enqueue(ClickPuddles.Patch patch, Vector4 impact)
        {
            Field field = null;
            foreach (var candidate in fields) if (candidate.patch == patch) { field = candidate; break; }
            if (field == null)
            {
                if (fields.Count >= MaximumFields)
                {
                    int oldest = 0;
                    for (int i = 1; i < fields.Count; i++) if (fields[i].lastImpulse < fields[oldest].lastImpulse) oldest = i;
                    Retire(oldest);
                }
                var pair = spare ?? NewPair(); spare = null;
                ClearPair(pair);
                field = new Field { patch = patch, textures = pair }; fields.Add(field);
            }
            if (field.impulses.Count < MaximumImpulses) { field.impulses.Add(impact); ImpulseCount++; }
            field.lastImpulse = clock;
        }

        private void Bind(Texture coverage)
        {
            draw.Clear(); draw.SetTexture("_Coverage", coverage); draw.SetVector("_Atlas", atlas); draw.SetVector("_Texel", texel);
            draw.SetVector("_Wetness", new Vector4(water.GetFloat("_WaterThreshold"), water.GetFloat("_EdgeSoftness"), 0, 0));
        }
        private Pair NewPair() => new Pair { current = NewTexture(), previous = NewTexture() };
        private RenderTexture NewTexture()
        {
            var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf) ? RenderTextureFormat.RGHalf : RenderTextureFormat.ARGBHalf;
            var texture = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear)
            { name = "Water contact height / previous height", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.Create(); return texture;
        }
        private void ClearPair(Pair pair)
        {
            commands.SetRenderTarget(pair.current); commands.ClearRenderTarget(false, true, Color.clear);
            commands.SetRenderTarget(pair.previous); commands.ClearRenderTarget(false, true, Color.clear);
        }
        private void Retire(int index)
        {
            var field = fields[index]; fields.RemoveAt(index);
            field.patch.properties.SetTexture("_RippleMap", Texture2D.blackTexture);
            field.patch.properties.SetFloat("_HasContactRipples", 0);
            if (field.patch.renderer != null) field.patch.renderer.SetPropertyBlock(field.patch.properties);
            if (spare == null) spare = field.textures; else Release(field.textures);
        }
        public void Remove(ClickPuddles.Patch patch)
        { for (int i = fields.Count - 1; i >= 0; i--) if (fields[i].patch == patch) Retire(i); }
        public void Clear()
        {
            for (int i = fields.Count - 1; i >= 0; i--) Retire(i);
            contacts.Clear(); accumulator = 0; commands.Clear();
        }
        private static void Release(Pair pair)
        {
            pair.current.Release(); pair.previous.Release();
            Object.Destroy(pair.current); Object.Destroy(pair.previous);
        }
        public void Dispose()
        {
            Clear(); if (spare != null) { Release(spare); spare = null; }
            commands.Release(); Object.Destroy(simulation);
        }
    }
}
