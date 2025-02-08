using UnityEngine;
using UnityEngine.Events;

public class OnStart : MonoBehaviour
{
    public UnityEvent ev;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ev.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
