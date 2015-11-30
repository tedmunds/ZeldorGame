using UnityEngine;
using System.Collections;

public class DroppedItem : MonoBehaviour {

    public enum EDropType {
        Points,
        Life, 
        Speed, 
        AttackRadius
    }

    [SerializeField]
    public EDropType dropType;

    [SerializeField]
    public AudioClip pickupSoundClip;

    void OnTriggerEnter(Collider other) {
        PlayerController player = other.GetComponent<PlayerController>();
        if(player != null) {
            player.PickUpItem(this);

            gameObject.SetActive(false);
        }
    }
}
