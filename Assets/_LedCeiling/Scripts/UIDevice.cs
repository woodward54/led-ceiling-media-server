using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDevice : MonoBehaviour
{
    public enum ConnectedStatus
    {
        Connected,
        Disconnected
    }

    [SerializeField] TMP_Text _hostname;
    [SerializeField] TMP_Text _ip;
    [SerializeField] TMP_Text _connectedStatus;
    [SerializeField] Image _connectedImg;

    public Vector2Int OffsetPosition;

    public LedSquare SquareData;
    public string Hostname = "";
    public string Ip = "";
    public ConnectedStatus Status = ConnectedStatus.Disconnected;

    public void Setup(LedSquare squareData, string hostname, ConnectedStatus status)
    {
        SquareData = squareData;
        Hostname = hostname;
        Status = status;

        DevicesUIMenuManager.Instance.RegisterDeviceUi(this);
    }

    void Update()
    {
        _hostname.text = Hostname;
        _ip.text = Ip;
        _connectedStatus.text = Status.ToString();

        switch (Status)
        {
            case ConnectedStatus.Connected:
                _connectedImg.color = Color.green;
                break;

            case ConnectedStatus.Disconnected:
                _connectedImg.color = Color.red;
                break;

            default:
                _connectedImg.color = Color.black;
                break;
        }
    }
}
