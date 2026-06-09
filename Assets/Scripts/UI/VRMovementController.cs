using UnityEngine;

public class VRMovementController : MonoBehaviour
{
    [Header("Locomotion Components")]
    [SerializeField] private MonoBehaviour moveProvider;
    [SerializeField] private MonoBehaviour teleportProvider;

    private bool _isMovementEnabled = false;

    private void Start()
    {
        DisableMovement();
    }

    /// <summary>
    /// Отключает перемещение и телепортацию
    /// Поворот (Turn) остаётся всегда включённым для взаимодействия с UI
    /// </summary>
    public void DisableMovement()
    {
        _isMovementEnabled = false;

        if (moveProvider != null)
        {
            moveProvider.enabled = false;
            Debug.Log($"[VRMovement] Move DISABLED: {moveProvider.GetType().Name}");
        }
        else
        {
            Debug.LogWarning("[VRMovement] Move provider NOT assigned!");
        }

        if (teleportProvider != null)
        {
            teleportProvider.enabled = false;
            Debug.Log($"[VRMovement] Teleport DISABLED: {teleportProvider.GetType().Name}");
        }
        else
        {
            Debug.LogWarning("[VRMovement] Teleport provider NOT assigned!");
        }

        Debug.Log("[VRMovement] Movement disabled. Player can rotate and interact with UI.");
    }

    /// <summary>
    /// Включает перемещение и телепортацию
    /// </summary>
    public void EnableMovement()
    {
        _isMovementEnabled = true;

        if (moveProvider != null)
        {
            moveProvider.enabled = true;
            Debug.Log($"[VRMovement] Move ENABLED: {moveProvider.GetType().Name}");
        }

        if (teleportProvider != null)
        {
            teleportProvider.enabled = true;
            Debug.Log($"[VRMovement] Teleport ENABLED: {teleportProvider.GetType().Name}");
        }

        Debug.Log("[VRMovement] Full locomotion enabled!");
    }

    public bool IsMovementEnabled()
    {
        return _isMovementEnabled;
    }
}