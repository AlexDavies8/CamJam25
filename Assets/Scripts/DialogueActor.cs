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
    private bool talking = false;
    private bool preparing = false;

    [SerializeField]
    private string dialogueJingle;

    MusicEngine musicEngine;

    private float timeToStart = 0;
    [SerializeField]
    private float dialoguePreAppear;

    private void Awake()
    {
        audio = GetComponent<AudioSource>();
        musicEngine = GameObject.FindGameObjectsWithTag("Music")[0].GetComponent<MusicEngine>();
    }

    private void FixedUpdate() 
    {
        if (preparing) {
            timeToStart -= Time.deltaTime;
            if (timeToStart - dialoguePreAppear <= 0) {
                talking = true;
                dialogueBox.gameObject.SetActive(true);
            }
            if (timeToStart <= 0) {
                dialogueBox.gameObject.SetActive(true);
                preparing = false;
            }
        }
        if (talking) {
            talkTimeRemaining -= Time.deltaTime;
            if (talkTimeRemaining <= 0) EndDialog();
        }
        if (!talking && !preparing && Vector2.Distance(transform.position, PlayerController.Instance.transform.position) < talkDistance) StartDialog();
    }

    private void LateUpdate()
    {
        if (dialogueBox.enabled)
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
        timeToStart = musicEngine.QueueJingleTime(dialogueJingle);
        preparing = true;
        // audio.Play();
    }

    private void EndDialog()
    {
        dialogueBox.gameObject.SetActive(false);
        talking = false;
    }
}