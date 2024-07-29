using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TextSetter : MonoBehaviour
{
    [SerializeField] string _baseText;

    TMP_Text _text;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    public void SetText(float value)
    {
        _text.text = _baseText + value.ToString("0.0");
    }
}
