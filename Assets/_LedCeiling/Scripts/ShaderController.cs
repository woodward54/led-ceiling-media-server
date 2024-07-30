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
    [SerializeField] Slider _control1;
    [SerializeField] Slider _control2;
    [SerializeField] Slider _control3;

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
        _control1.onValueChanged.AddListener(OnControl1Changed);
        _control2.onValueChanged.AddListener(OnControl2Changed);
        _control3.onValueChanged.AddListener(OnControl3Changed);
    }

    void OnDisable()
    {
        _colorPicker.onColorChanged -= OnColorChanged;
        _speedSlider.onValueChanged.RemoveListener(OnSpeedChanged);
        _control1.onValueChanged.RemoveListener(OnControl1Changed);
        _control2.onValueChanged.RemoveListener(OnControl2Changed);
        _control3.onValueChanged.RemoveListener(OnControl3Changed);
    }

    void SetFaderUiValues()
    {
        _colorPicker.gameObject.SetActive(_mat.HasColor("_Color"));
        _speedSlider.gameObject.SetActive(_mat.HasFloat("_Speed"));
        _control1.gameObject.SetActive(_mat.HasFloat("_Control1"));
        _control2.gameObject.SetActive(_mat.HasFloat("_Control2"));
        _control3.gameObject.SetActive(_mat.HasFloat("_Control3"));

        if (_mat.HasFloat("_Color"))
            _colorPicker.color = _mat.GetColor("_Color");

        if (_mat.HasFloat("_Speed"))
            _speedSlider.value = _mat.GetFloat("_Speed");

        if (_mat.HasFloat("_Control1"))
            _control1.value = _mat.GetFloat("_Control1");

        if (_mat.HasFloat("_Control2"))
            _control2.value = _mat.GetFloat("_Control2");

        if (_mat.HasFloat("_Control3"))
            _control3.value = _mat.GetFloat("_Control3");

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

    void OnControl1Changed(float value)
    {
        if (ContentSelector.Instance.IsTransitioning)
            return;

        _mat.SetFloat("_Control1", value);
        _complimentaryMat.SetFloat("_Control1", value);
    }

    void OnControl2Changed(float value)
    {
        if (ContentSelector.Instance.IsTransitioning)
            return;

        _mat.SetFloat("_Control2", value);
        _complimentaryMat.SetFloat("_Control2", value);
    }

    void OnControl3Changed(float value)
    {
        if (ContentSelector.Instance.IsTransitioning)
            return;
            
        _mat.SetFloat("_Control3", value);
        _complimentaryMat.SetFloat("_Control3", value);
    }
}
