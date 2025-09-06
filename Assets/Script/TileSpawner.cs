using UnityEngine;
using System.Collections.Generic;

public class TileSpawner : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] List<GameObject> tilePrefabs; // Straight/Turn/Hazard tile'ların prefab'ları
    [SerializeField] int prewarmCount = 8;
    [SerializeField] float tileLength = 5f;
    [SerializeField] float spawnAhead = 20f;
    [SerializeField] float despawnBehind = 15f;

    Queue<GameObject> pool = new Queue<GameObject>();
    LinkedList<GameObject> active = new LinkedList<GameObject>();
    float nextZ = 0f;

    void Awake()
    {
        if (tilePrefabs == null || tilePrefabs.Count == 0)
        {
            Debug.LogError("[TileSpawner] tilePrefabs list is empty! Assign at least 1 prefab in the Inspector.");
            enabled = false; // prevent Update()/Start() from running and throwing IndexOutOfRange
            return;
        }
    }

    void Start()
    {
        if (!enabled) return; // disabled in Awake due to missing prefabs

        int poolSize = Mathf.Max(1, prewarmCount + 6);
        for (int i = 0; i < poolSize; i++)
        {
            var prefab = tilePrefabs[Random.Range(0, tilePrefabs.Count)];
            var go = Instantiate(prefab, Vector3.one * 9999f, Quaternion.identity);
            go.SetActive(false);
            pool.Enqueue(go);
        }
        for (int i = 0; i < prewarmCount; i++) SpawnTile();
    }

    void Update()
    {
        if (!player) return;

        while (player.position.z + spawnAhead > nextZ)
            SpawnTile();

        while (active.Count > 0)
        {
            var first = active.First.Value;
            if (player.position.z - first.transform.position.z > despawnBehind + tileLength)
                Despawn(first);
            else break;
        }
    }

    void SpawnTile()
    {
        if (tilePrefabs == null || tilePrefabs.Count == 0)
        {
            Debug.LogError("[TileSpawner] Cannot spawn: tilePrefabs is empty.");
            return;
        }

        GameObject go;
        if (pool.Count > 0)
        {
            go = pool.Dequeue();
        }
        else
        {
            var prefab = tilePrefabs[Random.Range(0, tilePrefabs.Count)];
            go = Instantiate(prefab);
        }

        go.transform.SetPositionAndRotation(new Vector3(0, 0, nextZ), Quaternion.identity);
        go.SetActive(true);
        active.AddLast(go);
        nextZ += tileLength;
    }

    void Despawn(GameObject go)
    {
        active.Remove(go);
        go.SetActive(false);
        go.transform.position = Vector3.one * 9999f;
        pool.Enqueue(go);
    }
}