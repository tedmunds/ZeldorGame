using UnityEngine;
using System.Collections;

public class SceneLoader : MonoBehaviour {


    public void LoadSceneByName(string sceneName) {
        Application.LoadLevel(sceneName);
    }

}
