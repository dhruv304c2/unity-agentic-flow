using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Talker : MonoBehaviour {
    [SerializeField] GameObject talkPanel;
    [SerializeField] TextMeshProUGUI talkText;
    [SerializeField] Emoter emoter;
    [SerializeField] float typeSpeed = 0.05f; // seconds per character
    [SerializeField] Animator _animator;

    bool _isTalking;

    public async UniTask Talk(string line) {
        await UniTask.WaitWhile(() => _isTalking);
        _isTalking = true;

        talkPanel.SetActive(true);
        talkText.text = "";

        if(_animator){
            _animator.SetLayerWeight(1,1.0f);
        }

        // Typewriter effect
        for (int i = 0; i < line.Length; i++) {
            talkText.text += line[i];
            await UniTask.Delay((int)(typeSpeed * 1000));
        }

        // Wait briefly before closing
        await UniTask.WaitForSeconds(2f);

        talkPanel.SetActive(false);
        if(_animator){
            _animator.SetLayerWeight(1,0.0f);
        }
        emoter.ResetEmote().Forget();
        _isTalking = false;
    }
}
