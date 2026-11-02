using UnityEngine;

// 玩家进入终点区域后结束游戏并打开记录页。
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
