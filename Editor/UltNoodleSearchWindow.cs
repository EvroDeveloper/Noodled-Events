#if UNITY_EDITOR
using NoodledEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public static class NodeSearchIndex
{
    public class NodeSearchResult : IComparable<NodeSearchResult>
    {
        public CookBook.NodeDef nodeDef;
        public int keywordsMatched;

        public NodeSearchResult(CookBook.NodeDef node)
        {
            nodeDef = node;
            keywordsMatched = 0;
        }

        public int CompareTo(NodeSearchResult other)
        {
            return keywordsMatched - other.keywordsMatched;
        }
    }

    private static Dictionary<string, HashSet<CookBook.NodeDef>> _index = new();

    public static void IndexNode(CookBook.NodeDef nodeDef)
    {
        //TEMPORARY KEYWORD STUFF PLEASE REWORK LATER
        List<string> nodeKeywords = SplitSeachToIndex(nodeDef.Name).ToList();

        foreach(string keyword in nodeKeywords)
        {
            for(int i = 1; i <= keyword.Length; i++)
            {
                string subKeyword = keyword.Substring(0, i);
                if(!_index.ContainsKey(subKeyword)) _index.Add(subKeyword, new HashSet<CookBook.NodeDef>());
                _index[subKeyword].Add(nodeDef);
            }
        }
    }

    public static void IndexNodes(IEnumerable<CookBook.NodeDef> nodes)
    {
        foreach(CookBook.NodeDef node in nodes)
        {
            IndexNode(node);
        }
    }

    private static string[] SplitSeachToIndex(string search)
    {
        char[] separators = new char[] {' ', '.', ',', '_'};
        string[] splits = search.ToLower().Split(separators, StringSplitOptions.RemoveEmptyEntries);
        return splits.Append(search).ToArray();
    }

    public static CookBook.NodeDef[] SearchNodes(string search)
    {
        Dictionary<CookBook.NodeDef, int> nodeSearch = GetNodes(search);
        return Sort(nodeSearch);
    }

    private static CookBook.NodeDef[] Sort(Dictionary<CookBook.NodeDef, int> nodeSearch)
    {
        return nodeSearch.OrderByDescending(kvp => kvp.Value).Select(kvp => kvp.Key).ToArray();
    }

    public static Dictionary<CookBook.NodeDef, int> GetNodes(string search)
    {
        string[] splitIndex = SplitSeachToIndex(search);
        Dictionary<CookBook.NodeDef, int> attempts = new();
        List<string> intersectIndexes = new();
        foreach(string index in splitIndex)
        {
            if(_index.ContainsKey(index))
            {
                HashSet<CookBook.NodeDef> indexedNodes = _index[index];

                bool onlyPrioritize = false;
                if(indexedNodes.Count >= 1000)
                {
                    onlyPrioritize = true;
                }

                foreach(CookBook.NodeDef node in indexedNodes)
                {
                    if(onlyPrioritize)
                    {
                        if(attempts.ContainsKey(node)) attempts[node] = attempts[node] + 1;
                    }
                    else
                    {
                        if(!attempts.ContainsKey(node)) attempts.Add(node, 0);
                        attempts[node] = attempts[node] + 1;
                    }
                }
            }
        }
        List<CookBook.NodeDef> onesToRemove = new();
        foreach(string keyword in intersectIndexes)
        {
            if(_index.ContainsKey(keyword))
            {
                foreach(CookBook.NodeDef nodeDef in attempts.Keys)
                {
                    if(!_index[keyword].Contains(nodeDef)) onesToRemove.Add(nodeDef);
                }
            }
        }
        foreach(var one in onesToRemove)
        {
            attempts.Remove(one);
        }
        return attempts;
    }

    public static Dictionary<CookBook.NodeDef, int> GetNodes(string search, string[] requiredKeywords)
    {
        Dictionary<CookBook.NodeDef, int> allMatchingSearch = GetNodes(search);
        foreach(string keyword in requiredKeywords)
        {
            if(_index.ContainsKey(keyword))
            {
                foreach(CookBook.NodeDef nodeDef in allMatchingSearch.Keys)
                {
                    if(!_index[keyword].Contains(nodeDef)) allMatchingSearch.Remove(nodeDef);
                }
            }
        }
        return allMatchingSearch;
    }
}

public class UltNoodleSearchWindow : EditorWindow
{
    public static bool IsSearchOpen => _activeWindow != null;
    private static UltNoodleSearchWindow _activeWindow;

    public bool IsFocused => focusedWindow == this;

    private VisualElement _searchMenu;
    private TextField _searchBar;
    private Label _searchText;
    private ScrollView _searchedTypes;

