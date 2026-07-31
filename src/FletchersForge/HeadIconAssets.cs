using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using Jotunn.Entities;
using UnityEngine;

namespace FletchersForge;

/// Loads embedded PNG arrowhead icons (shipped in the DLL) with optional external overrides.
internal static class HeadIconAssets
{
    private const string IconFolderName = "Icons";
    private const string EmbeddedIconFolder = "EmbeddedIcons";
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, string> SourceCache = new Dictionary<string, string>();
    private static MethodInfo imageConversionLoadImage;
    private static bool loggedEmbeddedResources;

    internal static void ClearCache()
    {
        Cache.Clear();
        SourceCache.Clear();
    }

    internal static bool TryApplyCustomHeadIcon(CustomItem item)
    {
        if (item?.ItemDrop == null)
        {
            return false;
        }

        string prefabName = item.ItemPrefab != null
            ? item.ItemPrefab.name
            : item.ItemDrop.gameObject.name;
        Sprite sprite = GetOrLoad(prefabName);
        if (sprite == null)
        {
            return false;
        }

        item.ItemDrop.m_itemData.m_shared.m_icons = new[] { sprite };
        IconRigUtility.SyncIconsToObjectDb(prefabName, item.ItemDrop.m_itemData.m_shared.m_icons);
        string sourceLabel = "custom";
        if (SourceCache.TryGetValue(prefabName, out string cachedSource))
        {
            sourceLabel = cachedSource;
        }

        FletchersForgePlugin.Log?.LogInfo(
            $"Applied {sourceLabel} icon for {prefabName} ({sprite.texture.width}x{sprite.texture.height}).");
        return true;
    }

    internal static string GetIconsFolderPath()
    {
        string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return Path.Combine(pluginDir ?? Paths.PluginPath, IconFolderName);
    }

    private static Sprite GetOrLoad(string prefabName)
    {
        if (Cache.TryGetValue(prefabName, out Sprite cached))
        {
            return cached;
        }

        Sprite sprite = null;
        string source = "none";
        if (ModConfig.UseEmbeddedHeadIcons.Value)
        {
            sprite = LoadEmbeddedSprite(prefabName);
            if (sprite != null)
            {
                source = "embedded DLL";
            }
        }

        if (sprite == null && ModConfig.AllowExternalHeadIconOverrides.Value)
        {
            sprite = LoadFilesystemSprite(prefabName);
            if (sprite != null)
            {
                source = "external Icons folder";
            }
        }

        Cache[prefabName] = sprite;
        SourceCache[prefabName] = source;
        return sprite;
    }

    private static Sprite LoadEmbeddedSprite(string prefabName)
    {
        Assembly assembly = typeof(FletchersForgePlugin).Assembly;
        string suffix = $"{prefabName}.png";
        string resourceName = null;

        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                resourceName = name;
                break;
            }
        }

        if (resourceName == null)
        {
            if (!loggedEmbeddedResources)
            {
                loggedEmbeddedResources = true;
                FletchersForgePlugin.Log?.LogInfo(
                    $"Embedded icon resources: {string.Join(", ", assembly.GetManifestResourceNames())}");
            }

            return null;
        }

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                return null;
            }

            byte[] bytes = ReadAllBytes(stream);
            return CreateSpriteFromPng(bytes, prefabName);
        }
    }

    private static Sprite LoadFilesystemSprite(string prefabName)
    {
        string pngPath = Path.Combine(GetIconsFolderPath(), prefabName + ".png");
        if (!File.Exists(pngPath))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(pngPath);
            return CreateSpriteFromPng(bytes, prefabName);
        }
        catch (IOException ex)
        {
            FletchersForgePlugin.Log?.LogWarning($"Failed to load icon {pngPath}: {ex.Message}");
            return null;
        }
    }

    private static Sprite CreateSpriteFromPng(byte[] bytes, string prefabName)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!TryLoadPng(texture, bytes))
        {
            FletchersForgePlugin.Log?.LogWarning($"Could not decode PNG for {prefabName}.");
            return null;
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }

    private static bool TryLoadPng(Texture2D texture, byte[] bytes)
    {
        MethodInfo loadImage = GetImageConversionLoadImage();
        if (loadImage == null)
        {
            FletchersForgePlugin.Log?.LogWarning("ImageConversion.LoadImage is unavailable; cannot load PNG icons.");
            return false;
        }

        object[] args = loadImage.GetParameters().Length == 3
            ? new object[] { texture, bytes, false }
            : new object[] { texture, bytes };

        return (bool)loadImage.Invoke(null, args);
    }

    private static MethodInfo GetImageConversionLoadImage()
    {
        if (imageConversionLoadImage != null)
        {
            return imageConversionLoadImage;
        }

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        Type imageConversion = Type.GetType(
            "UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");

        if (imageConversion == null)
        {
            return null;
        }

        imageConversionLoadImage = imageConversion.GetMethod(
            "LoadImage",
            flags,
            null,
            new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) },
            null);

        if (imageConversionLoadImage == null)
        {
            imageConversionLoadImage = imageConversion.GetMethod(
                "LoadImage",
                flags,
                null,
                new[] { typeof(Texture2D), typeof(byte[]) },
                null);
        }

        return imageConversionLoadImage;
    }
}
