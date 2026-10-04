using System.Collections.Generic;
using UnityEngine;

namespace MVP04
{
    // A finite exterior emitter. The same projected RGB transmission is consumed by
    // URP surface lighting and the volume pass; no independent window beam directions.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Light))]
    public sealed class ChapelWindowLight : MonoBehaviour
    {
        public ChapelLightSettings settings;
        public static readonly Vector3 RoomMin=new Vector3(-8.86f,0,-13);
        public static readonly Vector3 RoomMax=new Vector3(8.86f,13.6f,17.57f);
        private Light lamp;
        private Texture2D cookie;
        private Vector3 lastPosition, lastTarget;
        private float lastCone=-1, lastTransmission=-1;
        private Texture2D lastRose,lastSide;
        private float lastIntensity=-1,lastRange=-1;
        private Color lastColour;
        private bool lastEnabled;
        private static readonly List<ChapelWindowLight> Active=new List<ChapelWindowLight>(3);
        private static readonly Vector4[] Positions=new Vector4[3],Forwards=new Vector4[3],Radiances=new Vector4[3],Parameters=new Vector4[3];
        public Light Source => lamp!=null?lamp:(lamp=GetComponent<Light>());

        private void OnEnable()
        {
            if(!Active.Contains(this))Active.Add(this);
            lastIntensity=-1;Apply();
        }
        private void Update() => Apply();
        public void Apply()
        {
            if(settings==null)return;
            Light light=Source;
            bool projectionChanged=cookie==null || lastPosition!=settings.position || lastTarget!=settings.target ||
                lastCone!=settings.coneAngle || lastTransmission!=settings.directTransmission ||
                lastRose!=settings.roseTransmission || lastSide!=settings.sideTransmission;
            if(!projectionChanged && lastIntensity==settings.intensity && lastRange==settings.range &&
                lastColour==settings.colour && lastEnabled==settings.sourceEnabled && light.enabled==settings.sourceEnabled &&
                !transform.hasChanged && light.intensity==Mathf.Max(0,settings.intensity) && light.spotAngle==Mathf.Clamp(settings.coneAngle,15,140))
                return;
            transform.position=settings.position;
            Vector3 direction=settings.target-settings.position;
            if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(direction);
            light.type=LightType.Spot;
            light.enabled=settings.sourceEnabled;
            light.intensity=Mathf.Max(0,settings.intensity);
            light.color=settings.colour;light.range=Mathf.Max(1,settings.range);
            light.spotAngle=Mathf.Clamp(settings.coneAngle,15,140);
            light.innerSpotAngle=light.spotAngle*.88f;
            light.shadows=LightShadows.Hard; // A point emitter has a geometric, hard shadow.
            light.shadowStrength=1;light.shadowBias=.015f;light.shadowNormalBias=.03f;
            light.shadowNearPlane=.15f;
            if(projectionChanged)RebuildCookie();
            light.cookie=cookie;
            lastIntensity=settings.intensity;lastRange=settings.range;lastColour=settings.colour;lastEnabled=settings.sourceEnabled;
            transform.hasChanged=false;
            PublishGlobals();
        }

        private static void PublishGlobals()
        {
            int count=0;
            foreach(var source in Active)
            {
                if(source==null || source.settings==null || count==3)continue;
                Light light=source.Source;
                Positions[count]=source.transform.position;Forwards[count]=source.transform.forward;
                Color colour=source.settings.colour.linear*(light.enabled?light.intensity:0);
                Radiances[count]=new Vector4(colour.r,colour.g,colour.b,0);
                float outer=Mathf.Cos(light.spotAngle*.5f*Mathf.Deg2Rad),inner=Mathf.Cos(light.innerSpotAngle*.5f*Mathf.Deg2Rad);
                float inverseCone=1/Mathf.Max(.001f,inner-outer);
                Parameters[count++]=new Vector4(1/(light.range*light.range),inverseCone,-outer*inverseCone,1-source.settings.directTransmission);
            }
            Shader.SetGlobalInteger("_ChapelSourceCount",count);
            Shader.SetGlobalVectorArray("_ChapelSourcePositions",Positions);Shader.SetGlobalVectorArray("_ChapelSourceForwards",Forwards);
            Shader.SetGlobalVectorArray("_ChapelSourceRadiances",Radiances);Shader.SetGlobalVectorArray("_ChapelSourceParameters",Parameters);
        }

