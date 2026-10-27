using UnityEngine;
using UnityEngine.InputSystem;

public class CourseCheckpoint : MonoBehaviour
{
    public Key interactionKey = Key.E;

    private bool activated;
    private BeginnerPlayerController nearbyPlayer;
    private Rigidbody nearbyBody;
    private Collider nearbyCollider;
    private float savedMessageUntil;

    void OnTriggerStay(Collider other)
    {
        if (activated)
            return;

        BeginnerPlayerController player = other.GetComponent<BeginnerPlayerController>();
        if (player == null)
            return;

        nearbyPlayer = player;
        nearbyBody = other.GetComponent<Rigidbody>();
        nearbyCollider = other;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<BeginnerPlayerController>() == nearbyPlayer)
        {
            nearbyPlayer = null;
            nearbyBody = null;
            nearbyCollider = null;
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
        savedMessageUntil = Time.time + 3f;

        Debug.Log("Checkpoint saved. Press R to return here.");
    }

    bool CanActivate()
    {
        return nearbyPlayer != null && nearbyBody != null && nearbyCollider != null
            && nearbyCollider.bounds.min.y >= transform.position.y - 0.15f
            && nearbyCollider.bounds.min.y <= transform.position.y + 0.25f
            && Mathf.Abs(nearbyBody.linearVelocity.y) <= 0.3f;
    }

    void OnGUI()
    {
        string message = null;
        if (activated && Time.time < savedMessageUntil)
            message = "Checkpoint saved! Press R to return here";
        else if (!activated && CanActivate())
            message = "Press E to save checkpoint";

        if (message == null)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.alignment = TextAnchor.MiddleCenter;
        GUIStyle shadow = new GUIStyle(style);
        shadow.normal.textColor = Color.black;
        style.normal.textColor = Color.white;
        Rect area = new Rect(Screen.width * 0.5f - 235f,
            Screen.height - 90f, 470f, 48f);
        GUI.Label(new Rect(area.x + 2f, area.y + 2f, area.width, area.height),
            message, shadow);
        GUI.Label(area, message, style);
    }
}
