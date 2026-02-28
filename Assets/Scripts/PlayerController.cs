using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Vanguard
{
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private float speed = 5;
        [SerializeField] private Vector3 cameraOffset = new(0, 13, -4.5f);
        [SerializeField] private float cameraAngle = -75;
        [SerializeField] private MeshRenderer renderer;
        
        private CharacterController _controller;
        private Camera _camera;

        private SyncVar<Color> _color = new (Color.deepPink);

        private void Awake()
        {
            enabled = false;
        }

        protected override void OnSpawned(bool asServer)
        {
            if (asServer) return;
            
            _controller = GetComponent<CharacterController>();
            _camera = Camera.main;

            
            if (!_camera || !_controller)
            {
                Debug.LogError("Count not initialize player due to missing references");
            }
            
            _color.onChanged += UpdateColor;
            enabled = true;
            UpdateColor(_color.value);
        }
        
        public void SetTeam( Team team)
        {
            _color.value = team == Team.Marines ? Color.blue : Color.red;
        }

        private void UpdateColor(Color current)
        {
            renderer.material.color = current;
        }
        
        private void Update()
        {
            if (!isOwner) return;
            
            var x = Input.GetAxis("Horizontal");
            var y = Input.GetAxis("Vertical");

            var move = Vector3.ClampMagnitude(new Vector3(x, 0, y), 1) * speed;

            _camera.transform.position = transform.position + cameraOffset;
            _camera.transform.rotation = Quaternion.Euler(cameraAngle, 0, 0);
            
            if (Physics.Raycast(_camera.ScreenPointToRay(Mouse.current.position.ReadValue()), out var hit, 100))
            {
                var target = new Vector3(hit.point.x, transform.position.y, hit.point.z);
                transform.LookAt(target);
            }

            _controller.SimpleMove(move);
        }
    }
}