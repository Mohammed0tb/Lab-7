using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ProductShowcaseRemixBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string MaterialsPath = "Assets/Materials";
    private const string TexturePath = "Assets/Materials/Luxe_Museum_Terrazzo_Base.png";
    private const string NormalPath = "Assets/Materials/Luxe_Grooved_Plinth_Normal.png";
    private const string WoodTexturePath = "Assets/Materials/Luxe_Walnut_Showroom_Wall_Base.png";
    private const string WoodNormalPath = "Assets/Materials/Luxe_Walnut_Showroom_Wall_Normal.png";

    [MenuItem("Tools/Build Product Showcase Remix")]
    public static void Build()
    {
        Directory.CreateDirectory(MaterialsPath);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var materials = CreateMaterials();

        var room = new GameObject("Room - enclosed walnut customer car showroom");
        var plinths = new GameObject("Plinths");
        var products = new GameObject("Products");
        var lights = new GameObject("Lights");
        var probes = new GameObject("Probes");

        CreateRoom(room.transform, materials);
        var productSpots = CreatePlatforms(plinths.transform, materials);
        CreateProducts(products.transform, productSpots, materials);
        CreateLighting(lights.transform, productSpots);
        CreateProbes(probes.transform);
        CreateCamera();
        CreateGlobalVolume();
        CreateThemeLabel(room.transform, materials);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.012f, 0.025f, 0.05f);
        RenderSettings.ambientEquatorColor = new Color(0.025f, 0.018f, 0.045f);
        RenderSettings.ambientGroundColor = new Color(0.006f, 0.006f, 0.010f);
        RenderSettings.ambientIntensity = 0.25f;
        RenderSettings.reflectionIntensity = 1.15f;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        try
        {
            Lightmapping.Bake();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Lighting bake was skipped or failed in batch mode: " + ex.Message);
        }
    }

    private static Dictionary<string, Material> CreateMaterials()
    {
        CreateTerrazzoTexture(TexturePath);
        CreateGrooveNormal(NormalPath);
        CreateWalnutTexture(WoodTexturePath);
        CreateWalnutNormal(WoodNormalPath);

        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            lit = Shader.Find("Standard");
        }

        var floorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        var grooveNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        var woodTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(WoodTexturePath);
        var woodNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(WoodNormalPath);

        var materials = new Dictionary<string, Material>
        {
            ["matte"] = MakeLit("Luxe_Matte_Charcoal_Display_Wall", lit, new Color(0.009f, 0.012f, 0.018f), 0f, 0.12f),
            ["paleFloor"] = MakeLit("Luxe_Textured_Slate_Terrazzo", lit, new Color(0.028f, 0.036f, 0.046f), 0f, 0.46f),
            ["warmStone"] = MakeLit("Luxe_Cool_Stone_Plinth", lit, new Color(0.11f, 0.12f, 0.14f), 0f, 0.34f),
            ["walnutWall"] = MakeLit("Luxe_Walnut_Showroom_Wall", lit, new Color(0.36f, 0.18f, 0.08f), 0f, 0.48f),
            ["walnutTrim"] = MakeLit("Luxe_Dark_Walnut_Showroom_Trim", lit, new Color(0.17f, 0.075f, 0.032f), 0f, 0.56f),
            ["ceiling"] = MakeLit("Luxe_Warm_Showroom_Ceiling", lit, new Color(0.22f, 0.20f, 0.18f), 0f, 0.32f),
            ["shiny"] = MakeLit("Luxe_Shiny_Ruby_Clearcoat", lit, new Color(0.70f, 0.045f, 0.09f), 0f, 0.90f),
            ["metal"] = MakeLit("Luxe_Polished_Brass_Metal", lit, new Color(0.86f, 0.63f, 0.31f), 1f, 0.91f),
            ["transparent"] = MakeLit("Luxe_Clear_Cyan_Glass", lit, new Color(0.58f, 0.90f, 1f, 0.38f), 0f, 0.82f),
            ["normal"] = MakeLit("Luxe_Grooved_Graphite_Edge", lit, new Color(0.15f, 0.16f, 0.18f), 0f, 0.46f),
            ["cyanEmission"] = MakeLit("Luxe_Cyan_Neon_Accent", lit, new Color(0.02f, 0.045f, 0.05f), 0f, 0.42f),
            ["amberEmission"] = MakeLit("Luxe_Amber_Neon_Accent", lit, new Color(0.06f, 0.035f, 0.012f), 0f, 0.38f),
            ["redEmission"] = MakeLit("Luxe_Red_Hero_Light_Pool", lit, new Color(0.07f, 0.006f, 0.008f), 0f, 0.50f),
            ["magentaEmission"] = MakeLit("Luxe_Magenta_Light_Pool", lit, new Color(0.045f, 0.008f, 0.055f), 0f, 0.45f),
            ["coolEmission"] = MakeLit("Luxe_Cool_White_Light_Pool", lit, new Color(0.035f, 0.045f, 0.060f), 0f, 0.45f)
        };

        SetTexture(materials["paleFloor"], "_BaseMap", floorTexture, new Vector2(8f, 8f));
        SetTexture(materials["paleFloor"], "_MainTex", floorTexture, new Vector2(8f, 8f));

        SetTexture(materials["walnutWall"], "_BaseMap", woodTexture, new Vector2(7f, 2f));
        SetTexture(materials["walnutWall"], "_MainTex", woodTexture, new Vector2(7f, 2f));
        SetTexture(materials["walnutWall"], "_BumpMap", woodNormal, new Vector2(7f, 2f));
        materials["walnutWall"].SetFloat("_BumpScale", 0.52f);
        materials["walnutWall"].EnableKeyword("_NORMALMAP");

        SetTexture(materials["walnutTrim"], "_BaseMap", woodTexture, new Vector2(3f, 1f));
        SetTexture(materials["walnutTrim"], "_MainTex", woodTexture, new Vector2(3f, 1f));

        SetTexture(materials["normal"], "_BumpMap", grooveNormal, new Vector2(5f, 3f));
        materials["normal"].SetFloat("_BumpScale", 0.82f);
        materials["normal"].EnableKeyword("_NORMALMAP");

        materials["transparent"].SetFloat("_Surface", 1f);
        materials["transparent"].SetFloat("_Blend", 0f);
        materials["transparent"].SetFloat("_ZWrite", 0f);
        materials["transparent"].SetOverrideTag("RenderType", "Transparent");
        materials["transparent"].renderQueue = (int)RenderQueue.Transparent;

        materials["cyanEmission"].EnableKeyword("_EMISSION");
        materials["cyanEmission"].SetColor("_EmissionColor", new Color(0.08f, 2.8f, 2.65f, 1f));

        materials["amberEmission"].EnableKeyword("_EMISSION");
        materials["amberEmission"].SetColor("_EmissionColor", new Color(3.0f, 1.38f, 0.36f, 1f));

        materials["redEmission"].EnableKeyword("_EMISSION");
        materials["redEmission"].SetColor("_EmissionColor", new Color(3.2f, 0.42f, 0.28f, 1f));

        materials["magentaEmission"].EnableKeyword("_EMISSION");
        materials["magentaEmission"].SetColor("_EmissionColor", new Color(2.0f, 0.35f, 2.8f, 1f));

        materials["coolEmission"].EnableKeyword("_EMISSION");
        materials["coolEmission"].SetColor("_EmissionColor", new Color(1.25f, 1.75f, 2.6f, 1f));

        var shaderGraphMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Shader Graphs_MovingTexture.mat");
        materials["shaderGraph"] = shaderGraphMaterial != null ? shaderGraphMaterial : materials["cyanEmission"];

        return materials;
    }

    private static Material MakeLit(string name, Shader shader, Color color, float metallic, float smoothness)
    {
        var assetPath = $"{MaterialsPath}/{name}.mat";
        AssetDatabase.DeleteAsset(assetPath);
        var material = new Material(shader)
        {
            name = name
        };

        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(material, assetPath);
        return material;
    }

    private static void SetTexture(Material material, string propertyName, Texture texture, Vector2 tiling)
    {
        if (texture != null && material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, texture);
            material.SetTextureScale(propertyName, tiling);
        }
    }

    private static void CreateTerrazzoTexture(string path)
    {
        var texture = new Texture2D(256, 256, TextureFormat.RGBA32, true);
        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                var grout = x % 64 < 2 || y % 64 < 2;
                var noise = Mathf.PerlinNoise(x * 0.075f, y * 0.075f);
                var fleck = Mathf.PerlinNoise(x * 0.31f + 12f, y * 0.31f + 4f) > 0.74f;
                var baseTone = grout ? 0.018f : 0.035f + noise * 0.026f;
                var color = new Color(baseTone, baseTone * 1.04f, baseTone * 1.13f, 1f);

                if (fleck)
                {
                    color = noise > 0.55f
                        ? new Color(0.10f, 0.14f, 0.18f, 1f)
                        : new Color(0.25f, 0.16f, 0.07f, 1f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
    }

    private static void CreateGrooveNormal(string path)
    {
        var texture = new Texture2D(256, 256, TextureFormat.RGBA32, true);
        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                var longGroove = Mathf.Sin(x * 0.18f) * 0.15f;
                var fineRidge = Mathf.Sin((x + y) * 0.42f) * 0.055f;
                var groove = longGroove + fineRidge;
                texture.SetPixel(x, y, new Color(0.5f + groove, 0.5f - groove * 0.58f, 1f, 1f));
            }
        }

        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }

    private static void CreateWalnutTexture(string path)
    {
        var texture = new Texture2D(512, 512, TextureFormat.RGBA32, true);
        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                var panelSeam = x % 128 < 4;
                var grain = Mathf.PerlinNoise(x * 0.030f, y * 0.012f);
                var fineGrain = Mathf.Sin((y * 0.18f) + Mathf.PerlinNoise(x * 0.018f, y * 0.018f) * 8f) * 0.035f;
                var tone = panelSeam ? 0.09f : 0.22f + grain * 0.17f + fineGrain;
                var warmBand = Mathf.PerlinNoise(x * 0.009f + 9f, y * 0.040f) * 0.08f;
                texture.SetPixel(x, y, new Color(tone + warmBand, tone * 0.48f + warmBand * 0.35f, tone * 0.19f, 1f));
            }
        }

        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
    }

    private static void CreateWalnutNormal(string path)
    {
        var texture = new Texture2D(512, 512, TextureFormat.RGBA32, true);
        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                var seam = x % 128 < 4 ? -0.26f : 0f;
                var grain = Mathf.Sin(y * 0.22f + Mathf.PerlinNoise(x * 0.025f, y * 0.025f) * 5f) * 0.035f;
                texture.SetPixel(x, y, new Color(0.5f + seam + grain, 0.5f - grain * 0.45f, 1f, 1f));
            }
        }

        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }

    private static void CreateRoom(Transform parent, Dictionary<string, Material> materials)
    {
        var ground = CreatePrimitive("Ground - dark textured showroom floor static", PrimitiveType.Cube, parent, new Vector3(0f, -0.08f, 0f), new Vector3(42f, 0.16f, 34f), materials["paleFloor"]);
        MarkStatic(ground);

        var backWall = CreatePrimitive("Back walnut wall - closed showroom static", PrimitiveType.Cube, parent, new Vector3(0f, 5.8f, 12.8f), new Vector3(42f, 11.6f, 0.30f), materials["walnutWall"]);
        MarkStatic(backWall);

        var leftWall = CreatePrimitive("Left walnut wall - closed showroom static", PrimitiveType.Cube, parent, new Vector3(-20.8f, 5.8f, -1.5f), new Vector3(0.30f, 11.6f, 28f), materials["walnutWall"]);
        MarkStatic(leftWall);

        var rightWall = CreatePrimitive("Right walnut wall - closed showroom static", PrimitiveType.Cube, parent, new Vector3(20.8f, 5.8f, -1.5f), new Vector3(0.30f, 11.6f, 28f), materials["walnutWall"]);
        MarkStatic(rightWall);

        var frontLeftWall = CreatePrimitive("Front left walnut return wall - showroom entrance static", PrimitiveType.Cube, parent, new Vector3(-15.2f, 5.8f, -15.3f), new Vector3(11.2f, 11.6f, 0.30f), materials["walnutWall"]);
        MarkStatic(frontLeftWall);

        var frontRightWall = CreatePrimitive("Front right walnut return wall - showroom entrance static", PrimitiveType.Cube, parent, new Vector3(15.2f, 5.8f, -15.3f), new Vector3(11.2f, 11.6f, 0.30f), materials["walnutWall"]);
        MarkStatic(frontRightWall);

        var frontHeader = CreatePrimitive("Front walnut entrance header static", PrimitiveType.Cube, parent, new Vector3(0f, 10.35f, -15.3f), new Vector3(42f, 2.5f, 0.30f), materials["walnutWall"]);
        MarkStatic(frontHeader);

        var ceiling = CreatePrimitive("Warm acoustic showroom ceiling static", PrimitiveType.Cube, parent, new Vector3(0f, 11.65f, -1.25f), new Vector3(42f, 0.28f, 28.4f), materials["ceiling"]);
        MarkStatic(ceiling);

        CreateWoodPanelDetails(parent, materials);

        var rearCyanLine = CreatePrimitive("Back wall cyan horizon line static", PrimitiveType.Cube, parent, new Vector3(0f, 5.6f, 12.6f), new Vector3(31f, 0.08f, 0.08f), materials["cyanEmission"]);
        MarkStatic(rearCyanLine);

        var rearAmberLine = CreatePrimitive("Back wall amber lower trim static", PrimitiveType.Cube, parent, new Vector3(0f, 1.15f, 12.57f), new Vector3(22f, 0.07f, 0.08f), materials["amberEmission"]);
        MarkStatic(rearAmberLine);

        for (var i = 0; i < 3; i++)
        {
            var guide = CreatePrimitive($"Curved path neon segment {i + 1} static", PrimitiveType.Cube, parent, new Vector3(-8.0f + i * 8.0f, 0.035f, -8.4f + Mathf.Abs(i - 1) * 0.9f), new Vector3(5.4f, 0.04f, 0.09f), i == 1 ? materials["cyanEmission"] : materials["amberEmission"]);
            guide.transform.rotation = Quaternion.Euler(0f, -16f + i * 16f, 0f);
            MarkStatic(guide);
        }
    }

    private static void CreateWoodPanelDetails(Transform parent, Dictionary<string, Material> materials)
    {
        for (var i = -4; i <= 4; i++)
        {
            var x = i * 4.2f;
            var seam = CreatePrimitive($"Back wall vertical walnut panel reveal {i + 5} static", PrimitiveType.Cube, parent, new Vector3(x, 5.8f, 12.45f), new Vector3(0.045f, 9.6f, 0.06f), materials["walnutTrim"]);
            MarkStatic(seam);
        }

        for (var i = 0; i < 6; i++)
        {
            var z = -11.6f + i * 4.1f;
            var leftSeam = CreatePrimitive($"Left wall walnut panel reveal {i + 1} static", PrimitiveType.Cube, parent, new Vector3(-20.45f, 5.8f, z), new Vector3(0.06f, 9.4f, 0.045f), materials["walnutTrim"]);
            MarkStatic(leftSeam);

            var rightSeam = CreatePrimitive($"Right wall walnut panel reveal {i + 1} static", PrimitiveType.Cube, parent, new Vector3(20.45f, 5.8f, z), new Vector3(0.06f, 9.4f, 0.045f), materials["walnutTrim"]);
            MarkStatic(rightSeam);
        }

        var baseboardBack = CreatePrimitive("Back wall dark walnut baseboard static", PrimitiveType.Cube, parent, new Vector3(0f, 0.55f, 12.42f), new Vector3(40.6f, 0.34f, 0.12f), materials["walnutTrim"]);
        MarkStatic(baseboardBack);

        var baseboardLeft = CreatePrimitive("Left wall dark walnut baseboard static", PrimitiveType.Cube, parent, new Vector3(-20.42f, 0.55f, -1.2f), new Vector3(0.12f, 0.34f, 27.2f), materials["walnutTrim"]);
        MarkStatic(baseboardLeft);

        var baseboardRight = CreatePrimitive("Right wall dark walnut baseboard static", PrimitiveType.Cube, parent, new Vector3(20.42f, 0.55f, -1.2f), new Vector3(0.12f, 0.34f, 27.2f), materials["walnutTrim"]);
        MarkStatic(baseboardRight);

        var entryTrimLeft = CreatePrimitive("Entrance left dark walnut jamb static", PrimitiveType.Cube, parent, new Vector3(-9.55f, 4.8f, -15.0f), new Vector3(0.22f, 8.4f, 0.16f), materials["walnutTrim"]);
        MarkStatic(entryTrimLeft);

        var entryTrimRight = CreatePrimitive("Entrance right dark walnut jamb static", PrimitiveType.Cube, parent, new Vector3(9.55f, 4.8f, -15.0f), new Vector3(0.22f, 8.4f, 0.16f), materials["walnutTrim"]);
        MarkStatic(entryTrimRight);
    }

    private static Vector3[] CreatePlatforms(Transform parent, Dictionary<string, Material> materials)
    {
        var centers = new[]
        {
            new Vector3(-13.0f, 0.50f, -3.4f),
            new Vector3(-6.6f, 0.58f, 3.0f),
            new Vector3(0f, 0.86f, -1.0f),
            new Vector3(6.6f, 0.58f, 3.0f),
            new Vector3(13.0f, 0.50f, -3.4f)
        };

        for (var i = 0; i < centers.Length; i++)
        {
            var isHero = i == 2;
            var baseMaterial = isHero ? materials["normal"] : (i % 2 == 0 ? materials["warmStone"] : materials["matte"]);
            var accentMaterial = i % 2 == 0 ? materials["cyanEmission"] : materials["amberEmission"];

            if (i == 0 || i == 4)
            {
                var oval = CreatePrimitive($"Dedicated oval car display platform {i + 1} static", PrimitiveType.Cylinder, parent, centers[i], new Vector3(2.25f, 0.46f, 1.35f), baseMaterial);
                oval.transform.rotation = Quaternion.Euler(0f, i == 0 ? -28f : 28f, 0f);
                MarkStatic(oval);

                var ring = CreatePrimitive($"Thin neon oval ring {i + 1} static", PrimitiveType.Cylinder, parent, centers[i] + Vector3.up * 0.49f, new Vector3(2.38f, 0.028f, 1.44f), accentMaterial);
                ring.transform.rotation = oval.transform.rotation;
                MarkStatic(ring);
                continue;
            }

            if (isHero)
            {
                var lowerStep = CreatePrimitive("Dedicated hero car display lower platform static", PrimitiveType.Cylinder, parent, centers[i] + Vector3.down * 0.25f, new Vector3(3.05f, 0.42f, 3.05f), materials["warmStone"]);
                MarkStatic(lowerStep);

                var upperStep = CreatePrimitive("Dedicated hero car grooved display platform static", PrimitiveType.Cylinder, parent, centers[i] + Vector3.up * 0.15f, new Vector3(2.25f, 0.52f, 2.25f), materials["normal"]);
                MarkStatic(upperStep);

                var diamond = CreatePrimitive("Raised hero rotated diamond neon trim static", PrimitiveType.Cube, parent, centers[i] + new Vector3(0f, 0.68f, 0f), new Vector3(3.75f, 0.045f, 3.75f), materials["cyanEmission"]);
                diamond.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                MarkStatic(diamond);
                continue;
            }

            var plinth = CreatePrimitive($"Dedicated rectangular car display platform {i + 1} static", PrimitiveType.Cube, parent, centers[i] + Vector3.down * 0.08f, new Vector3(3.6f, 0.56f, 2.5f), baseMaterial);
            plinth.transform.rotation = Quaternion.Euler(0f, i == 1 ? -12f : 12f, 0f);
            MarkStatic(plinth);

            var cap = CreatePrimitive($"Slim top slab {i + 1} static", PrimitiveType.Cube, parent, centers[i] + Vector3.up * 0.30f, new Vector3(2.9f, 0.16f, 1.95f), materials["normal"]);
            cap.transform.rotation = plinth.transform.rotation;
            MarkStatic(cap);

            var trim = CreatePrimitive($"Diamond side neon trim {i + 1} static", PrimitiveType.Cube, parent, centers[i] + new Vector3(0f, 0.43f, -1.05f), new Vector3(2.65f, 0.045f, 0.07f), accentMaterial);
            trim.transform.rotation = plinth.transform.rotation * Quaternion.Euler(0f, 45f, 0f);
            MarkStatic(trim);
        }

        CreateShowcaseLightPools(parent, centers, materials);

        return centers;
    }

    private static void CreateShowcaseLightPools(Transform parent, Vector3[] centers, Dictionary<string, Material> materials)
    {
        var poolMaterials = new[]
        {
            materials["cyanEmission"],
            materials["amberEmission"],
            materials["redEmission"],
            materials["coolEmission"],
            materials["magentaEmission"]
        };

        var poolScales = new[]
        {
            new Vector3(3.2f, 0.018f, 2.15f),
            new Vector3(3.0f, 0.018f, 2.05f),
            new Vector3(4.05f, 0.018f, 3.05f),
            new Vector3(3.0f, 0.018f, 2.05f),
            new Vector3(3.2f, 0.018f, 2.15f)
        };

        for (var i = 0; i < centers.Length; i++)
        {
            var pool = CreatePrimitive($"Colored showcase light pool {i + 1} static", PrimitiveType.Cylinder, parent, centers[i] + new Vector3(0f, -0.43f, 0f), poolScales[i], poolMaterials[i]);
            pool.transform.rotation = Quaternion.Euler(0f, new[] { -28f, -12f, 45f, 12f, 28f }[i], 0f);
            MarkStatic(pool);
        }
    }

    private static void CreateProducts(Transform parent, Vector3[] centers, Dictionary<string, Material> materials)
    {
        var prefabPaths = new[]
        {
            "Assets/Stylized Vehicles Pack Free/Prefabs/MicroBus4.prefab",
            "Assets/Stylized Vehicles Pack Free/Prefabs/Jeep2.prefab",
            "Assets/Stylized Vehicles Pack Free/Prefabs/SportCar2.prefab",
            "Assets/Stylized Vehicles Pack Free/Prefabs/Sedan1.prefab",
            "Assets/Stylized Vehicles Pack Free/Prefabs/Car2.prefab"
        };

        var primaryMaterials = new[]
        {
            materials["transparent"],
            materials["metal"],
            materials["shiny"],
            materials["shaderGraph"],
            materials["normal"]
        };

        var rotations = new[] { 132f, 154f, 180f, 206f, 228f };
        var scales = new[] { 0.76f, 0.82f, 0.92f, 0.82f, 0.80f };

        for (var i = 0; i < prefabPaths.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
            var product = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
            product.name = $"Showcase Vehicle {i + 1} - curved gallery order";
            product.transform.SetParent(parent, true);
            product.transform.localScale = Vector3.one * scales[i];
            product.transform.rotation = Quaternion.Euler(0f, rotations[i], 0f);

            AssignMaterials(product, primaryMaterials[i], i % 2 == 0 ? materials["cyanEmission"] : materials["amberEmission"], i);
            PlaceProduct(product, centers[i] + Vector3.up * (i == 2 ? 0.76f : 0.48f));
        }

        var crystal = CreatePrimitive("Transparent crystal accent object", PrimitiveType.Sphere, parent, new Vector3(16.0f, 1.45f, 2.4f), new Vector3(0.8f, 1.15f, 0.8f), materials["transparent"]);
        crystal.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

        var metalColumn = CreatePrimitive("Polished brass sample column", PrimitiveType.Cylinder, parent, new Vector3(-16.0f, 1.1f, 2.4f), new Vector3(0.48f, 1.05f, 0.48f), materials["metal"]);
        metalColumn.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    private static void AssignMaterials(GameObject product, Material primary, Material accent, int seed)
    {
        var renderers = product.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            var slots = renderer.sharedMaterials;
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = (i + seed) % 5 == 0 ? accent : primary;
            }
            renderer.sharedMaterials = slots;
        }
    }

    private static void PlaceProduct(GameObject product, Vector3 target)
    {
        var bounds = CalculateBounds(product);
        var bottomOffset = product.transform.position.y - bounds.min.y;
        product.transform.position = new Vector3(target.x, target.y + bottomOffset, target.z);
    }

    private static Bounds CalculateBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(go.transform.position, Vector3.one);
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    private static void CreateLighting(Transform parent, Vector3[] displayCenters)
    {
        var directional = new GameObject("Directional Light - cool moon wash").AddComponent<Light>();
        directional.transform.SetParent(parent, false);
        directional.transform.rotation = Quaternion.Euler(48f, -34f, 18f);
        directional.type = LightType.Directional;
        directional.color = new Color(0.42f, 0.60f, 0.88f);
        directional.intensity = 0.20f;
        directional.shadows = LightShadows.Soft;
        directional.lightmapBakeType = LightmapBakeType.Mixed;

        CreateSpotAt(parent, "Hero Key Light - cool white cyan", new Vector3(-3.8f, 7.2f, -7.6f), new Vector3(0f, 1.35f, -1f), new Color(0.68f, 0.96f, 1f), 28f, 30f, LightmapBakeType.Mixed, 17f);
        CreateSpotAt(parent, "Hero Fill Light - subtle magenta", new Vector3(5.0f, 5.9f, -5.0f), new Vector3(0f, 1.15f, -1f), new Color(1f, 0.30f, 0.72f), 10f, 38f, LightmapBakeType.Baked, 16f);
        CreateSpotAt(parent, "Hero Rim Light - warm amber", new Vector3(0f, 6.0f, 7.2f), new Vector3(0f, 1.2f, -1f), new Color(1f, 0.72f, 0.26f), 17f, 30f, LightmapBakeType.Baked, 16f);

        CreateSpotAt(parent, "Display Spotlight - left van", new Vector3(-14.0f, 5.8f, -7.6f), new Vector3(-13f, 1.0f, -3.4f), new Color(0.18f, 0.92f, 1f), 22f, 28f, LightmapBakeType.Baked, 16f);
        CreateSpotAt(parent, "Display Spotlight - brass jeep", new Vector3(-7.6f, 6.1f, -3.8f), new Vector3(-6.6f, 1.2f, 3.0f), new Color(1f, 0.66f, 0.20f), 21f, 28f, LightmapBakeType.Baked, 16f);
        CreateSpotAt(parent, "Display Spotlight - red hero car", new Vector3(0f, 7.0f, -7.2f), new Vector3(0f, 1.35f, -1f), new Color(1f, 0.38f, 0.28f), 30f, 26f, LightmapBakeType.Mixed, 17f);
        CreateSpotAt(parent, "Display Spotlight - glass sedan", new Vector3(7.6f, 6.1f, -3.8f), new Vector3(6.6f, 1.2f, 3.0f), new Color(0.74f, 0.94f, 1f), 21f, 28f, LightmapBakeType.Baked, 16f);
        CreateSpotAt(parent, "Display Spotlight - right car", new Vector3(14.0f, 5.8f, -7.6f), new Vector3(13f, 1.0f, -3.4f), new Color(0.76f, 0.52f, 1f), 22f, 28f, LightmapBakeType.Baked, 16f);

        CreateUnderCarUplights(parent, displayCenters);

        CreateArea(parent, "Area Light - warm showroom ceiling wash", new Vector3(0f, 10.85f, -1.8f), new Vector2(16.0f, 5.0f), new Color(1f, 0.78f, 0.50f), 1.25f);
        CreateArea(parent, "Area Light - baked faint cool reflection panel", new Vector3(0f, 8.8f, -4.2f), new Vector2(10.0f, 3.6f), new Color(0.50f, 0.74f, 1f), 0.85f);
        CreateArea(parent, "Area Light - baked faint warm rear reflection", new Vector3(0f, 7.8f, 4.8f), new Vector2(13.0f, 2.2f), new Color(1f, 0.48f, 0.22f), 0.70f);
    }

    private static void CreateUnderCarUplights(Transform parent, Vector3[] displayCenters)
    {
        var colors = new[]
        {
            new Color(0.05f, 1f, 0.92f),
            new Color(1f, 0.55f, 0.14f),
            new Color(1f, 0.22f, 0.16f),
            new Color(0.72f, 0.90f, 1f),
            new Color(1f, 0.25f, 0.72f)
        };

        for (var i = 0; i < displayCenters.Length; i++)
        {
            var center = displayCenters[i];
            var intensity = i == 2 ? 6.8f : 5.2f;
            var range = i == 2 ? 5.4f : 4.7f;
            var bakeType = i == 0 || i == 2 ? LightmapBakeType.Mixed : LightmapBakeType.Baked;
            var glow = CreatePoint(parent, $"Dedicated under-car uplight {i + 1}", new Vector3(center.x, 0.80f, center.z), colors[i], intensity, range, bakeType);
            glow.shadows = LightShadows.None;

            var highlight = CreateSpot(parent, $"Soft upward underbody highlight {i + 1}", new Vector3(center.x, 0.32f, center.z), Quaternion.LookRotation(Vector3.up, Vector3.forward), colors[i], 3.6f, i == 2 ? 72f : 62f, bakeType, 4.0f);
            highlight.shadows = LightShadows.None;
        }
    }

    private static Light CreateSpot(Transform parent, string name, Vector3 position, Quaternion rotation, Color color, float intensity, float angle, LightmapBakeType bakeType, float range = 18f)
    {
        var light = new GameObject(name).AddComponent<Light>();
        light.transform.SetParent(parent, false);
        light.transform.position = position;
        light.transform.rotation = rotation;
        light.type = LightType.Spot;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.spotAngle = angle;
        light.innerSpotAngle = angle * 0.50f;
        light.shadows = LightShadows.Soft;
        light.lightmapBakeType = bakeType;
        return light;
    }

    private static void CreateSpotAt(Transform parent, string name, Vector3 position, Vector3 target, Color color, float intensity, float angle, LightmapBakeType bakeType, float range = 18f)
    {
        var rotation = Quaternion.LookRotation(target - position, Vector3.up);
        CreateSpot(parent, name, position, rotation, color, intensity, angle, bakeType, range);
    }

    private static Light CreatePoint(Transform parent, string name, Vector3 position, Color color, float intensity, float range, LightmapBakeType bakeType)
    {
        var light = new GameObject(name).AddComponent<Light>();
        light.transform.SetParent(parent, false);
        light.transform.position = position;
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
        light.lightmapBakeType = bakeType;
        return light;
    }

    private static void CreateArea(Transform parent, string name, Vector3 position, Vector2 size, Color color, float intensity)
    {
        var area = new GameObject(name).AddComponent<Light>();
        area.transform.SetParent(parent, false);
        area.transform.position = position;
        area.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        area.type = LightType.Rectangle;
        area.color = color;
        area.intensity = intensity;
        area.areaSize = size;
        area.shadows = LightShadows.Soft;
        area.lightmapBakeType = LightmapBakeType.Baked;
    }

    private static void CreateProbes(Transform parent)
    {
        var lightProbes = new GameObject("Light Probe Group - curved gallery lighting changes").AddComponent<LightProbeGroup>();
        lightProbes.transform.SetParent(parent, false);
        lightProbes.probePositions = new[]
        {
            new Vector3(-13f, 1.2f, -4f), new Vector3(-7f, 1.3f, 3f), new Vector3(0f, 1.7f, -1f), new Vector3(7f, 1.3f, 3f), new Vector3(13f, 1.2f, -4f),
            new Vector3(-13f, 3.2f, -4f), new Vector3(-7f, 3.6f, 3f), new Vector3(0f, 4.0f, -1f), new Vector3(7f, 3.6f, 3f), new Vector3(13f, 3.2f, -4f),
            new Vector3(-9f, 5.6f, 7f), new Vector3(0f, 5.8f, 8f), new Vector3(9f, 5.6f, 7f)
        };

        var reflection = new GameObject("Reflection Probe - hero brass and glass reflections").AddComponent<ReflectionProbe>();
        reflection.transform.SetParent(parent, false);
        reflection.transform.position = new Vector3(0f, 2.8f, -0.2f);
        reflection.mode = ReflectionProbeMode.Baked;
        reflection.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        reflection.boxProjection = true;
        reflection.size = new Vector3(32f, 8f, 23f);
        reflection.intensity = 1.18f;
        reflection.resolution = 128;
    }

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;

        camera.transform.position = new Vector3(0f, 10.4f, -13.6f);
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.25f, -0.7f) - camera.transform.position, Vector3.up);
        camera.fieldOfView = 68f;
        camera.clearFlags = CameraClearFlags.Skybox;
    }

    private static void CreateGlobalVolume()
    {
        var volumeObject = new GameObject("Global Volume - showroom bloom and tone");
        var volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        if (profile != null)
        {
            if (profile.TryGet<Bloom>(out var bloom))
            {
                bloom.active = true;
                bloom.threshold.Override(0.78f);
                bloom.intensity.Override(0.72f);
            }

            if (profile.TryGet<Vignette>(out var vignette))
            {
                vignette.active = true;
                vignette.intensity.Override(0.16f);
            }

            if (profile.TryGet<Tonemapping>(out var tonemapping))
            {
                tonemapping.active = true;
                tonemapping.mode.Override(TonemappingMode.ACES);
            }

            volume.sharedProfile = profile;
        }
    }

    private static void CreateThemeLabel(Transform parent, Dictionary<string, Material> materials)
    {
        var label = new GameObject("Theme label - Premium Car Selection Showroom");
        label.transform.SetParent(parent, false);
        label.transform.position = new Vector3(-14.2f, 3.05f, 12.45f);

        var text = label.AddComponent<TextMesh>();
        text.text = "Premium Car Selection Showroom";
        text.anchor = TextAnchor.MiddleLeft;
        text.characterSize = 0.30f;
        text.fontSize = 66;
        text.color = new Color(1f, 0.88f, 0.68f);

        var renderer = label.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = materials["amberEmission"];
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
        return go;
    }

    private static void MarkStatic(GameObject go)
    {
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
    }
}
