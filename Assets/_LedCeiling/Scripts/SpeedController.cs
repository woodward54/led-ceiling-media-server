using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class SpeedController : ShaderControllerBase
{
    [SerializeField] Slider _speedSlider;

    VideoPlayer _videoPlayer;

    float _target;
    float _velocity;
    float _currValue;

    protected override void Awake()
    {
        base.Awake();

        _videoPlayer = GetComponent<VideoPlayer>();
    }

    void OnEnable()
    {
        _speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        SetFaderUiValues();
    }

    void OnDisable()
    {
        _speedSlider.onValueChanged.RemoveListener(OnSpeedChanged);
    }

    protected override void SetFaderUiValues()
    {
        _speedSlider.value = 1.0f;
        _videoPlayer.playbackSpeed = _speedSlider.value;
        _currValue = _speedSlider.value;
        _target = _currValue;
    }

    void OnSpeedChanged(float value)
    {
        if (ContentSelector.Instance.IsTransitioning)
            return;

        _target = value;
    }

    void Update()
    {
        _currValue = Mathf.SmoothDamp(_currValue, _target, ref _velocity, _movementDamp);

        _videoPlayer.playbackSpeed = _currValue;
    }
}