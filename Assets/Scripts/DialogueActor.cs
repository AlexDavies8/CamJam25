using System;
using UnityEngine;

public class DialogueActor : MonoBehaviour
{
    public float talkDistance = 0.5f;
    public float talkLength = 2f;

    public SpriteRenderer dialogueBox;
    public Transform dialoguePip;
    public float pipOffset = 0f;

    private AudioSource audio;

    private float talkTimeRemaining;
    private bool talking;

    private void Awake()
    {
        audio = GetComponent<AudioSource>();
    }

    private void FixedUpdate()
    {
        talkTimeRemaining -= Time.deltaTime;
        if (talking && talkTimeRemaining <= 0) EndDialog();
        if (!talking && Vector2.Distance(transform.position, PlayerController.Instance.transform.position) < talkDistance) StartDialog();
    }

    private void LateUpdate()
    {
        if (talking)
        {
            dialogueBox.transform.up = Vector2.up;
            var delta = dialogueBox.transform.position - transform.position;
            dialoguePip.up = delta;
            
            var bounds = dialogueBox.bounds;
            dialoguePip.transform.position = bounds.ClosestPoint(transform.position) - delta.normalized * pipOffset;
        }
    }

    private void StartDialog()
    {
        talkTimeRemaining = talkLength;
        audio.Play();
        talking = true;
        dialogueBox.gameObject.SetActive(true);
    }

    private void EndDialog()
    {
        dialogueBox.gameObject.SetActive(false);
        talking = false;
    }
}