using PurrNet;
using UnityEngine;

namespace Vanguard
{
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private float speed = 5;
        [SerializeField] private Transform cameraSocket;
        [SerializeField] private Vector3 cameraOffset = new(0, 13, -4.5f);
        [SerializeField] private float cameraAngle = -75;
        private CharacterController _controller;
        
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }
        
        private void Update()
        {
            if (!isOwner) return;
            
            var x = Input.GetAxis("Horizontal");
            var y = Input.GetAxis("Vertical");

            var move = Vector3.ClampMagnitude(new Vector3(x, 0, y), 1) * speed;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 100))
            {
                var target = new Vector3(hit.point.x, transform.position.y, hit.point.z);
                transform.LookAt(target);
            }

            _controller.SimpleMove(move);
        }

        private void LateUpdate()
        {
            if (!isOwner) return;
            
            if (!Camera.main)
            {
                Debug.LogError("No main camera!");
                return;
            }
    
            Camera.main.transform.SetParent(cameraSocket);
            Camera.main.transform.position = transform.position + cameraOffset;
            Camera.main.transform.rotation = Quaternion.Euler(cameraAngle, 0, 0);
        }
    }
}