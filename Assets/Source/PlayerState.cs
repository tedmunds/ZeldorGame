using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

[XmlRoot("PlayerState")]
public class PlayerState {

    [XmlElement("HighScore")]
    public int highScore;

    [XmlElement("LongestGameTime")]
    public float longestGameTime;



    public PlayerState() {
		
	}

    // Generate a file path name for a new save with the input name
    public static string CreateSavePath(string saveName) {
        return Path.Combine(Application.dataPath, "Saves/" + saveName + ".xml");
    }

    public static void SavePlayerState(string saveName, PlayerState state) {
        if(state == null) {
            Debug.Log("ERROR! Cannot save a null player state");
            return;
        }

        string filePath = CreateSavePath(saveName);

        XmlSerializer serializer = new XmlSerializer(typeof(PlayerState));

        try {
            FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
            serializer.Serialize(stream, state);
            //stream.Close();
        }
        catch(IOException e) {
            Debug.Log("ERROR! Couldn't save at " + filePath + " :: " + e.Message);
            return;
        }
    }

    public static PlayerState LoadPlayerState(string saveName) {
        XmlSerializer serializer = new XmlSerializer(typeof(PlayerState));

        string filePath = CreateSavePath(saveName);

        try {
            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return serializer.Deserialize(stream) as PlayerState;
        }
        catch(IOException e) {
            // Couldn;t find existing save by this name, create a new one
            PlayerState newState = new PlayerState();
            SavePlayerState(saveName, newState);

            return newState;
        }
    }



}



