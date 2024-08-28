using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(Image))]
public class ShaderController : MonoBehaviour
{
    [SerializeField] ColorPicker _colorPicker;
    [SerializeField] Slider _speedSlider;
    [SerializeField] Slider _SmthStep;

    Material _mat;
    Material _complimentaryMat;

    VideoPlayer _videoPlayer;

    void Awake()
    {
        _mat = GetComponent<Image>().material;

        // Find the other channel material
        var imgs = FindObjectsOfType<Image>(true).Where(o => o.name == name).ToList();

        var c = imgs.Where(o => o.gameObject.GetInstanceID() != gameObject.GetInstanceID()).First();
        _complimentaryMat = c.material;

        _videoPlayer = GetComponent<VideoPlayer>();
    }

    void Start()
    {
        _colorPicker.color = Color.white;
        SetFaderUiValues();
    }

    void OnEnable()
    {
        _colorPicker.onColorChanged += OnColorChanged;
        _speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        _SmthStep.onValueChanged.AddListener(OnSmthStepChanged);
    }

    void OnDisable()
    {
        _colorPicker.onColorChanged -= OnColorChanged;
        _speedSlider.onValueChanged.RemoveListener(OnSpeedChanged);
        _SmthStep.onValueChanged.RemoveListener(OnSmthStepChanged);
    }

    void SetFaderUiValues()
    {
        _colorPicker.gameObject.SetActive(_mat.HasColor("_Color"));
        _speedSlider.gameObject.SetActive(_mat.HasFloat("_Speed"));
        _SmthStep.gameObject.SetActive(_mat.HasFloat("_SmthStep"));

        if (_mat.HasFloat("_Color"))
            _colorPicker.color = _mat.GetColor("_Color");

        if (_mat.HasFloat("_Speed"))
            _speedSlider.value = _mat.GetFloat("_Speed");

        if (_mat.HasFloat("_SmthStep"))
            _SmthStep.value = _mat.GetFloat("_SmthStep");

        if (_videoPlayer)
            _videoPlayer.playbackSpeed = _mat.GetFloat("_Speed");
    }

    void OnColorChanged(Color c)
    {
        if (ContentSelector.Instance.IsTransitioning)
            return;

        _mat.SetColor("_Color", c);
        _complimentaryMat.SetColor("_Color", c);
    }

    void OnSpeedChanged(float value)
    {
        if (ContentSelector.Instance.IsTransitioning)
            return;

        _mat.SetFloat("_Speed", value);
        _complimentaryMat.SetFloat("_Speed", value);

        if (_videoPlayer)
            _videoPlayer.playbackSpeed = value;
    }

    void OnSmthStepChanged(float value)
    {
        if (ContentSelector.Instance.IsTransitioning)
            return;

        _mat.SetFloat("_SmthStep", value);
        _complimentaryMat.SetFloat("_SmthStep", value);
    }
}
