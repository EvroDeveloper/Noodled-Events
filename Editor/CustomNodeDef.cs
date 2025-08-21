#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu("New Custom Node", "NoodleEvents/Custom Node", 1)]
public class CustomNodeDef : ScriptableObject
{
    public string nodeName;
    public string bookTag;
    public SerializablePinData[] inputPins;
    public SerializablePinData[] outputPins;

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
    public string pinName;
    public PinType pinType;
    public string objectType;

    public Pin ToPin()
    {
        if(pinType == PinType.Flow)
        {
            return new Pin(pinName);
        }
        else if (pinType == PinType.Object)
        {
            Type pinObjType = Type.GetType(objectType);
            return new Pin(pinName, pinObjType);
        }
    }
}

[Serializable]
public struct SerializablePersistentCallSetup
{

}
#endif