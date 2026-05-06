using UnityEngine;

public class DevNote : MonoBehaviour
{
#if UNITY_EDITOR
    // 這裡所有的內容，只會在編輯器（Editor）裡存在
    // 打包成 .exe 或 .apk 時，這些欄位會被編譯器直接刪掉

    [Header("📝 筆記內容")]
    [TextArea(5, 15)]
    public string note;

    [Header("🔗 快速索引")]
    public Object[] references;
#endif
}