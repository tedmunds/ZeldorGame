using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class GameCamera : MonoBehaviour {

    // Game Camera is a singleton: makes sense, as there can only be one camera for the player
    public static GameCamera instance;

    private Vector3 baseLocation;

    private Vector3 shakeLocation;
    private bool bDoShake;
    private bool bReachedMax;

    private float shakeSpeed;

	void Start () {
        baseLocation = transform.position;
        instance = this;
	}
	
	
	void Update () {
        if(bDoShake) {
            if((transform.position - shakeLocation).magnitude > 0.1f && !bReachedMax) {
                transform.position = Vector3.Lerp(transform.position, shakeLocation, shakeSpeed * Time.deltaTime);
            }
            else if((transform.position - shakeLocation).magnitude < 0.1f && !bReachedMax) {
                bReachedMax = true;
            }
            else {
                transform.position = Vector3.Lerp(transform.position, baseLocation, shakeSpeed * Time.deltaTime);

                if((transform.position - baseLocation).magnitude < 0.01f) {
                    bDoShake = false;
                }
            }
        }
        else {
            transform.position = baseLocation;
        }
	}


    public void DoCameraShake(float strength, float speed) {
        // Dont shake if already shaking
        if(bDoShake) {
            return;
        }

        shakeSpeed = speed;
        shakeLocation = transform.position + transform.up * strength;

        bDoShake = true;
        bReachedMax = false;
    }
}
