// AI assistance: Codex generated this Actor-only sensor and its color feedback.
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class UnlockSensor : MonoBehaviour
{
    [SerializeField] private ChamberRun run;
    [SerializeField] private Rigidbody actor;
    [SerializeField] private Renderer indicator;

    private void OnTriggerEnter(Collider other)
    {
        // The Payload and other objects must not activate the sensor.
        if (run == null || actor == null || other.attachedRigidbody != actor ||
            !actor.CompareTag("Player"))
            return;

        if (run.UnlockRoadblock() && indicator != null)
        {
            var properties = new MaterialPropertyBlock();
            indicator.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", new Color(0.15f, 0.95f, 0.5f));
            indicator.SetPropertyBlock(properties);
        }
    }
}
