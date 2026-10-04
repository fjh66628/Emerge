using System;
using System.IO;
using MVP04;
using MVP04.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class MVP04Builder
{
    private const float SideHalfWidth=1.22f, SideBottom=3f, SideSpring=6.15f, SideCrown=1.35f;

    [MenuItem("MVP04/Capture Side Window Preview")]
    public static void CaptureSideWindowPreview()
        => CaptureSidePreview(1,"Previews/MVP04_SideWindows.png");

    [MenuItem("MVP04/Capture Left Window Preview")]
    public static void CaptureLeftWindowPreview()
        => CaptureSidePreview(-1,"Previews/MVP04_LeftWindows.png");

    private static void CaptureSidePreview(int side,string path)
    {
        Camera camera=Camera.main;if(camera==null)throw new InvalidOperationException("Open MVP04 first.");
        var position=camera.transform.position;var rotation=camera.transform.rotation;
        try
        {
            camera.transform.position=V(-side*.7f,3.4f,-6.5f);camera.transform.LookAt(V(side*8.7f,4.3f,3));
            CaptureCamera(camera,path);
        }
        finally{camera.transform.SetPositionAndRotation(position,rotation);}
    }

    [MenuItem("MVP04/Add Side Stained Glass Windows")]
    public static void ApplySideWindows()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying || scene.path!=ScenePath)
            throw new InvalidOperationException("Open MVP04 outside Play mode first.");
        Remove("Side stained glass and moonlight");
        for(int side=-1;side<=1;side+=2)
        {
            Remove("SideWallBacking"+side);
            for(int bay=0;bay<5;bay++)
            {
                Remove("AshlarWall"+side+"Bay"+bay);
                Remove("ColumnAndCarving"+side+"Bay"+bay);
            }
        }
        BuildSideWindows(AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/MidnightLimestone.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/WornCarvings.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/DeepMortar.mat"));
        ConfigurePhysicalLighting();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("MVP04: eight open side windows, stained glass, projected colour and shadowed volumes installed.");
        void Remove(string name){var go=GameObject.Find(name);if(go!=null)UnityEngine.Object.DestroyImmediate(go);}
    }

    private static void BuildSideWindows(Material stone,Material trim,Material mortar)
    {
        // Keep this update independent of the random sequence used by furniture and the rest of the chapel.
        var previous=random;random=new System.Random(20408);
        var root=new GameObject("Side stained glass and moonlight").transform;
        var texture=SideGlassTexture();
        var glass=FrostedGlass("LuminousSideGlass",texture,1.65f,new Vector2(2.44f,4.5f));
        var bronze=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/AgedBronze.mat");
        // Clustered lighting retains nearby candle/bounce lights alongside the exterior emitter.
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"/Rendering/NocturneRenderer.asset");
        var rendererSettings=new SerializedObject(renderer);rendererSettings.FindProperty("m_RenderingMode").intValue=2;
        rendererSettings.ApplyModifiedPropertiesWithoutUndo();renderer.SetDirty();
        for(int side=-1;side<=1;side+=2)
        {
            var backing=new ChapelMesh();
            float cursor=-13;
            for(int bay=0;bay<4;bay++)
            {
                float z=-6+bay*6;
                Solid(cursor,z-SideHalfWidth,0,13.6f);
                Solid(z-SideHalfWidth,z+SideHalfWidth,0,SideBottom);
                // Narrow solid strips follow the curved opening; the carved arch covers their edges.
                for(int s=0;s<48;s++)
                {
                    float a=-SideHalfWidth+s*2*SideHalfWidth/48,b=a+2*SideHalfWidth/48;
                    float top=SideSpring+SideCrown*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow((a+b)*.5f/SideHalfWidth,2)));
                    Solid(z+a,z+b,top,13.6f);
                }
                cursor=z+SideHalfWidth;
                MakeSideWindow(side,bay,z,root,trim,bronze,glass);
            }
            Solid(cursor,19,0,13.6f);
            backing.Save("SideWallBacking"+side,mortar,true);
            for(int bay=0;bay<5;bay++)
            {
                float z=-9+bay*6;var wall=new ChapelMesh();var detail=new ChapelMesh();
                for(int y=0;y<19;y++)for(int j=0;j<5;j++)
                    SideAshlar(wall,side,.35f+y*.69f,z+(j-2)*1.18f+(y%2)*.1f,Shade());
                for(int y=0;y<12;y++)
                {
                    float cy=.72f+y*.56f;
                    detail.Cylinder(V(side*5.35f,cy,z),.45f,.545f,Shade(.87f,1.04f));
                    for(int flute=-1;flute<=1;flute+=2)
                        detail.Cylinder(V(side*5.35f+flute*.41f,cy,z),.14f,.548f,Shade(.82f,1.06f),10);
                }
                detail.Box(V(side*5.35f,.21f,z),V(1.55f,.42f,1.5f),.065f);
                detail.Box(V(side*5.35f,.5f,z),V(1.13f,.16f,1.05f),.025f);
                detail.Box(V(side*5.35f,7.22f,z),V(1.18f,.33f,1.13f),.045f);
                detail.Box(V(side*5.35f,7.46f,z),V(1.48f,.16f,1.34f),.025f);
                detail.Box(V(side*8.55f,.32f,z),V(.6f,.55f,6),.04f);
                // Wall pilasters occupy the solid masonry between the new windows.
                detail.Box(V(side*8.48f,3.7f,z),V(.3f,6.8f,.42f),.035f);
                wall.Save("AshlarWall"+side+"Bay"+bay,stone);
                detail.Save("ColumnAndCarving"+side+"Bay"+bay,trim,true);
            }
            void Solid(float a,float b,float bottom,float top)
            {if(b>a)backing.Box(V(side*9.05f,(bottom+top)*.5f,(a+b)*.5f),V(.7f,top-bottom,b-a),0);}
        }
        random=previous;
    }

    private static void SideAshlar(ChapelMesh mesh,int side,float y,float z,Color color)
    {
        float low=y-.333f,high=y+.333f,left=z-.578f,right=z+.578f;
        int window=Mathf.RoundToInt((z+6)/6);
        float centre=-6+window*6;
        if(window<0||window>3||high<=SideBottom||low>=SideSpring+SideCrown||Mathf.Abs(z-centre)>SideHalfWidth+.578f)
        {Piece(left,right,low,high);return;}
        if(low<SideBottom)Piece(left,right,low,SideBottom);
        low=Mathf.Max(low,SideBottom);
        float width=low<=SideSpring?SideHalfWidth:SideHalfWidth*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow((low-SideSpring)/SideCrown,2)));
        Piece(left,Mathf.Min(right,centre-width),low,high);
        Piece(Mathf.Max(left,centre+width),right,low,high);
        void Piece(float a,float b,float c,float d)
        {if(b-a>.025f&&d-c>.025f)mesh.Box(V(side*8.73f,(c+d)*.5f,(a+b)*.5f),V(.25f,d-c,b-a),.018f,color);}
    }

    private static void MakeSideWindow(int side,int bay,float z,Transform parent,Material stone,Material bronze,Material glass)
    {
        string suffix=side+"Bay"+bay;
        Vector3 spring=V(side*8.65f,SideSpring,z);Quaternion rotation=Quaternion.Euler(0,side*90,0);
        var frame=new ChapelMesh();
        frame.Arch(spring,SideHalfWidth,SideCrown,.22f,.65f,40,rotation);
        frame.Arch(spring-V(side*.12f,0,0),SideHalfWidth+.26f,SideCrown+.26f,.09f,.30f,40,rotation);
        for(int edge=-1;edge<=1;edge+=2)
            frame.Box(V(side*8.65f,(SideBottom+SideSpring)*.5f,z+edge*(SideHalfWidth+.11f)),V(.65f,SideSpring-SideBottom,.22f),.02f);
        frame.Box(V(side*8.55f,SideBottom-.13f,z),V(.88f,.26f,3.02f),.035f);
        frame.Save("SideWindowStone"+suffix,stone,true).transform.SetParent(parent,true);
        var lead=new ChapelMesh();
        lead.Bar(V(side*8.68f,SideBottom,z),V(side*8.68f,7.46f,z),.058f);
        for(int row=0;row<3;row++)
            lead.Bar(V(side*8.68f,3.55f+row*.95f,z-SideHalfWidth),V(side*8.68f,3.55f+row*.95f,z+SideHalfWidth),.043f);
        lead.Arch(V(side*8.68f,6.56f,z),.48f,.48f,.042f,.06f,40,rotation,360);
        lead.Save("SideWindowLeading"+suffix,bronze).transform.SetParent(parent,true);
        var pane=new ChapelMesh();
        // Quad's local U is mirrored on the right wall to keep the front face towards the nave.
        float a=z-side*SideHalfWidth,b=z+side*SideHalfWidth;
        pane.Quad(V(side*8.86f,SideBottom,a),V(side*8.86f,SideBottom,b),V(side*8.86f,7.5f,b),V(side*8.86f,7.5f,a),V(-side,0,0),Color.white);
        var go=pane.Save("SideStainedGlass"+suffix,glass);go.transform.SetParent(parent,true);
        go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }

    private static Color SideGlass(float u,float v)
    {
        float x=u*2-1,y=SideBottom+v*4.5f;
        float top=SideSpring+SideCrown*Mathf.Sqrt(Mathf.Max(0,1-x*x));
        if(Mathf.Abs(x)>1||v<0||y>top)return Color.clear;
        Color blue=new Color(.12f,.35f,.66f),teal=new Color(.23f,.58f,.64f),gold=new Color(.82f,.51f,.17f);
        float cells=v*7,diamond=Mathf.Abs(Mathf.Abs(x)-.48f)*2.4f+Mathf.Abs(Mathf.Repeat(cells,1)-.5f)*1.5f;
        Color color=diamond<.52f?gold:((Mathf.FloorToInt(cells)+(x>0?1:0))%2==0?blue:teal);
        float rose=new Vector2(x*SideHalfWidth,y-6.56f).magnitude;
        if(y>6.08f)color=rose<.43f?gold:blue;
        bool lead=Mathf.Abs(x)<.023f||Mathf.Abs(diamond-.56f)<.037f||Mathf.Abs(Mathf.Repeat(cells,1)-.02f)<.035f||Mathf.Abs(rose-.47f)<.03f||Mathf.Abs(x)>.974f;
        color*=.78f+.22f*Mathf.PerlinNoise(u*74,v*123);
        if(lead)color=new Color(.014f,.021f,.032f);
        color.a=1;return color;
    }

    private static Texture2D SideGlassTexture()
    {
        var texture=new Texture2D(256,512,TextureFormat.RGBA32,false);
        for(int y=0;y<512;y++)for(int x=0;x<256;x++)texture.SetPixel(x,y,SideGlass((x+.5f)/256,(y+.5f)/512));
        return SaveWindowTexture(texture,"SideGlassTransmission",false);
    }

    private static Texture2D SaveWindowTexture(Texture2D texture,string name,bool cookie)
    {
        texture.Apply();string path=Root+"/Textures/"+name+".png";
        File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Clamp;
        importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=true;importer.isReadable=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.alphaSource=cookie?TextureImporterAlphaSource.None:TextureImporterAlphaSource.FromInput;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
