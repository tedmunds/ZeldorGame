using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public delegate void TimedFunction();

public class GameMode : MonoBehaviour {

    private struct TimedEvent {
        public TimedFunction function;
        public float addedTime;
        public float delay;
    }

    [System.Serializable]
    public struct DifficultyLevel {
        public float spawnRate;
        public int maxEnemies;
        public int minEnemies;
        public int pointsThreshold;
        public float speedModifier;
        public bool bIsBossLevel;
    }

    // Game mode is a singleton
    public static GameMode instance;

    [SerializeField] // Normal enemy types
    public List<EnemyController> enemyTypes;

    [SerializeField] // Boss enemy types
    public List<EnemyController> bossTypes;

    [SerializeField]
    public List<DroppedItem> itemDrops;
	
    [SerializeField]
    public GameObject field;

    [SerializeField]
    public DifficultyLevel[] difficultyLevels;

    private const float playerRespawnDelay = 1.0f;

    // Spawn constants
    private const float minSpawnInterval = 3.0f;
    private const float maxSpawnInterval = 10.0f;

    // Item drop constants
    private const float itemDropProb = 0.1f;

    // Index into difficulty levels
    private int currentLevel;

    private bool bIsPaused;
    private bool bPauseSpawning;

    private float lastSpawnTime;

    private List<EnemyController> spawnedEnemies;

    private ObjectPool objectPool;

    /** Really simple timer system */
    private List<TimedEvent> timerList;

    private PlayerController player;
    private PlayerState playerState;
    private AudioSource audioSource;
    private bool bSoundEffectsOn = true;

    private const string SAVE_NAME = "SavedGame";

    public bool GetPaused() { return bIsPaused; }
    public void PauseGame() { bIsPaused = true; }
    public void UnPauseGame() { bIsPaused = false; }
    public bool GetSoundEffectsOn() { return bSoundEffectsOn; }


	void Start () {
        instance = this;

        audioSource = GetComponent<AudioSource>();

        spawnedEnemies = new List<EnemyController>(15);
        objectPool = new ObjectPool();
        timerList = new List<TimedEvent>();

        player = FindObjectOfType<PlayerController>();
        if(player == null) {
            Debug.Log("ERROR! GameMode couldn't find player on initialization!");
        }

        playerState = PlayerState.LoadPlayerState(SAVE_NAME);
        player.ApplyNewState(playerState);

        bPauseSpawning = false;
	}
	
	
	void Update () {
	    
        float timeSinceSpawn = Time.time - lastSpawnTime;
        float randomSpawnFactor = Random.value;

        // Check if it is in intra stages, and the player has finished killing the enemies
        if(bPauseSpawning && spawnedEnemies.Count == 0) {

        }

        if(spawnedEnemies.Count < difficultyLevels[currentLevel].maxEnemies && !bPauseSpawning) {
            // Mandatory spawn case
            if(timeSinceSpawn > maxSpawnInterval || spawnedEnemies.Count < difficultyLevels[currentLevel].minEnemies) {
                SpawnEnemy();
            }
            else if(difficultyLevels[currentLevel].spawnRate * Time.deltaTime > randomSpawnFactor) { // random chance of spawn happening at any given time
                SpawnEnemy();
            }
        }

        // Check for level update
        if(player.GetPoints() > difficultyLevels[currentLevel].pointsThreshold && !bPauseSpawning) {
            // Increment level, if its the end of the level set, go to the next set
            currentLevel += 1;
            if(currentLevel >= difficultyLevels.Length) {
                EndOfLevelSet();
            }
            else {
                UpdateNewDifficultyLevel();
            }
        }

        // Update timed functions
        for(int i = timerList.Count - 1; i >= 0; i--) {
            if(Time.time - timerList[i].addedTime >= timerList[i].delay) {
                timerList[i].function();

                timerList.RemoveAt(i);
            }
        }
	}


    void OnApplicationQuit() {
        player.CacheCurrentState();
        PlayerState.SavePlayerState(SAVE_NAME, playerState);
    }


    private void EndOfLevelSet() {
        currentLevel = 0;
        bPauseSpawning = true;
        Debug.Log(" End Of Level ");
    }


    private void UpdateNewDifficultyLevel() {
        // Updates all the enemies speed and spawn a boss if its required
        foreach(EnemyController enemy in spawnedEnemies) {
            enemy.SetSpeedModifier(difficultyLevels[currentLevel].speedModifier);
        }

        if(difficultyLevels[currentLevel].bIsBossLevel) {
            SpawnEnemy(true);
        }
    }

