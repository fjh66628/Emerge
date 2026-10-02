using System;
using System.IO;
using System.Linq;
using MVP03;
using MVP04;
using MVP04.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class MVP04Builder
{
    private const string Root="Assets/MVP04";
    private const string ScenePath=Root+"/Scenes/MVP04_NocturneChapel.unity";
    private static readonly Vector3 Window=new Vector3(0,8.4f,17.45f);
    private static readonly Vector3 SunDirection=new Vector3(0,-.42f,-.91f).normalized;
    private static System.Random random;
    private static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    private static float R(float lo,float hi)=>Mathf.Lerp(lo,hi,(float)random.NextDouble());
    private static Color Shade(float lo=.78f,float hi=1.12f){float v=R(lo,hi);return new Color(v,v,v);}

    [MenuItem("MVP04/Build Dark Rose Window Chapel")]
    public static void Build()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before building MVP04.");
        random=new System.Random(20261004);
        foreach(string folder in new[]{"Scenes","Meshes","Materials","Rendering","Textures"})Directory.CreateDirectory(Root+"/"+folder);
        AssetDatabase.Refresh();
        var stone=Stone("MidnightLimestone",new Color(.48f,.53f,.61f),.18f);
        var trim=Stone("WornCarvings",new Color(.59f,.62f,.65f),.24f);
        var floor=Stone("SlatePaving",new Color(.34f,.39f,.45f),.3f);
        var mortar=Lit("DeepMortar",new Color(.045f,.055f,.075f),.05f);
        var wood=Lit("DarkOak",new Color(.26f,.16f,.085f),.32f);
        var bronze=Lit("AgedBronze",new Color(.27f,.16f,.055f),.55f,.72f);
        var fabric=Lit("AltarLinen",new Color(.51f,.48f,.40f),.12f);
        var wax=Lit("CandleWax",new Color(.59f,.49f,.29f),.25f);
        var flame=Lit("CandleFlame",new Color(1,.43f,.08f),.25f);
        Emission(flame,new Color(5.5f,1.9f,.32f));
        Texture2D rose=RoseTexture();
        int rendererIndex=Renderer(rose);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);

        Floor(floor,mortar,bronze);
        Structure(stone,trim,mortar);
        RoseWindow(rose,trim,bronze,mortar);
        Furniture(wood,trim,fabric,bronze,wax,flame);
        Lighting();
        Camera camera=CameraRig(rendererIndex);
        var hero=Player(camera);
        var atmosphere=new GameObject("Nocturne / exposure bloom and focus").AddComponent<Volume>();
        atmosphere.isGlobal=true;atmosphere.sharedProfile=Atmosphere();
        camera.gameObject.AddComponent<PixelFollowCamera>().Configure(hero.transform);
        camera.gameObject.AddComponent<PixelFocus>().Configure(camera,hero.transform,atmosphere);
        if(atmosphere.sharedProfile.TryGet(out DepthOfField focus))
            focus.focusDistance.Override(Vector3.Dot(hero.transform.position+Vector3.up-camera.transform.position,camera.transform.forward));
        Dust();

        EditorSceneManager.SaveScene(scene,ScenePath);
        var scenes=EditorBuildSettings.scenes.ToList();
        if(!scenes.Any(s=>s.path==ScenePath))scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
        EditorBuildSettings.scenes=scenes.ToArray();
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=camera.gameObject;
        Debug.Log("MVP04 built: dark nave, rose window, shadowed ray-marched window light, sprite controller and subtle pixels.");
    }

    private static void Floor(Material stone,Material mortar,Material bronze)
    {
        var foundation=new ChapelMesh();foundation.Box(V(0,-.25f,3),V(18,.5f,32),.03f);foundation.Save("NaveFoundation",mortar,true);
        for(int bay=0;bay<8;bay++)
        {
            var paving=new ChapelMesh();
            for(int row=0;row<5;row++)for(int col=0;col<20;col++)
            {
                float x=-8.55f+col*.9f,z=-12.5f+bay*3.9f+row*.78f;
                paving.Box(V(x,.025f,z),V(.876f,.07f,.756f),.018f,Shade(.72f,1.15f));
            }
            paving.Save("PavingBay"+bay,stone);
        }
        var inlay=new ChapelMesh();
        for(int side=-1;side<=1;side+=2)
            inlay.Box(V(side*1.9f,.065f,2.5f),V(.04f,.018f,30),.003f);
        for(int z=-10;z<=10;z+=4)
        {
            inlay.Box(V(0,.066f,z),V(.04f,.014f,1.2f),.002f);
            inlay.Box(V(0,.066f,z),V(.6f,.014f,.04f),.002f);
        }
        inlay.Save("AisleBrassInlay",bronze);
        Collision("Entrance limit",V(0,3,-12.9f),V(18,6,.4f));
    }

    private static void Structure(Material stone,Material trim,Material mortar)
    {
        BuildSideWindows(stone,trim,mortar);
        // Transverse arches and paired diagonal ribs make the vaulted nave readable from the entrance.
        for(int bay=0;bay<5;bay++)
        {
            float z=-9+bay*6;
            var ribs=new ChapelMesh();ribs.Arch(V(0,7.35f,z),5.34f,5.3f,.25f,.42f,40);
            if(bay<4)for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<20;i++)
                {
                    float t=i/20f,u=(i+1)/20f;
                    Vector3 a=V(side*5.32f*Mathf.Cos(t*Mathf.PI*.5f),7.4f+Mathf.Sin(t*Mathf.PI*.5f)*5.3f,z+t*3);
                    Vector3 b=V(side*5.32f*Mathf.Cos(u*Mathf.PI*.5f),7.4f+Mathf.Sin(u*Mathf.PI*.5f)*5.3f,z+u*3);
                    ribs.Bar(a,b,.15f);
                    a.z=z+6-t*3;b.z=z+6-u*3;ribs.Bar(a,b,.15f);
                }
            }
            ribs.Save("VaultRibs"+bay,trim);
        }
        var ceiling=new ChapelMesh();
        for(int i=0;i<28;i++)
        {
            float a=(i+.5f)*Mathf.PI/28;
            ceiling.Box(V(Mathf.Cos(a)*5.6f,7.3f+Mathf.Sin(a)*5.6f,3),V(.68f,.35f,32),.01f,Shade(.78f,.95f),Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90));
        }
        for(int side=-1;side<=1;side+=2)ceiling.Box(V(side*7.1f,8.2f,3),V(3.6f,.4f,32),.03f);
        ceiling.Save("VaultCeiling",stone,true);

        var end=new ChapelMesh();
        const int sectors=128;const float innerRadius=2.64f;
        Vector3[] inner=new Vector3[sectors+1],outer=new Vector3[sectors+1];
        for(int i=0;i<=sectors;i++)
        {
            float a=i*Mathf.PI*2/sectors,cs=Mathf.Cos(a),sn=Mathf.Sin(a);
            float distance=Mathf.Min(9.4f/Mathf.Max(.0001f,Mathf.Abs(cs)),(sn>=0?6.5f:8.7f)/Mathf.Max(.0001f,Mathf.Abs(sn)));
            inner[i]=Window+V(cs,sn,0)*innerRadius;outer[i]=Window+V(cs,sn,0)*distance;
        }
        for(int i=0;i<sectors;i++)
        {
            end.Quad(inner[i],inner[i+1],outer[i+1],outer[i],Vector3.back,Color.white);
            end.Quad(inner[i]+Vector3.forward*.8f,outer[i]+Vector3.forward*.8f,outer[i+1]+Vector3.forward*.8f,inner[i+1]+Vector3.forward*.8f,Vector3.forward,Color.white);
            end.Quad(inner[i]+Vector3.forward*.8f,inner[i+1]+Vector3.forward*.8f,inner[i+1],inner[i],(Window-(inner[i]+inner[i+1])*.5f).normalized,Color.white);
        }
        end.Save("SanctuaryWallWithCircularOpening",mortar,true);
        var ashlar=new ChapelMesh();
        for(int y=0;y<21;y++)for(int x=0;x<16;x++)
        {
            Vector3 p=V(-8.6f+x*1.14f,.33f+y*.69f,17.30f);
            float dx=Mathf.Max(0,Mathf.Abs(p.x)-.56f),dy=Mathf.Max(0,Mathf.Abs(p.y-Window.y)-.335f);
            if(dx*dx+dy*dy<3.18f*3.18f)continue;
            ashlar.Box(p,V(1.115f,.665f,.3f),.025f,Shade(.85f,1.1f));
        }
        ashlar.Save("SanctuaryAshlar",stone);
    }

    private static void RoseWindow(Texture2D texture,Material trim,Material bronze,Material mortar)
    {
        var frame=new ChapelMesh();
        frame.Arch(Window+Vector3.back*.09f,2.62f,2.62f,.24f,.6f,72,null,360);
        frame.Arch(Window+Vector3.back*.13f,2.95f,2.95f,.30f,.52f,72,null,360);
        frame.Arch(Window+Vector3.back*.22f,3.29f,3.29f,.11f,.35f,72,null,360);
        frame.Save("RoseCarvedStoneRings",trim,true);
        var leading=new ChapelMesh();
        leading.Arch(Window+Vector3.back*.03f,.38f,.38f,.065f,.1f,48,null,360);
        leading.Arch(Window+Vector3.back*.03f,1.93f,1.93f,.045f,.09f,64,null,360);
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI*2/12;Vector3 radial=V(Mathf.Cos(a),Mathf.Sin(a),0);
            leading.Bar(Window+radial*.44f,Window+radial*2.57f,.045f);
        }
        leading.Save("RoseBronzeTracery",bronze);
        Material glass=FrostedGlass("LuminousRoseGlass",texture,2.05f,new Vector2(5.26f,5.26f));
        var pane=new ChapelMesh();pane.Disc(Window+Vector3.forward*.12f,2.63f);
        var go=pane.Save("RoseStainedGlass",glass);
        go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }

    private static void Furniture(Material wood,Material stone,Material cloth,Material bronze,Material wax,Material flame)
    {
        for(int row=0;row<6;row++)for(int side=-1;side<=1;side+=2)
        {
            float x=side*3.35f,z=-5.8f+row*2.65f;var pew=new ChapelMesh();
            for(int plank=0;plank<3;plank++)pew.Box(V(x,.69f,z+plank*.22f),V(2.6f,.15f,.205f),.018f,Shade(.7f,1.1f));
            for(int plank=0;plank<3;plank++)pew.Box(V(x,.97f+plank*.16f,z-.12f),V(2.6f,.15f,.12f),.019f,Shade(.7f,1.1f));
            for(int edge=-1;edge<=1;edge+=2)
            {
                pew.Box(V(x+edge*1.15f,.36f,z+.20f),V(.17f,.68f,.68f),.03f);
                pew.Box(V(x+edge*1.3f,.94f,z+.2f),V(.14f,.54f,.68f),.03f);
            }
            pew.Save("OakPew"+side+"Row"+row,wood,true);
        }
        var altar=new ChapelMesh();
        for(int i=0;i<3;i++)altar.Box(V(0,.12f+i*.17f,14.4f+i*.4f),V(8.6f-i*.7f,.24f,4.4f-i*.8f),.045f);
        altar.Box(V(0,1.33f,15.6f),V(3.45f,.26f,1.45f),.045f);
        for(int side=-1;side<=1;side+=2)altar.Box(V(side*1.18f,.88f,15.6f),V(.38f,.85f,1.03f),.025f);
        altar.Save("AltarAndSanctuarySteps",stone,true);
        var linen=new ChapelMesh();linen.Box(V(0,1.472f,15.6f),V(2.88f,.022f,1.3f),.004f);
        linen.Box(V(0,1.20f,14.94f),V(2.88f,.55f,.024f),.005f);linen.Save("QuietAltarCloth",cloth);
        var metal=new ChapelMesh();metal.Box(V(0,3.25f,16.94f),V(.105f,2.5f,.13f),.012f);metal.Box(V(0,3.78f,16.92f),V(1.35f,.1f,.13f),.012f);
        metal.Save("SanctuaryCross",bronze);
        for(int side=-1;side<=1;side+=2)
        {
            for(int bay=0;bay<4;bay++)Candles(V(side*6.8f,.05f,-6+bay*6),bronze,wax,flame,3,true);
            Candles(V(side*2.55f,.49f,14.8f),bronze,wax,flame,5,true);
            Candles(V(side*1.05f,1.49f,15.6f),bronze,wax,flame,2,false);
        }
    }

    private static void Candles(Vector3 p,Material bronze,Material wax,Material flame,int count,bool stand)
    {
        string suffix=p.x.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"_"+p.z.ToString("F1",System.Globalization.CultureInfo.InvariantCulture);
        float height=stand?1.55f:0;
        var holder=new ChapelMesh();
        if(stand){holder.Cylinder(p+Vector3.up*.1f,.32f,.2f);holder.Cylinder(p+Vector3.up*.82f,.055f,1.5f);holder.Bar(p+V(-.48f,1.5f,0),p+V(.48f,1.5f,0),.055f);}
        var candles=new ChapelMesh();var flames=new ChapelMesh();
        for(int i=0;i<count;i++)
        {
            float x=(i-(count-1)*.5f)*.23f,h=R(.19f,.34f);Vector3 c=p+V(x,height,0);
            holder.Cylinder(c,.11f,.045f);candles.Cylinder(c+Vector3.up*h*.5f,.043f,h,null,12);
            flames.Cylinder(c+Vector3.up*(h+.055f),.023f,.10f,null,8);
        }
        holder.Save("CandleHolder"+suffix,bronze);candles.Save("Wax"+suffix,wax);
        var glow=flames.Save("Flames"+suffix,flame);glow.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        if(stand)
        {
            Light lamp=new GameObject("Candle pool "+suffix).AddComponent<Light>();lamp.type=LightType.Point;
            lamp.transform.position=p+V(0,1.9f,0);lamp.color=new Color(1,.57f,.29f);lamp.intensity=4.2f;lamp.range=6.2f;lamp.shadows=LightShadows.None;
        }
    }

    private static void Lighting()
    {
        Light sun=new GameObject("Moonlight / through the rose").AddComponent<Light>();
        sun.type=LightType.Directional;sun.transform.rotation=Quaternion.LookRotation(SunDirection);
        sun.color=new Color(.63f,.76f,1);sun.intensity=1.75f;sun.shadows=LightShadows.Soft;sun.shadowStrength=1;sun.shadowBias=.035f;sun.shadowNormalBias=.13f;
        RenderSettings.sun=sun;
        RenderSettings.skybox=null;RenderSettings.reflectionIntensity=.25f;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.011f;RenderSettings.fogColor=new Color(.025f,.036f,.06f);
        var fill=new GameObject("Rose interior bounce").AddComponent<Light>();fill.type=LightType.Point;fill.transform.position=V(0,7,15.1f);
        var air=new GameObject("Soft nave bounce").AddComponent<Light>();air.type=LightType.Directional;air.transform.rotation=Quaternion.LookRotation(V(.25f,-.55f,1));air.shadows=LightShadows.None;
        var entrance=new GameObject("Moonlight reflected into entrance").AddComponent<Light>();entrance.type=LightType.Point;entrance.transform.position=V(0,4.2f,-7);
        ConfigureAmbientLighting();
    }

    [MenuItem("MVP04/Apply Readable Ambient Lighting")]
    public static void ApplyAmbientLighting()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying || scene.path!=ScenePath)
            throw new InvalidOperationException("Open MVP04 outside Play mode to adjust its lighting.");
        ConfigureAmbientLighting();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureAmbientLighting()
    {
        // Lift unlit surfaces using environment and bounced light; keep direct window exposure intact.
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.56f,.61f,.72f);
        RenderSettings.ambientEquatorColor=new Color(.42f,.47f,.58f);
        RenderSettings.ambientGroundColor=new Color(.30f,.35f,.45f);
        Tune("Soft nave bounce",.55f,new Color(.52f,.60f,.76f),0);
        Tune("Moonlight reflected into entrance",8f,new Color(.50f,.60f,.75f),14);
        Tune("Rose interior bounce",8f,new Color(.49f,.65f,.88f),12);

        void Tune(string name,float intensity,Color color,float range)
        {
            var light=GameObject.Find(name)?.GetComponent<Light>();
            if(light==null)return;
            light.intensity=intensity;light.color=color;
            if(range>0)light.range=range;
            EditorUtility.SetDirty(light);
        }
    }

    private static Camera CameraRig(int index)
    {
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.transform.position=V(0,3.7f,-16);camera.transform.LookAt(V(0,1.08f,-3));camera.fieldOfView=56;
        camera.nearClipPlane=.12f;camera.farClipPlane=80;camera.allowHDR=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.009f,.013f,.025f);
        var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(index);data.renderPostProcessing=true;data.requiresDepthTexture=true;data.antialiasing=AntialiasingMode.None;
        camera.gameObject.AddComponent<AudioListener>();return camera;
    }

    private static GameObject Player(Camera camera)
    {
        var root=new GameObject("Pilgrim / WASD + Space");root.transform.position=V(0,.09f,-3);
        var portrait=new GameObject("Pixel portrait");portrait.transform.SetParent(root.transform,false);
        var sr=portrait.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MVP03/Textures/RebuiltTraveler1.png");sr.sortingOrder=2;
        Material material=Material("NocturneTraveler","MVP03/Pixel Cutout");material.SetTexture("_BaseMap",sr.sprite.texture);material.SetColor("_BaseColor",new Color(.62f,.68f,.78f));material.SetFloat("_Cutoff",.5f);material.SetFloat("_Cull",0);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;EditorUtility.SetDirty(material);
        sr.sharedMaterial=material;sr.shadowCastingMode=ShadowCastingMode.TwoSided;portrait.transform.rotation=Quaternion.LookRotation(Vector3.back);
        var cc=root.AddComponent<CharacterController>();cc.height=1.7f;cc.radius=.28f;cc.center=V(0,.85f,0);cc.stepOffset=.30f;cc.skinWidth=.015f;
        root.AddComponent<PixelPilgrim>().Configure(camera,sr);
        root.AddComponent<MagicBoltCaster>().Configure(AssetDatabase.LoadAssetAtPath<MagicBolt>("Assets/MVP03/Prefabs/MagicBolt.prefab"));
        return root;
    }

    private static VolumeProfile Atmosphere()
    {
        string path=Root+"/Rendering/NocturneAtmosphere.asset";var p=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(p==null){p=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(p,path);}
        T Add<T>()where T:VolumeComponent{if(!p.TryGet(out T c)){c=p.Add<T>(true);AssetDatabase.AddObjectToAsset(c,p);}EditorUtility.SetDirty(c);return c;}
        var bloom=Add<Bloom>();bloom.threshold.Override(.9f);bloom.intensity.Override(.28f);bloom.scatter.Override(.68f);
        var tone=Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);
        var color=Add<ColorAdjustments>();color.postExposure.Override(.15f);color.contrast.Override(13);color.saturation.Override(-12);
        var depth=Add<DepthOfField>();depth.mode.Override(DepthOfFieldMode.Bokeh);depth.focusDistance.Override(13.3f);depth.focalLength.Override(55);depth.aperture.Override(5.6f);
        var vignette=Add<Vignette>();vignette.intensity.Override(.20f);vignette.smoothness.Override(.6f);EditorUtility.SetDirty(p);return p;
    }

    private static int Renderer(Texture2D rose)
    {
        string path=Root+"/Rendering/NocturneRenderer.asset";
        if(!File.Exists(path))AssetDatabase.CopyAsset("Assets/MVP03/Rendering/RebuiltRenderer.asset",path);
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        string pixelsPath=Root+"/Materials/SubtlePixels.mat";
        if(!File.Exists(pixelsPath))AssetDatabase.CopyAsset("Assets/MVP03/Materials/SubtlePixels.mat",pixelsPath);
        foreach(var feature in renderer.rendererFeatures)
            if(feature is FullScreenPassRendererFeature full && full.passMaterial!=null && full.passMaterial.shader.name=="MVP03/Subtle Pixels")
            {full.passMaterial=AssetDatabase.LoadAssetAtPath<Material>(pixelsPath);EditorUtility.SetDirty(full);}
        Material volume=Material("RoseWindowVolume","MVP04/Window Volume");volume.SetTexture("_RoseMask",rose);volume.SetVector("_WindowOrigin",Window);volume.SetVector("_LightDirection",SunDirection);volume.SetFloat("_Radius",2.6f);volume.SetFloat("_Density",.045f);volume.SetFloat("_Length",26);volume.SetColor("_ScatterColor",new Color(1.3f,1.5f,1.9f));EditorUtility.SetDirty(volume);
        SharpenWindowBeams(volume);
        var light=renderer.rendererFeatures.OfType<WindowVolumeFeature>().FirstOrDefault();
        if(light==null){light=ScriptableObject.CreateInstance<WindowVolumeFeature>();light.name="Rose window / shadowed dust volume";AssetDatabase.AddObjectToAsset(light,renderer);renderer.rendererFeatures.Add(light);}
        light.material=volume;light.Create();EditorUtility.SetDirty(light);renderer.SetDirty();EditorUtility.SetDirty(renderer);
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");var so=new SerializedObject(pipeline);var list=so.FindProperty("m_RendererDataList");int index=-1;
        for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer)index=i;
        if(index<0){index=list.arraySize;list.InsertArrayElementAtIndex(index);list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;}
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);return index;
    }

    private static Texture2D RoseTexture()
    {
        const int size=512;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
        Color[] palette={new Color(.21f,.46f,.76f),new Color(.52f,.72f,.85f),new Color(.85f,.54f,.23f),new Color(.28f,.55f,.66f)};
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            Vector2 p=new Vector2((x+.5f)/size*2-1,(y+.5f)/size*2-1);float r=p.magnitude,a=Mathf.Atan2(p.y,p.x)+Mathf.PI;
            int sector=Mathf.FloorToInt(a/(Mathf.PI*2)*12);float mid=(sector+.5f)*Mathf.PI*2/12-Mathf.PI;
            Vector2 axis=new Vector2(Mathf.Cos(mid),Mathf.Sin(mid));float radial=Vector2.Dot(p,axis),tangent=p.x*-axis.y+p.y*axis.x;
            float petal=Mathf.Sqrt(Mathf.Pow((radial-.51f)/.30f,2)+Mathf.Pow(tangent/.125f,2));
            Color color=palette[sector%4];bool lead=false;
            if(r<.27f){color=new Color(.92f,.70f,.31f);lead=Mathf.Abs(r-(.19f+.025f*Mathf.Cos(a*8)))<.012f;}
            else if(r<.81f){lead=petal>1||Mathf.Abs(petal-.88f)<.024f;color*=.78f+.22f*Mathf.Sin(radial*27);}
            else {int tile=Mathf.FloorToInt(a/(Mathf.PI*2)*24);color=palette[(tile+2)%4];lead=Mathf.Abs(Mathf.Sin(a*12))<.06f;}
            lead|=Mathf.Abs(r-.27f)<.012f||Mathf.Abs(r-.80f)<.011f||r>.972f;
            float grain=.83f+.17f*Mathf.PerlinNoise(x*.12f,y*.12f);color*=grain;
            if(lead)color=new Color(.018f,.027f,.042f);
            if(r>1)color=Color.black;texture.SetPixel(x,y,color);
        }
        texture.Apply();string path=Root+"/Textures/RoseTransmission.png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void Dust()
    {
        var go=new GameObject("Sparse suspended dust");go.transform.position=V(0,4,4);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(12,20);main.startSpeed=.035f;main.startSize=new ParticleSystem.MinMaxCurve(.008f,.023f);main.maxParticles=100;main.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=ps.emission;emission.rateOverTime=4;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=V(7,6,21);
        Material material=Material("DustMotes","MVP03/Magic Glow");material.SetColor("_BaseColor",new Color(.45f,.57f,.7f,.14f));material.SetFloat("_RimWeight",0);EditorUtility.SetDirty(material);
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }

    private static Material Material(string name,string shader)
    {string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find(shader)){name=name};AssetDatabase.CreateAsset(m,path);}return m;}
    private static Material Lit(string name,Color tint,float smooth,float metal=0)
    {var m=Material(name,"Universal Render Pipeline/Lit");m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);EditorUtility.SetDirty(m);return m;}
    private static Material Stone(string name,Color tint,float smooth)
    {var m=Material(name,"MVP03/World Stone PBR");m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MVP03/Textures/RebuiltLimestone.png"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MVP03/Textures/RebuiltLimestoneNormal.png"));m.SetColor("_BaseColor",tint);m.SetFloat("_WorldScale",.8f);m.SetFloat("_BumpScale",.65f);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
    private static void Emission(Material material,Color color)
    {material.SetColor("_EmissionColor",color);material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;material.EnableKeyword("_EMISSION");EditorUtility.SetDirty(material);}
    private static void Collision(string name,Vector3 p,Vector3 size)
    {var go=new GameObject(name);go.transform.position=p;go.AddComponent<BoxCollider>().size=size;}

    [MenuItem("MVP04/Capture Dark Chapel Preview")]
    public static void CapturePreview()
    {
        Camera camera=Camera.main;if(camera==null)throw new InvalidOperationException("Open MVP04 first.");
        CaptureCamera(camera,"Previews/MVP04_DarkChapel.png");
    }

    private static void CaptureCamera(Camera camera,string path)
    {
        var target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();Directory.CreateDirectory("Previews");File.WriteAllBytes(path,pixels.EncodeToPNG());}
        finally{camera.targetTexture=oldTarget;RenderTexture.active=oldActive;UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);}
    }
}
