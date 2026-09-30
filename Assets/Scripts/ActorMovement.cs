// AI assistance: Aarya implemented the original controller with ChatGPT guidance.
// Codex added the Rigidbody requirement, focus reset, and completion guard.
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ActorMovement : MonoBehaviour
{
    [SerializeField] private float moveStrength = 20f; //controls how much force we apply

    private Rigidbody _body; //holds a reference to the actors existing rigidbody

    private Vector3 _moveInput; //will remember the direction requested by the keyboard
    [SerializeField] private ChamberRun run;

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        _moveInput = Vector3.zero;

        if (run != null && run.HasSucceeded)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.wKey.isPressed)
            _moveInput.z += 1f;

        if (keyboard.sKey.isPressed)
            _moveInput.z -= 1f;

        if (keyboard.aKey.isPressed)
            _moveInput.x -= 1f;

        if (keyboard.dKey.isPressed)
            _moveInput.x += 1f;
    }

    void FixedUpdate()
    {
        if (run != null && run.HasSucceeded)
            return;

        // ForceMode.Force already accounts for the physics timestep and body mass.
        _body.AddForce(_moveInput.normalized * moveStrength, ForceMode.Force);
    }

    private void OnDisable() => _moveInput = Vector3.zero;

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            _moveInput = Vector3.zero;
    }
}
