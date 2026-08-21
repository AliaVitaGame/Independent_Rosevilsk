using UnityEngine;

public static class MonoBehaviourExtensions
{
    public static void SetActive(this MonoBehaviour monoBehaviour, bool isActive)
    {
        monoBehaviour.gameObject.SetActive(isActive);
    }
}
