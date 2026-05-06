using UnityEngine;
using UnityEngine.InputSystem;

public class DebugToggle : MonoBehaviour
{
    [Header("按下對應按鍵會 Toggle 該 GameObject")]

    [SerializeField] private GameObject targetT;
    [SerializeField] private GameObject targetY;
    [SerializeField] private GameObject targetU;
    [SerializeField] private GameObject targetI;
    [SerializeField] private GameObject targetO;
    [SerializeField] private GameObject targetP;

    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame && targetT != null)
        {
            targetT.SetActive(!targetT.activeSelf);
        }

        if (Keyboard.current.yKey.wasPressedThisFrame && targetY != null)
        {
            targetY.SetActive(!targetY.activeSelf);
        }

        if (Keyboard.current.uKey.wasPressedThisFrame && targetU != null)
        {
            targetU.SetActive(!targetU.activeSelf);
        }

        if (Keyboard.current.iKey.wasPressedThisFrame && targetI != null)
        {
            targetI.SetActive(!targetI.activeSelf);
        }

        if (Keyboard.current.oKey.wasPressedThisFrame && targetO != null)
        {
            targetO.SetActive(!targetO.activeSelf);
        }

        if (Keyboard.current.pKey.wasPressedThisFrame && targetP != null)
        {
            targetP.SetActive(!targetP.activeSelf);
        }
    }
}