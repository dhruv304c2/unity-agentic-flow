using System;
using UnityEngine;
using UnityEngine.AI;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;

[Serializable]
public class MoveAction : IAction<DescritpiveContextData>{
    public string ActionId => "move";
    public string Description => "Move a game object to a specified position in 3D space";
    public ActionParameter[] Parameters => new ActionParameter[]
    {
        new ActionParameter("x", "float", "The target X coordinate"),
        new ActionParameter("y", "float", "The target Y coordinate"),
        new ActionParameter("z", "float", "The target Z coordinate")
    };

    public class MoveActionData
    {
        public float x;
        public float y;
        public float z;
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
        IContextManager<DescritpiveContextData> context,
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
            var data = JsonConvert.DeserializeObject<MoveActionData>(param);
            if (data == null)
            {
                Debug.LogError($"[MoveAction] Failed to parse action param");
                return false;
            }
            var mover = target.GetComponent<Mover>();
            if (mover == null)
            {
                Debug.LogError($"[MoveAction] Mover component not found on target {target.name}");
                return false;
            }

            var navAgent = mover.NavMeshAgent;
            if (navAgent == null || !navAgent.enabled)
            {
                Debug.LogError($"[MoveAction] NavMeshAgent not found or disabled on target {target.name}");
                return false;
            }

            // Get the desired destination
            Vector3 destinationVector = new Vector3(data.x, data.y, data.z);

            // Find the nearest point on NavMesh to the destination
            NavMeshHit hit;
            float maxDistance = 10f; // Maximum distance to search for a valid point
            if (!NavMesh.SamplePosition(destinationVector, out hit, maxDistance, NavMesh.AllAreas))
            {
                Debug.LogWarning($"[MoveAction] Could not find valid NavMesh position near {destinationVector}");
                return false;
            }

            Vector3 finalDestination = hit.position;
            Debug.Log($"[MoveAction] Moving {target.name} to nearest reachable point: {finalDestination} (requested: {destinationVector})");

            // Set the destination
            if (!navAgent.SetDestination(finalDestination))
            {
                Debug.LogError($"[MoveAction] Failed to set destination for {target.name}");
                return false;
            }

            // Wait for the agent to reach destination
            float startTime = Time.time;
            float stoppingDistance = navAgent.stoppingDistance > 0 ? navAgent.stoppingDistance : 0.5f;

            while (!cancellationToken.IsCancellationRequested)
            {
                // Check if we've reached the destination
                if (!navAgent.pathPending && navAgent.remainingDistance <= stoppingDistance)
                {
                    // Check if agent has stopped moving
                    if (navAgent.velocity.magnitude < 0.1f)
                    {
                        break;
                    }
                }

                // Wait for next frame
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                // Timeout after 30 seconds (increased for longer paths)
                if (Time.time - startTime > 30f)
                {
                    Debug.LogWarning($"[MoveAction] Timeout moving {target.name}");
                    navAgent.isStopped = true;
                    break;
                }
            }

            // Stop the agent
            navAgent.isStopped = true;

            Debug.Log($"[MoveAction] Completed moving {target.name} to {target.transform.position}");
            return true;

        }
        catch (Exception e)
        {
            Debug.LogError($"[MoveAction] Failed to parse action param: {e.Message}");
            return false;
        }
    }
}
