using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHighlight : MonoBehaviour
{
    [SerializeField] GameObject _highlight;

    void Start()
    {
        SetActive(false);
    }

    public void SetActive(bool isActive)
    {
        _highlight.SetActive(isActive);
    }
}
