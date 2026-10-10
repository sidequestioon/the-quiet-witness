using UnityEditor;
using UnityEngine;

namespace QuietWitness.EditorTools
{
    // Every image under Assets/_Project/Art is imported as crisp pixel art:
    // Sprite, Point filter, no compression, no mipmaps, one Pixels Per Unit for all.
    // Characters get their pivot at the feet (bottom center) the first time they are imported.
    public class PixelArtImporter : AssetPostprocessor
    {
        // 640x360 view with the camera's size 5 (10 units tall) -> 36 pixels per unit.
        public const int PixelsPerUnit = 36;
        private const string ArtRoot = "Assets/_Project/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;

            var importer = (TextureImporter)assetImporter;
            bool firstImport = importer.importSettingsMissing;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            if (firstImport && assetPath.Contains("/Characters/"))
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);
        }
    }
}
