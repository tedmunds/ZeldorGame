using UnityEngine;
using System.Collections;

public class DebugCommands : MonoBehaviour {

#if UNITY_EDITOR
	
	private void Update() {
	    if(Input.GetKeyDown(KeyCode.K)) {
            GameMode.instance.KillAllEnemies();
        }

        if(Input.GetKeyUp(KeyCode.I)) {
            GameMode.instance.GetPlayer().AddPoints(10);
        }

	}

#endif
}
