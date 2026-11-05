using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CourseCheckpoint : MonoBehaviour
{
    public Key interactionKey = Key.E;
    [SerializeField] private Text messageText;

    private static CourseCheckpoint messageOwner;
    private bool activated;
    private BeginnerPlayerController nearbyPlayer;
    private Rigidbody nearbyBody;
    private Collider nearbyCollider;
    private float savedMessageUntil;

    void OnTriggerStay(Collider other)
    {
        if (activated)
            return;
        BeginnerPlayerController player =
            other.GetComponentInParent<BeginnerPlayerController>();
        if (player == null)
            return;

        nearbyPlayer = player;
        nearbyBody = other.attachedRigidbody;
        nearbyCollider = other;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<BeginnerPlayerController>() != nearbyPlayer)
            return;
        nearbyPlayer = null;
        nearbyBody = null;
        nearbyCollider = null;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (!activated && keyboard != null && CanActivate()
            && keyboard[interactionKey].wasPressedThisFrame)
        {
            ActivateCheckpoint();
        }
        UpdateMessage();
    }

    void ActivateCheckpoint()
    {
        activated = true;
        nearbyPlayer.SetResetPoint(transform);
        PlayerLifeSystem lifeSystem = FindFirstObjectByType<PlayerLifeSystem>();
        if (lifeSystem != null)
            lifeSystem.ActivateLives(transform, nearbyPlayer);
        savedMessageUntil = Time.time + 3f;
    }

    bool CanActivate()
    {
        return nearbyPlayer != null && nearbyBody != null && nearbyCollider != null
            && nearbyCollider.bounds.min.y >= transform.position.y - 0.15f
            && nearbyCollider.bounds.min.y <= transform.position.y + 0.25f
            && Mathf.Abs(nearbyBody.linearVelocity.y) <= 0.3f;
    }

    void UpdateMessage()
    {
        string message = null;
        if (activated && Time.time < savedMessageUntil)
            message = "Checkpoint saved! Press R to return here";
        else if (!activated && CanActivate())
            message = "Press E to save checkpoint";

        if (message != null)
        {
            if (messageText != null)
            {
                messageText.text = message;
                messageText.gameObject.SetActive(true);
            }
            messageOwner = this;
        }
        else if (messageOwner == this)
        {
            if (messageText != null)
                messageText.gameObject.SetActive(false);
            messageOwner = null;
        }
    }
}
