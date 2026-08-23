using UnityEngine;

public class RotateLoop : MonoBehaviour
{
    [SerializeField] private Vector3 direction;
    private void FixedUpdate() => transform.Rotate(direction);
}
