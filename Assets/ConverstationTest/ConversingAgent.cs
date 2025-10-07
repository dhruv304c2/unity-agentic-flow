using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AgenticPromptCollector : IPromptCollector<LLMPromptData> {
    public Action OnNewPrompt { get; set;}
    private string latestPrompt = "";

    public void Ask(string question) {
	latestPrompt = question;
	OnNewPrompt?.Invoke();
    }

    public UniTask<LLMPromptData> CollectPrompt(){
	return UniTask.FromResult(new LLMPromptData{prompt = latestPrompt} );
    }
}


public class ConversingAgent : MonoBehaviour {
	[SerializeField] DescriptionManager contexManager;
	[SerializeField] GeminiAPIService apiService;
	[SerializeField] string Goal;

	private CancellationTokenSource _cts;

	public void Start(){
		var geminiAgentModel = new GeminiModelAgent(apiService,
			new  List<IAction<DescritpiveContextData>>{
				new TalkAction(),
			},
			Goal
		);
		var agent = new Agent<LLMPromptData,DescritpiveContextData>(
			geminiAgentModel,
			contexManager,
			new AgenticPromptCollector()
		);

		agent.Run(_cts.Token).Forget();
	}
}
