using UnityEngine;

namespace MVP04
{
    [CreateAssetMenu(menuName="MVP04/Exterior light settings")]
    public sealed class ChapelLightSettings : ScriptableObject
    {
        public bool sourceEnabled=true;
        public Vector3 position=new Vector3(-18,18,38);
        public Vector3 target=new Vector3(0,4,3);
        [Min(0)] public float intensity=50000;
        public Color colour=new Color(.78f,.86f,1);
        [Range(15,140)] public float coneAngle=75;
        [Min(1)] public float range=100;
        [Range(0,1)] public float directTransmission=.7f;
        public Texture2D roseTransmission;
        public Texture2D sideTransmission;
    }
}
