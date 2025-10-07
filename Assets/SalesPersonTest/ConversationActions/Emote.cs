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

    public ActionParameter[] Parameters => new ActionParameter[]
    {
        new ActionParameter("blendShapeBlink", "float", "Blink blend shape (0.0 to 1.0)"),
        new ActionParameter("blendShapeMouthA", "float", "Mouth A blend shape (0.0 to 1.0)"),
        new ActionParameter("blendShapeMouthI", "float", "Mouth I blend shape (0.0 to 1.0)"),
        new ActionParameter("blendShapeMouthU", "float", "Mouth U blend shape (0.0 to 1.0)"),
        new ActionParameter("blendShapeMouthE", "float", "Mouth E blend shape (0.0 to 1.0)"),
        new ActionParameter("blendShapeMouthO", "float", "Mouth O blend shape (0.0 to 1.0)"),
        new ActionParameter("blendShapeJoy", "float", "Joy expression blend shape (0.0 to 1.0)"),
        new ActionParameter("blendShapeAnger", "float", "Anger expression blend shape (0.0 to 1.0)"),
        new ActionParameter("blendShapeSorrow", "float", "Sorrow expression blend shape (0.0 to 1.0)"),
        new ActionParameter("blendShapeFun", "float", "Fun expression blend shape (0.0 to 1.0)")
    };

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
	emoter.Emote(emoteParams).Forget();
	return UniTask.FromResult(true);
    }
}
