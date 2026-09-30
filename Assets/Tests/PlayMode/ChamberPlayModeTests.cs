using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// AI assistance: Codex generated these tests. They run the real chamber scene,
// real Rigidbody contacts, and real trigger callbacks in Unity Play Mode.
public class ChamberPlayModeTests
{
    private ChamberRun _run;
    private Rigidbody _actor;
    private Rigidbody _payload;
    private GameObject _door;
    private Keyboard _testKeyboard;
    private Keyboard _previousKeyboard;
    private readonly WaitForFixedUpdate _physicsStep = new WaitForFixedUpdate();

    [UnitySetUp]
    public IEnumerator LoadChamber()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Chamber");
        yield return null;
        yield return _physicsStep;
        CacheScene();
    }

    private void CacheScene()
    {
        _run = GameObject.Find("ChamberRun").GetComponent<ChamberRun>();
        _actor = GameObject.Find("Actor").GetComponent<Rigidbody>();
        _payload = GameObject.Find("Payload").GetComponent<Rigidbody>();
        _door = GameObject.Find("Roadblock");
        _actor.GetComponent<ActorMovement>().enabled = false;
    }

    [TearDown]
    public void Cleanup()
    {
        Time.timeScale = 1;
        if (_testKeyboard != null)
        {
            InputSystem.RemoveDevice(_testKeyboard);
            _testKeyboard = null;
            if (_previousKeyboard != null && _previousKeyboard.added)
                _previousKeyboard.MakeCurrent();
        }
    }

    private void Place(Rigidbody body, Vector3 position)
    {
        body.position = position;
        body.rotation = Quaternion.identity;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.WakeUp();
        Physics.SyncTransforms();
    }

    private IEnumerator Steps(int count)
    {
        for (int i = 0; i < count; i++)
            yield return _physicsStep;
    }

    [UnityTest]
    public IEnumerator WasdKeysMoveInAllFourDirections()
    {
        // A batch test has no focused Game view. Temporarily allow injected
        // input, then restore both settings even if an assertion fails.
        var originalBackground = InputSystem.settings.backgroundBehavior;
        var originalEditorRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        _previousKeyboard = Keyboard.current;
        _testKeyboard = InputSystem.AddDevice<Keyboard>();
        _testKeyboard.MakeCurrent();
        _actor.GetComponent<ActorMovement>().enabled = true;
        Key[] keys = { Key.W, Key.S, Key.A, Key.D };
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        try
        {
            for (int i = 0; i < keys.Length; i++)
            {
                Place(_actor, new Vector3(0, 0.5f, 0));
                InputSystem.QueueStateEvent(_testKeyboard, new KeyboardState(keys[i]));
                yield return null;
                yield return Steps(20);
                Assert.Greater(Vector3.Dot(_actor.position - new Vector3(0, 0.5f, 0), directions[i]), 0.15f, keys[i].ToString());
                InputSystem.QueueStateEvent(_testKeyboard, new KeyboardState());
                yield return null;
            }
        }
        finally
        {
            InputSystem.settings.backgroundBehavior = originalBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorRouting;
        }
    }

    [UnityTest]
    public IEnumerator PayloadCannotActivateSensor()
    {
        Place(_payload, new Vector3(5, 0.5f, 1));
        yield return Steps(5);
        Assert.IsFalse(_run.IsUnlocked);
        Assert.IsTrue(_door.activeSelf);
    }

    [UnityTest]
    public IEnumerator ActorUnlocksRealDoorAndLogsOnlyOnce()
    {
        int unlockLogs = 0;
        Application.LogCallback listener = (message, trace, type) =>
        {
            if (message.StartsWith("Roadblock removed:")) unlockLogs++;
        };
        Application.logMessageReceived += listener;
        try
        {
            Place(_actor, new Vector3(5, 0.5f, 1));
            yield return Steps(5);
            Assert.IsTrue(_run.IsUnlocked);
            Assert.IsFalse(_door.activeSelf);
            Place(_actor, new Vector3(2, 0.5f, 0));
            yield return Steps(3);
            Place(_actor, new Vector3(5, 0.5f, 1));
            yield return Steps(3);
            Assert.AreEqual(1, unlockLogs);
        }
        finally { Application.logMessageReceived -= listener; }
    }

    [UnityTest]
    public IEnumerator DoorPhysicallyBlocksActorBeforeUnlock()
    {
        Place(_actor, new Vector3(-6, 0.5f, 1));
        for (int i = 0; i < 100; i++)
        {
            _actor.AddForce(Vector3.forward * 20, ForceMode.Force);
            yield return _physicsStep;
        }
        Assert.Less(_actor.position.z, 2.1f);
        Assert.IsFalse(_run.IsUnlocked);
    }

    [UnityTest]
    public IEnumerator EarlyDepositIsRejectedAndMustReenterAfterUnlock()
    {
        Place(_payload, new Vector3(-6, 0.5f, 6));
        yield return Steps(5);
        Assert.IsFalse(_run.HasSucceeded);
        Place(_actor, new Vector3(5, 0.5f, 1));
        yield return Steps(5);
        Assert.IsTrue(_run.IsUnlocked);
        Assert.IsFalse(_run.HasSucceeded, "Unlocking while the Payload is already inside must not award success.");
        Place(_payload, new Vector3(-2, 0.5f, 6));
        yield return Steps(3);
        Place(_payload, new Vector3(-6, 0.5f, 6));
        yield return Steps(3);
        Assert.IsTrue(_run.HasSucceeded);
    }

    [UnityTest]
    public IEnumerator ActorCannotCountAsDeposit()
    {
        Place(_actor, new Vector3(5, 0.5f, 1));
        yield return Steps(3);
        Place(_actor, new Vector3(-6, 0.5f, 6));
        yield return Steps(3);
        Assert.IsFalse(_run.HasSucceeded);
    }

    [UnityTest]
    public IEnumerator WrongPayloadTagCannotCountAsDeposit()
    {
        Place(_actor, new Vector3(5, 0.5f, 1));
        yield return Steps(3);
        _payload.tag = "Untagged";
        Place(_payload, new Vector3(-6, 0.5f, 6));
        yield return Steps(3);
        Assert.IsFalse(_run.HasSucceeded);
    }

    [UnityTest]
    public IEnumerator SuccessFiresOnceAndTimerIncludesTimeBeforeUnlock()
    {
        yield return Steps(20);
        double beforeUnlock = _run.ElapsedSeconds;
        int successEvents = 0;
        _run.PayloadSucceeded += elapsed => successEvents++;
        Place(_actor, new Vector3(5, 0.5f, 1));
        yield return Steps(5);
        Place(_payload, new Vector3(-6, 0.5f, 6));
        yield return Steps(5);
        Assert.IsTrue(_run.HasSucceeded);
        Assert.Greater(_run.TimeToSuccess, beforeUnlock);
        double savedTime = _run.TimeToSuccess;
        Place(_payload, new Vector3(-2, 0.5f, 6));
        yield return Steps(5);
        Place(_payload, new Vector3(-6, 0.5f, 6));
        yield return Steps(5);
        Assert.AreEqual(1, successEvents);
        Assert.AreEqual(savedTime, _run.ElapsedSeconds);
    }

    [UnityTest]
    public IEnumerator RestartResetsDoorPayloadAndSuccess()
    {
        Place(_actor, new Vector3(5, 0.5f, 1));
        yield return Steps(3);
        Place(_payload, new Vector3(-6, 0.5f, 6));
        yield return Steps(3);
        Assert.IsTrue(_run.HasSucceeded);
        _run.Restart();
        yield return null;
        yield return Steps(3);
        CacheScene();
        Assert.IsFalse(_run.HasSucceeded);
        Assert.IsFalse(_run.IsUnlocked);
        Assert.IsTrue(_door.activeSelf);
        Assert.Less(Vector3.Distance(_payload.position, new Vector3(-3, 0.5f, -6)), 0.1f);
    }

    [UnityTest]
    public IEnumerator ContinuousPushAndRecordedMassExperiment()
    {
        Time.timeScale = 5;
        var lines = new List<string> { "trial,payload_mass_kg,actor_mass_kg,actor_linear_damping,payload_linear_damping,force_N,duration_simulated_s,payload_distance_m,final_payload_speed_m_per_s,second_half_distance_m" };
        foreach (float mass in new[] { 0.5f, 1f, 3f })
        {
            Place(_actor, new Vector3(-4.05f, 0.5f, -6));
            Place(_payload, new Vector3(-3, 0.5f, -6));
            _payload.mass = mass;
            float halfwayX = _payload.position.x;
            for (int i = 0; i < 100; i++)
            {
                _actor.AddForce(Vector3.right * 20, ForceMode.Force);
                yield return _physicsStep;
                if (i == 49) halfwayX = _payload.position.x;
            }
            float distance = _payload.position.x + 3;
            float lateDistance = _payload.position.x - halfwayX;
            Assert.Greater(distance, 0.5f, $"Payload mass {mass} must be pushable.");
            Assert.Greater(lateDistance, 0.25f, "Pushing must continue without repeated impacts.");
            lines.Add(string.Format(CultureInfo.InvariantCulture,
                "mass_{0}, {0},1,4,0.5,20,2,{1:F4},{2:F4},{3:F4}", mass, distance, _payload.linearVelocity.magnitude, lateDistance));
        }
        string directory = Path.Combine(Application.dataPath, "..", "Validation");
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, "physics-experiment.csv"), lines);
    }

    [UnityTest]
    public IEnumerator FullRoutePushesPayloadThroughBothTurnsAndDeposits()
    {
        Time.timeScale = 5;
        // The Payload is never teleported in this test. All route motion is
        // caused by forces on the Actor and physical contact with the crate.
        yield return DriveActorTo(new Vector3(4.7f, 0.5f, -6));
        yield return DriveActorTo(new Vector3(4.1f, 0.5f, -7.25f));
        yield return DriveActorTo(new Vector3(_payload.position.x, 0.5f, -7.25f));
        yield return DriveActorTo(new Vector3(_payload.position.x, 0.5f, 0.2f));
        Assert.IsTrue(_run.IsUnlocked, $"Actor must cross the sensor during the first turn; Actor {_actor.position}, Payload {_payload.position}.");
        yield return DriveActorTo(new Vector3(7.2f, 0.5f, -1.2f));
        yield return DriveActorTo(new Vector3(7.2f, 0.5f, _payload.position.z));
        yield return DriveActorTo(new Vector3(-5, 0.5f, _payload.position.z));
        yield return DriveActorTo(new Vector3(-4.1f, 0.5f, -1.2f));
        yield return DriveActorTo(new Vector3(_payload.position.x, 0.5f, -1.2f));
        yield return DriveActorTo(new Vector3(_payload.position.x, 0.5f, 5.2f));
        Assert.IsTrue(_run.HasSucceeded, $"Payload ended at {_payload.position}");
    }

    private IEnumerator DriveActorTo(Vector3 target)
    {
        for (int i = 0; i < 1000; i++)
        {
            Vector3 error = target - _actor.position;
            error.y = 0;
            Vector3 velocity = _actor.linearVelocity;
            velocity.y = 0;
            if (error.magnitude < 0.35f && velocity.magnitude < 0.3f)
                yield break;
            _actor.AddForce(Vector3.ClampMagnitude(error * 25 - velocity * 8, 20), ForceMode.Force);
            yield return _physicsStep;
        }
        Assert.Fail($"Could not reach Actor waypoint {target}; Actor {_actor.position}, Payload {_payload.position}.");
    }
}
