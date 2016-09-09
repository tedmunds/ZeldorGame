using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Field is the block on which gameplay takes place
 */ 
public class GameField : MonoBehaviour {

    private bool bIsSliding = false;
    private float slideStartTime = 0.0f;
    private Vector3 slideToLocation;
    private float slideLengthTime;
    private AnimationCurve slideEasingCurve;
    private Vector3 slideVelocity;

    public List<GameObject> modifiers;

	private void Update() {
	    if(bIsSliding) {
            float elapsedTime = Time.time - slideStartTime;
            float velocityEasingFactor = slideEasingCurve.Evaluate(elapsedTime / slideLengthTime);

            transform.position += velocityEasingFactor * slideVelocity * Time.deltaTime;

            float distance = Vector3.Distance(transform.position, slideToLocation);
            if(distance <= slideVelocity.magnitude * Time.deltaTime) {
                bIsSliding = false;
                transform.position = slideToLocation;
            }
        }
	}


    public void OnLevelStart(GameLevel level) {
        modifiers = new List<GameObject>();
        foreach(GameObject modifierprototype in level.gameModeModifiers) {
            modifiers.Add(Instantiate<GameObject>(modifierprototype));
        }
    }

    public void OnLevelEnd() {
        for(int i = modifiers.Count - 1; i >= 0; i--) {
            Destroy(modifiers[i]);
        }
    }



    /// <summary>
    /// Causes teh field to slide out to the input point over the input length of time.
    /// Follows the slide velocity curve parameter of the level
    /// </summary>
    public void SlideTo(Vector3 targetLocation, float slideLength, AnimationCurve slideEasing) {
        slideStartTime = Time.time;
        bIsSliding = true;

        slideToLocation = targetLocation;
        slideLengthTime = slideLength;
        slideEasingCurve = slideEasing;

        Vector3 toTarget = targetLocation - transform.position;
        float distance = toTarget.magnitude;

        slideVelocity = toTarget.normalized * (distance / (slideLength / 2.0f));
    }
}
