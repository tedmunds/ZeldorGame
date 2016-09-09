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

    // Game mode is a singleton
    public static GameMode instance;

    #region params
    [SerializeField] // Normal enemy types
    public List<EnemyController> enemyTypes;

    [SerializeField] // Boss enemy types
    public List<EnemyController> bossTypes;

    [SerializeField]
    public List<DroppedItem> itemDrops;

    [SerializeField]
    private float itemDropProb = 0.1f;

    [SerializeField]
    public GameObject field;

    [SerializeField]
    public GameLevel[] levels;

    [SerializeField]
    private AnimationCurve levelFieldEasing;
    #endregion

    private const float playerRespawnDelay = 1.0f;

    // Spawn constants
    private const float maxSpawnInterval = 10.0f;

    // Index into difficulty levels
    private int currentLevel;
    private int levelDifficulty;

    // the playing fields game objects
    private GameField nextLevelField;
    private GameField currentLevelField;

    private bool bIsPaused;
    private bool bPauseSpawning;
    private bool bWaitingOnEnemys;

    private float lastSpawnTime;

    private List<EnemyController> spawnedEnemies;
    private List<GameObject> droppedItems;

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
    public PlayerController GetPlayer() { return player; }

	void Awake () {
        instance = this;

        audioSource = GetComponent<AudioSource>();

        spawnedEnemies = new List<EnemyController>(15);
        droppedItems = new List<GameObject>();
        objectPool = new ObjectPool();
        timerList = new List<TimedEvent>();

        player = FindObjectOfType<PlayerController>();
        if(player == null) {
            Debug.Log("ERROR! GameMode couldn't find player on initialization!");
        }

        playerState = PlayerState.LoadPlayerState(SAVE_NAME);
        player.ApplyNewState(playerState);

        bPauseSpawning = false;
        bWaitingOnEnemys = false;

        currentLevel = 0;
        levelDifficulty = 0;

        StartFirstLevel();
	}
	
	
	void Update () {
        float timeSinceSpawn = Time.time - lastSpawnTime;
        float randomSpawnFactor = Random.value;

        // Check if it is in intra stages, and the player has finished killing the enemies
        if(bPauseSpawning && spawnedEnemies.Count == 0) {

        }

        GameLevel currentGameLevel = levels[currentLevel];
        DifficultyLevel diffculty = currentGameLevel.difficultyLevels[levelDifficulty];

        if(spawnedEnemies.Count < diffculty.maxEnemies && !bPauseSpawning) {
            // Mandatory spawn case
            if(timeSinceSpawn > maxSpawnInterval || spawnedEnemies.Count < diffculty.minEnemies) {
                SpawnEnemy();
            }
            else if(diffculty.spawnRate * Time.deltaTime > randomSpawnFactor) { 
                // random chance of spawn happening at any given time
                SpawnEnemy();
            }
        }

        // Check for level update
        if(player.GetPoints() > diffculty.pointsThreshold && !bPauseSpawning) {
            // Increment difficulty level, if its the end of the level set, go to the next set
            levelDifficulty += 1;
            if(levelDifficulty >= currentGameLevel.difficultyLevels.Length) {
                levelDifficulty = currentGameLevel.difficultyLevels.Length - 1;
                EndOfLevelSet();
            }
            else {
                UpdateNewDifficultyLevel();
            }
        }

        // if waiting on enemies, go to next level when spawned are all killed
        if(bWaitingOnEnemys && spawnedEnemies.Count == 0) {
            TransitionToNextLevel();
        }

        // Update timed functions
        for(int i = timerList.Count - 1; i >= 0; i--) {
            if(Time.time - timerList[i].addedTime >= timerList[i].delay) {
                timerList[i].function();

                timerList.RemoveAt(i);
            }
        }
	}


    void StartFirstLevel() {
        nextLevelField = null;
        currentLevelField = Instantiate<GameField>(levels[currentLevel].GameFieldPrototype);
        currentLevelField.OnLevelStart(levels[currentLevel]);
    }


    void OnApplicationQuit() {
        player.CacheCurrentState();
        PlayerState.SavePlayerState(SAVE_NAME, playerState);
    }


    private void EndOfLevelSet() {
        bPauseSpawning = true;
        currentLevelField.OnLevelEnd();

        // flag that indicates the game is just waiting for enemies to be killed before transitioning levels
        bWaitingOnEnemys = true;

#if UNITY_EDITOR
        Debug.Log(" End Of Level ");
#endif
    }

    // Called to start the currently pending level, after any level transition
    void BeginNextLevel() {
        bPauseSpawning = false;
        Destroy(currentLevelField);
        currentLevelField = nextLevelField;
        currentLevelField.OnLevelStart(levels[currentLevel]);
        nextLevelField = null;
    }


    private void UpdateNewDifficultyLevel() {
        // Updates all the enemies speed and spawn a boss if its required
        foreach(EnemyController enemy in spawnedEnemies) {
            enemy.SetSpeedModifier(levels[currentLevel].difficultyLevels[levelDifficulty].speedModifier);
        }

        if(levels[currentLevel].difficultyLevels[levelDifficulty].bIsBossLevel) {
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

        float xWidth = currentLevelField.transform.localScale.x / 2.0f - 1.0f;
        float zWidth = currentLevelField.transform.localScale.z / 2.0f - 1.0f;

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
        enemy.SetSpeedModifier(levels[currentLevel].difficultyLevels[levelDifficulty].speedModifier);

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
        enemy.SetSpeedModifier(levels[currentLevel].difficultyLevels[levelDifficulty].speedModifier);

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

            // also keep track of the item so it can be removed on level transition
            droppedItems.Add(dropObj);
        }
    }

    /*
     * Called by player when they pickup an item to notify the game mode
     */ 
    public void OnItemPickup(DroppedItem item) {
        droppedItems.Remove(item.gameObject);
    }



    public void EnemyWasKilled(EnemyController victim) {
        if(spawnedEnemies.Contains(victim)) {
            spawnedEnemies.Remove(victim);

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

    public GameObject SpawnObjectFast(GameObject prototype, Vector3 position) {
        GameObject obj = objectPool.GetInactiveGameObjectInstance(prototype);
        obj.transform.position = position;
        obj.SetActive(true);
        return obj;
    }


    /// <summary>
    /// Kills all of the currenyl spawned enemies
    /// </summary>
    public void KillAllEnemies() {
        for(int i = spawnedEnemies.Count - 1; i >= 0; i--) {
            spawnedEnemies[i].Kill();
        }
    }


    public void TransitionToNextLevel() {
        Vector3 spawnLocation = new Vector3(-20.0f, -5.0f, 0.0f);
        float levelTransitionDelay = 1.0f;

        levelDifficulty = 0;
        currentLevel += 1;
        bWaitingOnEnemys = false;

        if(currentLevel >= levels.Length) {
            currentLevel = 0;
            Debug.LogWarning("Reached last level! Going back to the first one.");
        }
        else {
            Debug.Log("Going to next level!");
        }
        

        // create the new level field
        nextLevelField = Instantiate<GameField>(levels[currentLevel].GameFieldPrototype);
        nextLevelField.transform.position = spawnLocation;

        // clean up all remaining items
        for(int i = droppedItems.Count - 1; i >= 0; i--) {
            GameObject obj = droppedItems[i];
            droppedItems.RemoveAt(i);
            obj.SetActive(false);
        }

        SetTimer(SlideOutLevels, levelTransitionDelay);
    }

    // Slides in the new level and out the old level
    public void SlideOutLevels() {
        Vector3 exitLocation = new Vector3(20.0f, -5.0f, 0.0f);
        float slideTime = 1.0f;

        currentLevelField.SlideTo(exitLocation, slideTime, levelFieldEasing);
        nextLevelField.SlideTo(new Vector3(0.0f, -5.0f, 0.0f), slideTime, levelFieldEasing);

        // and finally set the level to start after the new level has slid into place
        SetTimer(BeginNextLevel, slideTime + 1.0f);
    }

}
