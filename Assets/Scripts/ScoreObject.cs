using UnityEngine;

public class ScoreObject : MonoBehaviour
{
    private bool alreadyScored;

    public void TryAwardPoint()
    {
        if (alreadyScored)
            return;

        if (PlayerProgressUI.Instance == null)
            return;

        alreadyScored = true;
        PlayerProgressUI.Instance.AddPoint();
    }
}
