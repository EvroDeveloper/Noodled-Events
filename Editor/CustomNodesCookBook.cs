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

        var nextNode = node.FlowOutputs[0].Target?.Node;
        if (nextNode != null)
            nextNode.Book.CompileNode(evt, nextNode, dataRoot);
    }
}
#endif