    private Button _settingsButton;
    private VisualElement _settingsMenu;

    private Vector2 _pendingScreenPos;

    private List<CookBook.NodeDef> FilteredNodeDefs = new();
    private Dictionary<Type, bool> BookFilters = new();

    public static void ForceClose()
    {
        _activeWindow?.Close();
        _activeWindow = null;

        if (UltNoodleEditor.Editor == null || UltNoodleEditor.Editor.TreeView == null) return;
        UltNoodleEditor.Editor.TreeView.PendingEdgeOriginPort = null; // either we made a connection or cancelled, clear this
    }

    public static UltNoodleSearchWindow Open(UltNoodleTreeView graphView, Vector2 screenPos)
    {
        return InternalOpen(graphView, screenPos, null);
    }

    public static UltNoodleSearchWindow Open(UltNoodleTreeView graphView, Vector2 screenPos, Edge edge)
    {
        if (edge.output != null)
        {
            if (edge.output.userData is not NoodleDataOutput dataOut) return Open(graphView, screenPos); // fallback to generic search
            return InternalOpen(graphView, screenPos, dataOut.Type);
        } else
        {
            if (edge.input.userData is not NoodleDataInput dataIn) return Open(graphView, screenPos); // fallback to generic search
            return InternalOpen(graphView, screenPos, dataIn.Type, false);
        }
    }

    private static UltNoodleSearchWindow InternalOpen(UltNoodleTreeView graphView, Vector2 screenPos, Type filterType, bool filterDir = true)
    {
        _activeWindow?.Close();
        _activeWindow = null;

        var window = CreateInstance<UltNoodleSearchWindow>();
        _activeWindow = window;
        window._pendingScreenPos = screenPos;
        window.position = new Rect(-10000, -10000, 1, 1); // hide until uxml loads and we can size properly
        if (filterType != null)
            window.SetSearchFilter(filterDir, filterType);
        else
            window.FilteredNodeDefs = UltNoodleEditor.AllNodeDefs;
        window.ShowPopup();
        return window;
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UltNoodleEditor.EditorFolder}/Styles/UltNoodleSearchWindow.uxml");
        visualTree.CloneTree(root);

        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{UltNoodleEditor.EditorFolder}/Styles/UltNoodleSearchWindow.uss");
        root.styleSheets.Add(styleSheet);

        _searchMenu = root.Q<VisualElement>("SearchMenu");
        _searchBar = _searchMenu.Q<TextField>("SearchBar");
        _searchText = _searchMenu.Q<Label>("SearchText");
        _searchedTypes = _searchMenu.Q<ScrollView>("SearchedTypes");

        _settingsButton = _searchMenu.Q<Button>("SettingsButton");
        _settingsMenu = _searchMenu.Q<VisualElement>("SettingsMenu");

        // handle setting to mouse position after size is known
        Action fixPosition = null;
        int waitCounter = 0;
        fixPosition = () =>
        {
            float w = _searchMenu.resolvedStyle.width;
            float h = _searchMenu.resolvedStyle.height;

            // root.schedule.Execute often runs too early - wait one more time.
            if (float.NaN.Equals(w) || float.NaN.Equals(h))
            {
                waitCounter++;
                if (waitCounter > 10) throw new Exception("[NoodledEvents] Search Menu failed to figure out a good Position and Size!");
                root.schedule.Execute(fixPosition);
                return;
            }

            if (w > 0 && h > 0)
            {
                minSize = maxSize = new Vector2(w, h);
                position = new Rect(_pendingScreenPos, new Vector2(w, h));
            }
        };
        root.schedule.Execute(fixPosition);

        // search bar callbacks
        _searchBar.RegisterValueChangedCallback((txt) =>
        {
            if (EditorPrefs.GetBool("SearchPerChar", true))
                SearchTypes(10);
        });
        _searchBar.RegisterCallback<KeyDownEvent>((evt) =>
        {
            if (evt.keyCode == KeyCode.Return)
            {
                SearchTypes(100);
            } else if (evt.keyCode == KeyCode.Escape)
            {
                this.OnLostFocus();
            }
        }, TrickleDown.TrickleDown);

