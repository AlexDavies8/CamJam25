using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Softbody softBody;
    [Tooltip("number of vertices")]
    public int vertices;

    [Tooltip("initial radius of the polygon")]
    public float radius;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float dtheta = 2 * Mathf.PI / vertices;
        for (int i = 0; i < vertices; i++)
        {
            float x = Mathf.Cos(dtheta * i) * radius + transform.position.x;
            float y = Mathf.Sin(dtheta * i) * radius + transform.position.y;

            softBody.AddPoint(x, y);
        }

        for (int i = 0; i < vertices; i++)
        {
            //softBody.AddSpring(i, (i + 1) % vertices);
            for (int j = i + 1; j < vertices; j++)
            {
                softBody.AddSpring(i, j);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            for (int i = 0; i < vertices; i++)
            {
                Vector2 d = softBody.points[i].position - mouse;
                softBody.points[i].velocity = 10 * d * (1 / (d.magnitude * d.magnitude));
            }
        }
    }
}
