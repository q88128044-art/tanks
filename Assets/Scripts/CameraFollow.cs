using UnityEngine;

namespace Tanks
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        
        [Header("Position")]
        public Vector3 offset = new Vector3(0f, 12f, -15f);
        public float smoothSpeed = 5f;
        
        [Header("Rotation")]
        public Vector3 rotationOffset = new Vector3(35f, 0f, 0f);
        public bool lookAtTarget = true;
        public float rotationSpeed = 5f;
        
        private Vector3 _currentVelocity;
        
        private void LateUpdate()
        {
            if (target == null) return;
            
            // Position
            Vector3 desiredPosition = target.position + target.rotation * offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, 1f / smoothSpeed);
            
            // Rotation
            if (lookAtTarget)
            {
                Quaternion targetRotation = Quaternion.Euler(rotationOffset) * target.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }
}