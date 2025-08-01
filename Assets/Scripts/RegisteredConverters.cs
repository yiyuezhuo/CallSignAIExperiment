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

        group = new ConverterGroup("pieceId to StyleBackground");
        group.AddConverter((ref int id) => {
            if(id == -1)
                return null;
            var piece = GameManager.Instance.gameState.pieces[id];
            return CachedResources.GetStyleBackground(PieceViewer.GetTextureName(piece));
        });
        ConverterGroups.RegisterConverterGroup(group);

        group = new ConverterGroup("pieceId to pieceName");
        group.AddConverter((ref int id) => {
            if(id == -1)
                return "";
            var piece = GameManager.Instance.gameState.pieces[id];
            return piece.name;
        });
        ConverterGroups.RegisterConverterGroup(group);

        group = new ConverterGroup("Piece to DisplayStyle");
        group.AddConverter((ref Piece p) => (StyleEnum<DisplayStyle>)(p == null ? DisplayStyle.None : DisplayStyle.Flex));
        ConverterGroups.RegisterConverterGroup(group);

        group = new ConverterGroup("Victory Status => DisplayStyle");
        group.AddConverter((ref GameState.VictoryStatus vs) => (StyleEnum<DisplayStyle>)(vs == GameState.VictoryStatus.OneSideVictory ? DisplayStyle.Flex : DisplayStyle.None));
        ConverterGroups.RegisterConverterGroup(group);

        group = new ConverterGroup("EngagmentDeclare.EngagementType => bool");
        group.AddConverter((ref EngagementDeclare.EngagementType et) => et == EngagementDeclare.EngagementType.Aircraft);
        ConverterGroups.RegisterConverterGroup(group);

        group = new ConverterGroup("bool => EngagmentDeclare.EngagementType");
        group.AddConverter((ref bool b) => b ? EngagementDeclare.EngagementType.Aircraft: EngagementDeclare.EngagementType.Carrier);
        ConverterGroups.RegisterConverterGroup(group);

        group = new ConverterGroup("EngagmentDeclare.EngagementRecord => shooter Name");
        group.AddConverter((ref EngagementDeclare.EngagementRecord r) => GetNameFromPieceId(r.shooterPieceId));
        ConverterGroups.RegisterConverterGroup(group);

        group = new ConverterGroup("EngagmentDeclare.EngagementRecord => target Name");
        group.AddConverter((ref EngagementDeclare.EngagementRecord r) => {
            if(r.type == EngagementDeclare.EngagementType.Carrier)
                return "Carrier";
            return GetNameFromPieceId(r.targetPieceId);
        });
        ConverterGroups.RegisterConverterGroup(group);

        group = new ConverterGroup("EngagementRecord => shooter image");
        group.AddConverter((ref EngagementDeclare.EngagementRecord r) => GetStyleBackgroundFromPieceId(r.shooterPieceId));
        ConverterGroups.RegisterConverterGroup(group);

        group = new ConverterGroup("EngagementRecord => target image");
        group.AddConverter((ref EngagementDeclare.EngagementRecord r) => {
            if(r.type == EngagementDeclare.EngagementType.Carrier)
                return null;
            return GetStyleBackgroundFromPieceId(r.targetPieceId);
        });
        ConverterGroups.RegisterConverterGroup(group);

        group = new ConverterGroup("GameManager.State => bool (Commitable)");
        group.AddConverter((ref GameManager.State s) => {
            return s switch
            {
                GameManager.State.SelectShooter => true,
                GameManager.State.SelectTarget => true,
                _ => false
            };
        });
        ConverterGroups.RegisterConverterGroup(group);

        Register("idx => agent", (ref int idx) => GameManager.Instance.agents[idx]);
        Register("Agent => idx", (ref AbstractAgent agent) => GameManager.Instance.agents.IndexOf(agent));

        Register("string => bool", (ref string s) => s != null && s != "");
        Register("StatusViewer => string (progress)", (ref StatusViewer statusViewer) => $"{statusViewer.currentCompleted}/{statusViewer.currentTotal}");
    }

    static void Register<TSource, TDestination>(string name, TypeConverter<TSource, TDestination> converter)
    {
        var group = new ConverterGroup(name);
        group.AddConverter(converter);
        ConverterGroups.RegisterConverterGroup(group);
    }

    static string GetNameFromPieceId(int id)
    {
        if(id == -1)
            return "";
        var piece = GameManager.Instance.gameState.pieces[id];
        return piece.name;
    }

    static StyleBackground GetStyleBackgroundFromPieceId(int id)
    {
        if(id == -1)
            return null;
        var piece = GameManager.Instance.gameState.pieces[id];
        return CachedResources.GetStyleBackground(PieceViewer.GetTextureName(piece));
    }
}