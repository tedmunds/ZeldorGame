using UnityEngine;
using System.Collections;

public class SplitterController : BossController {

    // The type of enemy spawned when it splits
    [SerializeField]
    protected EnemyController splitChildType;

    // How many enemyess are spwned when it splits
    [SerializeField]
    protected int splitNum;


	protected override void Start () {
        base.Start();
	}


    protected override void Update() {
        base.Update();
    }

    protected override void OnDeath() {
        base.OnDeath();
        SplitIntoChildren();
    }

    protected void SplitIntoChildren() {
        const float childInvulnerabilityTime = 0.5f;

        for(int i = 0; i < splitNum; i++) {
            Vector3 spawnLocation = transform.position;

            EnemyController spawned = gameMode.SpawnEnemyType(splitChildType, spawnLocation);
            spawned.GiveSpawnInvulnerability(childInvulnerabilityTime);
        }
    }
}