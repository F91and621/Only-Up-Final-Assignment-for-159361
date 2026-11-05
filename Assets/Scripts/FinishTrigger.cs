using UnityEngine;

// Finish area.
public class FinishTrigger : MonoBehaviour
{
    private bool finished;

    void OnTriggerEnter(Collider other)
    {
        if (finished)
            return;

        BeginnerPlayerController player =
            other.GetComponentInParent<BeginnerPlayerController>();
        if (player == null)
            return;

        finished = true;
        CourseStartMenu menu = FindFirstObjectByType<CourseStartMenu>();
        if (menu != null)
            menu.FinishGame();
    }
}
