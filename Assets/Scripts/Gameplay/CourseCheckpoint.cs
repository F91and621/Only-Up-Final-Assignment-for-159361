using UnityEngine;
using UnityEngine.InputSystem;

public class CourseCheckpoint : MonoBehaviour
{
    public Key interactionKey = Key.E;

    private bool activated;
    private BeginnerPlayerController nearbyPlayer;
    private Rigidbody nearbyBody;
    private Collider nearbyCollider;
    private PlayerProgressUI progressUI;

    void Start()
    {
        progressUI = FindFirstObjectByType<PlayerProgressUI>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (activated)
            return;

        BeginnerPlayerController player =
            other.GetComponentInParent<BeginnerPlayerController>();
        if (player == null)
            return;

        nearbyPlayer = player;
        nearbyBody = player.GetComponent<Rigidbody>();
        nearbyCollider = other;

        if (progressUI != null)
            progressUI.ShowMessage("Press E to save checkpoint");
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<BeginnerPlayerController>() == nearbyPlayer)
        {
            nearbyPlayer = null;
            nearbyBody = null;
            nearbyCollider = null;

            if (!activated && progressUI != null)
                progressUI.ClearMessage();
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (activated || keyboard == null || !CanActivate())
            return;

        if (!keyboard[interactionKey].wasPressedThisFrame)
            return;

        activated = true;
        nearbyPlayer.SetResetPoint(transform);

        if (progressUI != null)
            progressUI.ShowMessage(
                "Checkpoint saved! Press R to return here", 3f);

        Debug.Log("Checkpoint saved. Press R to return here.");
    }

    bool CanActivate()
    {
        return nearbyPlayer != null && nearbyBody != null && nearbyCollider != null
            && nearbyCollider.bounds.min.y >= transform.position.y - 0.15f
            && nearbyCollider.bounds.min.y <= transform.position.y + 0.25f
            && Mathf.Abs(nearbyBody.linearVelocity.y) <= 0.3f;
    }
}
