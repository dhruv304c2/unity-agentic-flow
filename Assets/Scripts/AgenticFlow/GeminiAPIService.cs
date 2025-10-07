using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;

// Gemini API request/response structures
[Serializable]
public class GeminiRequest
{
    public Content[] contents;
    public GenerationConfig generationConfig;

    [Serializable]
    public class Content
    {
        public Part[] parts;
        public string role; // "user" or "model"

        [Serializable]
        public class Part
        {
            public string text;
        }
    }

    [Serializable]
    public class GenerationConfig
    {
        public float temperature;
        public int maxOutputTokens;
    }
}

[Serializable]
public class GeminiResponse
{
    public Candidate[] candidates;

    [Serializable]
    public class Candidate
    {
        public Content content;

        [Serializable]
        public class Content
        {
            public Part[] parts;
            public string role;

            [Serializable]
            public class Part
            {
                public string text;
            }
        }
    }
}

public class GeminiAPIService : MonoBehaviour
{
    [SerializeField] private GeminiAPIConfig config;

    [Header("Chat History")]
    [SerializeField] private int maxHistoryLength = 20;

    private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1/models/{0}:generateContent?key={1}";

    // Session history management
    private List<GeminiRequest.Content> sessionHistory = new List<GeminiRequest.Content>();
    private bool systemPromptInitialized = false;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("[GeminiAPI] No API config assigned! Please assign a GeminiAPIConfig asset in the inspector.");
            return;
        }

        if (string.IsNullOrEmpty(config.ApiKey))
        {
            Debug.LogWarning("[GeminiAPI] API key is not set in the config! Please set it in the GeminiAPIConfig asset.");
        }
    }

    public async UniTask<string> GenerateContent(string prompt)
    {
        if (config == null)
        {
            Debug.LogError("[GeminiAPI] No API config assigned!");
            return null;
        }

        if (string.IsNullOrEmpty(config.ApiKey))
        {
            Debug.LogError("[GeminiAPI] API key is not set!");
            return null;
        }

        // Construct the API URL
        string url = string.Format(GEMINI_API_URL, config.Model, config.ApiKey);

        // Create user message
        var userContent = new GeminiRequest.Content
        {
            role = "user",
            parts = new[]
            {
                new GeminiRequest.Content.Part { text = prompt }
            }
        };

        // Build contents array with history + new user message
        var contents = new List<GeminiRequest.Content>(sessionHistory);
        contents.Add(userContent);

        // Create the request body
        var request = new GeminiRequest
        {
            contents = contents.ToArray(),
            generationConfig = new GeminiRequest.GenerationConfig
            {
                temperature = config.Temperature,
                maxOutputTokens = config.MaxOutputTokens
            }
        };

        string jsonRequest = JsonConvert.SerializeObject(request);
        Debug.Log($"[GeminiAPI] Sending request: {jsonRequest}");

        // Create UnityWebRequest
        using (var webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            // Send request and wait for response
            await webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GeminiAPI] Error: {webRequest.error}");
                Debug.LogError($"[GeminiAPI] Response: {webRequest.downloadHandler.text}");
                return null;
            }

            // Parse response
            try
            {
                var response = JsonConvert.DeserializeObject<GeminiResponse>(webRequest.downloadHandler.text);
                if (response?.candidates != null && response.candidates.Length > 0)
                {
                    var text = response.candidates[0].content.parts[0].text;
                    Debug.Log($"[GeminiAPI] Response received: {text}");

                    // Add user message and model response to session history
                    sessionHistory.Add(userContent);

                    var modelContent = new GeminiRequest.Content
                    {
                        role = "model",
                        parts = new[]
                        {
                            new GeminiRequest.Content.Part { text = text }
                        }
                    };
                    sessionHistory.Add(modelContent);

                    // Trim history if it exceeds max length
                    TrimHistory();

                    return text;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GeminiAPI] Failed to parse response: {e.Message}");
                Debug.LogError($"[GeminiAPI] Raw response: {webRequest.downloadHandler.text}");
            }
        }

        return null;
    }

    public string FormatAgentPrompt(
            string userPrompt,
            string context,
            List<IAction<DescritpiveContextData>> availableActions
        )
    {
        // Initialize system prompt on first use
        InitializeSystemPrompt(availableActions);

        var sb = new StringBuilder();

        sb.AppendLine("Current scene context:");
        sb.AppendLine(context);
        sb.AppendLine("\nUser request:");
        sb.AppendLine(userPrompt);

        return sb.ToString();
    }

    // History management methods
    private void TrimHistory()
    {
        // Keep only the last maxHistoryLength messages
        if (sessionHistory.Count > maxHistoryLength)
        {
            int itemsToRemove = sessionHistory.Count - maxHistoryLength;
            sessionHistory.RemoveRange(0, itemsToRemove);
            Debug.Log($"[GeminiAPI] Trimmed history to {maxHistoryLength} messages");
        }
    }

    public void ClearHistory()
    {
        sessionHistory.Clear();
        systemPromptInitialized = false;
        Debug.Log("[GeminiAPI] Session history cleared");
    }

    public int GetHistoryCount()
    {
        return sessionHistory.Count;
    }

    public void SetMaxHistoryLength(int newLength)
    {
        maxHistoryLength = Mathf.Max(0, newLength);
        TrimHistory();
    }

    // Get formatted history for debugging
    public string GetFormattedHistory()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Session History ===");
        foreach (var content in sessionHistory)
        {
            sb.AppendLine($"[{content.role}]: {content.parts[0].text}");
            sb.AppendLine("---");
        }
        return sb.ToString();
    }

    private string CreateSystemPrompt(List<IAction<DescritpiveContextData>> availableActions)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are an AI agent that can control objects in a Unity scene.");

        sb.AppendLine("\n=== IMPORTANT EXECUTION BEHAVIOR ===");
        sb.AppendLine("You must return an ARRAY OF ARRAYS. Each inner array is a sequence that executes sequentially.");
        sb.AppendLine("Multiple sequences (outer arrays) execute IN PARALLEL (simultaneously).");
        sb.AppendLine("");
        sb.AppendLine("- Actions within a sequence execute one after another (sequential)");
        sb.AppendLine("- Different sequences execute at the same time (parallel)");
        sb.AppendLine("- This allows complex choreography: e.g., Cube1 moves in a path while Cube2 simultaneously moves in a different path");

        sb.AppendLine("\n=== AVAILABLE ACTIONS ===");
        foreach (var action in availableActions)
        {
            sb.AppendLine($"- {action.GetActionDescription()}");
        }

        sb.AppendLine("\n=== RESPONSE FORMAT ===");
        sb.AppendLine("Always respond with a JSON ARRAY OF ARRAYS. Each inner array contains actions to execute sequentially.");
        sb.AppendLine("Each action must have:");
        sb.AppendLine("- actionId: the action to perform");
        sb.AppendLine("- targetId: the name of the GameObject to perform the action on");
        sb.AppendLine("- param: action-specific parameters as a JSON object (not a string)");

        sb.AppendLine("\n=== IMPORTANT: PARAM FIELD FORMAT ===");
        sb.AppendLine("The param field must contain a JSON object directly, NOT a stringified JSON.");
        sb.AppendLine("- Use regular JSON syntax for the param object");
        sb.AppendLine("- Do NOT wrap the param value in quotes");
        sb.AppendLine("- The param is a nested JSON object within each action");

        sb.AppendLine("\nExample - Two objects moving in parallel paths:");
        sb.AppendLine("[");
        sb.AppendLine("  [");
        sb.AppendLine("    {\"actionId\":\"move\",\"targetId\":\"Cube1\",\"param\":{\"x\":-2.5,\"y\":0,\"z\":3.0}},");
        sb.AppendLine("    {\"actionId\":\"move\",\"targetId\":\"Cube1\",\"param\":{\"x\":-2.5,\"y\":0,\"z\":-3.0}}");
        sb.AppendLine("  ],");
        sb.AppendLine("  [");
        sb.AppendLine("    {\"actionId\":\"move\",\"targetId\":\"Cube2\",\"param\":{\"x\":2.5,\"y\":0,\"z\":-3.0}},");
        sb.AppendLine("    {\"actionId\":\"move\",\"targetId\":\"Cube2\",\"param\":{\"x\":2.5,\"y\":0,\"z\":3.0}}");
        sb.AppendLine("  ]");
        sb.AppendLine("]");

        sb.AppendLine("\nExample - Sequential movement for one object:");
        sb.AppendLine("[");
        sb.AppendLine("  [");
        sb.AppendLine("    {\"actionId\":\"move\",\"targetId\":\"Cube1\",\"param\":{\"x\":0,\"y\":0,\"z\":5}},");
        sb.AppendLine("    {\"actionId\":\"move\",\"targetId\":\"Cube1\",\"param\":{\"x\":5,\"y\":0,\"z\":5}},");
        sb.AppendLine("    {\"actionId\":\"move\",\"targetId\":\"Cube1\",\"param\":{\"x\":5,\"y\":0,\"z\":0}}");
        sb.AppendLine("  ]");
        sb.AppendLine("]");

        sb.AppendLine("\n=== CRITICAL RULES ===");
        sb.AppendLine("1. ALWAYS return an ARRAY OF ARRAYS (even for single actions: [[{...}]])");
        sb.AppendLine("2. The param field is a JSON object, NOT a string");
        sb.AppendLine("3. Only respond with the raw JSON array, no additional text");
        sb.AppendLine("4. No markdown formatting, no code blocks, no backticks");
        sb.AppendLine("5. Response must start with [ and end with ]");
        sb.AppendLine("6. Each action object must be valid JSON");

        return sb.ToString();
    }

    public void InitializeSystemPrompt(List<IAction<DescritpiveContextData>> availableActions)
    {
        if (!systemPromptInitialized || sessionHistory.Count == 0)
        {
            // Clear history if reinitializing
            if (systemPromptInitialized && sessionHistory.Count > 0)
            {
                sessionHistory.Clear();
            }

            var systemPrompt = CreateSystemPrompt(availableActions);
            var systemContent = new GeminiRequest.Content
            {
                role = "user",
                parts = new[]
                {
                    new GeminiRequest.Content.Part { text = systemPrompt }
                }
            };

            // Add system prompt as first message
            sessionHistory.Insert(0, systemContent);

            // Add a model acknowledgment
            var modelAck = new GeminiRequest.Content
            {
                role = "model",
                parts = new[]
                {
                    new GeminiRequest.Content.Part { text = "Understood. I will control Unity objects using the specified JSON array of arrays format, where each inner array executes sequentially and multiple arrays execute in parallel. The param field will be a JSON object (not a string)." }
                }
            };
            sessionHistory.Insert(1, modelAck);

            systemPromptInitialized = true;
            Debug.Log("[GeminiAPI] System prompt initialized with updated formatting rules");
        }
    }
}
