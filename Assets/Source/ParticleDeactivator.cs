using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleDeactivator : MonoBehaviour {

    private ParticleSystem particleSystem;

    private float lifeTime;
    private float startTime;

	
	void Start () {
	    particleSystem = GetComponent<ParticleSystem>();
        lifeTime = particleSystem.duration;
	}
	
	
	void OnEnable() {
        startTime = Time.time;
	}

    void Update() {
        if(Time.time - startTime > lifeTime) {
            gameObject.SetActive(false);
        }
    }
}
