using System;
using System.Collections.Generic;
using System.IO;
using MVP03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class MVP03ReferenceBuilder
{
    const string Root="Assets/MVP03";
    static System.Random rng;
    static float R(float min,float max)=>min+(float)rng.NextDouble()*(max-min);
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Color Shade(float low=.78f,float high=1.06f){float v=R(low,high); return new Color(v,v,v,1);}

    [MenuItem("MVP03/Rebuild Reference Courtyard")]
    public static void Build()
    {
        rng=new System.Random(20261003);
        Directory.CreateDirectory(Root+"/Meshes");
        AssetDatabase.Refresh();
        var stone=StoneMaterial();
        var shadow=Material("RebuiltMortar",new Color(.26f,.28f,.26f),.05f);
        var bark=Material("RebuiltBark",new Color(.16f,.135f,.095f),.12f);
        var wood=Material("RebuiltOak",new Color(.19f,.1f,.055f),.2f);
        var iron=Material("RebuiltIron",new Color(.065f,.073f,.073f),.4f);
        var leafMats=new Material[5];
        Color[] leafColors={new Color(.31f,.42f,.14f),new Color(.47f,.59f,.21f),new Color(.64f,.73f,.34f),new Color(.38f,.51f,.20f),new Color(.74f,.79f,.43f)};
        for(int i=0;i<5;i++){leafMats[i]=Material("RebuiltLeaf"+i,leafColors[i],.14f);leafMats[i].SetFloat("_Cull",0);}
        var flowerMats=new[]{Material("RebuiltBlueFlower",new Color(.29f,.44f,.8f),.1f),Material("RebuiltGoldFlower",new Color(.9f,.73f,.22f),.1f)};
        Sprite[] travelers=ImportTravelers();
        int renderer=ConfigureRenderer();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var masonry=new Batch(); var floor=new Batch(); var steps=new Batch(); var trim=new Batch(); var oak=new Batch();

        // The playable route is a small, enclosed court. Individual beveled stones carry the surface detail.
        ColliderBox("Courtyard collision",V(-2,-.16f,-15),V(24,.4f,31));
        var baseFloor=new Batch(); baseFloor.Box(V(-2,-.19f,-15),V(24,.4f,31),.02f,Color.white);
        baseFloor.Save("RebuiltMortar",shadow);
        for(int row=0;row<39;row++) for(int col=0;col<26;col++)
        {
            float x=-12+col*.69f+(row%2)*.345f, z=-28+row*.72f;
            if(x>4.6f) continue;
            Vector3 pos=V(x,R(-.016f,.015f),z);
            floor.Paver(pos,new Vector2(R(.675f,.687f),R(.705f,.715f)),R(.035f,.055f),Shade(.76f,1.03f));
        }

        // Block courses, recessed mortar, stepped mouldings and arches retain visible stone scale.
        Wall(masonry,V(-10.2f,0,-14),V(1.4f,11,29),false);
        Wall(masonry,V(0,0,2.5f),V(27,14,1.4f),true);
        for(int i=0;i<3;i++)
        {
            float z=-24+i*9.4f;
            Pier(masonry,trim,V(-8.4f,0,z),8.7f);
            if(i<2) Arch(trim,V(-8.4f,6.1f,z+4.7f),4.15f,1.0f,true);
        }
        // A close foreground pillar supplies the reference's left occlusion.
        Pier(masonry,trim,V(-5.85f,0,-25.7f),9.1f);
        for(int side=-1;side<=1;side+=2)
        {
            Pier(masonry,trim,V(side*3.6f,0,.5f),8.5f);
            Wall(masonry,V(side*10f,0,.9f),V(2.1f,12.5f,3.8f),false);
        }
        Arch(trim,V(0,5.2f,.15f),3.25f,.8f,false);
        // Dark timber door panels and iron studs behind the carved portal.
        for(int side=-1;side<=1;side+=2) for(int j=0;j<6;j++)
            oak.Box(V(side*(.18f+j*.47f),2.65f,1.52f),V(.44f,5.3f,.3f),.025f,Shade(.75f,1.1f));
        var metal=new Batch();
        for(int y=1;y<5;y+=2) for(int side=-1;side<=1;side+=2)
            metal.Box(V(side*1.6f,y,1.28f),V(2.7f,.14f,.1f),.025f,Color.white);
        // Wooden pew at the edge of the playable route.
        for(int i=0;i<4;i++) oak.Box(V(-6.7f,.66f,-20.8f+i*.2f),V(2.7f,.15f,.18f),.03f,Shade());
        oak.Box(V(-6.7f,1.1f,-20.05f),V(2.7f,.7f,.14f),.02f,Shade());
        for(int side=-1;side<=1;side+=2) oak.Box(V(-6.7f+side*1,.3f,-20.5f),V(.2f,.6f,.65f),.03f,Shade());

        // A broad flight on the right, with worn stone blocks and individual caps.
        for(int i=0;i<22;i++)
        {
            float z=-25+i*.78f, y=.12f+i*.205f;
            for(int j=0;j<5;j++) steps.Box(V(6.65f+(j-2)*1.2f,y-.1f,z),V(1.18f,.3f,.79f),.045f,Shade(.87f,1.1f));
            ColliderBox("Stair collision "+i,V(6.65f,(y+.05f)*.5f,z),V(6.1f,y+.05f,.8f));
            if(i%2==0) for(int side=-1;side<=1;side+=2)
            {
                steps.Box(V(6.65f+side*3.45f,y+.43f,z+.36f),V(.63f,1.12f,1.55f),.045f,Shade(.78f,.94f));
                trim.Box(V(6.65f+side*3.45f,y+1.03f,z+.36f),V(.91f,.18f,1.58f),.055f,Shade(.94f,1.12f));
            }
        }
        steps.Box(V(6.65f,4.53f,-5.4f),V(7.6f,.34f,5.4f),.07f,Shade());
        // Front coping blocks and floor edging give cast and contact shadows at several scales.
        for(int i=0;i<22;i++) trim.Box(V(2.92f,.12f,-27+i*.86f),V(.37f,.23f,.84f),.025f,Shade(.74f,.92f));
        masonry.Save("RebuiltArchitecture",stone); floor.Save("RebuiltPaving",stone); steps.Save("RebuiltStairs",stone);
        trim.Save("RebuiltCarvedEdges",stone); oak.Save("RebuiltWoodwork",wood); metal.Save("RebuiltIronwork",iron);
        ColliderBox("West wall collision",V(-10.2f,5,-14),V(1.4f,10,29));
        ColliderBox("Church wall collision",V(0,6,2.5f),V(27,12,1.4f));

        Tree(bark,leafMats);
        Garden(leafMats,flowerMats);
        Camera camera=CameraRig(renderer);
        Actor("Pilgrim / WASD",V(-2.35f,.07f,-18.45f),travelers[1],camera,true);
        Actor("Ranger / companion",V(-4.35f,.07f,-18.9f),travelers[0],camera,false);
        Lighting();
        var volume=new GameObject("MVP03 optical focus and filmic exposure").AddComponent<Volume>();
        volume.isGlobal=true; volume.sharedProfile=Atmosphere();
        camera.gameObject.AddComponent<PixelFocus>().Configure(camera,UnityEngine.Object.FindFirstObjectByType<PixelPilgrim>().transform,volume);
        ConfigureFollowCamera();
        MVP03MagicBuilder.ConfigureScene();
        MVP03WarmLighting.ConfigureScene();
        MVP03SlimeBuilder.ConfigureScene();
        EditorSceneManager.SaveScene(scene,Root+"/Scenes/MVP03_StainedGlassChapel.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("Rebuilt MVP03: beveled masonry, leaf meshes, imported pixel travelers, dedicated SSAO and optical focus.");
    }

    public static void ConfigureFollowCamera()
    {
        Camera camera=Camera.main;
        PixelPilgrim player=UnityEngine.Object.FindFirstObjectByType<PixelPilgrim>();
        if(camera==null || player==null)throw new InvalidOperationException("Open the MVP03 courtyard first.");
        var follow=camera.GetComponent<PixelFollowCamera>();
        if(follow==null)follow=camera.gameObject.AddComponent<PixelFollowCamera>();
        follow.Configure(player.transform);
        var volume=UnityEngine.Object.FindFirstObjectByType<Volume>();
        if(volume!=null && volume.sharedProfile.TryGet(out DepthOfField depth))
        {
            depth.focusDistance.Override(Vector3.Dot(player.transform.position+Vector3.up-camera.transform.position,camera.transform.forward));
            EditorUtility.SetDirty(depth);
        }
        EditorUtility.SetDirty(follow);
        EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
    }

    static void Wall(Batch b,Vector3 basePos,Vector3 size,bool alongX)
    {
        int rows=Mathf.CeilToInt(size.y/.79f);
        float length=alongX?size.x:size.z;
        for(int row=0;row<rows;row++)
        {
            float cursor=-length*.5f;
            while(cursor<length*.5f-.03f)
            {
                float width=Mathf.Min(R(1.0f,1.8f),length*.5f-cursor);
                Vector3 pos=basePos+V(alongX?cursor+width*.5f:0,row*.79f+.39f,alongX?0:cursor+width*.5f);
                b.Box(pos,alongX?V(width-.025f,.765f,size.z):V(size.x,.765f,width-.025f),.045f,Shade(.66f,.98f));
                cursor+=width;
            }
        }
    }
    static void Pier(Batch b,Batch cap,Vector3 p,float h)
    {
        cap.Box(p+V(0,.2f,0),V(2.05f,.4f,2.05f),.09f,Shade(.62f,.82f));
        cap.Box(p+V(0,.49f,0),V(1.8f,.19f,1.8f),.05f,Shade(.85f,1.1f));
        for(float y=.64f;y<h-.6f;y+=.73f)
        {
            b.Box(p+V(0,y+.35f,0),V(1.35f,.71f,1.35f),.04f,Shade(.7f,1.0f));
            for(int side=-1;side<=1;side+=2)
                cap.Box(p+V(side*.68f,y+.35f,-.55f),V(.21f,.715f,.22f),.045f,Shade(.82f,1.03f));
        }
        cap.Box(p+V(0,h-.32f,0),V(1.8f,.22f,1.8f),.055f,Shade());
        cap.Box(p+V(0,h-.1f,0),V(2f,.22f,2f),.055f,Shade());
        ColliderBox("Pier collision",p+V(0,h*.5f,0),V(1.4f,h,1.4f));
    }
    static void Arch(Batch b,Vector3 center,float radius,float depth,bool alongZ)
    {
        for(int i=0;i<19;i++)
        {
            float a=(i+.5f)/19*Mathf.PI;
            Vector3 offset=V(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);
            Quaternion q=Quaternion.Euler(0,alongZ?90:0,a*Mathf.Rad2Deg-90);
            if(alongZ)offset=V(0,offset.y,offset.x);
            b.Box(center+offset,V(radius*Mathf.PI/19+.016f,.56f,depth),.035f,Shade(.8f,1.08f),q);
        }
    }

    static void Tree(Material bark,Material[] mats)
    {
        var branches=new Batch(); var leaves=new Batch[5]; for(int i=0;i<5;i++)leaves[i]=new Batch();
        Vector3 root=V(-4.6f,0,-6.7f);
        Branch(branches,root,root+V(.2f,6.4f,.1f),.24f,.08f);
        for(int i=0;i<16;i++)
        {
            float a=i*2.399f;
            Vector3 start=root+V(.14f,R(3.9f,6.1f),.06f);
            Vector3 end=root+V(Mathf.Cos(a)*R(1.8f,3.8f),R(6.1f,9f),Mathf.Sin(a)*R(1.7f,3.1f));
            Branch(branches,start,end,.11f,.018f);
            for(int twig=0;twig<3;twig++)
            {
                Vector3 tip=end+V(R(-1.4f,1.4f),R(-.5f,1.5f),R(-1.3f,1.3f));
                Branch(branches,Vector3.Lerp(start,end,.7f),tip,.035f,.006f);
                for(int j=0;j<40;j++)
                {
                    Vector3 p=tip+V(R(-1.15f,1.15f),R(-.65f,.65f),R(-1.0f,1.0f));
                    Vector3 n=V(R(-1,1),R(.15f,1),R(-1,1)).normalized;
                    leaves[(j+i)%5].Leaf(p,n,R(.12f,.26f),R(0,360),Color.white);
                }
            }
        }
        branches.Save("RebuiltTreeBranches",bark);
        for(int i=0;i<5;i++)leaves[i].Save("RebuiltTreeLeaves"+i,mats[i]);
        // Out-of-frame leafy canopy uses the same narrow leaf geometry, not round shadow blobs.
        var overhead=new Batch();
        for(int i=0;i<650;i++)overhead.Leaf(V(R(-9,-1),R(6.2f,7.4f),R(-28,-24)),Vector3.up,R(.12f,.29f),R(0,360),Color.white);
        var caster=overhead.Save("RebuiltOffscreenCanopy",mats[1]);
        caster.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.ShadowsOnly;
    }
    static void Branch(Batch b,Vector3 start,Vector3 end,float radius,float tip)
    {
        Vector3 axis=(end-start).normalized, u=Vector3.Cross(axis,Vector3.forward).normalized;
        if(u.sqrMagnitude<.1f)u=Vector3.right;
        Vector3 v=Vector3.Cross(axis,u);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4, c=(i+1)*Mathf.PI/4;
            Vector3 na=u*Mathf.Cos(a)+v*Mathf.Sin(a), nb=u*Mathf.Cos(c)+v*Mathf.Sin(c);
            b.Quad(start+na*radius,start+nb*radius,end+nb*tip,end+na*tip,(na+nb).normalized,Shade(.65f,1.2f));
        }
    }
    static void Garden(Material[] leaves,Material[] flowers)
    {
        var grass=new Batch();var f=new[]{new Batch(),new Batch()};
        for(int i=0;i<400;i++)
        {
            Vector3 p=V(R(-7.5f,1.2f),.045f,R(-10.5f,-3.5f));
            if(p.x>-.5f&&p.z>-6)continue;
            float h=R(.12f,.5f);
            for(int j=0;j<3;j++)grass.Leaf(p+V(0,h*.5f,0),V(R(-1,1),.2f,R(-1,1)).normalized,h*.6f,R(0,360),Shade(.7f,1.1f));
            if(i%2==0)for(int petal=0;petal<5;petal++)
            {
                float a=petal*Mathf.PI*.4f;
                f[i%3==0?0:1].Leaf(p+V(Mathf.Cos(a)*.065f,h,Mathf.Sin(a)*.065f),Vector3.up,.055f,petal*72,Color.white);
            }
        }
        grass.Save("RebuiltGroundPlants",leaves[1]);
        for(int i=0;i<2;i++)f[i].Save("RebuiltFlowers"+i,flowers[i]);
    }

    static Camera CameraRig(int renderer)
    {
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.transform.position=V(5.3f,7.1f,-29.8f);
        camera.transform.LookAt(V(-2.7f,2.05f,-15.5f));
        camera.fieldOfView=46;camera.nearClipPlane=.15f;camera.farClipPlane=90;
        camera.backgroundColor=new Color(.61f,.67f,.68f);camera.clearFlags=CameraClearFlags.SolidColor;
        camera.allowHDR=true;
        var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(renderer);data.renderPostProcessing=true;
        data.requiresDepthTexture=true;data.antialiasing=AntialiasingMode.None;
        camera.gameObject.AddComponent<AudioListener>();return camera;
    }
    static void Actor(string name,Vector3 p,Sprite sprite,Camera camera,bool player)
    {
        var root=new GameObject(name);root.transform.position=p;
        var obj=new GameObject("Pixel portrait");obj.transform.SetParent(root.transform,false);
        obj.transform.localPosition=Vector3.zero;
        var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=sprite;
        sr.sortingOrder=2;sr.color=Color.white;
        // An alpha-tested material writes character depth, keeping the pixel face in focus.
        string matPath=Root+"/Materials/"+sprite.name+"_DepthSprite.mat";
        var spriteMat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(spriteMat==null){spriteMat=new Material(Shader.Find("MVP03/Pixel Cutout"));AssetDatabase.CreateAsset(spriteMat,matPath);}
        spriteMat.shader=Shader.Find("MVP03/Pixel Cutout");
        spriteMat.SetTexture("_BaseMap",sprite.texture);spriteMat.SetColor("_BaseColor",Color.white);
        spriteMat.SetFloat("_Cutoff",.5f);spriteMat.SetFloat("_Cull",0);
        spriteMat.EnableKeyword("_ALPHATEST_ON");spriteMat.renderQueue=2450;EditorUtility.SetDirty(spriteMat);sr.sharedMaterial=spriteMat;sr.shadowCastingMode=ShadowCastingMode.TwoSided;
        Vector3 facing=camera.transform.position-obj.transform.position;facing.y=0;
        obj.transform.rotation=Quaternion.LookRotation(facing);
        if(player){var cc=root.AddComponent<CharacterController>();cc.height=1.7f;cc.radius=.28f;cc.center=V(0,.85f,0);cc.stepOffset=.3f;cc.skinWidth=.015f;root.AddComponent<PixelPilgrim>().Configure(camera,sr);}
        else root.AddComponent<PixelBillboard>().Configure(camera,sr);
        var contact=GameObject.CreatePrimitive(PrimitiveType.Sphere);contact.name="Ground contact shadow";
        UnityEngine.Object.DestroyImmediate(contact.GetComponent<Collider>());
        contact.transform.SetParent(root.transform,false);contact.transform.localPosition=V(0,-.005f,0);contact.transform.localScale=V(.63f,.015f,.35f);
        contact.GetComponent<Renderer>().sharedMaterial=Material("RebuiltContact",new Color(.08f,.1f,.09f),0);
    }
    static void Lighting()
    {
        var sun=new GameObject("Sun / warm diagonal daylight").AddComponent<Light>();sun.type=LightType.Directional;
        sun.transform.rotation=Quaternion.Euler(49,-30,0);sun.color=new Color(1f,.96f,.85f);sun.intensity=2.35f;
        sun.shadows=LightShadows.Soft;sun.shadowStrength=.95f;sun.shadowBias=.08f;sun.shadowNormalBias=.2f;
        RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.40f,.48f,.56f);RenderSettings.ambientEquatorColor=new Color(.25f,.30f,.33f);RenderSettings.ambientGroundColor=new Color(.12f,.13f,.12f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.57f,.63f,.62f);RenderSettings.fogStartDistance=24;RenderSettings.fogEndDistance=66;
    }
    static VolumeProfile Atmosphere()
    {
        string path=Root+"/Rendering/RebuiltAtmosphere.asset";
        var p=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(p==null){p=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(p,path);}
        var depth=Component<DepthOfField>(p);depth.mode.Override(DepthOfFieldMode.Bokeh);depth.focusDistance.Override(15.15f);depth.focalLength.Override(140f);depth.aperture.Override(1.8f);
        var bloom=Component<Bloom>(p);bloom.threshold.Override(1.05f);bloom.intensity.Override(.17f);bloom.scatter.Override(.6f);
        var color=Component<ColorAdjustments>(p);color.contrast.Override(12);color.saturation.Override(-9);color.postExposure.Override(.25f);
        var tone=Component<Tonemapping>(p);tone.mode.Override(TonemappingMode.ACES);
        var vignette=Component<Vignette>(p);vignette.intensity.Override(.16f);vignette.smoothness.Override(.5f);
        EditorUtility.SetDirty(p);return p;
    }
    static T Component<T>(VolumeProfile p) where T:VolumeComponent
    {
        if(!p.TryGet(out T c)){c=p.Add<T>(true);AssetDatabase.AddObjectToAsset(c,p);}EditorUtility.SetDirty(c);return c;
    }
    static int ConfigureRenderer()
    {
        string path=Root+"/Rendering/RebuiltRenderer.asset";
        if(!File.Exists(path))AssetDatabase.CopyAsset("Assets/MVP02/Rendering/MVP02_Renderer.asset",path);
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        MVP03PixelPostBuilder.Configure(renderer);
        ScreenSpaceAmbientOcclusion ao=null;
        foreach(var f in renderer.rendererFeatures)if(f is ScreenSpaceAmbientOcclusion value)ao=value;
        if(ao==null){ao=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();ao.name="Stone crevice contact shadows";AssetDatabase.AddObjectToAsset(ao,renderer);renderer.rendererFeatures.Add(ao);}
        var settings=new SerializedObject(ao);
        settings.FindProperty("m_Settings.Intensity").floatValue=.9f;
        settings.FindProperty("m_Settings.Radius").floatValue=.21f;
        settings.FindProperty("m_Settings.DirectLightingStrength").floatValue=.28f;
        settings.FindProperty("m_Settings.Source").enumValueIndex=0; // reconstruct normals from scene depth
        settings.ApplyModifiedPropertiesWithoutUndo();ao.Create();renderer.SetDirty();EditorUtility.SetDirty(renderer);
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        var so=new SerializedObject(pipeline);var list=so.FindProperty("m_RendererDataList");int index=-1;
        for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer)index=i;
        if(index<0){index=list.arraySize;list.InsertArrayElementAtIndex(index);list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;}
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);return index;
    }
    static Material Material(string name,Color color,float smooth)
    {
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
    }
    static Material StoneMaterial()
    {
        string texturePath=Root+"/Textures/RebuiltLimestone.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.isReadable=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.maxTextureSize=2048;importer.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        string normalPath=Root+"/Textures/RebuiltLimestoneNormal.png";
        // Derive restrained pore relief for the PBR material; geometry supplies the large edges.
        const int size=512;var normal=new Texture2D(size,size,TextureFormat.RGB24,false);
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=(float)x/size,v=(float)y/size,d=1f/size;
            float dx=tex.GetPixelBilinear(u-d,v).grayscale-tex.GetPixelBilinear(u+d,v).grayscale;
            float dy=tex.GetPixelBilinear(u,v-d).grayscale-tex.GetPixelBilinear(u,v+d).grayscale;
            Vector3 n=V(dx*1.2f,dy*1.2f,1).normalized;pixels[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f);
        }
        normal.SetPixels(pixels);normal.Apply();File.WriteAllBytes(normalPath,normal.EncodeToPNG());UnityEngine.Object.DestroyImmediate(normal);
        AssetDatabase.ImportAsset(normalPath);var ni=(TextureImporter)AssetImporter.GetAtPath(normalPath);ni.textureType=TextureImporterType.NormalMap;ni.mipmapEnabled=true;ni.SaveAndReimport();
        string path=Root+"/Materials/RebuiltWorldStone.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("MVP03/World Stone PBR"));AssetDatabase.CreateAsset(m,path);}
        m.shader=Shader.Find("MVP03/World Stone PBR");m.SetTexture("_BaseMap",tex);m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));m.SetColor("_BaseColor",new Color(.88f,.9f,.9f));m.SetFloat("_WorldScale",.65f);m.SetFloat("_BumpScale",.7f);m.SetFloat("_Smoothness",.17f);EditorUtility.SetDirty(m);return m;
    }
    static Sprite[] ImportTravelers()
    {
        string path=Root+"/Textures/RebuiltTravelers.png";var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.isReadable=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
        var source=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var result=new Sprite[2];
        for(int side=0;side<2;side++)
        {
            int minX=source.width,maxX=0,minY=source.height,maxY=0;
            for(int y=0;y<source.height;y++)for(int x=side*source.width/2;x<(side+1)*source.width/2;x++)
                if(source.GetPixel(x,y).a>.5f){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            int height=56,width=Mathf.RoundToInt(56f*(maxX-minX+1)/(maxY-minY+1));
            var cropped=new Texture2D(width,height,TextureFormat.RGBA32,false);
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                var c=source.GetPixel(minX+Mathf.RoundToInt((float)x/(width-1)*(maxX-minX)),minY+Mathf.RoundToInt((float)y/(height-1)*(maxY-minY)));c.a=c.a>.45f?1:0;cropped.SetPixel(x,y,c);
            }
            cropped.Apply();string output=Root+"/Textures/RebuiltTraveler"+side+".png";File.WriteAllBytes(output,cropped.EncodeToPNG());UnityEngine.Object.DestroyImmediate(cropped);
            AssetDatabase.ImportAsset(output);var imp=(TextureImporter)AssetImporter.GetAtPath(output);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=29;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.textureCompression=TextureImporterCompression.Uncompressed;
            var s=new TextureImporterSettings();imp.ReadTextureSettings(s);s.spriteAlignment=(int)SpriteAlignment.Custom;s.spritePivot=new Vector2(.5f,0);imp.SetTextureSettings(s);imp.SaveAndReimport();result[side]=AssetDatabase.LoadAssetAtPath<Sprite>(output);
        }
        return result;
    }
    static void ColliderBox(string name,Vector3 p,Vector3 size)
    {var go=new GameObject(name);go.transform.position=p;go.AddComponent<BoxCollider>().size=size;}

    sealed class Batch
    {
        readonly List<Vector3> verts=new List<Vector3>(),normals=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();readonly List<int> tris=new List<int>();
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 n,Color color)
        {int k=verts.Count;verts.AddRange(new[]{a,b,c,d});for(int i=0;i<4;i++){normals.Add(n);colors.Add(color);}tris.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});}
        public void Box(Vector3 center,Vector3 size,float bevel,Color color,Quaternion? rotation=null)
        {
            Quaternion q=rotation??Quaternion.identity;Vector3 half=size*.5f;bevel=Mathf.Min(bevel,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.8f);
            Vector3 inner=half-Vector3.one*bevel;
            Vector3[] ns={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            foreach(Vector3 n in ns)
            {
                Vector3 u=Mathf.Abs(n.y)>.5f?Vector3.right:Vector3.Cross(Vector3.up,n);Vector3 v=Vector3.Cross(n,u);
                float hu=Vector3.Dot(Abs(u),half),hv=Vector3.Dot(Abs(v),half),hn=Vector3.Dot(Abs(n),half);
                float[] us={-hu,-hu+bevel,hu-bevel,hu},vs={-hv,-hv+bevel,hv-bevel,hv};
                int offset=verts.Count;
                for(int y=0;y<4;y++)for(int x=0;x<4;x++)
                {
                    Vector3 p=n*hn+u*us[x]+v*vs[y];Vector3 clamped=V(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));Vector3 normal=(p-clamped).normalized;
                    verts.Add(center+q*(clamped+normal*bevel));normals.Add(q*normal);colors.Add(color);
                }
                for(int y=0;y<3;y++)for(int x=0;x<3;x++){int a=offset+y*4+x;tris.AddRange(new[]{a,a+1,a+5,a,a+5,a+4});}
            }
        }
        public void Paver(Vector3 p,Vector2 size,float h,Color color)
        {
            Vector3[] bottom=new Vector3[8],top=new Vector3[8];float rx=size.x*.5f,rz=size.y*.5f;
            Vector2[] corners={new Vector2(-.84f,-1),new Vector2(.84f,-1),new Vector2(1,-.84f),new Vector2(1,.84f),new Vector2(.84f,1),new Vector2(-.84f,1),new Vector2(-1,.84f),new Vector2(-1,-.84f)};
            for(int i=0;i<8;i++){float jitter=R(.99f,1.006f);bottom[i]=p+V(corners[i].x*rx*jitter,-.01f,corners[i].y*rz*jitter);top[i]=p+V(corners[i].x*(rx-.012f)*jitter,h,corners[i].y*(rz-.012f)*jitter);}
            for(int i=0;i<8;i++){int next=(i+1)%8;Quad(p+V(0,h,0),top[next],top[i],p+V(0,h,0),Vector3.up,color);Vector3 n=Vector3.Cross(top[next]-bottom[next],bottom[i]-bottom[next]).normalized;Quad(bottom[i],bottom[next],top[next],top[i],n,color);}
        }
        public void Leaf(Vector3 p,Vector3 normal,float length,float angle,Color color)
        {
            Quaternion q=Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(0,angle,0);
            Vector3 center=p+normal*.018f;
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI*.25f,b=(i+1)*Mathf.PI*.25f;
                Vector3 v0=p+q*V(Mathf.Sin(a)*length*.52f,0,Mathf.Cos(a)*length);
                Vector3 v1=p+q*V(Mathf.Sin(b)*length*.52f,0,Mathf.Cos(b)*length);
                Quad(center,v0,v1,center,normal,color);
            }
        }
        static Vector3 Abs(Vector3 v)=>V(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
        public GameObject Save(string name,Material material)
        {
            string path=Root+"/Meshes/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.name=name;mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var go=new GameObject(name);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
    }
}
