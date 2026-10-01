using UnityEngine;

namespace Tanks
{
    public class TankTurret : MonoBehaviour
    {
        [SerializeField] private TankInputSource _input;
        [SerializeField] private Camera _camera;
        [SerializeField] private float _aimHeight = 0.6f;
        [SerializeField] private float _turnSpeed = 12f;

        private void Reset()
        {
            _camera = Camera.main;
            _input = GetComponentInParent<TankInputSource>();
        }

        private void Awake()
        {
            if (_input == null)
            {
                _input = GetComponentInParent<TankInputSource>();
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            if (_camera == null || _input == null)
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(_input.PointerPosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, _aimHeight, 0f));

            if (!groundPlane.Raycast(ray, out float distance))
            {
                return;
            }

            Vector3 target = ray.GetPoint(distance) - transform.position;
            target.y = 0f;

            if (target.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(target.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);
        }
    }
}