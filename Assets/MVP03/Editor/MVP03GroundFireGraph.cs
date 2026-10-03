using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

// Graph authoring API is internal in VFX Graph 17.3. Keep the version-specific calls
// here; the player only consumes the saved, editable .vfx asset and public VFX APIs.
public static class MVP03GroundFireGraph
{
    public const string GraphPath = "Assets/MVP03/VFX/GroundFire.vfx";
    public const string TexturePath = "Assets/MVP03/Textures/FlameFlipbook.png";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [MenuItem("MVP03/Rebuild Ground Fire VFX Graph")]
    public static void Rebuild() => Build(true);

    public static VisualEffectAsset Build(bool rebuild = false)
    {
        var existing = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        if (existing != null && !rebuild) return existing;
        Directory.CreateDirectory("Assets/MVP03/VFX");
        string package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(TypeOf("UnityEditor.VFX.VFXGraph").Assembly).resolvedPath;
        if (!File.Exists(TexturePath))
            throw new FileNotFoundException("The authored 4 x 4 flame flipbook is required.", TexturePath);
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        importer.sRGBTexture = false; importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = true; importer.isReadable = false; importer.maxTextureSize = 2048;
        importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.SaveAndReimport();
        // Rebuilding preserves this graph's .meta / GUID and its scene references.
        File.Copy(Path.Combine(package, "Editor/Templates/01_Minimal_System.vfx"), GraphPath, true);
        AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceSynchronousImport);
        var resource = Invoke(TypeOf("UnityEditor.VFX.VisualEffectResource"), "GetResourceAtPath", GraphPath);
        var graph = Invoke(TypeOf("UnityEditor.VFX.VisualEffectResourceExtensions"), "GetOrCreateGraph", resource);
        var contexts = Items(Get(graph, "children")).ToArray();
        var spawn = contexts.Single(c => c.GetType().Name == "VFXBasicSpawner");
        var init = contexts.Single(c => c.GetType().Name == "VFXBasicInitialize");
        var update = contexts.Single(c => c.GetType().Name == "VFXBasicUpdate");
        var output = contexts.Single(c => c.GetType().Name == "VFXPlanarPrimitiveOutput" || c.GetType().Name == "VFXQuadOutput");
        foreach (var context in new[] { spawn, init, update, output })
            foreach (var child in Items(Get(context, "children")).ToArray()) Invoke(context, "RemoveChild", child, true);
        Set(spawn, "label", "Ground fire / continuous GPU particles"); Set(spawn, "position", new Vector2(0,0));
        Set(init, "label", "Birth on merged ground footprint"); Set(init, "position", new Vector2(0,200));
        Set(update, "label", "Buoyancy + Curl Noise turbulence"); Set(update, "position", new Vector2(0,580));
        Set(output, "label", "Yellow fire / animated 4 x 4 flipbook"); Set(output, "position", new Vector2(0,990));
        var data = Invoke(init, "GetData"); Setting(data, "capacity", 8192u); Setting(data, "boundsMode", "Manual");
        Set(init, "space", "World");

