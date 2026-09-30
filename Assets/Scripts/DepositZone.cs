// AI assistance: Codex generated this trigger handler and its connection to ChamberRun.
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class DepositZone : MonoBehaviour
{
    [SerializeField] private ChamberRun run;

    private void OnTriggerEnter(Collider other)
    {
        if (run != null)
            run.TryDeposit(other);
    }
}
