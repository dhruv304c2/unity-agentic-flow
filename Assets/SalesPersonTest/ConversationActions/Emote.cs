using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class EmoteActionParams
{
	public float blendShapeBlink;
	public float blendShapeMouthA;
	public float blendShapeMouthI;
	public float blendShapeMouthU;
	public float blendShapeMouthE;
	public float blendShapeMouthO;
	public float blendShapeJoy;
	public float blendShapeAnger;
	public float blendShapeSorrow;
	public float blendShapeFun;

}

public class Emote : IAction<DescritpiveContextData>{
    public string ActionId => "emote";
    public string Description => "Make the character perform an emote animation.";
    public string ParamDescription => @"{ 
	""blendShapeBlink"": float (0.0 to 1.0)
	""blendShapeMouthA"": float (0.0 to 1.0)
	""blendShapeMouthI"": float (0.0 to 1.0)
	""blendShapeMouthU"": float (0.0 to 1.0)
	""blendShapeMouthE"": float (0.0 to 1.0)
	""blendShapeMouthO"": float (0.0 to 1.0)
	""blendShapeJoy"": float (0.0 to 1.0)
	""blendShapeAnger"": float (0.0 to 1.0)
	""blendShapeSorrow"": float (0.0 to 1.0)
	""blendShapeFun"": float (0.0 to 1.0)
    }";

    public UniTask<bool> Execute(
	string param, 
	GameObject target, 
	IContextManager<DescritpiveContextData> context, 
	CancellationToken cancellationToken){
	var emoteParams = JsonUtility.FromJson<EmoteActionParams>(param);
	if(emoteParams == null){
	    Debug.LogError("[Emote] Failed to parse parameters.");
	    return UniTask.FromResult(false);
	}
	var emoter = target.GetComponent<Emoter>();
	if(emoter == null){
	    Debug.LogError("[Emote] Emoter component not found on target.");
	    return UniTask.FromResult(false);
	}
	emoter.Emote(emoteParams);
	return UniTask.FromResult(true);
    }
}