        var rate = Node("UnityEditor.VFX.VFXSpawnerConstantRate", spawn);
        var initialize = Custom(init, "Spawn from ground coverage", "Assets/MVP03/Shaders/GroundFireInitialize.hlsl");
        var turbulence = Node("UnityEditor.VFX.Block.Turbulence", update);
        Setting(turbulence, "NoiseType", "Perlin");
        Value(turbulence, "octaves", 3); Value(turbulence, "roughness", .5f); Value(turbulence, "lacunarity", 2f);
        Value(turbulence, "Drag", 2f);
        var animate = Custom(update, "Rise, shrink and cool", "Assets/MVP03/Shaders/GroundFireUpdate.hlsl");
        var orient = Node("UnityEditor.VFX.Block.Orient", output); Setting(orient, "mode", "FaceCameraPlane");
        Setting(output, "uvMode", "Flipbook"); Setting(output, "flipbookBlendFrames", true);
        Setting(output, "useSoftParticle", true); Setting(output, "sort", "Off");
        Value(output, "softParticleFadeDistance", .12f);
        var flipbook = Slot(output, "flipBookSize");
        Value(flipbook, "x", 4); Value(flipbook, "y", 4);
        Value(output, "mainTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));

        int order = 0;
        object Parameter(string name, Type type, object value)
        {
            var parameter = Node("UnityEditor.VFX.VFXParameter", null);
            Invoke(parameter, "Init", type); Setting(parameter, "m_ExposedName", name); Setting(parameter, "m_Exposed", true);
            Set(parameter, "value", value); Set(parameter, "order", order++);
            Invoke(graph, "AddChild", parameter, -1, true);
            Invoke(parameter, "CreateDefaultNode", new Vector2(-540, order * 100));
            return Items(Get(parameter, "outputSlots")).Single();
        }
        void Bind(object node, string slot, object parameter) => Invoke(Slot(node, slot), "Link", parameter, true);
        var coverage = Parameter("Coverage", typeof(Texture2D), Texture2D.blackTexture); Bind(initialize,"Coverage",coverage);
        Bind(initialize,"Atlas",Parameter("Atlas",typeof(Vector4),new Vector4(-10,-10,20,20)));
        Bind(initialize,"Region",Parameter("Region",typeof(Vector4),new Vector4(-2,-2,4,4)));
        Bind(initialize,"Elevation",Parameter("Elevation",typeof(float),.035f));
        var height = Parameter("FlameHeight",typeof(float),.55f);
        Bind(initialize,"FlameHeight",height); Bind(animate,"FlameHeight",height);
        var speed = Parameter("RiseSpeed",typeof(float),1.2f); Bind(initialize,"RiseSpeed",speed); Bind(animate,"RiseSpeed",speed);
        Bind(initialize,"Threshold",Parameter("Threshold",typeof(float),.32f));
        Bind(initialize,"EdgeSoftness",Parameter("EdgeSoftness",typeof(float),.14f));
        Bind(animate,"FlameWidth",Parameter("FlameWidth",typeof(float),.44f));
        var pivot = Parameter("ParticlePivot",typeof(Vector3),new Vector3(0,-.42f,0));
        Bind(initialize,"ParticlePivot",pivot); Bind(animate,"ParticlePivot",pivot);
        Bind(animate,"Brightness",Parameter("Brightness",typeof(float),1.65f));
        Bind(animate,"AnimationFPS",Parameter("AnimationFPS",typeof(float),16f));
        var delta = Node("UnityEditor.VFX.VFXDynamicBuiltInParameter", graph);
        Setting(delta,"m_BuiltInParameters","VfxDeltaTime"); Set(delta,"position",new Vector2(-500,560));
        Bind(animate,"DeltaTime",Items(Get(delta,"outputSlots")).Single());
        Bind(rate,"Rate",Parameter("SpawnRate",typeof(float),600f));
        Bind(turbulence,"Intensity",Parameter("Turbulence",typeof(float),1.1f));
        Bind(turbulence,"frequency",Parameter("TurbulenceFrequency",typeof(float),1.6f));
        var bounds = Slot(init, "bounds");
        Invoke(Slot(bounds,"center"),"Link",Parameter("BoundsCenter",typeof(Vector3),new Vector3(0,2,0)),true);
        Invoke(Slot(bounds,"size"),"Link",Parameter("BoundsSize",typeof(Vector3),new Vector3(8,6,8)),true);
        EditorUtility.SetDirty((Object)graph);
        Invoke(TypeOf("UnityEditor.VFX.VisualEffectResourceExtensions"), "WriteAssetWithSubAssets", resource);
        AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
    }

    private static object Custom(object parent, string label, string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var node = Node("UnityEditor.VFX.Block.CustomHLSL", parent);
        Setting(node,"m_BlockName",label); Setting(node,"m_ShaderFile",AssetDatabase.LoadMainAssetAtPath(path)); return node;
    }
    private static object Node(string name, object parent)
    {
        var node = ScriptableObject.CreateInstance(TypeOf(name));
        if(parent != null) Invoke(parent,"AddChild",node,-1,true); return node;
    }
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t != null);
    private static System.Collections.Generic.IEnumerable<object> Items(object value) => ((IEnumerable)value).Cast<object>();
    private static PropertyInfo Property(object owner,string name)
    {
        for(Type type=owner.GetType();type!=null;type=type.BaseType)
        {
            var property=type.GetProperty(name,Flags|BindingFlags.DeclaredOnly);
            if(property!=null) return property;
        }
        return null;
    }
    private static object Get(object owner,string name) => Property(owner,name).GetValue(owner);
    private static void Set(object owner,string name,object value)
    {
        var property=Property(owner,name);
        property.SetValue(owner,property.PropertyType.IsEnum && value is string text ? Enum.Parse(property.PropertyType,text) : value);
    }
    private static void Setting(object owner,string name,object value)
    {
        for(Type type=owner.GetType();type!=null;type=type.BaseType)
        {
            var field=type.GetField(name,Flags|BindingFlags.DeclaredOnly);
            if(field!=null && field.FieldType.IsEnum && value is string text) { value=Enum.Parse(field.FieldType,text); break; }
        }
        Invoke(owner,"SetSettingValue",name,value);
    }
    private static object Slot(object owner,string name)
    {
        var property=Property(owner,"inputSlots") ?? Property(owner,"children");
        return Items(property.GetValue(owner)).FirstOrDefault(s=>(string)Get(s,"name")==name || owner.GetType().Name=="CustomHLSL" && (string)Get(s,"name")=="_"+name)
            ?? throw new InvalidOperationException(owner.GetType().Name + " missing slot " + name + ": " + string.Join(",",Items(property.GetValue(owner)).Select(s=>Get(s,"name"))));
    }
    private static void Value(object owner,string name,object value) => Set(Slot(owner,name),"value",value);
    private static object Invoke(object owner,string name,params object[] args)
    {
        Type type=owner as Type ?? owner.GetType();
        var method=type.GetMethods(Flags).First(m=>m.Name==name && !m.IsGenericMethod && m.GetParameters().Length==args.Length && m.GetParameters().Select((p,i)=>args[i]==null || p.ParameterType.IsInstanceOfType(args[i])).All(x=>x));
        try { return method.Invoke(owner is Type ? null : owner,args); }
        catch(TargetInvocationException ex) { throw ex.InnerException ?? ex; }
    }
}
