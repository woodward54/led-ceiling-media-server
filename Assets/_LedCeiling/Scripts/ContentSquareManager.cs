using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
class ContentSquareDataWrapper
{
    public List<ContentSquareDataVideo> Videos;
    public List<ContentSquareDataShader> Shaders;
}

public class ContentSquareManager : Singleton<ContentSquareManager>
{
    [SerializeField] ContentSquare _squarePrefab;
    
    string SaveFile;
    ContentSquareDataWrapper Content;

    void Awake()
    {
        SaveFile = Application.persistentDataPath + "/content-data.json";
    }

    void Start()
    {
        // Load data from JSON

        ReadFile();

        // Initialize prefabs

    }

    public void SelectContent(ContentSquareData _squareData)
    {
        // TODO Set the main canvas content as the selected video/shader

    }

    public void NewContent()
    {

    }

    public void RemoveContent()
    {

    }

    public void ReadFile()
    {
        if (File.Exists(SaveFile))
        {
            string fileContents = File.ReadAllText(SaveFile);

            Content = JsonUtility.FromJson<ContentSquareDataWrapper>(fileContents);
        }
    }

    public void WriteFile()
    {
        string jsonString = JsonUtility.ToJson(Content);

        File.WriteAllText(SaveFile, jsonString);
    }
}
