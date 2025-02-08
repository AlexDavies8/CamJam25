using UnityEngine;

public class Orbit : MonoBehaviour
{

    public Transform orbitOrigin;

    public float orbitPeriod;

    private float orbitRadius;

    private float currentOrbitPosition;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        orbitRadius = Vector2.Distance(orbitOrigin.position, transform.position);

        var startOffset = transform.position - orbitOrigin.position;
        currentOrbitPosition = Mathf.Atan2(startOffset.y, startOffset.x);
    }

    void FixedUpdate()
    {
        currentOrbitPosition = (currentOrbitPosition + ( 2 * Mathf.PI * (Time.deltaTime / orbitPeriod))) % (2 * Mathf.PI);

        transform.position = (Vector2)orbitOrigin.position + orbitRadius * new Vector2(
            Mathf.Cos(currentOrbitPosition),
            Mathf.Sin(currentOrbitPosition)
        );
    }
}
