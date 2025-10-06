using Cysharp.Threading.Tasks;
using UnityEngine;

public class Emoter : MonoBehaviour{
    [SerializeField] SkinnedMeshRenderer _skinnedMeshRenderer;

    public async UniTask Emote(EmoteActionParams param){
        _skinnedMeshRenderer.SetBlendShapeWeight(0, param.blendShapeBlink * 100f);
        _skinnedMeshRenderer.SetBlendShapeWeight(1, param.blendShapeMouthA * 100f);
        _skinnedMeshRenderer.SetBlendShapeWeight(2, param.blendShapeMouthI * 100f);
        _skinnedMeshRenderer.SetBlendShapeWeight(3, param.blendShapeMouthU * 100f);
        _skinnedMeshRenderer.SetBlendShapeWeight(4, param.blendShapeMouthE * 100f);
        _skinnedMeshRenderer.SetBlendShapeWeight(5, param.blendShapeMouthO * 100f);
        _skinnedMeshRenderer.SetBlendShapeWeight(6, param.blendShapeJoy * 100f);
        _skinnedMeshRenderer.SetBlendShapeWeight(7, param.blendShapeAnger * 100f);
        _skinnedMeshRenderer.SetBlendShapeWeight(8, param.blendShapeSorrow * 100f);
         _skinnedMeshRenderer.SetBlendShapeWeight(8, param.blendShapeFun * 100f);
    }

    public void ResetEmote(){
        for (int i = 0; i < _skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
        {
            _skinnedMeshRenderer.SetBlendShapeWeight(i, 0f);
        }
    }
}
