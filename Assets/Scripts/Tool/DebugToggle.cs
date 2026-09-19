using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class DebugToggle : MonoBehaviour
{
    [Header("按下對應字母鍵會切換該 GameObject")]
    [SerializeField] private GameObject targetT;
    [SerializeField] private GameObject targetY;
    [SerializeField] private GameObject targetU;
    [SerializeField] private GameObject targetI;
    [SerializeField] private GameObject targetO;
    [SerializeField] private GameObject targetP;

    [Header("鍵盤上排數字鍵（非數字小鍵盤）")]
    [SerializeField] private GameObject target0;
    [SerializeField] private GameObject target1;
    [SerializeField] private GameObject target2;
    [SerializeField] private GameObject target3;
    [SerializeField] private GameObject target4;
    [SerializeField] private GameObject target5;
    [SerializeField] private GameObject target6;
    [SerializeField] private GameObject target7;
    [SerializeField] private GameObject target8;
    [SerializeField] private GameObject target9;

    [Header("功能鍵 F1～F9")]
    [SerializeField] private GameObject targetF1;
    [SerializeField] private GameObject targetF2;
    [SerializeField] private GameObject targetF3;
    [SerializeField] private GameObject targetF4;
    [SerializeField] private GameObject targetF5;
    [SerializeField] private GameObject targetF6;
    [SerializeField] private GameObject targetF7;
    [SerializeField] private GameObject targetF8;
    [SerializeField] private GameObject targetF9;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        ToggleOnPress(keyboard.tKey, targetT);
        ToggleOnPress(keyboard.yKey, targetY);
        ToggleOnPress(keyboard.uKey, targetU);
        ToggleOnPress(keyboard.iKey, targetI);
        ToggleOnPress(keyboard.oKey, targetO);
        ToggleOnPress(keyboard.pKey, targetP);

        ToggleOnPress(keyboard.digit0Key, target0);
        ToggleOnPress(keyboard.digit1Key, target1);
        ToggleOnPress(keyboard.digit2Key, target2);
        ToggleOnPress(keyboard.digit3Key, target3);
        ToggleOnPress(keyboard.digit4Key, target4);
        ToggleOnPress(keyboard.digit5Key, target5);
        ToggleOnPress(keyboard.digit6Key, target6);
        ToggleOnPress(keyboard.digit7Key, target7);
        ToggleOnPress(keyboard.digit8Key, target8);
        ToggleOnPress(keyboard.digit9Key, target9);

        ToggleOnPress(keyboard.f1Key, targetF1);
        ToggleOnPress(keyboard.f2Key, targetF2);
        ToggleOnPress(keyboard.f3Key, targetF3);
        ToggleOnPress(keyboard.f4Key, targetF4);
        ToggleOnPress(keyboard.f5Key, targetF5);
        ToggleOnPress(keyboard.f6Key, targetF6);
        ToggleOnPress(keyboard.f7Key, targetF7);
        ToggleOnPress(keyboard.f8Key, targetF8);
        ToggleOnPress(keyboard.f9Key, targetF9);
    }

    private static void ToggleOnPress(KeyControl key, GameObject target)
    {
        if (key.wasPressedThisFrame && target != null)
        {
            target.SetActive(!target.activeSelf);
        }
    }
}
