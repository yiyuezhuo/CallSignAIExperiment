using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using CallSignLib;



#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RegisteredConverters
{
#if UNITY_EDITOR
    [InitializeOnLoadMethod]    
#else
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
    public static void RegisterConverters()
    {
        Debug.Log("RegisterConverters");

        // group = new ConverterGroup("SignatureType to int");
        // group.AddConverter((ref XSection.SignatureType st) => (int)st);
        // ConverterGroups.RegisterConverterGroup(group);

        // group = new ConverterGroup("int to SignatureType");
        // group.AddConverter((ref int x) => (XSection.SignatureType)x);
        // ConverterGroups.RegisterConverterGroup(group);

        var group = new ConverterGroup("Piece to StyleBackground");
        group.AddConverter((ref Piece p) => p == null ? null : CachedResources.GetStyleBackground(PieceViewer.GetTextureName(p)));
        ConverterGroups.RegisterConverterGroup(group);

        group = new ConverterGroup("Piece to DisplayStyle");
        group.AddConverter((ref Piece p) => (StyleEnum<DisplayStyle>)(p == null ? DisplayStyle.None : DisplayStyle.Flex));
        ConverterGroups.RegisterConverterGroup(group);
    }
}