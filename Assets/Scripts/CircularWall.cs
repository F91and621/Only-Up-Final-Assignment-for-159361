using UnityEngine;

public class CircularWall : MonoBehaviour
{
    public float radius = 10f;
    public int segments = 36;
    public float height = 5f;
    public float thickness = 1f;

    void Start()
    {
        float angleStep = 360f / segments;
        // Make each section slightly wider to close gaps.
        float width = 2f * Mathf.PI * radius / segments * 1.05f;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 localPos = new Vector3(Mathf.Cos(rad) * radius, height / 2f, Mathf.Sin(rad) * radius);

            GameObject wall = new GameObject("Wall_" + i);
            wall.transform.SetParent(transform);
            wall.transform.localPosition = localPos;
            // Face each section toward the centre.
            wall.transform.LookAt(transform.position + Vector3.up * (height / 2f));

            BoxCollider box = wall.AddComponent<BoxCollider>();
            box.size = new Vector3(width, height, thickness);
        }
    }
}
