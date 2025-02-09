using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Jobs;

public class Player : MonoBehaviour
{
    public Softbody softBody;
    public SpacePhysics spacePhysics;
    public GameObject player;
    public GameObject leftEye;
    public GameObject rightEye;
    [Tooltip("number of vertices")]
    public int vertices;

    [Tooltip("initial radius of the polygon")]
    public float radius;

    public float anchorOffset;

    float root(float x, int e)
    {
        bool neg = false;
        if (x < 0)
        {
            neg = true;
            x = -x;
        }

        x = Mathf.Pow(x, 1f / e);
        if (neg && e % 2 == 1)
            x = -x;
        return x;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float dtheta = 2 * Mathf.PI / vertices;
        for (int i = 0; i < vertices; i++)
        {
            float x = (root(Mathf.Cos(dtheta * i), 3)) * radius + player.transform.position.x;
            float y = (root(Mathf.Sin(dtheta * i), 3)) * radius + player.transform.position.y;

            softBody.AddPoint(x, y, false);
            softBody.AddPointToDraw(i);
        }

        for (int i = 0; i < vertices; i++)
        {
            //softBody.AddSpring(i, (i + 1) % vertices);
            for (int j = i + 1; j < vertices; j++)
            {
                softBody.AddSpring(i, j, 1f);
            }
        }

        Vector2 dx = new Vector2(anchorOffset, 0);
        Vector2 dy = new Vector2(0, anchorOffset);
        softBody.AddPoint((Vector2)player.transform.position + dx, true);
        softBody.AddPoint((Vector2)player.transform.position + dy, true);
        softBody.AddPoint((Vector2)player.transform.position - dx, true);
        softBody.AddPoint((Vector2)player.transform.position - dy, true);

        for (int i = vertices; i < vertices + 4; i++)
        {
            for (int j = 0; j < i; j++)
            {
                softBody.AddSpring(i, j, 4f);
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        for (int _ = 0; _ < 16; _++)
        {
            float theta = player.transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
            Vector2 dx = new Vector2(anchorOffset * Mathf.Cos(theta), anchorOffset * Mathf.Sin(theta));
            Vector2 dy = new Vector2(-anchorOffset * Mathf.Sin(theta), anchorOffset * Mathf.Cos(theta));
            softBody.points[vertices].position = (Vector2)player.transform.position + dx;
            softBody.points[vertices + 1].position = (Vector2)player.transform.position + dy;
            softBody.points[vertices + 2].position = (Vector2)player.transform.position - dx;
            softBody.points[vertices + 3].position = (Vector2)player.transform.position - dy;

            if (spacePhysics.closestPlanet == null)
            {
                softBody.Physics();
                continue;
            }

            Planet cp = spacePhysics.closestPlanet;
            Vector2 planetPosition = cp.transform.position;
            for (int i = 0; i < vertices; i++)
            {
                if (softBody.points[i].anchor)
                    continue;
                Vector2 pointPosition = softBody.points[i].position;
                float distance = cp.SurfaceHeight(cp.AngleTo(pointPosition));
                Vector2 d = pointPosition - planetPosition;
                distance = d.magnitude - distance;
                d = d.normalized;

                if (distance < 0)
                {
                    softBody.net_force[i] = d * 10;
                }
            }
            softBody.Physics();
        }

        leftEye.transform.position = softBody.points[vertices].position;
        leftEye.transform.rotation = player.transform.rotation;

        rightEye.transform.position = softBody.points[vertices + 2].position;
        rightEye.transform.rotation = player.transform.rotation;
    }

    public void ApplyForce(Vector2 force)
    {
        for (int i = 0; i < vertices; i++)
        {
            softBody.net_force[i] += force;
        }
    }
}
