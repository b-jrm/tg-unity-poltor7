using UnityEngine;
using UnityEngine.InputSystem;

namespace Gamekit2D
{
    public class StartScreenSpriteOffsetter : MonoBehaviour {

        public float spriteOffset;
        Vector3 initialPosition;
        Vector3 newPosition;

        private void Start()
        {
            initialPosition = transform.position;
        }

        void Update ()
        {
            if (Mouse.current != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                transform.position = new Vector3((initialPosition.x + (spriteOffset * mousePosition.x)), (initialPosition.y + (spriteOffset * mousePosition.y)), initialPosition.z);
            }
        }
    }
}