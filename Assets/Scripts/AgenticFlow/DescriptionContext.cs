using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameObjectContextData{
    public string objectName;
    public string description;
    public SerializableVector3 position;
    public string[] availableActions;
}

public class DescriptionContext : MonoBehaviour{
    public string description;
    public List<string> availableActions;

    GameObjectContextData GetContextData(){
    	return new GameObjectContextData(){
	    objectName = gameObject.name,
	    description = description,
	    position = transform.position,
	    availableActions = availableActions.ToArray()
	};
    }
}
