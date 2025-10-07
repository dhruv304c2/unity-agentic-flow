using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TalkActionParams{
    public string dialogueLine;
}

public class TalkAction : IAction<DescritpiveContextData>{
    public string ActionId => "talk";
    public string Description => "Make the character say a line of dialogue";

    public ActionParameter[] Parameters => new ActionParameter[]
    {
        new ActionParameter("dialogueLine", "string", "The line of dialogue to speak")
    };

    public async UniTask<bool> Execute(
	string param,
	GameObject target,
	IContextManager<DescritpiveContextData> context,
	CancellationToken cancellationToken){

	var talkParams = JsonUtility.FromJson<TalkActionParams>(param);
	if(talkParams == null){
	    Debug.LogError("[TalkAction] Failed to parse parameters.");
	    return false;
	}

	var talker = target.GetComponent<Talker>();
	if(talker == null){
	    Debug.LogError("[TalkAction] Talker component not found on target.");
	    return false;
	}

	await talker.Talk(talkParams.dialogueLine);
	return true;
    }
}

