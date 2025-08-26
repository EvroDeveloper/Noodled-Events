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
public class CustomNodeDef : ScriptableObject, ISerializationCallbackReceiver
{
    public string _namespace = "custom";
    public string nodeName;
    public string bookTag;

    public SerializableFlowInputPin[] flowInputs = new[] { new SerializableFlowInputPin() { pinName = "Exec" } };
    public SerializableDataInputPin[] dataInputs = new[];

    public SerializableFlowOutputPin[] flowInputs = new[] { new SerializableFlowOutputPin() { pinName = "Done" } };
    public SerializableDataOutputPin[] dataOutputs = new[];

    public SerializablePinData[] inputPins = new[] { new SerializablePinData() { pinName = "Exec" } };
    public SerializablePinData[] outputPins = new[] { new SerializablePinData() { pinName = "Done" } };

    public SerializablePersistentCallExt[] persistentCalls;

    public SerializedNode[] NodeDatas;

    public Pin[] GetInputPins()
    {
        List<Pin> output = new();
        foreach(var flowPin in flowInputs)
        {
            output.Add(flowPin.ToPin());
        }
        foreach(var dataPin in dataInputs)
        {
            output.Add(dataPin.ToPin());
        }
        return output.ToArray();
    }

    public Pin[] GetOutputPins()
    {
        List<Pin> output = new();
        foreach(var flowPin in flowOutputs)
        {
            output.Add(flowPin.ToPin());
        }
        foreach(var outputPin in dataOutputs)
        {
            output.Add(outputPin.ToPin());
        }
        return output.ToArray();
    }

    [ContextMenu("Validate Pin Types")]
    public void ValidatePinTypes()
    {
        foreach(var pin in dataInputs)
        {
            pin.FixType();
        }
        foreach(var pin2 in dataOutputs)
        {
            pin2.FixType();
        }
    }

    public void OnBeforeSerialize()
    {

    }

    public void OnAfterDeserialize()
    {
        foreach (var node in NodeDatas)
            node.Bowl = this;
    }
}

[Serializable]
public class SerializableFlowInputPin : SerializablePinData
{
    public NoodleFlowOutput FlowInOutput;    
}

[Serializable]
public class SerializableFlowOutputPin : SerializablePinData
{
    public NoodleFlowOutput FlowOutInput;
}

[Serializable]
public class SerializableDataInputPin : SerializablePinData
{
    public NoodleDataOutput DataInOutput;

    public string objectType;

    public override Pin ToPin()
    {
        Type pinObjType = Type.GetType(objectType);
        return new Pin(pinName, pinObjType);
    }

    public void FixType()
    {
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
public class SerializableDataOutputPin : SerializablePinData
{
    public NoodleDataInput DataOutInput;

    public string objectType;

    public int persistentCallForReturn;

    public override Pin ToPin()
    {
        Type pinObjType = Type.GetType(objectType);
        return new Pin(pinName, pinObjType);
    }

    public void FixType()
    {
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
public class SerializablePinData
{
    public string pinName;

    public virtual void OnAfterDeserialize()
    {

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
