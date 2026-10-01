using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

internal static class MVP01FloraBuilder
{
    private const string Root = "Assets/MVP01";
    private const string MeshPath = Root + "/Meshes/UncannyFlora.asset";
    private const string MaterialPath = Root + "/Materials/Uncanny_Flora.mat";
    private const int GrassCount = 1000;
    private const int FlowerCount = 190;
    private const int GroundFlowerClusters = 650;
    private const float FlowerCircleRadius = 8.5f;
    private const float BoardClearanceRadius = 1.25f;

    private static readonly Color[] GroundFlowerPalette =
    {
        new Color(0.74f, 0.07f, 0.13f), // deep carmine
        new Color(0.82f, 0.25f, 0.10f), // rust
        new Color(0.88f, 0.60f, 0.18f), // amber
        new Color(0.50f, 0.27f, 0.68f), // violet
        new Color(0.27f, 0.58f, 0.63f), // oxidised teal
        new Color(0.78f, 0.43f, 0.58f), // dusty rose
        new Color(0.82f, 0.79f, 0.62f)  // old ivory
    };

    private static readonly List<Vector3> Vertices = new List<Vector3>();
    private static readonly List<Color> Colors = new List<Color>();
    private static readonly List<Vector2> UVs = new List<Vector2>();
    private static readonly List<int> Triangles = new List<int>();

