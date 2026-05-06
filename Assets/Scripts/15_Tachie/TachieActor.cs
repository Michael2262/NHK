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
    public List<TachieSpriteData> bodyDataList;
    public List<TachieSpriteData> eyebrowDataList;
    public List<TachieSpriteData> eyeDataList;
    public List<TachieSpriteData> mouthDataList;
    public List<TachieSpriteData> blushDataList;
    public List<TachieSpriteData> otherDataList;
    public List<TachieSpriteData> aboveDataList;

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

    public void ChangeBody(string name) => UpdateLayerSprite(bodyDataList, bodyImage, name);
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
}