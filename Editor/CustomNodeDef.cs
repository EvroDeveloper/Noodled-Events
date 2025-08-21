#if UNITY_EDITOR
using System;
using NoodledEvents;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using static NoodledEvents.CookBook.NodeDef;
using static NoodledEvents.CookBook.NodeDef.Pin;

[CreateAssetMenu(fileName = "New Custom Node", menuName = "NoodleEvents/Custom Node", order = 1)]
public class CustomNodeDef : ScriptableObject
{
    public string nodeName;
    public string bookTag;
    public SerializablePinData[] inputPins = new[] { new SerializablePinData() { pinName = "Exec" } };
    public SerializablePinData[] outputPins = new[] { new SerializablePinData() { pinName = "Done" } };

    public SerializablePersistentCallExt[] persistentCalls;

    public Pin[] GetInputPins()
    {
        List<Pin> output = new();
        foreach(var pin in inputPins)
        {
            output.Add(pin.ToPin());
        }
        return output.ToArray();
    }

    public Pin[] GetOutputPins()
    {
        List<Pin> output = new();
        foreach(var pin in outputPins)
        {
            output.Add(pin.ToPin());
        }
        return output.ToArray();
    }
}

[Serializable]
public struct SerializablePinData
{
    public enum PinType
    {
        Flow,
        Object,
    }
    public string pinName = "New Pin";
    public PinType pinType;
    public string objectType;
    public int persistentCallForReturn = 0;

    public Pin ToPin()
    {
        if(pinType == PinType.Flow)
        {
            return new Pin(pinName);
        }
        else if (pinType == PinType.Object)
        {
            Type pinObjType = Type.GetType(objectType);
            Debug.Log(pinObjType == null);
            return new Pin(pinName, pinObjType);
        }
        return null;
    }
}

[Serializable]
public struct SerializablePersistentCallExt
{
    public UnityEngine.Object target;
    public string methodName;
    public SerializablePersistentArgumentExt[] persistentArguments;
}

[Serializable]
public struct SerializablePersistentArgumentExt
{
    public enum PersistentArgumentTypeExt
    {
        None,
        Bool,
        String,
        Int,
        Enum,
        Float,
        Vector2,
        Vector3,
        Vector4,
        Quaternion,
        Color,
        Color32,
        Rect,
        Object,
        Parameter,
        ReturnValue,
        NodeParameter
    }

    public PersistentArgumentTypeExt Type;
    public int _Int;
    public string _String;
    public float _X;
    public float _Y;
    public float _Z;
    public float _W;
    public UnityEngine.Object _Object;
}
#endif