using System.Collections.Generic;
using UnityEngine;

namespace FletchersForge;

/// Builds simple arrowhead inventory icons (standalone textures, not cropped arrows).
internal static class HeadIconGenerator
{
    private static readonly Dictionary<string, Color> ArrowTipColors = new Dictionary<string, Color>
    {
        { "ArrowFire", new Color(0.95f, 0.45f, 0.12f) },
        { "ArrowFlint", new Color(0.55f, 0.52f, 0.48f) },
        { "ArrowBronze", new Color(0.78f, 0.48f, 0.22f) },
        { "ArrowIron", new Color(0.62f, 0.66f, 0.72f) },
        { "ArrowSilver", new Color(0.82f, 0.86f, 0.92f) },
        { "ArrowObsidian", new Color(0.22f, 0.20f, 0.28f) },
        { "ArrowPoison", new Color(0.42f, 0.72f, 0.28f) },
        { "ArrowFrost", new Color(0.45f, 0.78f, 0.95f) },
        { "ArrowNeedle", new Color(0.68f, 0.70f, 0.74f) },
        { "ArrowCarapace", new Color(0.58f, 0.32f, 0.62f) },
        { "ArrowCharred", new Color(0.18f, 0.18f, 0.20f) },
    };

    internal static Sprite CreateForArrow(string sourceArrow)
    {
        if (!ArrowTipColors.TryGetValue(sourceArrow, out Color tipColor))
        {
            tipColor = new Color(0.65f, 0.65f, 0.65f);
        }

        return CreateArrowheadSprite(tipColor);
    }

    private static Sprite CreateArrowheadSprite(Color tipColor)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color edge = new Color(tipColor.r * 0.55f, tipColor.g * 0.55f, tipColor.b * 0.55f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)size;
                float ny = y / (float)size;

                // Arrowhead wedge in upper-right quadrant (matches Valheim icon angle).
                bool inTip =
                    nx >= 0.38f &&
                    ny >= 0.22f &&
                    nx <= 0.92f &&
                    ny <= 0.88f &&
                    (nx - 0.38f) >= (ny - 0.22f) * 0.55f;

                bool inEdge =
                    inTip &&
                    nx >= 0.38f &&
                    nx <= 0.50f;

                texture.SetPixel(x, y, inEdge ? edge : inTip ? tipColor : clear);
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
    }
}
