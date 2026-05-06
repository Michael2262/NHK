using UnityEngine;
public class ReceiverTest : MonoBehaviour
{
    public void ReceiveID(string id)
    {
        Debug.Log($"ReceiverTest Received: '{id}'");
    }
}