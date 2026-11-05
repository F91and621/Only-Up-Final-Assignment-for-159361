using UnityEngine;

public class ScoreObject : MonoBehaviour
{
    private bool alreadyScored;

    // Add scoring to the original prefab instances along the climb route.
    public static void ConfigureCoursePlatforms()
    {
        GameObject onlyUpRoot = GameObject.Find("Only_Up");
        if (onlyUpRoot == null)
            return;

        foreach (Transform child in onlyUpRoot.transform)
        {
            bool isPlatformAsset = child.name.StartsWith("rpgpp_lt_")
                || child.name == "Box_1"
                || child.name == "Armchair_Classic"
                || child.name == "Coffee_table_2"
                || child.name == "Night_light_1"
                || child.name == "Gamepad_Classic";
            Vector3 position = child.position;
            bool isOnCourse = position.x >= 58f && position.x <= 86f
                && position.y >= 24f && position.y <= 100f
                && position.z >= 35f && position.z <= 64f;

            if (!isPlatformAsset || !isOnCourse)
                continue;

            ConfigurePlatform(child);
        }
    }

    private static void ConfigurePlatform(Transform platform)
    {
        // Only configure objects that can actually receive a landing collision.
        if (platform.GetComponentInChildren<Collider>(true) == null)
            return;

        if (platform.GetComponent<ScoreObject>() == null)
            platform.gameObject.AddComponent<ScoreObject>();
    }

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
