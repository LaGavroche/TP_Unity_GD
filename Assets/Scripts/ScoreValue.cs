using UnityEngine;

// Valeur en points d'un objet lancé. À mettre sur le prefab de l'objet (sur le même objet que le Rigidbody,
// ou sur l'un de ses parents). La ScoreZone lit cette valeur quand l'objet entre dans la cible.
public class ScoreValue : MonoBehaviour
{
    public int points = 1;
}
