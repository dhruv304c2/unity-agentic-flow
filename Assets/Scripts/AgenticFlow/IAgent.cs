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

public interface IAction< TContext> where TContext : IContext<TContext>{
	public string ActionId {get;}
	public string Description {get;}
	public string ParamDescription {get;}

	public string GetActionDescription();

	UniTask<bool> Execute(
		string param,
		UnityEngine.GameObject target,
		IContextManager<TContext> context,
		CancellationToken cancellationToken
	);
}

public interface IAgentModel<TPrompt, TContext>
	where TPrompt : IPrompt<TPrompt>
	where TContext : IContext<TContext>{

	List<IAction<TContext>> GetAvailableActions();

	UniTask<List<(string actionId, string targetId, string param)>> GetResponse(
		TPrompt prompt,
		TContext context,
		int maxTokens
	);
}