    public static void Build(Transform parent)
    {
        Directory.CreateDirectory(Root + "/Meshes");
        Vertices.Clear();
        Colors.Clear();
        UVs.Clear();
        Triangles.Clear();

        // A fixed seed keeps the central growth in the same place after every rebuild.
        var random = new System.Random(20261002);
        for (int i = 0; i < GrassCount; i++)
        {
            Vector2 point = i < 880
                ? DiscPoint(random, 9.4f, BoardClearanceRadius)
                : AnnulusPoint(random, 9.4f, 16.5f);
            AddGrass(random, point);
        }
        for (int i = 0; i < FlowerCount; i++)
        {
            Vector2 point = i < 178
                ? DiscPoint(random, FlowerCircleRadius, BoardClearanceRadius)
                : AnnulusPoint(random, FlowerCircleRadius, 14f);
            AddFlower(random, point);
        }
        for (int i = 0; i < GroundFlowerClusters; i++)
        {
            Vector2 centre = i < 610
                ? DiscPoint(random, FlowerCircleRadius, BoardClearanceRadius + 0.4f)
                : AnnulusPoint(random, FlowerCircleRadius, 15f);
            Color clusterColor = GroundFlowerPalette[random.Next(GroundFlowerPalette.Length)];
            int blossoms = random.Next(3, 6);
            for (int j = 0; j < blossoms; j++)
            {
                float angle = Next(random, 0f, Mathf.PI * 2f);
                float offset = Next(random, 0f, 0.38f);
                Vector2 point = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * offset;
                AddGroundFlower(random, point, clusterColor);
            }
        }

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        bool newMesh = mesh == null;
        if (newMesh) mesh = new Mesh { name = "Central grass and layered flowers" };
        mesh.Clear();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(Vertices);
        mesh.SetColors(Colors);
        mesh.SetUVs(0, UVs);
        mesh.SetTriangles(Triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (newMesh) AssetDatabase.CreateAsset(mesh, MeshPath);
        else EditorUtility.SetDirty(mesh);

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/UncannyFlora.shader");
        if (shader == null) throw new InvalidOperationException("UncannyFlora shader has not imported.");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "Uncanny Flora" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.shader = shader;
        material.SetColor("_GlowColor", new Color(0.65f, 0.75f, 1f));
        material.SetFloat("_GlowStrength", 0.28f);
        material.SetFloat("_WindStrength", 0.045f);
        EditorUtility.SetDirty(material);

        Transform existing = parent.Find("Central growth | dense to sparse");
        GameObject flora = existing != null ? existing.gameObject : new GameObject("Central growth | dense to sparse");
        flora.transform.SetParent(parent);
        MeshFilter filter = flora.GetComponent<MeshFilter>();
        if (filter == null) filter = flora.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = flora.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = flora.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        // Thin plants remain passable during first-person exploration.
    }

    private static Vector2 DiscPoint(System.Random random, float radius, float clearRadius)
    {
        float angle = Next(random, 0f, Mathf.PI * 2f);
        float distance = Mathf.Sqrt(Mathf.Lerp(clearRadius * clearRadius, radius * radius,
            Next(random, 0f, 1f)));
        return new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
    }

    private static Vector2 AnnulusPoint(System.Random random, float innerRadius, float outerRadius)
    {
        float angle = Next(random, 0f, Mathf.PI * 2f);
        float distance = Mathf.Sqrt(Mathf.Lerp(innerRadius * innerRadius,
            outerRadius * outerRadius, Next(random, 0f, 1f)));
        return new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
    }

    private static void AddGrass(System.Random random, Vector2 point)
    {
        int blades = random.Next(3, 6);
        float phase = Next(random, 0f, 6.28f);
        float height = Next(random, 0.45f, 1.25f);
        Color dark = Color.Lerp(new Color(0.012f, 0.035f, 0.04f, 0f),
            new Color(0.055f, 0.09f, 0.08f, 0f), Next(random, 0f, 1f));
        for (int i = 0; i < blades; i++)
        {
            float angle = i * Mathf.PI * 2f / blades + Next(random, -0.24f, 0.24f);
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 side = new Vector3(-direction.z, 0, direction.x);
            Vector3 basePos = new Vector3(point.x, 0.035f, point.y);
            float bladeHeight = height * Next(random, 0.68f, 1.15f);
            float width = Next(random, 0.035f, 0.07f);
            Vector3 mid = basePos + direction * bladeHeight * 0.24f + Vector3.up * bladeHeight * 0.58f;
            Vector3 tip = basePos + direction * bladeHeight * 0.54f + Vector3.up * bladeHeight;
            Color tipColor = Color.Lerp(dark, new Color(0.15f, 0.17f, 0.15f, 0f), 0.18f);
            AddTriangle(basePos - side * width, basePos + side * width, mid + side * width * 0.32f,
                dark, dark, tipColor, phase, 0f, 0f, 0.58f);
            AddTriangle(basePos - side * width, mid + side * width * 0.32f, mid - side * width * 0.32f,
                dark, tipColor, tipColor, phase, 0f, 0.58f, 0.58f);
            AddTriangle(mid - side * width * 0.32f, mid + side * width * 0.32f, tip,
                tipColor, tipColor, tipColor, phase, 0.58f, 0.58f, 1f);
        }
    }

    private static void AddFlower(System.Random random, Vector2 point)
    {
        float phase = Next(random, 0f, 6.28f);
        float height = Next(random, 1.0f, 2.05f);
        float size = Next(random, 0.27f, 0.49f);
        Vector3 basePos = new Vector3(point.x, 0.035f, point.y);
        Vector3 head = basePos + new Vector3(Next(random, -0.14f, 0.14f), height, Next(random, -0.14f, 0.14f));
        Vector3 middle = Vector3.Lerp(basePos, head, 0.55f) + new Vector3(0.055f, 0, -0.045f);
        Color stem = new Color(0.025f, 0.055f, 0.055f, 0f);
        for (int i = 0; i < 2; i++)
        {
            Vector3 side = i == 0 ? Vector3.right : Vector3.forward;
            float width = 0.013f;
            AddQuad(basePos - side * width, basePos + side * width,
                middle + side * width * 0.7f, middle - side * width * 0.7f,
                stem, stem, phase, 0f, 0.55f);
            AddQuad(middle - side * width * 0.7f, middle + side * width * 0.7f,
                head + side * width * 0.6f, head - side * width * 0.6f,
                stem, stem, phase, 0.55f, 1f);
        }

        int petals = random.Next(5, 8);
        float rotation = Next(random, 0f, Mathf.PI * 2f);
        Color baseColor = new Color(0.67f, 0.64f, 0.69f, 0.27f);
        Color tipColor = Color.Lerp(new Color(0.62f, 0.64f, 0.72f, 0.42f),
            new Color(0.40f, 0.33f, 0.48f, 0.25f), Next(random, 0f, 1f));
        for (int i = 0; i < petals; i++)
        {
            float angle = rotation + i * Mathf.PI * 2f / petals;
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
            float length = size * Next(random, 0.85f, 1.18f);
            float lift = Next(random, -0.11f, 0.20f);
            Vector3 root = head + radial * 0.055f;
            Vector3 left = head + radial * length * 0.58f - tangent * size * 0.28f + Vector3.up * (lift * 0.3f);
            Vector3 right = head + radial * length * 0.58f + tangent * size * 0.28f + Vector3.up * (lift * 0.3f);
            Vector3 tip = head + radial * length + Vector3.up * lift;
            AddTriangle(root, left, tip, baseColor, baseColor, tipColor, phase, 1f, 1f, 1f);
            AddTriangle(root, tip, right, baseColor, tipColor, baseColor, phase, 1f, 1f, 1f);
        }

        // A dark faceted seed pod gives each bloom an eye-like centre.
        Color core = new Color(0.025f, 0.035f, 0.05f, 0f);
        Vector3 top = head + Vector3.up * 0.09f;
        Vector3 bottom = head - Vector3.up * 0.05f;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 0.25f;
            float b = (i + 1) * Mathf.PI * 0.25f;
            Vector3 p = head + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.085f;
            Vector3 q = head + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * 0.085f;
            AddTriangle(top, p, q, core, core, core, phase, 1f, 1f, 1f);
            AddTriangle(bottom, q, p, core, core, core, phase, 1f, 1f, 1f);
        }
    }

