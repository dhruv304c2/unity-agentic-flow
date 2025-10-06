using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class AgentController : MonoBehaviour
{
    [Header("Agent Components")]
    [SerializeField] private GeminiAPIService geminiAPIService;
    [SerializeField] private TextPromptCollector promptCollector;
    [SerializeField] private DescriptionContextCollector contextCollector;

    private Agent<LLMPromptData, DescritpiveContextData> agent;
    private CancellationTokenSource cancellationTokenSource;

    private void Start()
    {
        InitializeAgent();
    }

    private void InitializeAgent()
    {
        // Check if components are assigned
        if (geminiAPIService == null)
        {
            geminiAPIService = GetComponent<GeminiAPIService>();
            if (geminiAPIService == null)
            {
                Debug.LogError("[AgentController] GeminiAPIService not found! Please add it to this GameObject.");
                return;
            }
        }

        if (promptCollector == null)
        {
            promptCollector = GetComponent<TextPromptCollector>();
            if (promptCollector == null)
            {
                Debug.LogError("[AgentController] TextPromptCollector not found! Please add it to this GameObject.");
                return;
            }
        }

        if (contextCollector == null)
        {
            contextCollector = GetComponent<DescriptionContextCollector>();
            if (contextCollector == null)
            {
                Debug.LogError("[AgentController] DescriptionContextCollector not found! Please add it to this GameObject.");
                return;
            }
        }

        // Create the model with API service
        var actions = new System.Collections.Generic.List<IAction<DescritpiveContextData>>{
            new MoveAction(),
            new Emote(),
            new TalkAction()
        };

        var model = new GeminiModelAgent(geminiAPIService, actions);

        // Create the agent
        agent = new Agent<LLMPromptData, DescritpiveContextData>(
            model,
            contextCollector,
            promptCollector
        );

        // Start the agent
        StartAgent();
    }

    private void StartAgent()
    {
        cancellationTokenSource = new CancellationTokenSource();
        RunAgentAsync(cancellationTokenSource.Token).Forget();
    }

    private async UniTask RunAgentAsync(CancellationToken cancellationToken)
    {
        Debug.Log("[AgentController] Starting agent...");
        try
        {
            await agent.Run(cancellationToken);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AgentController] Agent error: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            cancellationTokenSource?.Cancel();
        }
        else
        {
            StartAgent();
        }
    }
}
