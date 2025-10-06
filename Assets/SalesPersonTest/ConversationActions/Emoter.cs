using Cysharp.Threading.Tasks;
using UnityEngine;

public class Emoter : MonoBehaviour{
    [SerializeField] SkinnedMeshRenderer _skinnedMeshRenderer;
    [SerializeField] float _emoteSpeed = 2.0f;

    bool _isEmoting = false;

    public async UniTask Emote(EmoteActionParams param){
        await UniTask.WaitWhile(() => _isEmoting);
        _isEmoting = true;
        while(true){
            bool allAtTarget = true;
            for (int i = 0; i < _skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
            {
                float targetWeight = 0f;
                switch(i){
                    case 0: targetWeight = param.blendShapeBlink * 100f; break;
                    case 1: targetWeight = param.blendShapeMouthA * 100f; break;
                    case 2: targetWeight = param.blendShapeMouthI * 100f; break;
                    case 3: targetWeight = param.blendShapeMouthU * 100f; break;
                    case 4: targetWeight = param.blendShapeMouthE * 100f; break;
                    case 5: targetWeight = param.blendShapeMouthO * 100f; break;
                    case 6: targetWeight = param.blendShapeJoy * 100f; break;
                    case 7: targetWeight = param.blendShapeAnger * 100f; break;
                    case 8: targetWeight = param.blendShapeSorrow * 100f; break;
                    case 9: targetWeight = param.blendShapeFun * 100f; break;
                }
                float currentWeight = _skinnedMeshRenderer.GetBlendShapeWeight(i);
                if(Mathf.Abs(currentWeight - targetWeight) > 0.1f){
                    allAtTarget = false;
                    float newWeight = Mathf.MoveTowards(currentWeight, targetWeight, _emoteSpeed * Time.deltaTime);
                    _skinnedMeshRenderer.SetBlendShapeWeight(i, newWeight);
                }
            }
            if(allAtTarget) break;
            await UniTask.Yield();
        }
        _isEmoting = false;
    }

    public async UniTask ResetEmote(){
        await UniTask.WaitWhile(() => _isEmoting);
        _isEmoting = true;
        while(true){
            bool allAtZero = true;
            for (int i = 0; i < _skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
            {
                float currentWeight = _skinnedMeshRenderer.GetBlendShapeWeight(i);
                if(currentWeight > 0.1f){
                    allAtZero = false;
                    float newWeight = Mathf.MoveTowards(currentWeight, 0f, _emoteSpeed * Time.deltaTime);
                    _skinnedMeshRenderer.SetBlendShapeWeight(i, newWeight);
                }
            }
            if(allAtZero) break;
            await UniTask.Yield();
        }
        _isEmoting = false;
    }
}
