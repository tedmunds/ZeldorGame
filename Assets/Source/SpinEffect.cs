using UnityEngine;
using System.Collections;


/** Causes the game object to spin around the up vector once when acctivated */
public class SpinEffect : MonoBehaviour {

    [SerializeField]
    private int numSpins = 1;

    /** How long to make a full rotation */
    [SerializeField]
    private float spinTime = 0.5f;

    private bool bDoSpin;
    private float accumulatedAngle;

    TrailRenderer trailRenderer;

    ParticleSystem particleSystem;

	void Start () {
        //trailRenderer = GetComponent<TrailRenderer>();
        //if(trailRenderer == null && transform.childCount > 0) {
        //    Transform child = transform.GetChild(0);
        //    trailRenderer = child.GetComponent<TrailRenderer>();
        //}

        particleSystem = GetComponent<ParticleSystem>();
	}
	
	
	void Update () {
        //if(bDoSpin) {
        //    float angle = (360.0f / spinTime) * Time.deltaTime;

        //    transform.Rotate(Vector3.up, angle);
        //    accumulatedAngle += angle;

        //    if(accumulatedAngle > 360.0f * numSpins) {
        //        bDoSpin = false;
        //    }
        //}
        //else if(trailRenderer.enabled) {
        //    trailRenderer.enabled = false;
        //}
	}


    public void ActivateSpin() {
        bDoSpin = true;
        //trailRenderer.enabled = true;
        //accumulatedAngle = 0.0f;

        particleSystem.Play();
    }


}
