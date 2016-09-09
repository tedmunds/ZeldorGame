#define DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {

    private const int POINTS_MULTIKILL = 5;
    private const int POINTS_KILL = 1;
    private const int POINTS_COIN = 5;
    private const int POINTS_BOSS = 10;

    private struct CachedAnimInfo {
        public float speed;
        public bool bAttackFlag;
    }

    [SerializeField]
    private LayerMask attackLayers;

    [SerializeField]
    private SpinEffect spinEffect;

    [SerializeField]
    private AudioClip attackSound;

    [SerializeField]
    public Renderer meshRenderer;

    private CharacterController characterController;
    private PlayerGUI player_ui;

    private const float attackRadius = 1.0f;
    private const float attackCooldown = 0.5f;
    private const float attackBaseImobilityTime = 0.3f;

    public const float moveSpeed = 3.0f;
    public float bonusMoveSpeed = 0.0f;

    // Should the game do a freeze frame when the player gets a kill
    private const bool bFreezeFrameOnKill = true;

    /** how many hits can the player survive */
    private const int maxHits = 3;

    private float lastAttackTime;
    private int timesHit;
    private bool bIsDead;
    private bool bIsAttacking;
    
    /** Total global points */
    private int points;
    private int highScore;
    private float longestGameTime;
    private float gameStartTime;

    /** Used for doing little bounces */
    private float verticalVelocity;
    private bool bDoPhysics;

    private PlayerState playerState;

    private GameMode gameMode;
    private GameCamera gameCamera;
    private AudioSource audioSource;

    // The object that contains the model for this character
    private Transform model;
    private Animator animator;
    private CachedAnimInfo cachedAnimInfo;

    // Effector functionality: effectors modify player behaviour in some way
    List<PlayerEffector> effectors; 

    public int NumHitsLeft() { return maxHits - timesHit; }
    public int GetPoints() { return points; }
    public int GetHighScore() { return highScore; }
    public float GetLongestGameTime() { return longestGameTime; }
    public void AddPoints(int p) { points += p; }


	void Start () {
	    characterController = GetComponent<CharacterController>();
        player_ui = GetComponent<PlayerGUI>();
        audioSource = GetComponent<AudioSource>();
        bIsDead = false;
        highScore = 0;
        longestGameTime = 0.0f;

        if(transform.childCount > 0) {
            model = transform.GetChild(0);
        }
        else {
            model = transform;
        }

        animator = model.GetComponent<Animator>();
        if(animator != null) {
            cachedAnimInfo = new CachedAnimInfo();
        }

        effectors = new List<PlayerEffector>();
	}


    public void Respawn() {
        timesHit = 0;
        points = 0;
        bIsDead = false;
        gameStartTime = Time.time;

        transform.position = Vector3.zero;
        gameObject.SetActive(true);
    }

	
	void Update () {
        if(gameMode == null) {
            gameMode = GameMode.instance;
        }
        if(gameCamera == null) {
            gameCamera = GameCamera.instance;
        }

        float hMag = Input.GetAxis("Horizontal");
        float vMag = Input.GetAxis("Vertical");

        cachedAnimInfo.speed = new Vector3(hMag, 0.0f, vMag).magnitude;

        if(AttackRequested() && Time.time - lastAttackTime >= attackCooldown) {
            StartAttack();

            cachedAnimInfo.bAttackFlag = true;
        }

        // Can not move if they attacked too recently
        if(Time.time - lastAttackTime > attackBaseImobilityTime) {
            Vector3 direction = new Vector3(hMag, 0.0f, vMag).normalized;

            Move(direction, moveSpeed + bonusMoveSpeed);
        }

        if(bDoPhysics) {
            transform.position += new Vector3(0.0f, verticalVelocity * Time.deltaTime, 0.0f);

            verticalVelocity += -9.8f * Time.deltaTime;

            if(transform.position.y < transform.localScale.y / 2.0f) {
                bDoPhysics = false;
            }

            cachedAnimInfo.speed = 0.0f;
        }
        else {
            transform.position = new Vector3(transform.position.x, transform.localScale.z / 2.0f, transform.position.z);
        }

        // Decide what direction to orient the model
        if(Mathf.Abs(hMag) > 0.1f || Mathf.Abs(vMag) > 0.1f) {
            model.forward = new Vector3(hMag, 0.0f, vMag);
        }

        // Check attack hits
        if(bIsAttacking) {
            DoAttack();

            if(Time.time - lastAttackTime >= attackBaseImobilityTime) {
                bIsAttacking = false;
            }
        }

        UpdateEffectors();

        // updates the animator params to drive teh state machine
        ApplyCachedAnimInfo();

        // Always update high scores constantly
        float gameTime = Time.time - gameStartTime;
        if(gameTime > longestGameTime) {
            longestGameTime = gameTime;
        }

        if(points > highScore) {
            highScore = points;
        }
	}


    // Checks if the player is requesting an attack
    private bool AttackRequested() {
        return (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Fire1"));
    }


    private void ApplyCachedAnimInfo() {
        if(animator == null) {
            return;
        }

        animator.SetFloat("Speed", cachedAnimInfo.speed);

        if(cachedAnimInfo.bAttackFlag) {
            animator.SetTrigger("Attack");
        }

        cachedAnimInfo.bAttackFlag = false;
    }



    public void Move(Vector3 direction, float speed) {
        characterController.Move(direction.normalized * speed * Time.deltaTime);
    }


    /*
     * Handles damaging the player and all subsequent effects
     */ 
    public void ReceiveHit(EnemyController instigator) {
        timesHit += 1;

        if(timesHit > maxHits) {
            bIsDead = true;
            gameMode.PlayerDied(this);
            if(points > highScore) {
                highScore = points;
            }

            float gameTime = Time.time - gameStartTime;
            if(gameTime > longestGameTime) {
                longestGameTime = gameTime;
            }
            
            gameObject.SetActive(false);
        }
        else {
            verticalVelocity = 2.0f;
            bDoPhysics = true;
        }

        // Do camera shake
        if(gameCamera != null) {
            gameCamera.DoCameraShake(1.0f, 25.0f);
        }

        // Do a little freeze on hit to dramaticize it
        gameMode.PauseGame();
        gameMode.SetTimer(gameMode.UnPauseGame, 0.3f);

        player_ui.DoScreenFlash(0.05f);
    }


    /*
     * Starts an attack that lasts for some amount of time, performing DoAttacak each frame 
     */ 
    public void StartAttack() {
        lastAttackTime = Time.time;

        // Do camera shake
        if(gameCamera != null) {
            gameCamera.DoCameraShake(0.5f, 25.0f);
        }

        // Activate the spin effect
        if(spinEffect != null) {
            spinEffect.ActivateSpin();
        }

        PlaySound(attackSound, 0.5f, 1.0f);

        bIsAttacking = true;
    }


    /*
     * Checks for enemies around the player performing damage and handles points. Single frame attack
     */ 
    public void DoAttack() {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRadius, attackLayers);

        int numKills = 0;
        bool bKilledBoss = false;

        foreach(Collider col in hits) {
            EnemyController victim = col.GetComponent<EnemyController>();
            if(victim != null && victim.gameObject.activeSelf) {
                // Count the kill
                if(victim.HitByPlayer(this)) {
                    numKills += 1;

                    if(victim.GetType() == typeof(BossController)) {
                        bKilledBoss = true;
                    }
                }
            }
        }

        int pointsToAdd = 0;

        if(numKills == 1) {
            pointsToAdd += POINTS_KILL;
        }
        else if(numKills > 1) {
            pointsToAdd += numKills * POINTS_KILL + POINTS_MULTIKILL;
        }

        if(bKilledBoss) {
            pointsToAdd += POINTS_BOSS;
        }

        if(numKills > 0 && bFreezeFrameOnKill) {
            gameMode.PauseGame();
            gameMode.SetTimer(gameMode.UnPauseGame, 0.2f);
        }

        if(pointsToAdd > 0 && player_ui != null) {
            player_ui.AddKickerNumber(transform, pointsToAdd);
        }

        points += pointsToAdd;
    }


    /*
     * Performs the items functionality for pickups on the player. Does not handle removing the item from the world
     */ 
    public void PickUpItem(DroppedItem item) {
        switch(item.dropType) {
            case DroppedItem.EDropType.Points:
                points += POINTS_COIN;
                if(player_ui != null) {
                    player_ui.AddKickerNumber(transform, POINTS_COIN);
                }
                break;
            case DroppedItem.EDropType.Life:
                timesHit = Mathf.Max(0, timesHit - 1);
                break;
            case DroppedItem.EDropType.Speed:
                AddEffector(new Effector_Speed(), 5.0f);
                break;
            case DroppedItem.EDropType.AttackRadius:
                // TODO:
                break;
        }

        PlaySound(item.pickupSoundClip, 1.0f, 1.0f);
        gameMode.OnItemPickup(item);
    }


    /*
     * Plays a sound on the players audio source. All sounds should go through here. Can also modulate pitch randomly.
     */ 
    public void PlaySound(AudioClip clip, float minShift = 0.0f, float maxShift = 1.0f) {
        if(audioSource == null || clip == null || !gameMode.GetSoundEffectsOn()) {
            return;
        }

        audioSource.pitch = Random.Range(minShift, maxShift);
        audioSource.PlayOneShot(clip);
    }



    // Applies all the data from the new state and caches it
    public void ApplyNewState(PlayerState newState) {
        if(newState == null) {
            return;
        }

        playerState = newState;
        highScore = playerState.highScore;
        longestGameTime = playerState.longestGameTime;
    }

    // Caches teh player state for saving, updating any transient values
    public void CacheCurrentState() {
        if(playerState == null) {
            return;
        }

        playerState.highScore = highScore;
        playerState.longestGameTime = longestGameTime;
    }

    /*
     * Adds the input effector type, with the input lifetime. if timelimit is 0.0f, the effector will 
     * not be removed automatically, so it must remove itself.
     * 
     * returns true only if the effector was actually added to the list.
     */ 
    public bool AddEffector(PlayerEffector effector, float timelimit) {
        // cehck that the effector is not already added
        foreach(PlayerEffector currentEffector in effectors) {
            if(currentEffector.GetType() == effector.GetType()) {
                return false;
            }
        }

        effector.lifeTime = timelimit;

        effectors.Add(effector);
        effector.OnApplied(this);
        return true;
    }

    /*
     * Removes the input effector, returns whether it was removed. If it returns false it 
     * means that the input effector was not in the players list
     */ 
    public bool RemoveEffector(PlayerEffector effector) {
        bool removed = effectors.Remove(effector);
        if(removed) {
            effector.OnRemoved(this);
        }
        
        return removed;
    }

    private void UpdateEffectors() {
        // track the indexes of any effectors that go inactive this frame
        int[] markedInactive = new int[effectors.Count];
        int numInactive = 0;

        for(int i = 0; i < effectors.Count; i++) {
            effectors[i].OnUpdate(this);

            if(!effectors[i].bActive) {
                markedInactive[numInactive] = i;
                numInactive += 1;
            }
        }

        // remove any inactive effectors
        for(int i = 0; i < numInactive; i++) {
            RemoveEffector(effectors[markedInactive[i]]);
        }
    }
}
