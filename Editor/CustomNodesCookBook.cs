#if UNITY_EDITOR
using NoodledEvents;
using UltEvents;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using static NoodledEvents.CookBook.NodeDef;
using static SerializablePersistentArgumentExt;

public class CustomNodesCookBook : CookBook
{
    public Dictionary<string, CustomNodeDef> bookTagToNodeDef = new();
    
    public override void CollectDefs(Action<IEnumerable<NodeDef>, float> progressCallback, Action completedCallback)
    {
        bookTagToNodeDef.Clear();
        List<NodeDef> allDefs = new();

        string[] customNodeGuids = AssetDatabase.FindAssets("t:CustomNodeDef", null);

        foreach(var guid in customNodeGuids)
        {
            CustomNodeDef customNode = AssetDatabase.LoadAssetAtPath<CustomNodeDef>(AssetDatabase.GUIDToAssetPath(guid));
            if(bookTagToNodeDef.ContainsKey(customNode.bookTag))
            {
                Debug.LogWarning($"[Noodled Events] Could not add custom node {customNode.nodeName}: Book Tag already exists", customNode);
                continue;
            }
            bookTagToNodeDef.Add($"{customNode._namespace}.{customNode.bookTag}", customNode);
            
            Pin[] inputPins = customNode.GetInputPins();
            Pin[] outputPins = customNode.GetOutputPins();
            
            allDefs.Add(new NodeDef(this, $"{customNode._namespace}.{customNode.nodeName}",
                inputs: () => inputPins,
                outputs: () => outputPins,
                bookTag: $"{customNode._namespace}.{customNode.bookTag}"));
        }

        progressCallback.Invoke(allDefs, 1);
        completedCallback.Invoke();
    }

    public override void CompileNode(UltEventBase evt, SerializedNode node, Transform dataRoot)
    {
        base.CompileNode(evt, node, dataRoot);

        CustomNodeDef targetNode = bookTagToNodeDef[node.BookTag];
        SerializablePinData[] dataOutputs = targetNode.GetDataOutputs();

        for(int i = 0; i < targetNode.persistentCalls.Length; i++)
        {
            var call = targetNode.persistentCalls[i];
            PersistentCall ultCall = new PersistentCall();
            ultCall.FSetMethodName(call.methodName);
            List<PersistentArgument> argList = new();
            foreach(var arg in call.persistentArguments)
            {
                PersistentArgument ultArg = new PersistentArgument();
                    
                if(arg.Type != PersistentArgumentTypeExt.NodeParameter)
                    ultArg.FSetType((PersistentArgumentType)arg.Type);
                else
                    ultArg.FSetType(PersistentArgumentType.ReturnValue);

                switch (arg.Type)
                {
                    case (PersistentArgumentTypeExt.Bool):
                        ultArg.Bool = arg._Int >= 1;
                        break;
                    case (PersistentArgumentTypeExt.String):
                        ultArg.String = arg._String;
                        break;
                    case (PersistentArgumentTypeExt.Int):
                        ultArg.Int = arg._Int;
                        break;
                    case (PersistentArgumentTypeExt.Enum):
                        ultArg.Enum = arg._Int;
                        break;
                    case (PersistentArgumentTypeExt.Float):
                        ultArg.Float = arg._X;
                        break;
                    case (PersistentArgumentTypeExt.Vector2):
                        ultArg.Vector2 = new Vector2(arg._X, arg._Y);
                        break;
                    case (PersistentArgumentTypeExt.Vector3):
                        ultArg.Vector3 = new Vector3(arg._X, arg._Y, arg._Z);
                        break;
                    case (PersistentArgumentTypeExt.Vector4):
                        ultArg.Vector4 = new Vector4(arg._X, arg._Y, arg._Z, arg._W);
                        break;
                    case (PersistentArgumentTypeExt.Quaternion):
                        ultArg.Quaternion = Quaternion.Euler(arg._X, arg._Y, arg._Z);
                        break;
                    case (PersistentArgumentTypeExt.Color):
                        ultArg.Color = new Color(arg._X, arg._Y, arg._Z, arg._W);
                        break;
                    case (PersistentArgumentTypeExt.Color32):
                        ultArg.Color32 = new Color32((byte)(arg._Int), (byte)(arg._Int >> 8), (byte)(arg._Int >> 16), (byte)(arg._Int >> 24));
                        break;
                    case (PersistentArgumentTypeExt.Rect):
                        ultArg.Rect = new Rect(arg._X, arg._Y, arg._Z, arg._W);
                        break;
                    case (PersistentArgumentTypeExt.Object):
                        ultArg.Object = arg._Object;
                        break;
                    case (PersistentArgumentTypeExt.Parameter):
                        // WARNING
                        break;
                    case (PersistentArgumentTypeExt.ReturnValue):
                        // WARNING
                        break;
                    case (PersistentArgumentTypeExt.NodeParameter):
                        // WARNING
                        break;
                        
                }
                argList.Add(ultArg);
            }
            ultCall.FSetArguments(argList.ToArray());

            for(int j = 0; j < dataOutputs.Length; j++)
            {
                if(dataOutputs[j].persistentCallForReturn == i) // If this call is needed for one of the returns
                {
                    node.DataOutputs[j].CompEvt = evt;
                    node.DataOutputs[j].CompCall = ultCall;
                }
            }
            
            evt.PersistentCallsList.Add(ultCall);
        }

        var nextNode = node.FlowOutputs[0].Target?.Node;
        if (nextNode != null)
            nextNode.Book.CompileNode(evt, nextNode, dataRoot);
    }
}
#endif
