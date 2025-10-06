using UnityEngine;

public class Emoter : MonoBehaviour{
    [SerializeField] SkinnedMeshRenderer _skinnedMeshRenderer;

    public void Emote(EmoteActionParams param){
	_skinnedMeshRenderer.SetBlendShapeWeight(0, param.blendShapeBlink * 100f);
    }
}
