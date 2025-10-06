using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

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

		var actions = await _model.GetResponse(prompt, context, 1000);

		Debug.Log($"[Agent] Received {actions.Count} actions");

		// Group actions by target
		var actionGroups = actions.GroupBy(a => a.targetId)
		    .ToDictionary(g => g.Key, g => g.ToList());

		Debug.Log($"[Agent] Grouped into {actionGroups.Count} target groups");

		// Temporarily disable triggers during action execution
		var originalContextCallback = _contextManager.OnContextUpdated;
		_contextManager.OnContextUpdated = null;

		// Execute action groups in parallel
		var parallelTasks = new List<UniTask>();

		foreach(var (targetId, targetActions) in actionGroups){
		    // Find the target GameObject
		    var targetObject = UnityEngine.GameObject.Find(targetId);
		    if(targetObject == null){
			Debug.LogWarning($"[Agent] Target object '{targetId}' not found");
			continue;
		    }

		    // Create a task for this target's sequential actions
		    var targetTask = ExecuteActionsForTarget(targetObject, targetActions, cancellationToken);
		    parallelTasks.Add(targetTask);
		}

		// Wait for all parallel tasks to complete
		await UniTask.WhenAll(parallelTasks);

		// Restore context callback
		_contextManager.OnContextUpdated = originalContextCallback;
	    }
	    catch(System.Exception ex){
		Debug.LogError($"[Agent] Error: {ex.Message}\n{ex.StackTrace}");
	    }
	}

	Debug.Log("[Agent] Run loop ended");
    }

    private async UniTask ExecuteActionsForTarget(
	GameObject target,
	List<(string actionId, string targetId, string param)> actions,
	CancellationToken cancellationToken)
    {
	Debug.Log($"[Agent] Starting {actions.Count} actions for target: {target.name}");

	foreach(var (actionId, _, param) in actions){
	    if(cancellationToken.IsCancellationRequested) break;

	    Debug.Log($"[Agent] Executing {actionId} on {target.name}");

	    var availableActions = _model.GetAvailableActions();
	    var action = availableActions.Find(a => a.ActionId == actionId);

	    if(action == null){
		Debug.LogWarning($"[Agent] Action {actionId} not found");
		continue;
	    }

	    try{
		var success = await action.Execute(param, target, _contextManager, cancellationToken);
		if(success){
		    Debug.Log($"[Agent] Action {actionId} completed on {target.name}");
		}
		else{
		    Debug.LogWarning($"[Agent] Action {actionId} failed on {target.name}");
		}
	    }
	    catch(System.Exception ex){
		Debug.LogError($"[Agent] Error executing {actionId} on {target.name}: {ex.Message}");
	    }
	}

	Debug.Log($"[Agent] Completed all actions for target: {target.name}");
    }
}
