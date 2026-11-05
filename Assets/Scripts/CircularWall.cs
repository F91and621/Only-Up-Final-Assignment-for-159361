using UnityEngine;

public class CircularWall : MonoBehaviour
{
    public float radius = 10f;
    public int segments = 36;
    public float height = 5f;
    public float thickness = 1f;

    void Start()
    {
        int count = Mathf.Max(3, segments);
        float angleStep = 360f / count;
        float width = 2f * Mathf.PI * radius / count * 1.05f;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius,
                height / 2f, Mathf.Sin(angle) * radius);

            GameObject wall = new GameObject("Wall_" + i);
            wall.transform.SetParent(transform);
            wall.transform.localPosition = position;
            wall.transform.LookAt(transform.position + Vector3.up * (height / 2f));
            BoxCollider box = wall.AddComponent<BoxCollider>();
            box.size = new Vector3(width, height, thickness);
        }
    }
}
