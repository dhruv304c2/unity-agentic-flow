using Cysharp.Threading.Tasks;
using UnityEngine;

public class Presenter : MonoBehaviour{
    [SerializeField] Transform _presentPoint;

    public async UniTask Present(string target){
        Transform itemToPresent = GameObject.Find(target).transform;
        Vector3 orignalPos = itemToPresent.position;
        Quaternion originalRot = itemToPresent.rotation;

        float duration = 0.5f; // seconds
        float elapsed = 0f;

        while(elapsed < duration){
            float t = elapsed / duration;
            itemToPresent.position = Vector3.Lerp(orignalPos, _presentPoint.transform.position, t);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }

        itemToPresent.position = _presentPoint.transform.position;

        //Rotate the presenter object in place at present point
        var rotationDuration = 5.0f; // seconds
        var startTime = Time.time;
        while(Time.time - startTime < rotationDuration){
            itemToPresent.Rotate(Vector3.up, 90f * Time.deltaTime);
            await UniTask.Yield();
        }

        // Return to start position and rotation
        elapsed = 0f;
        var startPositionReturn = itemToPresent.position;
        while(elapsed < duration){
            float t = elapsed / duration;
            itemToPresent.position = Vector3.Lerp(startPositionReturn, orignalPos, t);
            itemToPresent.rotation = Quaternion.Slerp(itemToPresent.rotation, originalRot, t);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
    }
}
