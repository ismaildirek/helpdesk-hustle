using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Automatically applies pixel-art friendly import settings to every
/// texture under Assets/Art (point filtering, no compression, PPU 32).
/// </summary>
public class PixelArtImporter : AssetPostprocessor
{
    private const string ArtRoot = "Assets/Art/";

    private void OnPreprocessTexture()
    {
        string normalizedPath = assetPath.Replace('\\', '/');
        if (!normalizedPath.StartsWith(ArtRoot))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = normalizedPath.EndsWith(
            "/sprites_atlas.png",
            System.StringComparison.OrdinalIgnoreCase)
                ? SpriteImportMode.Multiple
                : SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
    }

    [MenuItem("Tools/Helpdesk Hustle/Slice Generated Mini Game Atlases")]
    public static void SliceGeneratedMiniGameAtlases()
    {
        ApplySlices(
            "Assets/Art/Görev_assets/Server_Cooling/sprites_atlas.png",
            new[]
            {
                "fan_stopped", "fan_running", "fan_frosty",
                "fan_overheated", "cooling_canister",
                "snowflake_burst", "airflow_arrow", "heat_waves",
                "steel_wrench", "warning_beacon"
            },
            BuildFiveByTwoRects(
                new[] { 0f, 270f, 520f, 745f, 1030f, 1254f }));

        ApplySlices(
            "Assets/Art/Görev_assets/Security_Check/sprites_atlas.png",
            new[]
            {
                "id_green", "id_amber", "id_red", "id_damaged",
                "approve_icon", "reject_icon", "scanner_frame",
                "lock_closed", "lock_open", "alert_beacon"
            },
            new[]
            {
                new Rect(0f, 627f, 285f, 627f),
                new Rect(285f, 627f, 275f, 627f),
                new Rect(560f, 627f, 275f, 627f),
                new Rect(835f, 627f, 419f, 627f),
                new Rect(0f, 0f, 230f, 627f),
                new Rect(230f, 0f, 230f, 627f),
                new Rect(460f, 0f, 230f, 627f),
                new Rect(690f, 0f, 170f, 627f),
                new Rect(860f, 0f, 190f, 627f),
                new Rect(1050f, 0f, 204f, 627f)
            });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Rect[] BuildFiveByTwoRects(float[] boundaries)
    {
        Rect[] rects = new Rect[10];
        for (int column = 0; column < 5; column++)
        {
            float x = boundaries[column];
            float width = boundaries[column + 1] - x;
            rects[column] = new Rect(x, 627f, width, 627f);
            rects[column + 5] = new Rect(x, 0f, width, 627f);
        }

        return rects;
    }

    private static void ApplySlices(
        string assetPath,
        string[] names,
        Rect[] rects)
    {
        if (names.Length != rects.Length)
            throw new System.ArgumentException("Sprite names and rects differ.");

        TextureImporter importer = AssetImporter.GetAtPath(assetPath)
            as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException(
                $"Texture importer not found: {assetPath}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factories =
            new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider =
            factories.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
            throw new System.InvalidOperationException(
                $"Sprite data provider not found: {assetPath}");

        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] spriteRects = new SpriteRect[names.Length];
        SpriteNameFileIdPair[] namePairs =
            new SpriteNameFileIdPair[names.Length];

        for (int index = 0; index < names.Length; index++)
        {
            GUID spriteId = GUID.Generate();
            spriteRects[index] = new SpriteRect
            {
                name = names[index],
                rect = rects[index],
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = spriteId
            };
            namePairs[index] = new SpriteNameFileIdPair(
                names[index],
                spriteId);
        }

        dataProvider.SetSpriteRects(spriteRects);
        ISpriteNameFileIdDataProvider nameProvider =
            dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameProvider?.SetNameFileIdPairs(namePairs);
        dataProvider.Apply();
        AssetDatabase.ImportAsset(
            assetPath,
            ImportAssetOptions.ForceUpdate);
    }
}