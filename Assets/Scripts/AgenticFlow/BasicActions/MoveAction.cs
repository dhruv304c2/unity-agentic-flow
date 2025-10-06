using System;
using UnityEngine;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;

[Serializable]
public class MoveAction : IAction<GeminiContextData>
{
    public string ActionId => "move";

    public string Description => "Move a game object to a specified position in 3D space";
    public string ParamDescription => "JSON object with 'destination' (x,y,z coordinates)";

    public class ActionData
    {
        public SerializableVector3 destination;
    }

    string targetObjectName;
    SerializableVector3 destination;

    public void ReadFrom(string str)
    {
        var data = JsonConvert.DeserializeObject<MoveAction>(str);
        targetObjectName = data.targetObjectName;
        destination = data.destination;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }

    public async UniTask<bool> Execute(
        string param,
        UnityEngine.GameObject target,
        IContextManager<GeminiContextData> context,
        System.Threading.CancellationToken cancellationToken
    )
    {
        if (target == null)
        {
            Debug.LogError($"[MoveAction] Target object is null");
            return false;
        }

        Debug.Log($"[MoveAction] Moving {target.name} with param: {param}");
        try
        {
            var data = JsonConvert.DeserializeObject<ActionData>(param);

            // Move object smoothly on main thread
            Vector3 destinationVector = data.destination; // Implicit conversion
            float startTime = Time.time;

            while (Vector3.Distance(target.transform.position, destinationVector) > 0.1f
                    && !cancellationToken.IsCancellationRequested)
            {
                target.transform.position = Vector3.MoveTowards(
                    target.transform.position,
                    destinationVector,
                    Time.deltaTime * 2.0f
                );
                // Wait for next frame on main thread
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                // Timeout after 10 seconds
                if (Time.time - startTime > 10f)
                {
                    Debug.LogWarning($"[MoveAction] Timeout moving {target.name}");
                    break;
                }
            }

            context.UptateContext();
            Debug.Log($"[MoveAction] Completed moving {target.name} to {destinationVector}");
            return true;

        }
        catch (Exception e)
        {
            Debug.LogError($"[MoveAction] Failed to parse action param: {e.Message}");
            return false;
        }
    }

    public string GetActionDescription()
    {
        return $"{ActionId}: {Description}. Parameters: {ParamDescription}";
    }
}
