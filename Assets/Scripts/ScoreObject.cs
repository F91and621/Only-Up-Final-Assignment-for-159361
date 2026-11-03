using UnityEngine;

public class ScoreObject : MonoBehaviour
{
    private bool alreadyScored;

    // The wsq scene uses prefab instances directly instead of the old landing
    // proxy objects. Add the scoring component to every current course platform
    // at runtime so prefab replacements do not silently remove scoring again.
    public static int ConfigureCoursePlatforms()
    {
        GameObject courseRoot = GameObject.Find("Only Up Course");
        if (courseRoot == null)
            return 0;

        int configuredCount = 0;

        foreach (Transform child in courseRoot.transform)
        {
            bool isFurniturePlatform = child.name.StartsWith("Furniture Visual ");
            bool isExtensionPlatform = child.name.StartsWith("Course ");
            bool isStartingPlatform = child.name.StartsWith("Furniture Visual 00");

            if ((!isFurniturePlatform && !isExtensionPlatform) || isStartingPlatform)
                continue;

            if (ConfigurePlatform(child))
                configuredCount++;
        }

        // The upper half of the wsq course is made from root-level prefab
        // instances rather than children of Only Up Course. They form a narrow
        // ascending route above the original furniture section.
        foreach (GameObject rootObject in courseRoot.scene.GetRootGameObjects())
        {
            Transform platform = rootObject.transform;
            Vector3 position = platform.position;

            bool isUpperCourseAsset = platform.name.StartsWith("rpgpp_lt_")
                || platform.name == "Night_light_1"
                || platform.name == "Gamepad_Classic";
            bool isInsideUpperCourse = position.x >= 58f && position.x <= 86f
                && position.y >= 39f && position.y <= 100f
                && position.z >= 35f && position.z <= 64f;

            if (isUpperCourseAsset && isInsideUpperCourse
                && ConfigurePlatform(platform))
            {
                configuredCount++;
            }
        }

        return configuredCount;
    }

    private static bool ConfigurePlatform(Transform platform)
    {
        // Only configure objects that can actually receive a landing collision.
        if (platform.GetComponentInChildren<Collider>(true) == null)
            return false;

        if (platform.GetComponent<ScoreObject>() == null)
            platform.gameObject.AddComponent<ScoreObject>();

        return true;
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