    private static void AddGroundFlower(System.Random random, Vector2 point, Color clusterColor)
    {
        float phase = Next(random, 0f, 6.28f);
        float height = Next(random, 0.16f, 0.43f);
        float radius = Next(random, 0.075f, 0.15f);
        Vector3 basePos = new Vector3(point.x, 0.036f, point.y);
        Vector3 head = basePos + new Vector3(Next(random, -0.025f, 0.025f), height,
            Next(random, -0.025f, 0.025f));
        Color stem = new Color(0.025f, 0.055f, 0.055f, 0f);
        Vector3 width = Vector3.right * 0.006f;
        AddQuad(basePos - width, basePos + width, head + width, head - width,
            stem, stem, phase, 0f, 0.5f);

        float variation = Next(random, 0.78f, 1.12f);
        Color tipColor = new Color(
            Mathf.Min(1f, clusterColor.r * variation),
            Mathf.Min(1f, clusterColor.g * variation),
            Mathf.Min(1f, clusterColor.b * variation), 0.34f);
        Color baseColor = Color.Lerp(tipColor, new Color(0.07f, 0.045f, 0.08f, 0.2f), 0.27f);
        int petals = random.Next(5, 7);
        float rotation = Next(random, 0f, Mathf.PI * 2f);
        for (int i = 0; i < petals; i++)
        {
            float angle = rotation + i * Mathf.PI * 2f / petals;
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 tangent = new Vector3(-radial.z, 0, radial.x);
            float petalRadius = radius * Next(random, 0.85f, 1.12f);
            Vector3 left = head + radial * 0.022f - tangent * petalRadius * 0.38f;
            Vector3 right = head + radial * 0.022f + tangent * petalRadius * 0.38f;
            Vector3 tip = head + radial * petalRadius + Vector3.up * Next(random, 0.015f, 0.055f);
            AddTriangle(left, right, tip, baseColor, baseColor, tipColor, phase, 0.5f, 0.5f, 0.65f);
        }

        Color core = new Color(0.035f, 0.03f, 0.045f, 0f);
        Vector3 coreTop = head + Vector3.up * 0.025f;
        AddTriangle(head + Vector3.left * 0.024f, head + Vector3.forward * 0.024f, coreTop,
            core, core, core, phase, 0.5f, 0.5f, 0.5f);
        AddTriangle(head + Vector3.right * 0.024f, head + Vector3.back * 0.024f, coreTop,
            core, core, core, phase, 0.5f, 0.5f, 0.5f);
    }

    private static void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
        Color bottom, Color top, float phase, float bottomWeight, float topWeight)
    {
        AddTriangle(a, b, c, bottom, bottom, top, phase, bottomWeight, bottomWeight, topWeight);
        AddTriangle(a, c, d, bottom, top, top, phase, bottomWeight, topWeight, topWeight);
    }

    private static void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc,
        float phase, float wa, float wb, float wc)
    {
        int start = Vertices.Count;
        Vertices.Add(a);
        Vertices.Add(b);
        Vertices.Add(c);
        Colors.Add(ca);
        Colors.Add(cb);
        Colors.Add(cc);
        UVs.Add(new Vector2(phase, wa));
        UVs.Add(new Vector2(phase, wb));
        UVs.Add(new Vector2(phase, wc));
        Triangles.Add(start);
        Triangles.Add(start + 1);
        Triangles.Add(start + 2);
    }

    private static float Next(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
}
