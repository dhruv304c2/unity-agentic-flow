using UnityEngine;

public class Mover : MonoBehaviour{
    public bool isMoving = false;
    Camera _mainCamera;

    void Start(){
	_mainCamera = Camera.main;
    }

    void Update(){
	if(!isMoving){
	    Vector3 lookPos = _mainCamera.transform.position - transform.position;
	    lookPos.y = 0;
	    Quaternion rotation = Quaternion.LookRotation(lookPos);
	    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 2.0f);
	}
    }
}
