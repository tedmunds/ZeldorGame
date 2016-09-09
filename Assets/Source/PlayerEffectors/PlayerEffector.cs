using UnityEngine;
using System.Collections;

/*
 * Player Effectors are simple modifiers that update the player each turn they are applied
 */
public abstract class PlayerEffector {

    /*
     * Lifetime indicates when the effector will be automatically removed. if 0.0 then it is not removed
     */ 
    public float lifeTime = 0.0f;

    /*
     * Flags that this effector is active. If it is set false, the effector will be removed at the end of the next update
     */ 
    public bool bActive = false;

    /*
     * Tracks the time that this effector was first applied to the player
     */ 
    protected float appliedTime;
    protected float elapsedTime;


    public virtual void OnApplied(PlayerController player) {
        appliedTime = Time.time;
        bActive = true;
    }

    public virtual void OnRemoved(PlayerController player) {
        bActive = false;
    }

    /*
     * Called each frame that this effector is applied to the input player
     */
    public virtual void OnUpdate(PlayerController player) {
        elapsedTime = Time.time - appliedTime;
        if(lifeTime > 0.0f && elapsedTime >= lifeTime) {
            bActive = false;
        }
    }
}
