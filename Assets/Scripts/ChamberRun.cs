// AI assistance: Codex generated the unlock state, deposit validation, timer, and restart logic.
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// The single unlock state is shared by the sensor, deposit trigger, and HUD.
public class ChamberRun : MonoBehaviour
{
    [SerializeField] private GameObject roadblock;
    [SerializeField] private Rigidbody payload;
    [SerializeField] private string payloadTag = "Payload";

    private bool _unlocked;
    public bool IsUnlocked => _unlocked && roadblock != null && !roadblock.activeSelf;
    public bool HasSucceeded { get; private set; }
    public double TimeToSuccess { get; private set; }
    public double ElapsedSeconds => HasSucceeded ? TimeToSuccess : Time.timeSinceLevelLoadAsDouble;
    public event Action<double> PayloadSucceeded;

    private void Awake()
    {
        _unlocked = false;
        HasSucceeded = false;
        TimeToSuccess = 0;
        if (roadblock != null)
            roadblock.SetActive(true);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Restart();
    }

    public bool UnlockRoadblock()
    {
        if (_unlocked || HasSucceeded || roadblock == null)
            return false;

        // Disabling the whole door removes its visible mesh AND solid collider.
        roadblock.SetActive(false);
        _unlocked = true;
        Debug.Log("Roadblock removed: the Actor activated the sensor.", this);
        return true;
    }

    public bool TryDeposit(Collider other)
    {
        // A different object, including the Actor, cannot complete the chamber.
        if (HasSucceeded || other == null || payload == null ||
            other.attachedRigidbody != payload || !payload.CompareTag(payloadTag))
            return false;

        if (!IsUnlocked)
        {
            Debug.Log("Deposit rejected: unlock the roadblock, then re-enter the DepositZone.", this);
            return false;
        }

        // This scene is the first build scene. Its clock starts at Play, not unlock.
        // Reloading with R starts a fresh run and a fresh scene clock.
        TimeToSuccess = Time.timeSinceLevelLoadAsDouble;
        HasSucceeded = true;
        Debug.Log($"Payload deposited successfully in {TimeToSuccess:F2} seconds since Play / restart.", this);
        PayloadSucceeded?.Invoke(TimeToSuccess);
        return true;
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
