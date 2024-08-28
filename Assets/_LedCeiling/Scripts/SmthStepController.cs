using UnityEngine;
using UnityEngine.UI;

public class SmthStepController : ShaderControllerBase
{
    [SerializeField] Slider _SmthStep;

    float _target;
    float _velocity;
    float _currValue;

    void OnEnable()
    {
        _SmthStep.onValueChanged.AddListener(OnSmthStepChanged);
        SetFaderUiValues();
    }

    void OnDisable()
    {
        _SmthStep.onValueChanged.RemoveListener(OnSmthStepChanged);
    }

    protected override void SetFaderUiValues()
    {
        _mat.SetFloat("_SmthStep", _SmthStep.value);
        _currValue = _SmthStep.value;
        _target = _currValue;
    }

    void OnSmthStepChanged(float value)
    {
        if (ContentSelector.Instance.IsTransitioning)
            return;

        _target = value;
    }

    void Update()
    {
        _currValue = Mathf.SmoothDamp(_currValue, _target, ref _velocity, _movementDamp);

        _mat.SetFloat("_SmthStep", _currValue);
        _complimentaryMat.SetFloat("_SmthStep", _currValue);
    }
}