using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(Image))]
public abstract class ShaderControllerBase : MonoBehaviour
{
    protected float _movementDamp = 0.7f;

    protected Material _mat;
    protected Material _complimentaryMat;

    protected virtual void Awake()
    {
        _mat = GetComponent<Image>().material;

        // Find the other channel material
        var imgs = FindObjectsOfType<Image>(true).Where(o => o.name == name).ToList();

        var c = imgs.Where(o => o.gameObject.GetInstanceID() != gameObject.GetInstanceID()).First();
        _complimentaryMat = c.material;
    }

    protected virtual void Start()
    {
        SetFaderUiValues();
    }

    abstract protected void SetFaderUiValues();
}
