using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TempForVideo
{
    // Throwaway tutorial-style controller for a video placeholder:
    // WASD moves, mouse turns (yaw on body, pitch on camera pivot),
    // Space jumps, Escape frees the cursor. Not part of the game.
    [RequireComponent(typeof(CharacterController))]
    public sealed class CubeController : MonoBehaviour
    {
        [SerializeField] Transform cameraPivot;
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float jumpHeight = 1.2f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float lookSensitivity = 2f;
        [SerializeField] float minPitch = -30f;
        [SerializeField] float maxPitch = 60f;

        CharacterController body;
        float verticalSpeed;
        float pitch = 15f;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            if (cameraPivot == null) cameraPivot = transform.Find("CameraPivot");
        }

        void OnEnable() => Cursor.lockState = CursorLockMode.Locked;

        void Update()
        {
            if (EscapePressed()) Cursor.lockState = CursorLockMode.None;

            Vector2 look = ReadLook() * lookSensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            Vector2 input = ReadMove();
            Vector3 move = transform.right * input.x + transform.forward * input.y;
            move = Vector3.ClampMagnitude(move, 1f) * moveSpeed;

            if (body.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            if (body.isGrounded && JumpPressed())
                verticalSpeed = Mathf.Sqrt(2f * jumpHeight * -gravity);
            verticalSpeed += gravity * Time.deltaTime;

            move.y = verticalSpeed;
            body.Move(move * Time.deltaTime);
        }

        // Works with either input backend; new Input System wins if both.
#if ENABLE_INPUT_SYSTEM
        static Vector2 ReadMove()
        {
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }

        // Pixel delta scaled to roughly match legacy "Mouse X/Y" axes.
        static Vector2 ReadLook() =>
            Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.05f
                                  : Vector2.zero;

        static bool JumpPressed() =>
            Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        static bool EscapePressed() =>
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        static Vector2 ReadMove() =>
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        static Vector2 ReadLook() =>
            new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        static bool JumpPressed() => Input.GetKeyDown(KeyCode.Space);

        static bool EscapePressed() => Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
