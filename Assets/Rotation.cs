using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotation : MonoBehaviour
{
    [SerializeField] private GameObject portal;
    [SerializeField] private GameObject effect;
    [SerializeField] private float vitesseRotation = 10f;

    void Update()
    {
        portal.transform.Rotate(Vector3.up * vitesseRotation * Time.deltaTime, Space.World);
        effect.transform.Rotate(Vector3.up * vitesseRotation * Time.deltaTime, Space.World);
    }
}
