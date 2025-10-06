using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Talker : MonoBehaviour {
    [SerializeField] GameObject talkPanel;
    [SerializeField] TextMeshProUGUI talkText;
    [SerializeField] Emoter emoter;
    [SerializeField] float typeSpeed = 0.05f; // seconds per character

    bool _isTalking;

    public async UniTask Talk(string line) {
        await UniTask.WaitWhile(() => _isTalking);
        _isTalking = true;

        talkPanel.SetActive(true);
        talkText.text = "";

        // Typewriter effect
        for (int i = 0; i < line.Length; i++) {
            talkText.text += line[i];
            await UniTask.Delay((int)(typeSpeed * 1000));
        }

        // Wait briefly before closing
        await UniTask.WaitForSeconds(2f);

        talkPanel.SetActive(false);
        emoter.ResetEmote().Forget();
        _isTalking = false;
    }
}
