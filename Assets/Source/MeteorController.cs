using UnityEngine;
using System.Collections;

public class MeteorController : MonoBehaviour {

    [SerializeField]
    private float fallSpeed;

    [SerializeField]
    private float damageRange;

    [SerializeField]
    private GameObject explosionEffectPrototype;

    [SerializeField]
    public AudioClip impactSound;

    [SerializeField]
    public Projector shadowProjector;

    [SerializeField]
    private float maxShadowSize = 0.5f;
    private float startShadowSize;

    private Vector3 velocity;
    private float boundsScale;
    private float startHeight;
	
	private void OnEnable() {
        velocity = new Vector3(0.0f, -fallSpeed, 0.0f);
        boundsScale = transform.localScale.y;
        startHeight = 15.0f;
        startShadowSize = shadowProjector != null ? shadowProjector.orthographicSize : 0.0f;
	}

    private void OnDisable() {
        velocity = Vector3.zero;
        if(shadowProjector != null) {
            shadowProjector.orthographicSize = startShadowSize;
        }
    }
	
	
	private void Update() {
        transform.position += velocity * Time.deltaTime;
        float yHeight = transform.position.y;

        if(shadowProjector != null) {
            float progress = 1.0f - (yHeight / startHeight);
            shadowProjector.orthographicSize = startShadowSize + progress * (maxShadowSize - startShadowSize);
        }

        if(yHeight < boundsScale) {
            OnImpact();
        }
	}


    private void OnImpact() {
        Vector3 groundLocation = transform.position;
        groundLocation.y = 0.0f;
        GameMode.instance.SpawnObjectFast(explosionEffectPrototype, groundLocation);
        
        // check for player nearby
        Transform playerTransform = GameMode.instance.GetPlayer().transform;
        Vector3 toPlayer = playerTransform.position - transform.position;
        if(toPlayer.magnitude <= damageRange) {
            PlayerController player = playerTransform.gameObject.GetComponent<PlayerController>();
            player.ReceiveHit(null);
        }

        if(impactSound != null) {
            GameMode.instance.PlaySound(impactSound, 1.0f, 1.0f);
        }
        
        gameObject.SetActive(false);
    }

}
