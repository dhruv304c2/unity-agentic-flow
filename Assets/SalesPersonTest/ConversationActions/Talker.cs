using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Talker : MonoBehaviour{
    [SerializeField] GameObject talkPanel;
    [SerializeField] TextMeshProUGUI talkText;
    [SerializeField] Emoter emoter;

    public async UniTask Talk(string line){
	talkPanel.SetActive(true);
	talkText.text = line;
	await UniTask.WaitForSeconds(line.Length * 0.05f);
	talkPanel.SetActive(false);
	emoter.ResetEmote();
    }
}
