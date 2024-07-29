using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using SlimUI;
using System.Linq;

public class DevicesUIMenuManager : Singleton<DevicesUIMenuManager>
{
    [SerializeField] PanelSelector _mySelector;

    public void RegisterDeviceUi(UIDevice ui)
    {
        var parent = _mySelector.PanelGroups[ui.SquareData.Row - 1];

        ui.transform.SetParent(parent.Panel.transform, false);

        SortRow(parent.Panel.gameObject);
    }

    void SortRow(GameObject row)
    {
        var devices = row.GetComponentsInChildren<UIDevice>();

        devices.OrderBy(d => d.OffsetPosition.x);

        for (int i = 0; i < devices.Length; i++)
        {
            var d = devices[i];
            d.transform.SetSiblingIndex(i);
        }
    }
}
