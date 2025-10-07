using System;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;

[Serializable]
public class LLMPromptData : IPrompt<LLMPromptData>{
    public string prompt;

    public void ReadFrom(string str){
	prompt = str;
	Debug.Log($"[GeminiPromptData] ReadFrom: {str}");
    }

    public override string ToString(){
	var json = JsonConvert.SerializeObject(this);
	Debug.Log($"[GeminiPromptData] ToString: {json}");
	return json;
    }
}

public class DescritpiveContextData : IContext<DescritpiveContextData>{
    public GameObjectContextData[] objects;

    public void ReadFrom(string str){
	Debug.Log($"[GeminiContextData] ReadFrom: {str}");
	try{
	    objects = JsonConvert.DeserializeObject<GameObjectContextData[]>(str);
	    Debug.Log($"[GeminiContextData] Deserialized {objects?.Length ?? 0} objects");
	}
	catch(Exception e){
	    Debug.LogError($"[GeminiContextData] Failed to deserialize: {e.Message}");
	    objects = new GameObjectContextData[0];
	}
    }

    public override string ToString(){
	var json = JsonConvert.SerializeObject(this);
	Debug.Log($"[GeminiContextData] ToString: {json}");
	return json;
    }
}


public class GeminiModelAgent : IAgentModel<LLMPromptData, DescritpiveContextData>
{
    private GeminiAPIService _apiService;
    private string _goalDescription;
    private List<IAction<DescritpiveContextData>> _actions;

    public GeminiModelAgent(GeminiAPIService apiService, List<IAction<DescritpiveContextData>> action,string goalDescription = null)
    {
	this._apiService = apiService;
	this._goalDescription = goalDescription;
	this._actions = action;
    }

    public void SetGoalDescription(string goal)
    {
	_goalDescription = goal;
	Debug.Log($"[GeminiModelAgent] Goal description set: {goal}");
    }

    public List<IAction<DescritpiveContextData>> GetAvailableActions(){
	return _actions;	
    }

    public async UniTask<List<List<AgentActionResponse>>> GetResponse(
	LLMPromptData prompt,
	DescritpiveContextData context,
	int maxTokens
    ){
	Debug.Log($"[GeminiModelAgent] Processing prompt: {prompt.ToString()}");

	// If API service is not available, fall back to dummy response
	if (_apiService == null)
	{
	    Debug.LogWarning("[GeminiModelAgent] No API service provided, using dummy response");
	    return new List<List<AgentActionResponse>>();
	}

	try
	{
	    // Format the prompt for Gemini API
	    var promptWithGoal = prompt.prompt;
	    if (!string.IsNullOrEmpty(_goalDescription))
	    {
		promptWithGoal = $"Goal: {_goalDescription}\n\nCurrent request: {prompt.prompt}";
	    }

	    var formattedPrompt = _apiService.FormatAgentPrompt(
		promptWithGoal,
		context.ToString(),
		GetAvailableActions()
	    );

	    // Call Gemini API
	    var response = await _apiService.GenerateContent(formattedPrompt);

	    if (string.IsNullOrEmpty(response))
	    {
		Debug.LogWarning("[GeminiModelAgent] Empty response from API, using dummy response");
		return new List<List<AgentActionResponse>>();
	    }

	    // Parse the response
	    try
	    {
		// Clean up response - Gemini might include markdown or extra text
		// Remove common markdown code block patterns
		response = response.Trim();
		if (response.StartsWith("```json"))
		{
		    response = response.Substring(7); // Remove ```json
		}
		else if (response.StartsWith("```"))
		{
		    response = response.Substring(3); // Remove ```
		}

		if (response.EndsWith("```"))
		{
		    response = response.Substring(0, response.Length - 3); // Remove trailing ```
		}

		response = response.Trim();

		// Find the JSON array boundaries
		var jsonStart = response.IndexOf('[');
		var jsonEnd = response.LastIndexOf(']');
		if (jsonStart >= 0 && jsonEnd > jsonStart)
		{
		    response = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
		}

		// Debug log the original response
		Debug.Log($"[GeminiModelAgent] Original response: {response}");

		// Parse the response - expecting array of arrays
		List<List<ActionResponse>> actionSequences = null;
		try
		{
		    actionSequences = JsonConvert.DeserializeObject<List<List<ActionResponse>>>(response);
		    Debug.Log($"[GeminiModelAgent] Successfully parsed {actionSequences.Count} sequences");
		}
		catch (Exception e)
		{
		    Debug.LogError($"[GeminiModelAgent] Failed to parse response: {e.Message}");
		    throw;
		}

		var result = new List<List<AgentActionResponse>>();

		foreach (var sequence in actionSequences)
		{
		    var sequenceResult = new List<AgentActionResponse>();

		    foreach (var action in sequence)
		    {
			// Convert JObject param to string for the action execution
			try
			{
			    // JObject is already parsed JSON, just convert to string
			    var paramString = action.param.ToString(Formatting.None);
			    sequenceResult.Add(new AgentActionResponse(action.actionId, action.targetId, paramString));
			    Debug.Log($"[GeminiModelAgent] Action {action.actionId} param: {paramString}");
			}
			catch (Exception paramEx)
			{
			    Debug.LogError($"[GeminiModelAgent] Could not convert param for action {action.actionId}: {action.param}");
			    Debug.LogError($"[GeminiModelAgent] Error: {paramEx.Message}");
			}
		    }

		    if (sequenceResult.Count > 0)
		    {
			result.Add(sequenceResult);
		    }
		}

		Debug.Log($"[GeminiModelAgent] Parsed {result.Count} sequences with total {result.Sum(s => s.Count)} actions");
		return result;
	    }
	    catch (Exception e)
	    {
		Debug.LogError($"[GeminiModelAgent] Failed to parse API response: {e.Message}");
		return new List<List<AgentActionResponse>>();
	    }
	}
	catch (Exception e)
	{
	    Debug.LogError($"[GeminiModelAgent] API call failed: {e.Message}");
	    return new List<List<AgentActionResponse>>();
	}
    }


    [Serializable]
    private class ActionResponse
    {
	public string actionId;
	public string targetId;
	public JObject param;
    }
}
