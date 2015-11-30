using UnityEngine;
using System.Collections.Generic;

public class ObjectPool {
    /** 
     * Basically storing a big  histogram of spawned Actors, so they do'nt get constantly created and destoryed
     */
    private const int DEFAULT_POOL_SIZE = 10;
    private const int DEFAULT_EXPANTION_NUM = 5;

    // Main pool: one list for each type of object: associated with their class name
    private Dictionary<string, List<GameObject>> objectPools;

    public ObjectPool() {
        objectPools = new Dictionary<string, List<GameObject>>();
    }


    /** 
     * Returns an inactive game object instance of the input prototype 
     */ 
    public GameObject GetInactiveGameObjectInstance(GameObject prototype) {
        // First check if there is an entry in the table for this object type
        if(objectPools.ContainsKey(prototype.name)) {
            // Search for an inactiv object in that list
            List<GameObject> selectedPool;
            objectPools.TryGetValue(prototype.name, out selectedPool);

            if(selectedPool != null && selectedPool.Count > 0) {
                for(int i = 0; i < selectedPool.Count; i++ ) {
                    if(!selectedPool[i].activeSelf) {
                        return selectedPool[i];
                    }
                }
            }

            // There was no active object in the pool or the pool was empty for some reason, we will need to expand it
            return ExpandPoolList(prototype, DEFAULT_EXPANTION_NUM);
        }
        else {
            // Will need to create a new one: initialize some default objects to save some time later
            List<GameObject> newObjectPool = GetNewPoolList(prototype, DEFAULT_POOL_SIZE);
            objectPools.Add(prototype.name, newObjectPool);
            return newObjectPool[0];
        }
    }


    /** 
     * Expands the pool for the prototype game boject, and returns the first objects added
     * Returns null if there is not already a pool for the prototype
     */ 
    private GameObject ExpandPoolList(GameObject prototype, int numToAdd) {
        List<GameObject> poolToExpand;

        objectPools.TryGetValue(prototype.name, out poolToExpand);
        if(poolToExpand != null) {
            List<GameObject> expansionPool = GetNewPoolList(prototype, DEFAULT_EXPANTION_NUM);
            poolToExpand.AddRange(expansionPool);

            return expansionPool[0];
        }

        return null;
    }


    /**
     * Creates a new list of the default size for the input prototype
     */
    private List<GameObject> GetNewPoolList(GameObject prototype, int startSize) {
        List<GameObject> newObjectPool = new List<GameObject>(startSize);
        for(int i = 0; i < startSize; i++) {
            GameObject obj = (GameObject)GameObject.Instantiate(prototype);
            obj.SetActive(false);
            newObjectPool.Add(obj);
        }

        return newObjectPool;
    }

}
