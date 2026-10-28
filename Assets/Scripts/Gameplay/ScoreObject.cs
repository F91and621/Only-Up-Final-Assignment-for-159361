using UnityEngine;

public class ScoreObject : MonoBehaviour
{
    private bool alreadyScored;
    private PlayerProgressUI progressUI;

    void Start()
    {
        progressUI = FindFirstObjectByType<PlayerProgressUI>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (alreadyScored)
            return;

        BeginnerPlayerController player =
            collision.gameObject.GetComponentInParent<BeginnerPlayerController>();

        if (player == null || progressUI == null)
            return;

        alreadyScored = true;
        progressUI.AddPoint();
    }
}
