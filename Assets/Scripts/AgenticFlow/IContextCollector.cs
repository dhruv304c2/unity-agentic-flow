using System;
using Cysharp.Threading.Tasks;

public interface IContextManager<TContext> where TContext : IContext<TContext>{
    UniTask<TContext> CollectContext();
    Action OnContextUpdated {get; set;}
    void UptateContext();
}
