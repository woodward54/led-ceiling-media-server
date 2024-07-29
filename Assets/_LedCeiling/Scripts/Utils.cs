using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static GameObject FindChildByNameRecursive(GameObject obj, string name, bool exactMatch = true)
    {
        var children = obj.GetComponentsInChildren<Transform>(true);

        foreach (Transform c in children)
        {
            if (exactMatch && c.name == name)
                return c.gameObject;
            else if (c.name.Contains(name))
                return c.gameObject;
        }

        return null;
    }

    public static GameObject FindChildByName(GameObject obj, string name, bool exactMatch = true)
    {
        foreach (Transform c in obj.transform)
        {
            if (exactMatch && c.name == name)
                return c.gameObject;
            else if (c.name.Contains(name))
                return c.gameObject;
        }

        return null;
    }
}