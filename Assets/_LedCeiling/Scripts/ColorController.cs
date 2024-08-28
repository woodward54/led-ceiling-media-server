using UnityEngine;
using UnityEngine.UI;

public class ColorController : ShaderControllerBase
{
    [SerializeField] ColorPicker _colorPicker;

    protected override void Start()
    {
        _colorPicker.color = Color.white;
        base.Start();
    }

    void OnEnable()
    {
        _colorPicker.onColorChanged += OnColorChanged;
        SetFaderUiValues();
    }

    void OnDisable()
    {
        _colorPicker.onColorChanged -= OnColorChanged;
    }

    protected override void SetFaderUiValues()
    {
        _mat.SetColor("_Color", _colorPicker.color);
    }

    void OnColorChanged(Color c)
    {
        if (ContentSelector.Instance.IsTransitioning)
            return;

        _mat.SetColor("_Color", c);
        _complimentaryMat.SetColor("_Color", c);
    }
}