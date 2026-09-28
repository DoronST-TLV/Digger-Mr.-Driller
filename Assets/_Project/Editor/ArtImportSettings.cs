using UnityEditor;

namespace Strata.EditorTools
{
    /// <summary>Import rules for our own sprites: single sprite, 120 px per unit (1 block = 1 unit), no compression, no mipmaps.</summary>
    public class ArtImportSettings : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/_Project/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = assetPath.EndsWith("background.png") ? 30 : 120;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