    public void SpawnEnemy(bool bSpawnBoss = false) {
        if(enemyTypes.Count == 0) {
            return;
        }

        // Min distance away from the player to spawn
        const float exclusionRadius = 1.0f;

        Vector3 spawnLoc = Vector3.zero;

        float xWidth = field.transform.localScale.x / 2.0f - 1.0f;
        float zWidth = field.transform.localScale.z / 2.0f - 1.0f;

        spawnLoc.x = Random.Range(-xWidth, xWidth);
        spawnLoc.z = Random.Range(-zWidth, zWidth);

        // Check that the player is not too close to this spot
        Collider[] overlaps = Physics.OverlapSphere(spawnLoc, exclusionRadius);
        foreach(Collider overlap in overlaps) {
            if(overlap.GetComponent<PlayerController>()) {
                Vector3 awayFromPlayer = (spawnLoc - overlap.transform.position).normalized;
                spawnLoc += awayFromPlayer * exclusionRadius;
            }
        }

        // Either take a random enemy type or a boss
        EnemyController chosenType;
        if(bSpawnBoss && bossTypes.Count > 0) {
            chosenType = bossTypes[Random.Range(0, bossTypes.Count)];
        }
        else {
            chosenType = enemyTypes[Random.Range(0, enemyTypes.Count)];
        }

        GameObject spawned = objectPool.GetInactiveGameObjectInstance(chosenType.gameObject);
        spawned.transform.position = new Vector3(spawnLoc.x, spawned.transform.localScale.y / 2.0f, spawnLoc.z);
        spawned.SetActive(true);

        // Spawn Notifications
        EnemyController enemy = spawned.GetComponent<EnemyController>();
        enemy.OnSpawn();
        enemy.SetSpeedModifier(difficultyLevels[currentLevel].speedModifier);

        spawnedEnemies.Add(enemy);

        lastSpawnTime = Time.time;
    }


    // Spawns an enemy of the inptut type at the location, and returns the spwned enemy
    public EnemyController SpawnEnemyType(EnemyController enemyType, Vector3 spawnLocation) {
        GameObject spawned = objectPool.GetInactiveGameObjectInstance(enemyType.gameObject);
        spawned.transform.position = new Vector3(spawnLocation.x, spawned.transform.localScale.y / 2.0f, spawnLocation.z);
        spawned.SetActive(true);

        // Spawn Notifications
        EnemyController enemy = spawned.GetComponent<EnemyController>();
        enemy.OnSpawn();
        enemy.SetSpeedModifier(difficultyLevels[currentLevel].speedModifier);

        spawnedEnemies.Add(enemy);

        return enemy;
    }



    public void SpawnDroppedItem(EnemyController dropper, bool bForceDrop = false) {
        if(dropper == null || itemDrops.Count == 0) {
            return;
        }

        float randDropVal = Random.value;
        if(itemDropProb > randDropVal || bForceDrop) {
            DroppedItem chosenType = itemDrops[Random.Range(0, itemDrops.Count)];


            GameObject dropObj = objectPool.GetInactiveGameObjectInstance(chosenType.gameObject);
            dropObj.transform.position = new Vector3(dropper.transform.position.x, 
                                                     dropObj.transform.localScale.y,
                                                     dropper.transform.position.z);
            dropObj.SetActive(true);
        }
    }



    public void EnemyWasKilled(EnemyController victim) {
        if(spawnedEnemies.Contains(victim)) {
            spawnedEnemies.Remove(victim);

            //GameObject effect = objectPool.GetInactiveGameObjectInstance(bloodEffectPrototype);
            //effect.transform.position = victim.transform.position;
            //effect.SetActive(true);

            SpawnParticleSystem(victim.deathEffectPrototype, victim.transform.position);

            // Death sound
            if(victim.deathSoundClip != null) {
                PlaySound(victim.deathSoundClip, 0.5f, 1.0f);
            }
        }
    }


    public void SpawnParticleSystem(ParticleSystem prototype, Vector3 position) {
        if(prototype == null) {
            return;
        }

        GameObject effect = objectPool.GetInactiveGameObjectInstance(prototype.gameObject);
        effect.transform.position = position;
        effect.SetActive(true);
    }


    public void PlayerDied(PlayerController player) {
        SetTimer(player.Respawn, playerRespawnDelay);
        SetTimer(ResetGameMode, playerRespawnDelay);

        bIsPaused = true;
    }



    public void ResetGameMode() {
        currentLevel = 0;
        for(int i = spawnedEnemies.Count - 1; i >= 0; i--) {
            spawnedEnemies[i].gameObject.SetActive(false);
            spawnedEnemies.RemoveAt(i);
        }

        DroppedItem[] allItems = FindObjectsOfType<DroppedItem>();
        for(int i = 0; i < allItems.Length; i++) {
            allItems[i].gameObject.SetActive(false);
        }

        spawnedEnemies.Clear();

        bIsPaused = false;
    }



    public void SetTimer(TimedFunction function, float delay) {
        TimedEvent e = new TimedEvent();
        e.function = function;
        e.delay = delay;
        e.addedTime = Time.time;

        timerList.Add(e);
    }




    public void PlaySound(AudioClip clip, float minShift = 0.0f, float maxShift = 1.0f) {
        if(audioSource == null || clip == null || !bSoundEffectsOn) {
            return;
        }

        audioSource.pitch = Random.Range(minShift, maxShift);
        audioSource.PlayOneShot(clip);
    }

}
