using UnityEngine;
using System.Collections;

public class BossController : EnemyController {

    [SerializeField]
    public AudioClip armorDestroyedSound;

    [SerializeField]
    protected GameObject armorObject;

    [SerializeField]
    protected ParticleSystem armorDestroyedEffect;


    private bool bIsDamaged;

    public override void OnSpawn() {
        base.OnSpawn();

        // reset damaged state
        if(bIsDamaged && armorObject != null) {
            armorObject.SetActive(true);
        }

        bIsDamaged = false;
    }


	
	protected override void Start () {
        base.Start();
	}


    protected override void Update() {
        base.Update();

        CheckWallCollision();

        if(numHitsTaken > 0) {
            if(!bIsDamaged) {
                bIsDamaged = true;

                // Go to the damaged visuals
                if(armorObject != null) {
                    armorObject.SetActive(false);
                }
            }
        }
	}


    protected override void NonLethalHit() {
        base.NonLethalHit();

        gameMode.PlaySound(armorDestroyedSound, 0.5f, 1.0f);
        gameMode.SpawnParticleSystem(armorDestroyedEffect, transform.position);
    }

    private void CheckWallCollision() {
        Vector3 testDir = GetDirection(currentDirection);

        Vector3 skinOffset = testDir * characterController.radius;

        RaycastHit hit;
        Physics.Linecast(transform.position, transform.position + skinOffset + testDir * Time.deltaTime, out hit);

        if(hit.collider != null && hit.collider.tag == "Wall") {
            Vector3 collisionNormal = -testDir;

            if(Mathf.Abs(collisionNormal.x) > Mathf.Abs(collisionNormal.z)) {
                if(collisionNormal.x > 0.0f) {
                    currentDirection = EDirection.East;
                }
                else {
                    currentDirection = EDirection.West;
                }
            }
            else {
                if(collisionNormal.z > 0.0f) {
                    currentDirection = EDirection.North;
                }
                else {
                    currentDirection = EDirection.South;
                }
            }

            //Debug.Log("[" + collisionNormal.x + ", " + collisionNormal.z + "] Decision: " + currentDirection);
        }
    }


}
