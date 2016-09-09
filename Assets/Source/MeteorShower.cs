using UnityEngine;
using System.Collections;

public class MeteorShower : MonoBehaviour {

    [SerializeField]
    public GameObject meteorPrototype;

    // Frequency of meteor spawns
    [SerializeField]
    public float frequency;

    [SerializeField]
    public float maxRandFreqOffset;

    // defines the are in which meteors will spawn
    [SerializeField]
    public Vector2 spawnDimensions;

    // flags if metoers will activly spawn, usually just always onduring lifetime
    private bool bSpawingActive;
    private float lastSpawnTime;
    private float spawnDelay;


    public void Start() {
        bSpawingActive = true;
        spawnDelay = 1.0f / frequency;
    }

    public void OnDisable() {
        bSpawingActive = false;
    }
	
	private void Update() {
        if(bSpawingActive) {
            float elapsedSinceSpawn = Time.time - lastSpawnTime;
            float randOffset = Random.Range(0.0f, maxRandFreqOffset);
            if(elapsedSinceSpawn > spawnDelay + randOffset) {
                SpawnMeteor();
            }
        }
	}


    public void SpawnMeteor() {
        const float spawnHeight = 15.0f;
        lastSpawnTime = Time.time;

        Vector3 spawnLoc = transform.position + new Vector3(Random.Range(-spawnDimensions.x, spawnDimensions.x),
                                                            spawnHeight,
                                                            Random.Range(-spawnDimensions.y, spawnDimensions.y));

        GameObject meteor = GameMode.instance.SpawnObjectFast(meteorPrototype, spawnLoc);
    }
}
