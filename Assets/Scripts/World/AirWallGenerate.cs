using UnityEngine;

// 在场景周围生成一圈只有碰撞体的空气墙。
public class AirWallGenerate : MonoBehaviour
{
    public float radius = 10f;
    public int segments = 36;
    public float height = 5f;
    public float thickness = 1f;

    void Start()
    {
        float angleStep = 360f / segments;
        float width = 2f * Mathf.PI * radius / segments * 1.05f;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float radians = angle * Mathf.Deg2Rad;

            Vector3 position = new Vector3(
                Mathf.Cos(radians) * radius,
                height / 2f,
                Mathf.Sin(radians) * radius);

            GameObject wall = new GameObject("Wall_" + i);
            wall.transform.SetParent(transform);
            wall.transform.localPosition = position;
            wall.transform.LookAt(transform.position + Vector3.up * (height / 2f));

            BoxCollider box = wall.AddComponent<BoxCollider>();
            box.size = new Vector3(width, height, thickness);
        }
    }
}
