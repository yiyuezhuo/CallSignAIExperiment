using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mono.Cecil;

public class CachedResources
{
    public static Dictionary<string, StyleBackground> styleBackgroundMap = new();

    public static StyleBackground GetStyleBackground(string name)
    {
        if(!styleBackgroundMap.TryGetValue(name, out var styleBackground))
        {
            var sprite = Resources.Load<Sprite>(name);
            styleBackground = styleBackgroundMap[name] = new StyleBackground(sprite);
        }
        return styleBackground;
    }
}