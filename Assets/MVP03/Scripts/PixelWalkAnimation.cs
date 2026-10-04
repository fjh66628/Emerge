using UnityEngine;

namespace MVP03
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PixelWalkAnimation : MonoBehaviour
    {
        [SerializeField] private PixelWalkSet animationSet;
        [SerializeField] private PixelFacing initialFacing = PixelFacing.Back;
        private SpriteRenderer portrait;
        private MaterialPropertyBlock properties;
        private Texture boundTexture;
        private float cycle;

        public PixelFacing Facing { get; private set; }
        public int FrameIndex { get; private set; }
        public bool IsWalking { get; private set; }
        public bool IsConfigured => animationSet != null;

        private void OnEnable()
        {
            portrait = GetComponent<SpriteRenderer>();
            Facing = initialFacing;
            cycle = 0;
            IsWalking = false;
            ApplyFrame(0);
        }

        public void Configure(PixelWalkSet set)
        {
            animationSet = set;
            portrait = GetComponent<SpriteRenderer>();
            Facing = initialFacing;
            cycle = 0;
            IsWalking = false;
            ApplyFrame(0);
        }

        // The controller supplies actual planar displacement after collision resolution.
        // Four independent rows keep face, clothing and silhouette correct in every view.
        public void Advance(Vector2 input, float planarDistance)
        {
            if (animationSet == null) return;
            PixelFacing next = Facing;
            if (input.sqrMagnitude > .01f)
                next = Mathf.Abs(input.x) > Mathf.Abs(input.y)
                    ? (input.x < 0 ? PixelFacing.Left : PixelFacing.Right)
                    : (input.y < 0 ? PixelFacing.Front : PixelFacing.Back);
            if (next != Facing) { Facing = next; cycle = 0; }
            IsWalking = input.sqrMagnitude > .01f && planarDistance > .0001f;
            if (!IsWalking) { cycle = 0; ApplyFrame(0); return; }

            cycle = Mathf.Repeat(cycle + planarDistance / Mathf.Max(.1f, animationSet.metresPerCycle), 1f);
            Sprite[] frames = animationSet.Frames(Facing);
            if (frames == null || frames.Length == 0) return;
            // Start with a stride immediately, instead of holding idle for the first step.
            ApplyFrame((1 + Mathf.FloorToInt(cycle * frames.Length)) % frames.Length);
        }

        private void ApplyFrame(int index)
        {
            if (portrait == null || animationSet == null) return;
            Sprite[] frames = animationSet.Frames(Facing);
            if (frames == null || frames.Length == 0) return;
            Sprite frame = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            if (frame == null) return;
            FrameIndex = index;
            portrait.flipX = false;
            portrait.sprite = frame;
            // Pixel Cutout and its depth/shadow passes sample _BaseMap, not _MainTex.
            if (boundTexture == frame.texture) return;
            properties ??= new MaterialPropertyBlock();
            portrait.GetPropertyBlock(properties);
            properties.SetTexture("_BaseMap", frame.texture);
            portrait.SetPropertyBlock(properties);
            boundTexture = frame.texture;
        }
    }
}
