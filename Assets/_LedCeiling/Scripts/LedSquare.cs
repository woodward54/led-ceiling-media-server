using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "esp", menuName = "LedSquare")]
public class LedSquare : ScriptableObject
{
    public string Hostname;
    public Vector2Int Position;
    public bool Enabled = true;
    public int Row;
}