        _searchMenu.RegisterCallback<KeyDownEvent>((evt) =>
        {
            if (evt.keyCode == KeyCode.DownArrow)
            {
                if (_searchBar.panel.focusController.focusedElement == _searchBar && _searchedTypes.childCount > 1)
                {
                    _searchedTypes[0].Focus();
                    _searchedTypes.ScrollTo(_searchedTypes[0]);
                } else if (_searchBar.panel.focusController.focusedElement is Button b && b.parent == _searchedTypes)
                {
                    int btIdx = _searchedTypes.IndexOf(b);
                    if ((btIdx + 1) < _searchedTypes.childCount && _searchedTypes[btIdx + 1] is Button)
                    {
                        _searchedTypes[btIdx + 1].Focus();
                        _searchedTypes.ScrollTo(_searchedTypes[btIdx + 1]);
                    }
                }
            } else if (evt.keyCode == KeyCode.UpArrow)
            {
                if (_searchBar.panel.focusController.focusedElement is Button b && b.parent == _searchedTypes)
                {
                    int btIdx = _searchedTypes.IndexOf(b);
                    if (btIdx > 0)
                    {
                        _searchedTypes[btIdx - 1].Focus();
                        _searchedTypes.ScrollTo(_searchedTypes[btIdx - 1]);
                    }
                    else _searchBar.Focus(); 
                }
            }
        }, TrickleDown.TrickleDown);

        // settings button
        _settingsButton.clicked += () =>
        {
            _settingsMenu.visible = !_settingsMenu.visible;
        };

        // settings
        Toggle spcToggle = new Toggle("Search Per Character");
        spcToggle.value = EditorPrefs.GetBool("SearchPerChar", true);
        spcToggle.RegisterValueChangedCallback((evt) =>
        {
            EditorPrefs.SetBool("SearchPerChar", evt.newValue);
        });
        _settingsMenu.Add(spcToggle);

        foreach (var cbType in UltNoodleEditor.AllBooks.Select(b => b.GetType()).Distinct())
        {
            var cbToggle = new Toggle(cbType.Name.Replace("CookBook", ""));
            cbToggle.value = EditorPrefs.GetBool($"Search{cbType.Name}", true);
            BookFilters[cbType] = cbToggle.value;
            cbToggle.RegisterValueChangedCallback((evt) =>
            {
                EditorPrefs.SetBool($"Search{cbType.Name}", evt.newValue);
                BookFilters[cbType] = evt.newValue;
                SearchTypes(100);
            });
            _settingsMenu.Add(cbToggle);
        }

        _searchBar.Focus();
        SearchTypes(25);
    }

    public void OnLostFocus()
    {
        // attempt to force clear any hints that may be visible
        GUIUtility.hotControl = 0;
        GUIUtility.keyboardControl = 0;

        focusedWindow?.rootVisualElement.Blur();

        ForceClose();
    }

    private void SetSearchFilter(bool pinIn, Type t)
    {
        // lets cache the searchables

        // reset FilteredNodeDefs
        if (FilteredNodeDefs == UltNoodleEditor.AllNodeDefs)
            FilteredNodeDefs = new();
        else
            FilteredNodeDefs.Clear();

        foreach (var node in UltNoodleEditor.AllNodeDefs)
        {
            try
            {
                foreach (var pin in pinIn ? node.Inputs : node.Outputs)
                {
                    if (pin.Flow) continue;

                    if ((pinIn ? pin.Type : t).IsAssignableFrom(pinIn ? t : pin.Type))
                    {
                        FilteredNodeDefs.Add(node);
                        break;
                    }
                }
            }
            catch (TypeLoadException) { /* ignore evil types */ }
        }

        FilteredNodeDefs.Sort((a, b) =>
        {
            var aTs = pinIn ? a.Inputs : a.Outputs;
            var bTs = pinIn ? b.Inputs : b.Outputs;
            bool aHasT = aTs.Any(p => p.Type == t);
            bool bHasT = bTs.Any(p => p.Type == t);
            if (aHasT && !bHasT) return -1;
            if (aHasT == bHasT) return 0;
            if (bHasT && !aHasT) return 1;
            throw new NotImplementedException();
        });
    }

    private void SearchTypes(int findNum)
    {
        _searchedTypes.Clear();
        string targetSearch = _searchBar.value;

        if(string.IsNullOrEmpty(targetSearch))
        {
            for(int i = 0; i < findNum; i++)
            {
                _searchedTypes.Add(FilteredNodeDefs[i].SearchItem);
            }
        }
        else
        {
            var nodes = NodeSearchIndex.SearchNodes(targetSearch);
            int found = 0;
            foreach (CookBook.NodeDef node in nodes)
            {
                if(found > findNum) break;
                _searchedTypes.Add(node.SearchItem);
                found++;
            }
        }
    }
    
    private VisualElement GetIncompleteListDisplay()
    {
        var o = new Label() {
            text = "Press Enter for a Full Search! (...)"
        };

        o.style.alignContent = Align.Center;
        o.style.alignSelf = Align.Center;
        o.style.unityTextAlign = TextAnchor.MiddleCenter;

        return o;
    }
}
#endif
