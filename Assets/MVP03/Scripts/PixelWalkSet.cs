using UnityEngine;

namespace MVP03
{
    public enum PixelFacing { Front, Left, Right, Back }

    [CreateAssetMenu(menuName = "MVP/Pixel Walk Set")]
    public sealed class PixelWalkSet : ScriptableObject
    {
        public Sprite[] front, left, right, back;
        [Min(.1f)] public float metresPerCycle = 2.25f;

        public Sprite[] Frames(PixelFacing facing)
        {
            switch (facing)
            {
                case PixelFacing.Left: return left;
                case PixelFacing.Right: return right;
                case PixelFacing.Back: return back;
                default: return front;
            }
        }
    }
}
