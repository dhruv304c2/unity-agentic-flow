using UnityEngine;
using UnityEngine.AI;

public class Mover : MonoBehaviour{
    public bool isMoving = false;
    [SerializeField] Animator _animator;
    [SerializeField] float moveSpeed = 2.0f;
    [SerializeField] NavMeshAgent _navMeshAgent;
    Camera _mainCamera;

    public float MoveSpeed => moveSpeed;
    public NavMeshAgent NavMeshAgent => _navMeshAgent;

    void Start(){
	_mainCamera = Camera.main;

	// Get NavMeshAgent if not assigned
	if(_navMeshAgent == null){
	    _navMeshAgent = GetComponent<NavMeshAgent>();
	}

	// Configure NavMeshAgent
	if(_navMeshAgent != null){
	    _navMeshAgent.speed = moveSpeed;
	}
    }

    void Update(){
	// Update isMoving based on NavMeshAgent velocity
	if(_navMeshAgent != null && _navMeshAgent.enabled){
	    isMoving = _navMeshAgent.velocity.magnitude > 0.1f && !_navMeshAgent.isStopped;
	}

	if(!isMoving){
	    Vector3 lookPos = _mainCamera.transform.position - transform.position;
	    lookPos.y = 0;
	    Quaternion rotation = Quaternion.LookRotation(lookPos);
	    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 2.0f);
	    _animator.SetBool("isMoving", false);
	}else{
	    _animator.SetBool("isMoving", true);
	}
    }
}
