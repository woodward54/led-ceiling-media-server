using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;


public class SettingsManager : Singleton<SettingsManager>
{
    [SerializeField] Slider _brightnessSlider;
    [SerializeField] TMP_Text _showDebugSquares;
    [SerializeField] GameObject _debugSquares;

    public float Brightness { get { return _brightnessSlider.value; } }

    void Start()
    {
        _brightnessSlider.onValueChanged.AddListener(delegate { BrightnessValueChanged(); });
        _brightnessSlider.value = PlayerPrefs.GetFloat("BrightnessSlider");

        _showDebugSquares.text = "Off";
        _debugSquares.SetActive(false);
    }

    public void BrightnessValueChanged()
    {
        PlayerPrefs.SetFloat("BrightnessSlider", _brightnessSlider.value);
    }

    public void ToggleDebugSquares()
    {
        if (_showDebugSquares.text == "On")
        {
            _debugSquares.SetActive(false);
            _showDebugSquares.text = "Off";
        }
        else
        {
            _debugSquares.SetActive(true);
            _showDebugSquares.text = "On";
        }

    }
}