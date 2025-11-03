using UnityEngine;

public class TrackTarget : MonoBehaviour
{
    [SerializeField] private GameObject car;
    [SerializeField] private float cameraFollowSpeed = 0.8f;
    [SerializeField] private float cameraRotateSpeed = 0.05f;

    private float _fixedCameraXRotation; 
    private Vector3 _localOffset;
    
    private void Start()
    {
        _localOffset = car.transform.InverseTransformPoint(transform.position);
        _fixedCameraXRotation = transform.rotation.eulerAngles.x;
    }
    
    private void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, car.transform.TransformPoint(_localOffset), cameraFollowSpeed);
        
        Quaternion targetRotation = car.transform.rotation;
        Vector3 targetEuler = targetRotation.eulerAngles;
        targetEuler.x = _fixedCameraXRotation; // Lock X rotation
        
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(targetEuler), cameraRotateSpeed);
    }
}