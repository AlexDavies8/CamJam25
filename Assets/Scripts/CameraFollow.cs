using System;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float damping = 5f;

    private void FixedUpdate()
    {
        transform.position = Vector2.Lerp(transform.position, target.position, 1f - Mathf.Exp(-damping * Time.deltaTime));
    }
}