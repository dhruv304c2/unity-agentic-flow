using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

public class DescriptionContextCollector :MonoBehaviour, IContextManager<GeminiContextData>{
    public System.Action OnContextUpdated {get; set;}

    DescriptionContext[] descriptionContexts;

    void Start(){
	descriptionContexts = FindObjectsByType<DescriptionContext>(FindObjectsSortMode.None);
	Debug.Log($"[DescriptionContextCollector] Found {descriptionContexts.Length} description contexts");
    }

    public async UniTask<GeminiContextData> CollectContext(){
	var context = new GeminiContextData();

	// Collect GameObjectContextData from each DescriptionContext
	var contextDataArray = new GameObjectContextData[descriptionContexts.Length];
	for(int i = 0; i < descriptionContexts.Length; i++){
	    contextDataArray[i] = new GameObjectContextData(){
		objectName = descriptionContexts[i].gameObject.name,
		description = descriptionContexts[i].description,
		position = descriptionContexts[i].transform.position
	    };
	}

	// Serialize the array properly
	var json = JsonConvert.SerializeObject(contextDataArray);
	Debug.Log($"[DescriptionContextCollector] Serialized context: {json}");

	context.ReadFrom(json);
	return context;
    }

    public void UptateContext(){
	OnContextUpdated?.Invoke();
    }
}
