using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnRate = 2.0f; // 2 giây sinh 1 con
    public float spawnAreaWidth = 8.0f;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            // Chờ theo thời gian
            yield return new WaitForSeconds(spawnRate);

            // Sinh ra ở vị trí ngẫu nhiên
            float randomX = Random.Range(-spawnAreaWidth / 2, spawnAreaWidth / 2);
            Vector3 spawnPosition = new Vector3(randomX, 6.0f, 0); // 6.0 là ở trên đỉnh

            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
    }
}