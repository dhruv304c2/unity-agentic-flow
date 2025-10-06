using System;
using UnityEngine;

[Serializable]
public class GameObjectContextData{
    public string objectName;
    public string description;
    public SerializableVector3 position;
}

public class DescriptionContext : MonoBehaviour{
    public string description;
    GameObjectContextData GetContextData(){
    	return new GameObjectContextData(){
	    objectName = gameObject.name,
	    description = description,
	    position = transform.position
	};
    }
}
