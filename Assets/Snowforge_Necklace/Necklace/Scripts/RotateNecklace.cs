using UnityEngine;

public class RotationObjet : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 10f;

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
    }
}
