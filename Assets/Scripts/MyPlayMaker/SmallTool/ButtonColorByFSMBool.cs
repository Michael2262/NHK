using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HutongGames.PlayMaker;

public class ButtonColorByFSMBool : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("PlayMaker 設定")]
     [UnityEngine.Tooltip("留空 = 使用 Global Variable")]
    public PlayMakerFSM targetFSM;
    public string boolVariableName = "MyBool";

    [Header("顏色設定")]
    public Color normalColor = new Color(1f, 1f, 1f);           // #FFFFFF
    public Color highlightedColor = new Color(0.961f, 0.961f, 0.961f); // #F5F5F5
    public Color selectedColor = new Color(0.784f, 0.784f, 0.784f);    // #C8C8C8

    private Graphic targetGraphic;
    private FsmBool fsmBool;
    private bool isHovering = false;

    void Start()
    {
        targetGraphic = GetComponent<Button>().targetGraphic;

        if (targetFSM != null)
            fsmBool = targetFSM.FsmVariables.GetFsmBool(boolVariableName);
        else
            fsmBool = FsmVariables.GlobalVariables.GetFsmBool(boolVariableName);
    }

    void Update()
    {
        if (fsmBool == null || targetGraphic == null) return;

        if (fsmBool.Value)
        {
            targetGraphic.color = selectedColor;
        }
        else
        {
            targetGraphic.color = isHovering ? highlightedColor : normalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }
}