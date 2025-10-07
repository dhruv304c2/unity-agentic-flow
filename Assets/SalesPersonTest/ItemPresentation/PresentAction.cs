using System;
using UnityEngine;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;

[Serializable]
public class PresentAction : IAction<DescritpiveContextData>
{
    public string ActionId => "present";

    public string Description => "Present an item by moving it to a presentation point and rotating it, then returning it back. can only be performed when you are close to the item.";

    public ActionParameter[] Parameters => new ActionParameter[]
    {
        new ActionParameter("presentaionTarget", "string", "Item to present"),
        new ActionParameter("duration", "float", "Presentation duration in seconds (default 2.0)")
    };

    public class PresentActionData{
        public string presentaionTarget; // Item to present
        public float duration = 2.0f; // Default presentation duration
    }

    public async UniTask<bool> Execute(
        string param,
        UnityEngine.GameObject target,
        IContextManager<DescritpiveContextData> context,
        System.Threading.CancellationToken cancellationToken
    )
    {
        if (target == null)
        {
            Debug.LogError($"[PresentAction] Target object is null");
            return false;
        }

        Debug.Log($"[PresentAction] Presenting {target.name} with param: {param}");

        try
        {
            // Parse parameters
            var data = new PresentActionData();
            if (!string.IsNullOrEmpty(param))
            {
                data = JsonConvert.DeserializeObject<PresentActionData>(param) ?? new PresentActionData();
            }

            // Get Presenter component
            var presenter = target.GetComponent<Presenter>();
            if (presenter == null)
            {
                Debug.LogError($"[PresentAction] Presenter component not found on target {target.name}");
                return false;
            }

            // Execute presentation
            await presenter.Present(data.presentaionTarget);

            // Check if cancelled during presentation
            if (cancellationToken.IsCancellationRequested)
            {
                Debug.Log($"[PresentAction] Presentation cancelled for {target.name}");
                return false;
            }

            Debug.Log($"[PresentAction] Completed presenting {target.name}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[PresentAction] Failed to execute presentation: {e.Message}");
            return false;
        }
    }
}
