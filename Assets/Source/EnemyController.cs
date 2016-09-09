using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class EnemyController : MonoBehaviour {

    /** which layers to detect for kills / hits */
    [SerializeField]
    protected LayerMask hitDetect;

    [SerializeField]
    protected bool diesOneShot = true;

    [SerializeField] // used if diesOneShot is false
    protected int maxHits = 0;

    protected CharacterController characterController;

    [SerializeField]
    protected float minDecisionInterval = 0.5f;

    [SerializeField]
    protected float maxDecisionInterval = 2.0f;

    [SerializeField]
    protected float minAttackCooldown = 1.0f;

    [SerializeField]
    protected float moveSpeed = 2.0f;

    [SerializeField]
    public ParticleSystem deathEffectPrototype;

    [SerializeField]
    public AudioClip deathSoundClip;

    /** how long does the enemy stay neutral after being spawned */
    protected const float neutralTime = 1.0f;
    protected const float hitInvulnerability = 0.5f;

    // effects constants
    protected const float neutralFlashFrequency = 60.0f;

    protected float nextDecisionInterval;
    protected float lastDecisionTime;
    protected float lastAttackTime;
    protected Vector3 previousPosition;
    protected float lastHitTime;

    protected int numHitsTaken;

    protected float speedModifier = 1.0f;

    protected Renderer baseRenderer;
    protected GameMode gameMode;

    /** time that the enemy was spawned at */
    protected float spawntime;
    protected float spawnInvulnerability = 0.0f;

    // movement directions
    protected enum EDirection {
        North, South, East, West
    }

    protected EDirection currentDirection;

    public void SetSpeedModifier(float n) { speedModifier = n; }
    public void GiveSpawnInvulnerability(float t) { spawnInvulnerability = t; }

    public virtual void OnSpawn() {
        spawntime = Time.time;
        numHitsTaken = 0;

        // defauilt is no unvulnerability, can be set after spawn
        spawnInvulnerability = 0.0f;
    }


    protected virtual void Start() {
        characterController = GetComponent<CharacterController>();
        currentDirection = EDirection.North;

        gameMode = GameMode.instance;
        if(gameMode == null) {
            Debug.Log("ERROR! "+name+" Was spawned before the game mode intialised!");
        }

        baseRenderer = GetComponent<Renderer>();
        
	}


    protected virtual void Update() {
        if(gameMode.GetPaused()) {
            return;
        }


        float sinceLastDecision = Time.time - lastDecisionTime;
        if(sinceLastDecision >= nextDecisionInterval / speedModifier) {
            MakeDirectionDecision();
        }

        Vector3 moveDir = Vector3.zero;
        moveDir = GetDirection(currentDirection);

        previousPosition = transform.position;

        Move(moveDir, moveSpeed * speedModifier);

        transform.position = new Vector3(transform.position.x, 0.15f, transform.position.z);

        CheckHitPlayer();

        // Update the flashing effect when neutral
        float timesinceSpawn = Time.time - spawntime;
        if(timesinceSpawn < neutralTime) {
            float mag = Mathf.Sin(timesinceSpawn * neutralFlashFrequency);

            if(mag > 0.0f) {
                baseRenderer.enabled = true;
            }
            else {
                baseRenderer.enabled = false;
            }
        }
        else {
            baseRenderer.enabled = true;
        }
	}



    protected void MakeDirectionDecision() {
        lastDecisionTime = Time.time;
        nextDecisionInterval = Random.Range(minDecisionInterval, maxDecisionInterval);

        int dirIdx = Random.Range(0, 4);

        currentDirection = (EDirection)dirIdx;
    }


    public void Move(Vector3 direction, float speed) {
        characterController.Move(direction.normalized * speed * Time.deltaTime);
    }


    /** 
     * Checks if it overlaps player at all, and handles the hit if it does
     */
    protected void CheckHitPlayer() {
        // Dont hurt player until this interval is over
        if(Time.time - spawntime < neutralTime || Time.time - lastAttackTime < minAttackCooldown) {
            return;
        }

        float verticalOffset = transform.localScale.y / 2.0f;
        float xOffset = transform.localScale.x / 2.0f;
        float zOffset = transform.localScale.z / 2.0f;

        Vector3[] corners = new Vector3[8];

        // top 4 corners
        corners[0] = transform.position + new Vector3(xOffset, verticalOffset, zOffset);
        corners[1] = transform.position + new Vector3(-xOffset, verticalOffset, zOffset);
        corners[2] = transform.position + new Vector3(-xOffset, verticalOffset, -zOffset);
        corners[3] = transform.position + new Vector3(xOffset, verticalOffset, -zOffset);

        // bottom corners
        corners[4] = transform.position + new Vector3(xOffset, -verticalOffset, zOffset);
        corners[5] = transform.position + new Vector3(-xOffset, -verticalOffset, zOffset);
        corners[6] = transform.position + new Vector3(-xOffset, -verticalOffset, -zOffset);
        corners[7] = transform.position + new Vector3(xOffset, -verticalOffset, -zOffset);

        for(int i = 0; i < corners.Length; i++) {
            RaycastHit hit;
            if(Physics.Linecast(transform.position, corners[i], out hit, hitDetect)) {
                if(hit.collider.GetComponent<PlayerController>() != null) {
                    // Hit the player
                    PlayerController player = hit.collider.GetComponent<PlayerController>();
                    player.ReceiveHit(this);

                    lastAttackTime = Time.time;
                    break;
                }
            }
        }

    }


    // Handles killing of the enemy in one place. Notifies game mode etc.
    public void Kill() {
        OnDeath();
        gameMode.EnemyWasKilled(this);
        gameMode.SpawnDroppedItem(this);
        gameObject.SetActive(false);
    }


    // Called when the player hits this enemy, returns whther or not it died
    public virtual bool HitByPlayer(PlayerController instigator) {

        // certain cases the enemy will have some invulnerability
        if(Time.time - spawntime < spawnInvulnerability) {
            return false;
        }

        if(diesOneShot) {
            Kill();
            return true;
        }
        else {
            if(Time.time - lastHitTime < hitInvulnerability) {
                return false;
            }

            numHitsTaken += 1;

            if(numHitsTaken >= maxHits) {
                Kill();
                return true;
            }
            else {
                NonLethalHit();
            }
            
            lastHitTime = Time.time;
        }

        return false;
    }

    // Called when a hit is taken that doesnt kill the enemy
    protected virtual void NonLethalHit() {

    }

    // Called when the enemy has been killed an is about to be deactivated
    protected virtual void OnDeath() {
        
    }

    protected Vector3 GetDirection(EDirection dirEnum) {
        Vector3 d = Vector3.zero;
        switch(currentDirection) {
            case EDirection.North:
                d.z = 1.0f;
                break;
            case EDirection.South:
                d.z = -1.0f;
                break;
            case EDirection.East:
                d.x = 1.0f;
                break;
            case EDirection.West:
                d.x = -1.0f;
                break;
        }

        return d;
    }

}