        [ContextMenu("Rebuild window projection")]
        public void RebuildCookie()
        {
            if(settings==null)return;
            const int size=1024;
            if(cookie==null)cookie=new Texture2D(size,size,TextureFormat.RGBA32,false,true)
            {name="Chapel / projected window transmission",hideFlags=HideFlags.HideAndDontSave,
                wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var pixels=new Color32[size*size];
            float spread=Mathf.Tan(Source.spotAngle*.5f*Mathf.Deg2Rad);
            Quaternion rotation=transform.rotation;
            Vector3 origin=transform.position;
            // Readable masks are configured by the builder. Only regenerate when projection changes.
            var rose=new Mask(settings.roseTransmission);
            var side=new Mask(settings.sideTransmission);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                Vector3 ray=rotation*new Vector3(((x+.5f)*2/size-1)*spread,((y+.5f)*2/size-1)*spread,1);
                Color c=Color.black;
                if(Entry(origin,ray,out Vector3 hit,out int face))
                {
                    if(face==2 && Mathf.Abs(hit.z-RoomMax.z)<.01f)
                    {
                        Vector2 p=new Vector2(hit.x,hit.y-8.4f)/2.63f;
                        if(p.sqrMagnitude<1)c=rose.Sample(p.x*.5f+.5f,p.y*.5f+.5f);
                    }
                    else if(face==0)
                    {
                        float sign=Mathf.Sign(hit.x);
                        int bay=Mathf.RoundToInt((hit.z+6)/6);
                        if(bay>=0 && bay<4)
                        {
                            float u=.5f+sign*(hit.z-(-6+bay*6))/2.44f,v=(hit.y-3)/4.5f;
                            if(u>0 && u<1 && v>0 && v<1)c=side.Sample(u,v);
                        }
                    }
                }
                c*=Mathf.Clamp01(settings.directTransmission);c.a=1;
                pixels[y*size+x]=c;
            }
            cookie.SetPixels32(pixels);cookie.Apply(false,false);
            lastPosition=settings.position;lastTarget=settings.target;lastCone=settings.coneAngle;
            lastTransmission=settings.directTransmission;lastRose=settings.roseTransmission;lastSide=settings.sideTransmission;
        }

        // First intersection with the room envelope. Roof/back/opaque facade rays are black,
        // so a far-side window cannot light through the near exterior wall.
        public static bool Entry(Vector3 origin,Vector3 ray,out Vector3 hit,out int face)
        {
            float near=0,far=float.PositiveInfinity;face=-1;
            for(int axis=0;axis<3;axis++)
            {
                if(Mathf.Abs(ray[axis])<1e-7f)
                {if(origin[axis]<RoomMin[axis] || origin[axis]>RoomMax[axis]){hit=default;return false;}continue;}
                float a=(RoomMin[axis]-origin[axis])/ray[axis],b=(RoomMax[axis]-origin[axis])/ray[axis];
                if(a>b){float t=a;a=b;b=t;}
                if(a>near){near=a;face=axis;}far=Mathf.Min(far,b);
            }
            hit=origin+ray*near;return far>near && face>=0;
        }

        private readonly struct Mask
        {
            private readonly Color[] pixels;
            private readonly int width,height;
            public Mask(Texture2D texture)
            {width=texture!=null?texture.width:0;height=texture!=null?texture.height:0;pixels=texture!=null && texture.isReadable?texture.GetPixels():null;}
            public Color Sample(float u,float v)
            {
                if(pixels==null)return Color.black;
                float x=Mathf.Clamp(u*width-.5f,0,width-1),y=Mathf.Clamp(v*height-.5f,0,height-1);
                int ix=(int)x,iy=(int)y,jx=Mathf.Min(ix+1,width-1),jy=Mathf.Min(iy+1,height-1);
                Color a=Color.Lerp(pixels[iy*width+ix],pixels[iy*width+jx],x-ix);
                Color b=Color.Lerp(pixels[jy*width+ix],pixels[jy*width+jx],x-ix);
                Color c=Color.Lerp(a,b,y-iy);
                return c.linear*c.a;
            }
        }
        private void OnDisable()
        {
            if(lamp!=null){lamp.cookie=null;lamp.enabled=false;}
            Active.Remove(this);PublishGlobals();
            if(cookie!=null){if(Application.isPlaying)Destroy(cookie);else DestroyImmediate(cookie);cookie=null;}
        }
        private void OnDrawGizmosSelected()
        {
            if(settings==null)return;
            Gizmos.color=new Color(.4f,.7f,1,.8f);
            Gizmos.DrawWireSphere(transform.position,.6f);
            Gizmos.DrawLine(transform.position,new Vector3(0,8.4f,17.57f));
            for(int side=-1;side<=1;side+=2)for(int bay=0;bay<4;bay++)
                Gizmos.DrawLine(transform.position,new Vector3(side*8.86f,5.25f,-6+bay*6));
        }
    }
}
