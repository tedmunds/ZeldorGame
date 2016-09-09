using UnityEngine;
using System.Collections;

/*
 * Difines a single difficulty level
 */ 
[System.Serializable]
public struct DifficultyLevel {
    public float spawnRate;
    public int maxEnemies;
    public int minEnemies;
    public int pointsThreshold;
    public float speedModifier;
    public bool bIsBossLevel;
}

/*
 * Defines a game level and the sequence of difficulties in it
 */
[System.Serializable]
public struct GameLevel {

    [SerializeField]
    public DifficultyLevel[] difficultyLevels;

    /// <summary>
    /// Modifer objects that will be spawned and can do whatever is wanted to change up the level. 
    /// When the level ends they will all be cleaned up
    /// </summary>
    [SerializeField]
    public GameObject[] gameModeModifiers;

    [SerializeField]
    public GameField GameFieldPrototype;
}
