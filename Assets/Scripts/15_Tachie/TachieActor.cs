using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

[System.Serializable]
public struct TachieSpriteData
{
    public string name;
    public Sprite sprite;
}

/// <summary>
/// 身體模式 (mode)：同一套 body ID 在不同模式下對應不同的圖片。
/// 例如「制服」「假日便服」兩個 mode，各自有一份對齊的身體圖片清單，
/// 讓對話腳本用同一個 body ID 也能切換不同服裝。
/// 清單第一筆 = 該 mode 的「第一順位」，找不到對應 ID 時的退路。
/// </summary>
[System.Serializable]
public class TachieBodyMode
{
    [Tooltip("模式名稱，例如 Default / Weekend，切 mode 時用這個比對")]
    public string modeName;
    [Tooltip("此模式下的身體圖片清單，第一筆為第一順位 (fallback)")]
    public List<TachieSpriteData> bodyDataList = new List<TachieSpriteData>();
}

public class TachieActor : MonoBehaviour
{
    public string characterID;
    public CanvasGroup canvasGroup;
    public Image bodyImage;

    [Header("面部圖層組件")]
    public Image eyebrowImage;
    public Image eyeImage;
    public Image mouthImage;
    public Image blushImage;
    public Image otherImage;
    public Image aboveImage;

    [Header("圖片資料表")]
    public List<TachieSpriteData> eyebrowDataList;
    public List<TachieSpriteData> eyeDataList;
    public List<TachieSpriteData> mouthDataList;
    public List<TachieSpriteData> blushDataList;
    public List<TachieSpriteData> otherDataList;
    public List<TachieSpriteData> aboveDataList;

    [Header("身體模式 (Mode) 設定")]
    [Tooltip("每個 mode 一個區塊，內含模式名稱 + 一份身體圖片清單。第一個 mode 視為預設。切 mode 只影響 body 圖層。")]
    public List<TachieBodyMode> bodyModes = new List<TachieBodyMode>();

    [Header("表情預設配置")]
    [Tooltip("可多個角色共用同一份 config")]
    public TachieExpressionConfig expressionConfig;

    [Header("縮放設定 (作用在 bodyImage 那層，臉部會跟著縮)")]
    [Tooltip("正常大小的倍率。填 0 或負數 = Awake 時自動取 bodyImage 目前的 scale 當正常值")]
    public float normalScale = 0.92f;
    [Tooltip("縮小時的倍率")]
    public float smallScale = 0.85f;

    [Header("嚇到 (上下晃動) 預設參數")]
    [Tooltip("上下位移強度 (UI 像素)")]
    public float shockStrength = 22f;
    [Tooltip("晃動次數，越大晃越密")]
    public int shockVibrato = 10;
    [Tooltip("回彈彈性 0~1，越大晃越久")]
    public float shockElasticity = 0.6f;

    // ===== 縮放 / 晃動 執行期狀態 =====
    private Tween bodyScaleTween;            // 縮放專用（與晃動互不 Kill）
    private Tween bodyShockTween;            // 上下晃動專用
    private Vector2 bodyBaseAnchoredPos;     // bodyImage 的原始位置，晃動結束/被打斷時還原

    // ===== 身體模式 (Mode) 執行期狀態 =====
    // 模式查詢表：Key = modeName, Value = 該模式的身體圖片清單
    private Dictionary<string, List<TachieSpriteData>> bodyModeDict;
    // 模式順序（第一個 = 預設 mode）
    private List<string> bodyModeOrder;

    private string currentModeName;          // 目前所在的 mode
    private string currentBodyName;          // 目前顯示中的 body ID（尚未呼叫 ChangeBody 前為 null）

    private void Awake()
    {
        BuildBodyModes();
        CacheBodyTransform();
    }

    /// <summary>
    /// 記下 bodyImage 的原始位置；normalScale 沒填時，用場上目前的 scale 當正常值。
    /// </summary>
    private void CacheBodyTransform()
    {
        if (bodyImage == null) return;

        bodyBaseAnchoredPos = bodyImage.rectTransform.anchoredPosition;

        if (normalScale <= 0f)
            normalScale = bodyImage.rectTransform.localScale.x;
    }

    /// <summary>
    /// 建立 mode 查詢表。可重複呼叫（idempotent）。
    /// </summary>
    private void BuildBodyModes()
    {
        bodyModeDict = new Dictionary<string, List<TachieSpriteData>>(System.StringComparer.OrdinalIgnoreCase);
        bodyModeOrder = new List<string>();

        if (bodyModes != null)
        {
            foreach (var mode in bodyModes)
            {
                if (mode == null || string.IsNullOrEmpty(mode.modeName)) continue;

                if (bodyModeDict.ContainsKey(mode.modeName))
                {
                    Debug.LogWarning($"[TachieActor] {characterID} 有重複的 mode 名稱: {mode.modeName}");
                    continue;
                }

                bodyModeDict.Add(mode.modeName, mode.bodyDataList ?? new List<TachieSpriteData>());
                bodyModeOrder.Add(mode.modeName);
            }
        }

        // 預設 mode = 第一個
        currentModeName = bodyModeOrder.Count > 0 ? bodyModeOrder[0] : null;
    }

