using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

public class Agent< TPrompt, TContext>
	where TPrompt : IPrompt<TPrompt>
	where TContext : IContext<TContext>
{
    IAgentModel<TPrompt,TContext> _model;
    IContextManager<TContext> _contextManager;
    IPromptCollector<TPrompt> _promptCollector;

    bool triggerUpdate;

    public Agent(
	IAgentModel<TPrompt,TContext> model,
	IContextManager<TContext> contextCollector,
	IPromptCollector<TPrompt> promptCollector
    ){
	_model = model;
	_contextManager = contextCollector;
	_promptCollector = promptCollector;

	_contextManager.OnContextUpdated += () => triggerUpdate = true;
	_promptCollector.OnNewPrompt += () => triggerUpdate = true;
    }

    public Action OnProcessing {get; set;}
    public Action OnIdle {get; set;}

    public async UniTask Run(CancellationToken cancellationToken=default){
	// Ensure we're running on the main thread
	await UniTask.SwitchToMainThread();

	while(!cancellationToken.IsCancellationRequested){
	    while(!triggerUpdate && !cancellationToken.IsCancellationRequested){
		// Wait one frame on main thread
		await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
	    }

	    if(cancellationToken.IsCancellationRequested) break;

	    Debug.Log($"[Agent] Triggered on frame {Time.frameCount}");

	    triggerUpdate = false;
	    OnProcessing?.Invoke();

	    try{
		// Collect prompt - this should return immediately if a prompt is cached
		var prompt = await _promptCollector.CollectPrompt();

		// If no prompt was available, skip this cycle
		if(prompt == null){
		    Debug.Log("[Agent] No prompt available, skipping cycle");
		    continue;
		}

		var context = await _contextManager.CollectContext();

		Debug.Log("[Agent] Getting response from model...");

		var actionSequences = await _model.GetResponse(prompt, context, 1000);

		Debug.Log($"[Agent] Received {actionSequences.Count} sequences with total {actionSequences.Sum(s => s.Count)} actions");

		// Temporarily disable triggers during action execution
		var originalContextCallback = _contextManager.OnContextUpdated;
		_contextManager.OnContextUpdated = null;

		// Execute sequences in parallel
		var parallelTasks = new List<UniTask>();

		for(int i = 0; i < actionSequences.Count; i++){
		    var sequence = actionSequences[i];

		    // Create a task for this sequence's sequential actions
		    var sequenceTask = ExecuteActionSequence(sequence, i, cancellationToken);
		    parallelTasks.Add(sequenceTask);
		}

		// Wait for all parallel sequences to complete
		await UniTask.WhenAll(parallelTasks);

		// Restore context callback
		_contextManager.OnContextUpdated = originalContextCallback;
	    }
	    catch(System.Exception ex){
		Debug.LogError($"[Agent] Error: {ex.Message}\n{ex.StackTrace}");
	    }

	    OnIdle?.Invoke();
	}

	Debug.Log("[Agent] Run loop ended");
    }

    private async UniTask ExecuteActionSequence(
	List<AgentActionResponse> sequence,
	int sequenceIndex,
	CancellationToken cancellationToken)
    {
	Debug.Log($"[Agent] Starting sequence {sequenceIndex} with {sequence.Count} actions");

	foreach(var actionResponse in sequence){
	    if(cancellationToken.IsCancellationRequested) break;

	    // Find the target GameObject
	    var targetObject = UnityEngine.GameObject.Find(actionResponse.targetId);
	    if(targetObject == null){
		Debug.LogWarning($"[Agent] Target object '{actionResponse.targetId}' not found");
		continue;
	    }

	    Debug.Log($"[Agent] Executing {actionResponse.actionId} on {targetObject.name}");

	    var availableActions = _model.GetAvailableActions();
	    var action = availableActions.Find(a => a.ActionId == actionResponse.actionId);

	    if(action == null){
		Debug.LogWarning($"[Agent] Action {actionResponse.actionId} not found");
		continue;
	    }

	    try{
		var success = await action.Execute(actionResponse.param, targetObject, _contextManager, cancellationToken);
		if(success){
		    Debug.Log($"[Agent] Action {actionResponse.actionId} completed on {targetObject.name}");
		}
		else{
		    Debug.LogWarning($"[Agent] Action {actionResponse.actionId} failed on {targetObject.name}");
		}
	    }
	    catch(System.Exception ex){
		Debug.LogError($"[Agent] Error executing {actionResponse.actionId} on {targetObject.name}: {ex.Message}");
	    }
	}

	Debug.Log($"[Agent] Completed sequence {sequenceIndex}");
    }
}
