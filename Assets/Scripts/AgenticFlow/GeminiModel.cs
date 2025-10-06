using System;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

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

    public async UniTask<List<(string actionId, string targetId, string param)>> GetResponse(
	LLMPromptData prompt,
	DescritpiveContextData context,
	int maxTokens
    ){
	Debug.Log($"[GeminiModelAgent] Processing prompt: {prompt.ToString()}");

	// If API service is not available, fall back to dummy response
	if (_apiService == null)
	{
	    Debug.LogWarning("[GeminiModelAgent] No API service provided, using dummy response");
	    return GetDummyResponse();
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
		return GetDummyResponse();
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

		// First, try to fix the response if param fields have unescaped quotes
		// Updated regex to handle nested JSON objects better
		var fixedResponse = System.Text.RegularExpressions.Regex.Replace(
		    response,
		    @"""param"":""({(?:[^{}]|{[^{}]*})*})""",
		    match => {
			var paramContent = match.Groups[1].Value;
			// Escape the quotes in the JSON content
			var escapedContent = paramContent.Replace("\"", "\\\"");
			return $@"""param"":""{escapedContent}""";
		    }
		);

		Debug.Log($"[GeminiModelAgent] Fixed response: {fixedResponse}");

		List<ActionResponse> actions = null;
		try
		{
		    actions = JsonConvert.DeserializeObject<List<ActionResponse>>(fixedResponse);
		    Debug.Log($"[GeminiModelAgent] Successfully parsed {actions.Count} actions");
		}
		catch (Exception e)
		{
		    // If that didn't work, try the original response
		    Debug.LogWarning($"[GeminiModelAgent] Failed with fixed response: {e.Message}");
		    Debug.LogWarning("[GeminiModelAgent] Trying original response");
		    try
		    {
			actions = JsonConvert.DeserializeObject<List<ActionResponse>>(response);
		    }
		    catch (Exception e2)
		    {
			Debug.LogError($"[GeminiModelAgent] Failed to parse original response: {e2.Message}");
			throw;
		    }
		}

		var result = new List<(string actionId, string targetId, string param)>();

		foreach (var action in actions)
		{
		    // The param field might have incorrect escaping from Gemini
		    var cleanParam = action.param;

		    // If the param has escaped quotes, unescape them
		    if (cleanParam.Contains("\\\""))
		    {
			cleanParam = cleanParam.Replace("\\\"", "\"");
		    }

		    // Validate that param is valid JSON
		    try
		    {
			JsonConvert.DeserializeObject(cleanParam);
			result.Add((action.actionId, action.targetId, cleanParam));
		    }
		    catch (Exception paramEx)
		    {
			Debug.LogError($"[GeminiModelAgent] Could not parse param for action {action.actionId}: {action.param}");
			Debug.LogError($"[GeminiModelAgent] Error: {paramEx.Message}");
		    }
		}

		Debug.Log($"[GeminiModelAgent] Parsed {result.Count} actions from API response");
		return result;
	    }
	    catch (Exception e)
	    {
		Debug.LogError($"[GeminiModelAgent] Failed to parse API response: {e.Message}");
		return GetDummyResponse();
	    }
	}
	catch (Exception e)
	{
	    Debug.LogError($"[GeminiModelAgent] API call failed: {e.Message}");
	    return GetDummyResponse();
	}
    }

    private List<(string actionId, string targetId, string param)> GetDummyResponse()
    {
	var rand_x = UnityEngine.Random.Range(-5.0f, 5.0f);
	var rand_z = UnityEngine.Random.Range(-5.0f, 5.0f);
	return new List<(string actionId, string targetId, string param)>(){
	    ("move",
	    "Cube",
	    JsonConvert.SerializeObject(
		new MoveAction.ActionData(){
		destination = new SerializableVector3(rand_x, 0, rand_z)
	    }))
	};
    }

    [Serializable]
    private class ActionResponse
    {
	public string actionId;
	public string targetId;
	public string param;
    }
}