    // 取得目前 mode 的身體圖片清單；找不到目前 mode 時退回第一個 mode
    private List<TachieSpriteData> GetCurrentBodyList()
    {
        if (bodyModeDict == null) BuildBodyModes();

        if (currentModeName != null && bodyModeDict.TryGetValue(currentModeName, out var list))
            return list;

        return bodyModeOrder.Count > 0 ? bodyModeDict[bodyModeOrder[0]] : null;
    }

    /// <summary>
    /// 依目前 currentModeName + currentBodyName 重新套用身體圖片（即「立刻做一次判定」）。
    /// 找不到對應 ID（或尚未指定過 body）時，退回該 mode 的第一順位圖片。
    /// 明確指定 None/空字串時則隱藏，不做 fallback。
    /// </summary>
    private void ApplyCurrentBody()
    {
        var list = GetCurrentBodyList();
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning($"[TachieActor] {characterID} 的 mode '{currentModeName}' 沒有任何身體圖片可用");
            return;
        }

        // 明確要求隱藏 → 不退第一順位
        if (!string.IsNullOrEmpty(currentBodyName) && currentBodyName.ToLower() == "none")
        {
            UpdateLayerSprite(list, bodyImage, currentBodyName);
            return;
        }

        bool found = !string.IsNullOrEmpty(currentBodyName) && list.Any(d => d.name == currentBodyName);
        if (!found)
        {
            string fallback = list[0].name;
            if (!string.IsNullOrEmpty(currentBodyName))
                Debug.Log($"[TachieActor] {characterID} 在 mode '{currentModeName}' 找不到 body '{currentBodyName}'，改用第一順位 '{fallback}'");
            currentBodyName = fallback;
        }

