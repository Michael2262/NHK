using UnityEngine;
using UnityEngine.UI;

public class IrregularButton : MonoBehaviour
{
    void Start()
    {
        // 取得 Image 組件
        Image image = GetComponent<Image>();

        // 設定點擊門檻
        // 0.5f 代表當像素的 Alpha 值大於 0.5 時，才判定為點擊到
        if (image != null)
        {
            image.alphaHitTestMinimumThreshold = 0.5f;
        }
    }
}