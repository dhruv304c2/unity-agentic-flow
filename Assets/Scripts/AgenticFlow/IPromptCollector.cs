using System;
using Cysharp.Threading.Tasks;

public interface IPromptCollector<TPrompt> where TPrompt : IPrompt<TPrompt>{
    UniTask<TPrompt> CollectPrompt();
    Action OnNewPrompt {get; set;}
}
