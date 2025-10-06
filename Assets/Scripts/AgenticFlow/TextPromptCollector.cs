using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

public class TextPromptCollector : MonoBehaviour, IPromptCollector<LLMPromptData>{
    [Header("UI References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;

    [Header("Optional UI Elements")]
    [SerializeField] private TMP_Text placeholderText;
    [SerializeField] private int characterLimit = 500;

    public Action OnNewPrompt { get; set; }

    private UniTaskCompletionSource<LLMPromptData> currentPromptTask;
    private CancellationTokenSource cancellationTokenSource;
    private LLMPromptData cachedPrompt;
    private bool hasNewPrompt = false;

    private void Awake(){
        // Set character limit if specified
        if(inputField != null && characterLimit > 0){
            inputField.characterLimit = characterLimit;
        }

        // Set placeholder text if provided
        if(inputField != null && placeholderText != null){
            inputField.placeholder = placeholderText;
        }
    }

    private void OnEnable(){
        // Subscribe to submit button click
        if(submitButton != null){
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
        }

        // Subscribe to input field submit (Enter key)
        if(inputField != null){
            inputField.onSubmit.AddListener(OnInputFieldSubmit);
        }
    }

    private void OnDisable(){
        // Unsubscribe from events
        if(submitButton != null){
            submitButton.onClick.RemoveListener(OnSubmitButtonClicked);
        }

        if(inputField != null){
            inputField.onSubmit.RemoveListener(OnInputFieldSubmit);
        }

        // Cancel any pending prompt collection
        cancellationTokenSource?.Cancel();
    }

    public async UniTask<LLMPromptData> CollectPrompt(){
        // If we have a cached prompt ready, return it immediately
        if(hasNewPrompt && cachedPrompt != null){
            hasNewPrompt = false;
            Debug.Log($"[TextPromptCollector] Returning cached prompt: {cachedPrompt.ToString()}");

            // Re-enable UI for next input
            if(inputField != null){
                inputField.interactable = true;
                inputField.text = string.Empty;
            }
            if(submitButton != null){
                submitButton.interactable = true;
            }

            return cachedPrompt;
        }

        // No prompt available - return null instead of waiting
        Debug.Log("[TextPromptCollector] No prompt available");
        return null;
    }

    private void OnSubmitButtonClicked(){
        SubmitPrompt();
    }

    private void OnInputFieldSubmit(string text){
        SubmitPrompt();
    }

    private void SubmitPrompt(){
        if(inputField == null || string.IsNullOrWhiteSpace(inputField.text)){
            return;
        }

        Debug.Log($"[TextPromptCollector] Submitting prompt: {inputField.text}");

        // Create prompt data
        var promptData = new LLMPromptData();
        promptData.ReadFrom(inputField.text);

        // Cache the prompt
        cachedPrompt = promptData;
        hasNewPrompt = true;

        // Disable UI during processing
        if(inputField != null){
            inputField.interactable = false;
        }

        if(submitButton != null){
            submitButton.interactable = false;
        }

        // Complete any waiting task
        currentPromptTask?.TrySetResult(promptData);

        // Invoke callback to trigger the agent
        OnNewPrompt?.Invoke();
    }

    // Helper method to set prompt programmatically
    public void SetPromptText(string text){
        if(inputField != null){
            inputField.text = text;
        }
    }

    // Helper method to focus the input field
    public void FocusInput(){
        if(inputField != null){
            inputField.Select();
            inputField.ActivateInputField();
        }
    }
}

