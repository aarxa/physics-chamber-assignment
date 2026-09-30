using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// AI assistance: Codex generated this editor utility to finish and wire Aarya's
// existing maze, preserving its walls, floor, Actor, Payload, and scene asset.
// Gameplay does not depend on this editor script.
public static class ChamberSetup
{
    [MenuItem("Tools/Physics Chamber/Open chamber")]
    public static void OpenForReview()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Chamber.unity");
        var gameView = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
        gameView.Focus();
    }

    [MenuItem("Tools/Physics Chamber/Capture Game preview")]
    public static void CapturePreview()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Enter Play mode before capturing the Game view.");
            return;
        }
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Validation"));
        Directory.CreateDirectory(directory);
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, "scene-preview.png"));
    }

    [MenuItem("Tools/Physics Chamber/Configure chamber scene")]
    public static void Configure()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Chamber.unity");
        EnsureTag("Payload");

        var actor = GameObject.Find("Actor");
        var payload = GameObject.Find("Payload");
        var floor = GameObject.Find("Floor");
        var actorBody = actor.GetComponent<Rigidbody>();
        var payloadBody = payload.GetComponent<Rigidbody>();
        actor.tag = "Player";
        payload.tag = "Payload";
        actor.transform.position = new Vector3(-6, 0.5f, -6);
        payload.transform.position = new Vector3(-3, 0.5f, -6);

        ConfigureBody(actorBody, 1, 4, RigidbodyConstraints.FreezeRotation);
        ConfigureBody(payloadBody, 1, 0.5f,
            RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ);

        var contact = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/ChamberContact.physicMaterial");
        if (contact == null)
        {
            contact = new PhysicsMaterial("ChamberContact");
            AssetDatabase.CreateAsset(contact, "Assets/Materials/ChamberContact.physicMaterial");
        }
        contact.staticFriction = 0.15f;
        contact.dynamicFriction = 0.12f;
        contact.bounciness = 0;
        contact.frictionCombine = PhysicsMaterialCombine.Minimum;
        contact.bounceCombine = PhysicsMaterialCombine.Minimum;
        EditorUtility.SetDirty(contact);
        foreach (var obj in new[] { actor, payload, floor })
        {
            var collider = obj.GetComponent<Collider>();
            collider.enabled = true;
            collider.isTrigger = false;
            collider.sharedMaterial = contact;
        }

        ApplyColor(actor, Material("ActorBlue", new Color(0.08f, 0.57f, 0.95f)));
        ApplyColor(payload, Material("PayloadOrange", new Color(1f, 0.48f, 0.05f)));
        ApplyColor(floor, Material("FloorSlate", new Color(0.11f, 0.15f, 0.22f)));
        var wallMaterial = Material("WallBlueGrey", new Color(0.3f, 0.39f, 0.51f));
        foreach (string name in new[] { "NorthWall", "SouthWall", "EastWall", "WestWall", "LowerDivider", "UpperDivider" })
            ApplyColor(GameObject.Find(name), wallMaterial);

        var door = Cube("Roadblock", new Vector3(-6, 1, 3), new Vector3(4, 2, 1),
            Material("DoorRed", new Color(0.85f, 0.17f, 0.16f)));
        door.GetComponent<BoxCollider>().isTrigger = false;
        var runObject = FindOrCreate("ChamberRun");
        var run = GetOrAdd<ChamberRun>(runObject);
        SetObject(run, "roadblock", door);
        SetObject(run, "payload", payloadBody);
        var movement = actor.GetComponent<ActorMovement>();
        SetObject(movement, "run", run);
        SetFloat(movement, "moveStrength", 20);

        var sensor = FindOrCreate("UnlockSensor");
        sensor.transform.position = new Vector3(5, 0, 1);
        var sensorCollider = GetOrAdd<BoxCollider>(sensor);
        sensorCollider.isTrigger = true;
        sensorCollider.center = new Vector3(0, 0.75f, 0);
        sensorCollider.size = new Vector3(2, 1.5f, 2);
        var sensorPad = Pad(sensor, "SensorPad", new Vector3(2, 0.08f, 2),
            Material("SensorYellow", new Color(1f, 0.76f, 0.08f)));
        var sensorScript = GetOrAdd<UnlockSensor>(sensor);
        SetObject(sensorScript, "run", run);
        SetObject(sensorScript, "actor", actorBody);
        SetObject(sensorScript, "indicator", sensorPad.GetComponent<Renderer>());

        var deposit = FindOrCreate("DepositZone");
        deposit.transform.position = new Vector3(-6, 0, 6);
        var depositCollider = GetOrAdd<BoxCollider>(deposit);
        depositCollider.isTrigger = true;
        depositCollider.center = new Vector3(0, 0.7f, 0);
        depositCollider.size = new Vector3(2.6f, 1.4f, 2.6f);
        Pad(deposit, "DepositPad", new Vector3(2.6f, 0.08f, 2.6f),
            Material("DepositGreen", new Color(0.12f, 0.72f, 0.42f)));
        SetObject(GetOrAdd<DepositZone>(deposit), "run", run);
        SetObject(GetOrAdd<ChamberHUD>(runObject), "run", run);

        var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
        camera.transform.position = new Vector3(0, 18, -12);
        camera.transform.rotation = Quaternion.Euler(56.31f, 0, 0);
        camera.orthographic = true;
        camera.orthographicSize = 11.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
        camera.farClipPlane = 100;

        PlayerSettings.productName = "Physics Chamber";
        PlayerSettings.companyName = "Aarya";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Chamber.unity", true) };
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Chamber setup complete: real door, Actor-only sensor, gated deposit, timer, restart, and tuned contact material.");
    }

    private static void ConfigureBody(Rigidbody body, float mass, float damping, RigidbodyConstraints constraints)
    {
        body.mass = mass;
        body.linearDamping = damping;
        body.angularDamping = 0.05f;
        body.useGravity = true;
        body.isKinematic = false;
        body.constraints = constraints;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private static void EnsureTag(string tag)
    {
        var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = manager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        manager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material Material(string name, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.18f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyColor(GameObject obj, Material material) => obj.GetComponent<Renderer>().sharedMaterial = material;

    private static GameObject FindOrCreate(string name) => GameObject.Find(name) ?? new GameObject(name);
    private static T GetOrAdd<T>(GameObject obj) where T : Component
    {
        var component = obj.GetComponent<T>();
        return component != null ? component : obj.AddComponent<T>();
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material)
    {
        var obj = GameObject.Find(name);
        if (obj == null)
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
        }
        obj.transform.SetPositionAndRotation(position, Quaternion.identity);
        obj.transform.localScale = scale;
        ApplyColor(obj, material);
        return obj;
    }

    private static GameObject Pad(GameObject parent, string name, Vector3 scale, Material material)
    {
        var pad = Cube(name, parent.transform.position + Vector3.up * 0.045f, scale, material);
        pad.transform.SetParent(parent.transform, true);
        var collider = pad.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);
        return pad;
    }

    private static void SetObject(UnityEngine.Object obj, string property, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(obj);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(UnityEngine.Object obj, string property, float value)
    {
        var serialized = new SerializedObject(obj);
        serialized.FindProperty(property).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