        UpdateLayerSprite(list, bodyImage, currentBodyName);
    }

    /// <summary>
    /// 切換身體 mode，並立刻用目前的 body ID 在新 mode 重新判定一次圖片。
    /// </summary>
    public void SetMode(string modeName)
    {
        if (bodyModeDict == null) BuildBodyModes();

        if (string.IsNullOrEmpty(modeName) || !bodyModeDict.ContainsKey(modeName))
        {
            Debug.LogWarning($"[TachieActor] {characterID} 沒有 mode '{modeName}'，維持原 mode '{currentModeName}'");
            return;
        }

        currentModeName = modeName;
        ApplyCurrentBody(); // 立刻判定
    }

    // 通用的更換圖片邏輯
    private void UpdateLayerSprite(List<TachieSpriteData> dataList, Image targetImage, string spriteName)
    {
        if (targetImage == null) return;

        // 如果傳入空字串或 "None"，則隱藏該圖層
        if (string.IsNullOrEmpty(spriteName) || spriteName.ToLower() == "none")
        {
            targetImage.sprite = null;
            targetImage.enabled = false;
            return;
        }

        var data = dataList.FirstOrDefault(d => d.name == spriteName);
        if (data.sprite != null)
        {
            targetImage.enabled = true;
            targetImage.sprite = data.sprite;
        }
        else
        {
            Debug.LogWarning($"[TachieActor] {characterID} 找不到圖片: {spriteName}");
        }
    }

    // 換身體：在目前 mode 的清單中查圖，找不到則退第一順位（None/空 則隱藏）
    public void ChangeBody(string name)
    {
        currentBodyName = name;
        ApplyCurrentBody();
    }
    public void ChangeEyebrow(string name) => UpdateLayerSprite(eyebrowDataList, eyebrowImage, name);
    public void ChangeEye(string name) => UpdateLayerSprite(eyeDataList, eyeImage, name);
    public void ChangeMouth(string name) => UpdateLayerSprite(mouthDataList, mouthImage, name);
    public void ChangeBlush(string name) => UpdateLayerSprite(blushDataList, blushImage, name);
    public void ChangeOther(string name) => UpdateLayerSprite(otherDataList, otherImage, name);
    public void ChangeAbove(string name) => UpdateLayerSprite(aboveDataList, aboveImage, name);

    // 保留舊有的 ChangeFace 作為快速設定（可選：同時更換多個部位）
    public void ChangeFace(string eyeName, string mouthName, string eyebrowName = "")
    {
        ChangeEye(eyeName);
        ChangeMouth(mouthName);
        if (!string.IsNullOrEmpty(eyebrowName)) ChangeEyebrow(eyebrowName);
    }

    /// <summary>
    /// 套用表情預設。從 expressionConfig 中查找預設名稱，
    /// 只更換預設中有填值的部位，留空的部位維持現狀。
    /// </summary>
    public bool ApplyExpression(string presetName)
    {
        if (expressionConfig == null)
        {
            Debug.LogWarning($"[TachieActor] {characterID} 沒有設定 expressionConfig");
            return false;
        }

        var preset = expressionConfig.GetPreset(presetName);
        if (preset == null)
        {
            Debug.LogWarning($"[TachieActor] {characterID} 的 config 中找不到表情預設: {presetName}");
            return false;
        }

        if (!string.IsNullOrEmpty(preset.eye)) ChangeEye(preset.eye);
        if (!string.IsNullOrEmpty(preset.mouth)) ChangeMouth(preset.mouth);
        if (!string.IsNullOrEmpty(preset.eyebrow)) ChangeEyebrow(preset.eyebrow);
        if (!string.IsNullOrEmpty(preset.blush)) ChangeBlush(preset.blush);
        if (!string.IsNullOrEmpty(preset.other)) ChangeOther(preset.other);
        if (!string.IsNullOrEmpty(preset.above)) ChangeAbove(preset.above);

        return true;
    }

    public void Fade(float targetAlpha, float duration)
    {
        canvasGroup.DOKill();
        canvasGroup.DOFade(targetAlpha, duration);
    }

    public void MoveX(float targetX, float duration)
    {
        transform.DOKill();
        transform.DOLocalMoveX(targetX, duration).SetEase(Ease.OutCubic);
    }

    public void SetFlip(bool isFlipped)
    {
        transform.localScale = new Vector3(isFlipped ? -1 : 1, 1, 1);
    }

    // ===== 縮放 (bodyImage 那層) =====

    /// <summary>
    /// 把 bodyImage 縮放到指定倍率。duration &lt;= 0 則瞬間套用。
    /// </summary>
    public void ScaleTo(float targetScale, float duration)
    {
        if (bodyImage == null)
        {
            Debug.LogWarning($"[TachieActor] {characterID} 沒有指定 bodyImage，無法縮放");
            return;
        }

        var rt = bodyImage.rectTransform;
        KillScaleTween();

        if (duration <= 0f)
        {
            rt.localScale = new Vector3(targetScale, targetScale, 1f);
            return;
        }

        bodyScaleTween = rt.DOScale(new Vector3(targetScale, targetScale, 1f), duration)
                           .SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// 切換「正常 / 縮小」兩段大小。
    /// </summary>
    public void SetSmall(bool isSmall, float duration)
    {
        ScaleTo(isSmall ? smallScale : normalScale, duration);
    }

    /// <summary>目前是不是縮小狀態（以最接近哪一段判定）。</summary>
    public bool IsSmall()
    {
        if (bodyImage == null) return false;
        float cur = bodyImage.rectTransform.localScale.x;
        return Mathf.Abs(cur - smallScale) < Mathf.Abs(cur - normalScale);
    }

    // ===== 嚇到 (上下晃動一下) =====

    /// <summary>
    /// 嚇到效果：bodyImage 上下彈動一下後回原位。
    /// 參數傳負數 = 使用 Inspector 上的預設值。
    /// </summary>
    public void Shock(float duration = 0.4f, float strength = -1f, int vibrato = -1)
    {
        if (bodyImage == null)
        {
            Debug.LogWarning($"[TachieActor] {characterID} 沒有指定 bodyImage，無法播放嚇到效果");
            return;
        }

        float str = strength < 0f ? shockStrength : strength;
        int vib = vibrato < 0 ? shockVibrato : vibrato;
        float dur = duration <= 0f ? 0.4f : duration;

        var rt = bodyImage.rectTransform;

        // 打斷上一次晃動並先歸位，避免位移累加
        StopShock();

        bodyShockTween = rt.DOPunchAnchorPos(new Vector2(0f, str), dur, vib, shockElasticity)
                           .SetEase(Ease.OutQuad)
                           .OnComplete(() => rt.anchoredPosition = bodyBaseAnchoredPos);
    }

    private void KillScaleTween()
    {
        if (bodyScaleTween != null && bodyScaleTween.IsActive())
            bodyScaleTween.Kill();
        bodyScaleTween = null;
    }

    /// <summary>停止晃動並回到原始位置。</summary>
    public void StopShock()
    {
        if (bodyImage == null) return;

        if (bodyShockTween != null && bodyShockTween.IsActive())
            bodyShockTween.Kill();
        bodyShockTween = null;

        bodyImage.rectTransform.anchoredPosition = bodyBaseAnchoredPos;
    }

    private void OnDisable()
    {
        // 物件被關掉時把 tween 收乾淨，避免下次啟用時停在中途的狀態
        KillScaleTween();
        StopShock();
    }
}