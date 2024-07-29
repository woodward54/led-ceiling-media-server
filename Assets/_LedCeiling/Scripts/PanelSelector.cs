using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using SlimUI;
using System.Linq;
using System.Collections.Generic;
using System;


public class PanelSelector : MonoBehaviour
{
    [Serializable]
    public class PanelButtonGroup
    {
        public CanvasGroup Panel;
        public Button Button;
    }

    [SerializeField] List<PanelButtonGroup> _panelGroups;

    public List<PanelButtonGroup> PanelGroups { get { return _panelGroups; } }

    void Awake()
    {
        foreach (var grp in _panelGroups)
        {
            grp.Button.onClick.AddListener(delegate { SelectPanel(grp); });

            // Ensure they are active
            grp.Button.gameObject.SetActive(true);
            grp.Panel.gameObject.SetActive(true);
        }
    }

    void Start()
    {
        SelectPanel(_panelGroups[0]);
    }

    public void SelectPanel(PanelButtonGroup grp)
    {
        DisablePanels();

        UIUtils.ActivateGroup(grp.Panel, true);

        if (grp.Button.TryGetComponent<ButtonHighlight>(out var highlight))
        {
            highlight.SetActive(true);
        }
    }


    void DisablePanels()
    {
        foreach (var grp in _panelGroups)
        {
            UIUtils.ActivateGroup(grp.Panel, false);

            if (grp.Button.TryGetComponent<ButtonHighlight>(out var highlight))
            {
                highlight.SetActive(false);
            }
        }
    }
}