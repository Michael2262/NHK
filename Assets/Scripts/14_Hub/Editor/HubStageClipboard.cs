using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>僅在 Editor 複製舞台劇本；不改動地點設定與環境視覺。</summary>
internal static class HubStageClipboard
{
    private const string CopyMenu = "CONTEXT/HubController/複製全部舞台劇本";
    private const string PasteMenu = "CONTEXT/HubController/貼上全部舞台劇本（覆蓋）";
    private static List<HeroineSpawnRule> heroineSnapshot;
    private static List<RiskSpawnRule> riskSnapshot;
    private static bool hasSnapshot;

    [MenuItem(CopyMenu, true)]
    private static bool CanCopy(MenuCommand command)
        => !EditorApplication.isPlayingOrWillChangePlaymode && command.context is HubController;

    [MenuItem(PasteMenu, true)]
    private static bool CanPaste(MenuCommand command) => CanCopy(command) && hasSnapshot;

    [MenuItem(CopyMenu)]
    private static void Copy(MenuCommand command)
    {
        if (!CanCopy(command)) return;
        var source = (HubController)command.context;
        int ignored = 0;
        heroineSnapshot = CloneHeroines(source.heroineRules, null, ref ignored);
        riskSnapshot = CloneRisks(source.riskRules, null, ref ignored);
        hasSnapshot = true;
        Debug.Log($"[HubController] 已複製舞台劇本：女主角 {heroineSnapshot.Count} 筆、風險角色 {riskSnapshot.Count} 筆。", source);
    }

    [MenuItem(PasteMenu)]
    private static void Paste(MenuCommand command)
    {
        if (!CanPaste(command)) return;
        var destination = (HubController)command.context;
        int cleared = 0;
        var heroines = CloneHeroines(heroineSnapshot, destination, ref cleared);
        var risks = CloneRisks(riskSnapshot, destination, ref cleared);
        Undo.RecordObject(destination, "貼上全部舞台劇本");
        destination.heroineRules = heroines;
        destination.riskRules = risks;
        PrefabUtility.RecordPrefabInstancePropertyModifications(destination);
        EditorUtility.SetDirty(destination);
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        Debug.Log($"[HubController] 已覆蓋貼上舞台劇本：女主角 {heroines.Count} 筆、風險角色 {risks.Count} 筆。可用 Undo 還原。", destination);
        if (cleared > 0)
            Debug.LogWarning($"[HubController] 有 {cleared} 個物件參照已失效或屬於其他場景，已清空；請在貼上後重新綁定。", destination);
    }

    private static List<HeroineSpawnRule> CloneHeroines(List<HeroineSpawnRule> source, HubController target, ref int cleared)
    {
        var result = new List<HeroineSpawnRule>();
        if (source == null) return result;
        foreach (var rule in source)
        {
            if (rule == null) { result.Add(null); continue; }
            result.Add(new HeroineSpawnRule
            {
                activityState = rule.activityState,
                characterObjectToShow = CopyReference(rule.characterObjectToShow, target, ref cleared),
                objectsToClose = CloneObjects(rule.objectsToClose, target, ref cleared)
            });
        }
        return result;
    }

    private static List<RiskSpawnRule> CloneRisks(List<RiskSpawnRule> source, HubController target, ref int cleared)
    {
        var result = new List<RiskSpawnRule>();
        if (source == null) return result;
        foreach (var rule in source)
        {
            if (rule == null) { result.Add(null); continue; }
            result.Add(new RiskSpawnRule
            {
                inspectionTypeID = rule.inspectionTypeID,
                riskObjectToShow = CopyReference(rule.riskObjectToShow, target, ref cleared),
                objectsToClose = CloneObjects(rule.objectsToClose, target, ref cleared)
            });
        }
        return result;
    }

    private static List<GameObject> CloneObjects(List<GameObject> source, HubController target, ref int cleared)
    {
        if (source == null) return null;
        var result = new List<GameObject>(source.Count);
        foreach (var obj in source) result.Add(CopyReference(obj, target, ref cleared));
        return result;
    }

    private static GameObject CopyReference(GameObject obj, HubController target, ref int cleared)
    {
        // 複製當下先保留參照；每次貼上才依目的端檢查，剪貼簿本身不受影響。
        if (target == null || ReferenceEquals(obj, null)) return obj;
        if (obj != null && (EditorUtility.IsPersistent(obj)
            || (!EditorUtility.IsPersistent(target) && obj.scene == target.gameObject.scene))) return obj;
        cleared++;
        return null;
    }
}
