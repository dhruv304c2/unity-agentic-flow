using UnityEngine;

[CreateAssetMenu(fileName = "GeminiAPIConfig", menuName = "AgenticFlow/Gemini API Config", order = 1)]
public class GeminiAPIConfig : ScriptableObject
{
    [Header("API Configuration")]
    [Tooltip("Your Gemini API key from Google AI Studio")]
    [SerializeField] private string apiKey = "";

    [Tooltip("Gemini model to use (e.g., gemini-1.5-flash, gemini-1.5-pro)")]
    [SerializeField] private string model = "gemini-1.5-flash";  // Free tier model

    [Tooltip("Maximum tokens for response")]
    [SerializeField] private int maxOutputTokens = 1000;

    [Tooltip("Temperature for response randomness (0.0 - 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float temperature = 0.7f;

    [Header("Security")]
    [Tooltip("Hide API key in inspector (still saved in asset)")]
    [SerializeField] private bool hideApiKey = true;

    // Properties to access private fields
    public string ApiKey => apiKey;
    public string Model => model;
    public int MaxOutputTokens => maxOutputTokens;
    public float Temperature => temperature;

    // Method to set API key (useful for runtime configuration)
    public void SetApiKey(string key)
    {
        apiKey = key;
    }

    // Custom inspector behavior
    private void OnValidate()
    {
        maxOutputTokens = Mathf.Clamp(maxOutputTokens, 1, 8192);
        temperature = Mathf.Clamp01(temperature);
    }

    // Override ToString to avoid accidentally logging the API key
    public override string ToString()
    {
        return $"GeminiAPIConfig(Model: {model}, MaxTokens: {maxOutputTokens}, Temp: {temperature})";
    }
}