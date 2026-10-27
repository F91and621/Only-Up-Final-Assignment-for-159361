using UnityEngine;

public class CircularWall : MonoBehaviour
{
    public float radius = 10f;     // 圆半径
    public int segments = 36;      // 分段数,越多越圆
    public float height = 5f;      // 墙高
    public float thickness = 1f;   // 墙厚

    void Start()
    {
        float angleStep = 360f / segments;
        // 每段墙的宽度 = 圆周弦长,略微加大避免缝隙
        float width = 2f * Mathf.PI * radius / segments * 1.05f;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 localPos = new Vector3(Mathf.Cos(rad) * radius, height / 2f, Mathf.Sin(rad) * radius);

            GameObject wall = new GameObject("Wall_" + i);
            wall.transform.SetParent(transform);
            wall.transform.localPosition = localPos;
            // 让墙面朝向圆心
            wall.transform.LookAt(transform.position + Vector3.up * (height / 2f));

            BoxCollider box = wall.AddComponent<BoxCollider>();
            box.size = new Vector3(width, height, thickness);
        }
    }
}