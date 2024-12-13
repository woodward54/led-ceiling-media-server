using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static LedSquareChannel;

public class UIDevice : MonoBehaviour
{
    [SerializeField] TMP_Text _hostname;
    [SerializeField] TMP_Text _ip;
    [SerializeField] TMP_Text _connectedStatus;
    [SerializeField] Image _connectedImg;

    public LedSquare SquareData;

    private ConnectionState _status;
    public ConnectionState Status
    {
        get => _status;
        set
        {
            _status = value;
            UpdateStatusDisplay();
        }
    }

    public string Ip
    {
        set => _ip.text = value;
    }

    public string Hostname
    {
        set => _hostname.text = value;
    }

    public void Setup(LedSquare squareData, string hostname, ConnectionState status)
    {
        SquareData = squareData;
        Hostname = hostname;
        Status = status;

        DevicesUIMenuManager.Instance.RegisterDeviceUi(this);
    }

    private void UpdateStatusDisplay()
    {
        var color = Status switch
        {
            ConnectionState.Connected => Color.green,
            ConnectionState.Connecting => Color.yellow,
            ConnectionState.Reconnecting => Color.yellow,
            _ => Color.red
        };

        _connectedStatus.text = Status.ToString();
        _connectedImg.color = color;
    }
}
