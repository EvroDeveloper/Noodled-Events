#if UNITY_EDITOR
using System;
using NoodledEvents;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using static NoodledEvents.CookBook.NodeDef;
using static NoodledEvents.CookBook.NodeDef.Pin;

[CreateAssetMenu(fileName = "New Custom Node", menuName = "NoodleEvents/Custom Node", order = 1)]
public class CustomNodeDef : ScriptableObject
{
    public string _namespace = "custom";
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

    public SerializablePinData[] GetDataOutputs()
    {
        List<SerializablePinData> output = new();
        foreach(SerializablePinData pin in outputPins)
        {
            if(pin.pinType == SerializablePinData.PinType.Object)
            {
                output.Add(pin);
            }
        }
        return output.ToArray();
    }

    [ContextMenu("Validate Pin Types")]
    public void ValidatePinTypes()
    {
        foreach(var pin in inputPins)
        {
            pin.FixType();
        }
        foreach(var pin2 in outputPins)
        {
            pin2.FixType();
        }
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
    public int persistentCallForReturn;

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

    public void FixType()
    {
        if(pinType == PinType.Flow) return;

        objectType = objectType.Trim();
        if (TypeTranslator.SimpleNames2Types.TryGetValue(objectType.ToLower(), out Type v))
        {
            objectType = string.Join(',', v.AssemblyQualifiedName.Split(',').Take(2));
            return;
        }
        foreach (Type t in UltNoodleEditor.SearchableTypes)
        {
            if (string.Compare(t.Name, objectType, StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                objectType = string.Join(',', t.AssemblyQualifiedName.Split(',').Take(2));
                return;
            }
        }
    }
}

[Serializable]
public struct SerializablePersistentCallExt
{
    // public enum PeristentCallTargetType
    // {
    //     None,
    //     NodeInput
    // }
    // public PeristentCallTargetType target;
    // public int targetParameterIndex;
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
