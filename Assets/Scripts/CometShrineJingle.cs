using System;
using UnityEngine;

public class CometShrineJingle : MonoBehaviour
{
    public static CometShrineJingle Instance;

    public GameObject comet;

    public bool TreeFlag = false;
    public bool FlowerFlag = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (TreeFlag && FlowerFlag && !comet.activeSelf) comet.SetActive(true);
    }
}