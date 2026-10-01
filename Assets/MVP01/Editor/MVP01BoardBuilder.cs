using System;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class MVP01BoardBuilder
{
    private const string Root = "Assets/MVP01";
    private const string CanvasTexturePath = Root + "/Textures/BoardCanvas.asset";

    public static void Build(Transform parent)
    {
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Textures");
        Material canvas = MakeMaterial("Painting_Board_Canvas", Color.white, 0f, 0.18f);
        canvas.SetTexture("_BaseMap", MakeCanvasTexture());
        EditorUtility.SetDirty(canvas);
        Material frame = MakeMaterial("Painting_Board_Blackened_Metal",
            new Color(0.12f, 0.15f, 0.16f), 0.88f, 0.44f);

        Transform previous = parent.Find("Central painting board");
        if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
        GameObject board = new GameObject("Central painting board");
        board.transform.SetParent(parent, false);

        // The front faces the player's approach along +Z. A metal easel holds a rough canvas.
        Cube("Canvas panel", new Vector3(0, 1.90f, -0.14f), new Vector3(2.05f, 2.18f, 0.07f),
            canvas, board.transform);
        Cube("Left metal rim", new Vector3(-1.08f, 1.90f, -0.13f), new Vector3(0.13f, 2.52f, 0.18f),
            frame, board.transform);
        Cube("Right metal rim", new Vector3(1.08f, 1.90f, -0.13f), new Vector3(0.13f, 2.52f, 0.18f),
            frame, board.transform);
        Cube("Upper metal rim", new Vector3(0, 3.13f, -0.13f), new Vector3(2.30f, 0.13f, 0.18f),
            frame, board.transform);
        Cube("Lower metal rim", new Vector3(0, 0.67f, -0.13f), new Vector3(2.30f, 0.15f, 0.18f),
            frame, board.transform);
        Cube("Paint tray", new Vector3(0, 0.64f, -0.30f), new Vector3(2.38f, 0.08f, 0.32f),
            frame, board.transform);
        Cube("Top clamp", new Vector3(0, 3.22f, -0.25f), new Vector3(0.42f, 0.18f, 0.25f),
            frame, board.transform);

        Beam("Left easel leg", new Vector3(-0.58f, 1.35f, 0.14f),
            new Vector3(-0.98f, 0.06f, -0.57f), 0.11f, frame, board.transform);
        Beam("Right easel leg", new Vector3(0.58f, 1.35f, 0.14f),
            new Vector3(0.98f, 0.06f, -0.57f), 0.11f, frame, board.transform);
        Beam("Rear easel leg", new Vector3(0, 1.68f, 0.16f),
            new Vector3(0, 0.06f, 1.13f), 0.11f, frame, board.transform);
        Cube("Easel crossbar", new Vector3(0, 0.73f, 0.10f), new Vector3(1.75f, 0.11f, 0.13f),
            frame, board.transform);
    }

    private static Material MakeMaterial(string name, Color tint, float metallic, float smoothness)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D MakeCanvasTexture()
    {
        const int resolution = 256;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CanvasTexturePath);
        bool isNew = texture == null;
        if (isNew) texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true)
        {
            name = "Rough canvas with charcoal study"
        };
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Trilinear;
        var pixels = new Color32[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float u = (x + 0.5f) / resolution - 0.5f;
            float v = (y + 0.5f) / resolution - 0.5f;
            float grain = Mathf.PerlinNoise(x * 0.37f + 17f, y * 0.37f + 9f) - 0.5f;
            float broad = Mathf.PerlinNoise(x * 0.025f + 4f, y * 0.025f + 2f) - 0.5f;
            float angle = Mathf.Atan2(v, u);
            float radius = Mathf.Sqrt(u * u + v * v);
            float ringRadius = 0.215f + Mathf.Sin(angle * 7f) * 0.009f + broad * 0.015f;
            float ring = Mathf.Exp(-Mathf.Pow((radius - ringRadius) / 0.012f, 2f));
            ring *= 0.70f + 0.30f * Mathf.PerlinNoise(x * 0.11f, y * 0.11f);
            float paper = 0.87f + grain * 0.055f + broad * 0.035f;
            Color baseColor = new Color(paper, paper * 0.975f, paper * 0.91f);
            Color charcoal = new Color(0.09f, 0.11f, 0.13f);
            pixels[y * resolution + x] = Color.Lerp(baseColor, charcoal, ring * 0.82f);
        }
        texture.SetPixels32(pixels);
        texture.Apply(true, false);
        if (isNew) AssetDatabase.CreateAsset(texture, CanvasTexturePath);
        else EditorUtility.SetDirty(texture);
        return texture;
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 size,
        Material material, Transform parent)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = name;
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = position;
        piece.transform.localScale = size;
        piece.GetComponent<MeshRenderer>().sharedMaterial = material;
        piece.isStatic = true;
        return piece;
    }

    private static void Beam(string name, Vector3 start, Vector3 end, float width,
        Material material, Transform parent)
    {
        Vector3 direction = end - start;
        GameObject piece = Cube(name, (start + end) * 0.5f,
            new Vector3(width, direction.magnitude, width), material, parent);
        piece.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
    }
}
