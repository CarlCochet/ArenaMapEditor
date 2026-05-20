using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class TextureAtlasGenerator
{
    private const int AtlasSize = 4096;

    public struct AtlasEntry
    {
        public int AtlasIndex;
        public float UMin, VMin, UMax, VMax;
        public int Width, Height;
    }

    public static (ImageTexture3D texture, Dictionary<int, AtlasEntry> entries, int totalLayers) Generate(
        Dictionary<int, TileData> validAssets)
    {
        var entries = new Dictionary<int, AtlasEntry>();
        var atlasImages = new List<Image>();
        var currentImage = Image.CreateEmpty(AtlasSize, AtlasSize, false, Image.Format.Rgba8);
        currentImage.Fill(Colors.Transparent);

        var textures = validAssets
            .Where(kv => kv.Value.Image != null && kv.Value.Image.GetWidth() > 0 && kv.Value.Image.GetHeight() > 0)
            .Select(kv => (
                gfxId: kv.Key,
                width: kv.Value.Image.GetWidth(),
                height: kv.Value.Image.GetHeight(),
                image: kv.Value.Image
            ))
            .OrderByDescending(t => t.height)
            .ThenByDescending(t => t.width)
            .ToList();

        var shelves = new List<(int y, int height, int cursorX)>();
        var atlasIndex = 0;

        bool StartNewAtlas()
        {
            atlasImages.Add(currentImage);
            atlasIndex++;
            currentImage = Image.CreateEmpty(AtlasSize, AtlasSize, false, Image.Format.Rgba8);
            currentImage.Fill(Colors.Transparent);
            shelves.Clear();
            return true;
        }

        foreach (var tex in textures)
        {
            var placed = false;

            foreach (var shelf in shelves.OrderBy(s => s.y).ToList())
            {
                var idx = shelves.IndexOf(shelf);
                if (shelf.height < tex.height)
                    continue;
                if (shelf.cursorX + tex.width > AtlasSize)
                    continue;

                currentImage.BlitRect(tex.image,
                    new Rect2I(0, 0, tex.width, tex.height),
                    new Vector2I(shelf.cursorX, shelf.y));

                entries[tex.gfxId] = new AtlasEntry
                {
                    AtlasIndex = atlasIndex,
                    UMin = (float)shelf.cursorX / AtlasSize,
                    VMin = (float)shelf.y / AtlasSize,
                    UMax = (float)(shelf.cursorX + tex.width) / AtlasSize,
                    VMax = (float)(shelf.y + tex.height) / AtlasSize,
                    Width = tex.width,
                    Height = tex.height
                };

                shelves[idx] = (shelf.y, shelf.height, shelf.cursorX + tex.width);
                placed = true;
                break;
            }

            if (placed)
                continue;

            var currentY = shelves.Count > 0 ? shelves.Max(s => s.y + s.height) : 0;
            if (currentY + tex.height > AtlasSize)
            {
                StartNewAtlas();
                currentY = 0;
            }

            currentImage.BlitRect(tex.image,
                new Rect2I(0, 0, tex.width, tex.height),
                new Vector2I(0, currentY));

            entries[tex.gfxId] = new AtlasEntry
            {
                AtlasIndex = atlasIndex,
                UMin = 0f,
                VMin = (float)currentY / AtlasSize,
                UMax = (float)tex.width / AtlasSize,
                VMax = (float)(currentY + tex.height) / AtlasSize,
                Width = tex.width,
                Height = tex.height
            };

            shelves.Add((currentY, tex.height, tex.width));
        }

        atlasImages.Add(currentImage);

        var imagesArray = new Godot.Collections.Array<Image>();
        foreach (var img in atlasImages)
            imagesArray.Add(img);
        var texture3D = new ImageTexture3D();
        texture3D.Create(Image.Format.Rgba8, AtlasSize, AtlasSize, imagesArray.Count, false, imagesArray);

        return (texture3D, entries, imagesArray.Count);
    }
}
