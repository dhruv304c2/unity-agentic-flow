using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Talker : MonoBehaviour {
    [SerializeField] GameObject talkPanel;
    [SerializeField] TextMeshProUGUI talkText;
    [SerializeField] Emoter emoter;
    [SerializeField] float typeSpeed = 0.05f; // seconds per character

    bool skip;

    public async UniTask Talk(string line) {
        talkPanel.SetActive(true);
        talkText.text = "";
        skip = false;

        // Run input listener for skip
        var skipListener = UniTask.Create(async () => {
            while (!skip) {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                    skip = true;
                await UniTask.Yield();
            }
        });

        // Typewriter effect
        for (int i = 0; i < line.Length; i++) {
            if (skip) {
                talkText.text = line;
                break;
            }

            talkText.text += line[i];
            await UniTask.Delay((int)(typeSpeed * 1000));
        }

        // Wait briefly before closing
        await UniTask.WaitForSeconds(0.5f);

        talkPanel.SetActive(false);
        emoter.ResetEmote();
    }
}
