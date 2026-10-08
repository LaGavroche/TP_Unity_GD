using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SwordSpawner : MonoBehaviour
{
    [Header("Prefab et emplacement")]
    [SerializeField] GameObject swordPrefab;
    [SerializeField] Transform spawnPoint;

    [Header("Génération")]
    [SerializeField] float spawnCooldown = 3f;
    [SerializeField] bool spawnImmediately = true;

    Coroutine spawnCoroutine;

    void OnEnable()
    {
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
    }

    IEnumerator SpawnLoop()
    {
        if (!spawnImmediately)
            yield return new WaitForSeconds(spawnCooldown);

        while (true)
        {
            if (swordPrefab != null && !IsAnObjectHeld())
                SpawnSword();

            yield return new WaitForSeconds(spawnCooldown);
        }
    }

    bool IsAnObjectHeld()
    {
        XRGrabInteractable[] interactables = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (XRGrabInteractable interactable in interactables)
        {
            if (interactable.isSelected)
                return true;
        }

        return false;
    }

    void SpawnSword()
    {
        Transform point = spawnPoint != null ? spawnPoint : transform;
        Instantiate(swordPrefab, point.position, point.rotation);
    }
}
