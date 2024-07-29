using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using SlimUI;
using DG.Tweening;


public static class UIUtils
{
    public static void ActivateGroup(CanvasGroup group, bool status)
    {
        group.alpha = status ? 1f : 0f;
        group.interactable = status;
        group.blocksRaycasts = status;
    }
}