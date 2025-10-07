using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public interface IStringConvertible<T> where T : IStringConvertible<T>{
	string ToString();
	void ReadFrom(string str);
}

public interface IPrompt<T> : IStringConvertible<T> where T : IPrompt<T>{
}

public interface IContext<T> : IStringConvertible<T> where T : IContext<T>{
}

[System.Serializable]
public struct ActionParameter
{
	public string key;
	public string type;
	public string description;

	public ActionParameter(string key, string type, string description)
	{
		this.key = key;
		this.type = type;
		this.description = description;
	}
}

public interface IAction< TContext> where TContext : IContext<TContext>{
	public string ActionId {get;}
	public string Description {get;}
	public ActionParameter[] Parameters {get;}

	public string GetActionDescription(){
		return $"{ActionId}: {Description}\nParameters: {GetParametersAsJson()}";
	}

	public string GetParametersAsJson(){
		if (Parameters == null || Parameters.Length == 0)
			return "{}";

		var jsonBuilder = new System.Text.StringBuilder();
		jsonBuilder.Append("{\n");

		for (int i = 0; i < Parameters.Length; i++)
		{
			var param = Parameters[i];
			jsonBuilder.Append($"    \"{param.key}\": {param.type}");
			if (!string.IsNullOrEmpty(param.description))
			{
				jsonBuilder.Append($" // {param.description}");
			}

			if (i < Parameters.Length - 1)
				jsonBuilder.Append(",");
			jsonBuilder.Append("\n");
		}

		jsonBuilder.Append("}");
		return jsonBuilder.ToString();
	}

	UniTask<bool> Execute(
		string param,
		UnityEngine.GameObject target,
		IContextManager<TContext> context,
		CancellationToken cancellationToken
	);
}

[System.Serializable]
public struct AgentActionResponse
{
	public string actionId;
	public string targetId;
	public string param;

	public AgentActionResponse(
		string actionId, 
		string targetId,
		string param
	){
		this.actionId = actionId;
		this.targetId = targetId;
		this.param = param;
	}
}

public interface IAgentModel<TPrompt, TContext>
	where TPrompt : IPrompt<TPrompt>
	where TContext : IContext<TContext>{

	List<IAction<TContext>> GetAvailableActions();

	UniTask<List<List<AgentActionResponse>>> GetResponse(
		TPrompt prompt,
		TContext context,
		int maxTokens
	);
}
