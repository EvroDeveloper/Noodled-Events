#if UNITY_EDITOR
using NoodledEvents;
using SLZ.Marrow.Warehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UltEvents;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;


public class UltNoodleEditor : EditorWindow
{
    [MenuItem("NoodledEvents/Noodle Editor")]
    public static void ShowExample()
    {
        UltNoodleEditor wnd = GetWindow<UltNoodleEditor>();
        wnd.Show();
        wnd.titleContent = new GUIContent("Scene Noodle Editor");
    }
    [MenuItem("NoodledEvents/test")]
    public static void test()
    {
        Debug.Log(typeof(Rigidbody).AssemblyQualifiedName);
        //Selection.activeGameObject.GetComponent<UltEventHolder>().Event.PersistentCallsList[0].SetMethod(typeof(NavMeshHit).GetConstructors(UltEventUtils.AnyAccessBindings)[0], null);
        //AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<CommonsCookBook>(), "Assets/CommonsCookBook.asset");
        return; 
        
    }

    public static string ScriptPath
    {
        get
        {
            var g = AssetDatabase.FindAssets($"t:Script {nameof(UltNoodleEditor)}");
            return AssetDatabase.GUIDToAssetPath(g[0]);
        }
    }

    public VisualElement NodesFrame;
    public VisualElement A;
    public VisualElement B;
    public VisualElement C;
    public VisualElement D;
    public VisualElement SearchMenu;
    public TextField SearchBar;
    public ScrollView SearchedTypes;
    public Toggle StaticsToggle;
    private VisualElement cog;
    public VisualElement SearchSettings;
    public static Label TypeHinter;
    private static UltNoodleEditor s_Editor;
    public void CreateGUI()
    {
        s_Editor = this;
        VisualElement root = rootVisualElement;

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ScriptPath.Replace(".cs", ".uxml"));
        VisualElement loadedUXML = visualTree.Instantiate();
        root.Add(loadedUXML);
         
        NodesFrame = root.Q(nameof(NodesFrame));
        // These are the Node Viewing Pivots.
        A = root.Q(nameof(A));
        B = root.Q(nameof(B));
        C = root.Q(nameof(C));
        D = root.Q(nameof(D));
        SearchMenu = root.Q(nameof(SearchMenu));
        SearchBar = SearchMenu.Q<TextField>(nameof(SearchBar));
        SearchedTypes = SearchMenu.Q<ScrollView>(nameof(SearchedTypes));
        SearchMenu.visible = false;
        TypeHinter = root.Q<Label>(nameof(TypeHinter));
        //StaticsToggle = SearchMenu.Q<Toggle>("StaticsToggle");
        //StaticsToggle.RegisterValueChangedCallback((v) => SearchTypes());
        //StaticsToggle.Children().ToArray()[1].style.flexGrow = 0;

        NodesFrame.RegisterCallback<WheelEvent>(OnScroll);
        NodesFrame.RegisterCallback<MouseDownEvent>(NodeFrameMouseDown);
        NodesFrame.RegisterCallback<MouseMoveEvent>(NodeFrameMouseMove);
        NodesFrame.RegisterCallback<MouseUpEvent>(NodeFrameMouseUp);
        NodesFrame.RegisterCallback<KeyDownEvent>(NodeFrameKeyDown);
        root.panel.visualTree.RegisterCallback<KeyDownEvent>(NodeFrameKeyDown);

        SearchBar.RegisterValueChangedCallback((txt) =>
        {
            if (EditorPrefs.GetBool("SearchPerChar", true))
                SearchTypes(10);
        });
        SearchBar.RegisterCallback<KeyDownEvent>((evt) => {
            if (evt.keyCode == KeyCode.Return) {
                SearchTypes(100);
            }
        }, TrickleDown.TrickleDown);
        //SearchedTypes.RegisterCallback<WheelEvent>(OnSearchScroll);

        SearchSettings = root.Q(nameof(SearchSettings));
        root.Q<Button>("SettingsBT").clicked += () =>
        {
            SearchSettings.visible = !SearchSettings.visible;
            SearchSettings.style.display = DisplayStyle.Flex;
        };

        cog = root.Q(nameof(cog));
        EditorApplication.update += OnUpdate;

        // search autorefresh tog
        var spcTog = new Toggle("Search Per Char") { value = EditorPrefs.GetBool("SearchPerChar", true) };
        spcTog.RegisterValueChangedCallback(e =>
        {
            EditorPrefs.SetBool("SearchPerChar", e.newValue);
        });
        SearchSettings.Add(spcTog);



        AllNodeDefs.Clear();
        var cookBooks = AssetDatabase.FindAssets("t:" + nameof(CookBook)).Select(guid => AssetDatabase.LoadAssetAtPath<CookBook>(AssetDatabase.GUIDToAssetPath(guid)));
        foreach (CookBook sdenhr in cookBooks)
        {
            CookBook book = sdenhr; //lol (this is like this for a reason trust me)
            book.CollectDefs(AllNodeDefs);

            //also search toggle
            var tog = new Toggle(book.name) { value = true };
            tog.RegisterValueChangedCallback(e => 
            {
                BookFilters[book] = e.newValue;
                SearchTypes(100);
            });
            BookFilters[book] = true;
            SearchSettings.Add(tog);
        }
    }
    Dictionary<CookBook, bool> BookFilters = new Dictionary<CookBook, bool>();
    private bool _created = false;
    private bool _currentlyZooming;
    private float _zoom = 1;
    private Vector2 _zoomMousePos;
    public float Zoom
    {
        get => _zoom;
        private set
        {
            // Zoom logic; I'm bad at math and UXML doesn't compute transforms on-get/set, so we're doin it via a 1-update delay.
            _zoom = value;
            if (_currentlyZooming)
                return;
            _currentlyZooming = true;

            var cpos = C.LocalToWorld(Vector2.zero);
            A.style.left = new StyleLength(new Length(_zoomMousePos.x, LengthUnit.Pixel));
            A.style.top = new StyleLength(new Length(_zoomMousePos.y, LengthUnit.Pixel));

            C.schedule.Execute(() =>  
            {
                cpos = B.WorldToLocal(cpos);
                C.style.left = new StyleLength(new Length(cpos.x - 1, LengthUnit.Pixel));
                C.style.top = new StyleLength(new Length(cpos.y - 1, LengthUnit.Pixel));
                B.transform.scale = Vector3.one * _zoom;
                _currentlyZooming = false;
            }).ExecuteLater(1);
        }
    }
    public void SetZoom(float value, Vector2 pivot) { _zoomMousePos = pivot; Zoom = value; }
    private void OnScroll(WheelEvent evt)
    {
        float scalar = Mathf.Pow((evt.delta.y < 0) ? 1.01f : .99f, 4);
        SetZoom(Zoom * scalar, evt.localMousePosition);
    }


    public List<UltNoodleBowlUI> BowlUIs = new();

    private void OnFocus()
    {
        // on focus we refresh the nodes
        // lets get all ult event sources in the scene;
        // then, graph them out
        
        var curScene = SceneManager.GetActiveScene();

        foreach (var bowl in BowlUIs.ToArray())
            bowl.Validate();

        // autogen bowlsUIs
        foreach (var bowl in Resources.FindObjectsOfTypeAll<SerializedBowl>())
            if (bowl.gameObject.scene == curScene && !BowlUIs.Any(b => b.SerializedData == bowl) && bowl.gameObject.activeInHierarchy)
                UltNoodleBowlUI.New(this, D, bowl.EventHolder, bowl.BowlEvtHolderType, bowl.EventFieldPath);

        
    }
    [MenuItem("CONTEXT/UltEventHolder/Make Noodle Bowl")]
    static void BowlSingle(MenuCommand command)
    {
        if (!((UltEventHolder)command.context).GetComponent<SerializedBowl>())
        {
            if (s_Editor == null) ShowExample();
            UltNoodleBowlUI.New(s_Editor, s_Editor.D, (UltEventHolder)command.context, new SerializedType(typeof(UltEventHolder)), "_Event");
        }
    }
    [MenuItem("CONTEXT/CrateSpawner/Make Noodle Bowl")]
    static void CrateBowl(MenuCommand command)
    {
        if (!((CrateSpawner)command.context).GetComponent<SerializedBowl>())
        {
            if (s_Editor == null) ShowExample();
            UltNoodleBowlUI.New(s_Editor, s_Editor.D, (CrateSpawner)command.context, new SerializedType(typeof(CrateSpawner)), "onSpawnEvent");
        }
    }
    private void OnLostFocus()
    {
        foreach (var bowlUI in BowlUIs.ToArray())
        {
            if (bowlUI.SerializedData != null)
                bowlUI.SerializedData.Compile();
        }
        Debug.Log("Compiled!");
    }

    private bool _dragging;
    private void NodeFrameMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 1)
        {
            NodesFrame.CaptureMouse(); // to ensure we get MouseUp
            _dragging = true;
            NodesFrame.name = "grabby";
        }
        SearchMenu.visible = false;
        SearchSettings.visible = false;
    }
    public Vector2 _frameMousePosition;
    private void NodeFrameMouseMove(MouseMoveEvent evt)
    {
        _frameMousePosition = evt.localMousePosition;
        if (_dragging)
        {
            D.style.left = new StyleLength(new Length(D.style.left.value.value + (evt.mouseDelta.x / B.transform.scale.x), LengthUnit.Pixel));
            D.style.top = new StyleLength(new Length(D.style.top.value.value + (evt.mouseDelta.y / B.transform.scale.y), LengthUnit.Pixel));
        }
        TypeHinter.style.left = evt.mousePosition.x;
        TypeHinter.style.top = evt.mousePosition.y;
    }
    private void NodeFrameMouseUp(MouseUpEvent evt)
    {
        if (evt.button == 1)
        {
            NodesFrame.ReleaseMouse();
            _dragging = false;
            NodesFrame.name = nameof(NodesFrame);
        }
    }
    public static UltNoodleBowlUI NewNodeBowl;
    public static Vector2 NewNodePos;
    private void NodeFrameKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Space && UltNoodleBowlUI.CurrentBowlUI != null) // open Create Node Menu
        {
            ResetSearchFilter();
            OpenSearchMenu(false);
        }
    }

    public void ResetSearchFilter() 
    {
        FilteredNodeDefs = AllNodeDefs;
    }
    public void SetSearchFilter(bool pinIn, Type t) 
    {
        // lets cache the searchables

        // reset FilteredNodeDefs
        if (FilteredNodeDefs == AllNodeDefs)
            FilteredNodeDefs = new();
        else FilteredNodeDefs.Clear();

        foreach (var node in AllNodeDefs)
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
            } catch(TypeLoadException) { /* ignore evil types */ }
        }
        // awesome
    }

    public void OpenSearchMenu(bool useNodePos = true)
    {
        NewNodeBowl = UltNoodleBowlUI.CurrentBowlUI;
        if (NewNodeBowl == null) return;
        NewNodePos = NewNodeBowl.MousePos - new Vector2(48, 39);
        SearchMenu.visible = !SearchMenu.visible;
        SearchSettings.visible = false;
        if (SearchMenu.visible)
        {
            SearchMenu.style.left = useNodePos ? (NewNodeBowl.LocalToWorld(NewNodePos).x + 55) : _frameMousePosition.x;
            SearchMenu.style.top = useNodePos ? (NewNodeBowl.LocalToWorld(NewNodePos).y + 20) : _frameMousePosition.y + 25;
            SearchBar.value = "";
            SearchBar.Focus();
            SearchBar.SelectAll();
            SearchBar.schedule.Execute(() =>
            {
                SearchBar.Focus();
                SearchBar.ElementAt(0).Focus();
                SearchBar.schedule.Execute(() =>
                {
                    SearchBar.Focus();
                    SearchBar.ElementAt(0).Focus();
                    SearchTypes(25);
                });
                SearchTypes(25);
            });
        }
    }
    private void SearchTypes(int dispNum)
    {
        this.SearchedTypes.Clear();
       
        // To be replaced with some better comparison algorithm.
        bool CompareString(string stringOne, string stringTwo) {
            return stringOne.Contains(stringTwo, StringComparison.CurrentCultureIgnoreCase);
        }

        // Collect first x that match
        int i = dispNum;
        foreach(var nd in FilteredNodeDefs)
        {
            if (i <= 0)
            {
                SearchedTypes.Add(GetIncompleteListDisplay());
                break;
            }
            if (!BookFilters[nd.CookBook]) continue;

            string targetSearch = SearchBar.value;
            string[] splitResults = null;

            // Ex. "rigidbody.kinematic" will search for things start start with "rigidbody." and contain "kinematic".
            // But things like ".kinematic" need to be accounted for as obviously nothing can start with "".
            if (!targetSearch.StartsWith(".") && targetSearch.Contains(".")) {
                splitResults = targetSearch.Split('.');
                targetSearch = splitResults[0] + ".";
            }

            // Primary filter, either strict startswith or loose compare
            if (((splitResults != null) && nd.Name.StartsWith(targetSearch, StringComparison.CurrentCultureIgnoreCase)) || ((splitResults == null) && CompareString(nd.Name, targetSearch)))
            {
                // Secondary filter, second part compare check
                if ((splitResults != null) && !CompareString(nd.Name, splitResults[1]))
                        continue;

                i--;
                SearchedTypes.Add(nd.SearchItem);
            }
        } 
    }

    private VisualElement GetIncompleteListDisplay() {
        var o = new Label() {
            text = "..."
            
        };

        o.style.alignContent = Align.Center;
        o.style.alignSelf = Align.Center;
        o.style.unityTextAlign = TextAnchor.MiddleCenter;

        return o;
    }

    
    List<CookBook.NodeDef> AllNodeDefs = new();
    List<CookBook.NodeDef> FilteredNodeDefs = new();

    /*
    private int LoadedSearchPages;
    private void SearchTypes()
    {
        SearchedTypes.Clear();
        LoadedSearchPages = 0;
        SearchedTypes.scrollOffset = Vector2.zero;

        if (SearchBar.value == "")
            foreach (var f in TypeFolds.Values)
                f.value = false;

        string typeSearchString = SearchBar.value.Contains('.') ? SearchBar.value.Split('.').First() : SearchBar.value;
        if (!StaticsToggle.value)
            SortedTypes = SearchableTypes.Where(t => (t.IsSubclassOf(typeof(UnityEngine.Object)) || t == typeof(UnityEngine.Object)) && t.Name.StartsWith(typeSearchString, true, null)).ToArray();
        else
            SortedTypes = SearchableTypes.Where(t => t.Name.StartsWith(typeSearchString, true, null)).Where(t =>
            {
                try
                {
                    return t.GetMethods(UltEventUtils.AnyAccessBindings).Any(m => m.IsStatic);
                } catch(TypeLoadException ex)
                {
                    return false;
                }
            }).ToArray();
        EditorUtility.ClearProgressBar(); //unity bug

        // sort todo
        BottomSearchSpacer ??= new();
        SearchedTypes.Add(BottomSearchSpacer);
        LoadNextSearchPage();


    }
    private VisualElement BottomSearchSpacer;
    private void LoadNextSearchPage()
    {
        LoadedSearchPages++;

        for (int i = (LoadedSearchPages - 1)*20; i < Math.Min(LoadedSearchPages*20, SortedTypes.Length); i++)
        {
            Foldout f = null;
            if (!TypeFolds.TryGetValue(SortedTypes[i], out f))
            {
                Type t = SortedTypes[i];
                f = new Foldout();
                f.text = t.FullName;
                TypeFolds[t] = f;

                f.value = false;

                // when open, show each method :3
                // if search has '.', filter methods by search
                void SearchMethods()
                {
                    if (f.contentContainer.childCount == 0) // gotta generate bt for each func
                    {
                        // generate the Property Buttons
                        PropertyInfo[] props = t.GetProperties(UltEventUtils.AnyAccessBindings);
                        Foldout propFold = new Foldout();
                        propFold.name = "Properties";
                        propFold.text = "Properties:";
                        propFold.Q<Toggle>().style.marginLeft = 4;
                        f.contentContainer.Add(propFold);
                        foreach (var prop in props)
                        {
                            if (prop.DeclaringType != t || prop.Name.EndsWith("_Injected")) continue;
                            string propDisplayName = prop.PropertyType.GetFriendlyName() + " " + prop.Name;
                            propDisplayName += " {";
                            if (prop.GetMethod != null)
                                propDisplayName += " get;";
                            if (prop.SetMethod != null)
                                propDisplayName += " set;";
                            propDisplayName += " }";
                            var newBT = new Button(() => 
                            {
                                // on prop bt click
                                curCreateNodeBowl.SerializedData.NodeDatas.Add(new SerializedNode(curCreateNodeBowl.SerializedData, prop.GetMethod ?? prop.SetMethod)
                                { Position = curCreateNodePos });
                                curCreateNodeBowl.Validate();
                                SearchMenu.visible = false;
                            });
                            newBT.text = propDisplayName;
                            newBT.name = prop.Name;
                            newBT.style.unityTextAlign = TextAnchor.MiddleLeft;
                            propFold.Add(newBT);
                        }

                        // Generate the Method buttons
                        var meths = t.GetMethods(UltEventUtils.AnyAccessBindings);//.Where(m => !props.Any(p => p.SetMethod == m || p.GetMethod == m));
                        Foldout methFold = new Foldout();
                        methFold.name = "Methods";
                        methFold.text = "Methods:";
                        methFold.Q<Toggle>().style.marginLeft = 4;
                        f.contentContainer.Add(methFold);
                        foreach (var meth in meths)
                        {
                            if (meth.DeclaringType != t || meth.Name.EndsWith("_Injected")) continue;
                            string memLong = meth.Name + "(";
                            //if (meth.ReturnType != null && meth.ReturnType != typeof(void))
                                memLong = meth.ReturnType.GetFriendlyName() + " " + memLong;
                            ParameterInfo[] paramz = meth.GetParameters();
                            foreach (var param in paramz)
                                memLong += $"{param.ParameterType.GetFriendlyName()}, ";
                            if (paramz != null && paramz.Length > 0)
                                memLong = memLong.Substring(0, memLong.Length - 2);
                            memLong += ')';
                            var newBT = new Button(() => 
                            {
                                // on meth bt click
                                curCreateNodeBowl.SerializedData.NodeDatas.Add(new SerializedNode(curCreateNodeBowl.SerializedData, meth)
                                { Position = curCreateNodePos });
                                curCreateNodeBowl.Validate();
                                SearchMenu.visible = false;
                            });
                            newBT.text = memLong;
                            newBT.name = meth.Name;
                            newBT.style.unityTextAlign = TextAnchor.MiddleLeft;
                            methFold.Add(newBT);
                        }
                    }
                    // '.' search
                    if (SearchBar.value.Contains('.'))
                    {
                        string memSearchTerm = SearchBar.value.Split('.').Last();
                        foreach (var chil in f.contentContainer.Children().SelectMany(c => { /*((Foldout)c).value = true;*/
    /* return ((Foldout)c).contentContainer.Children(); }))
                            chil.style.display = chil.name.StartsWith(memSearchTerm, true, null) ? DisplayStyle.Flex : DisplayStyle.None; 
                    }
                }

                SearchBar.RegisterValueChangedCallback((v) =>
                {
                    if (f.value) // is open
                        SearchMethods();
                    else // not open
                        if (f.parent != null && SearchBar.value.EndsWith('.'))
                    {
                        f.value = true;
                    }
                });
                f.RegisterValueChangedCallback((v) =>
                {
                    if (v.newValue) // on open
                        SearchMethods();
                }); 

                // if it's a UnityEngine.Object, show icon too
                if (t.IsSubclassOf(typeof(UnityEngine.Object)) || t == typeof(UnityEngine.Object)) 
                {
                    var icon = EditorGUIUtility.ObjectContent(null, t).image;
                    if (icon != null)
                    {
                        var iconElement = new VisualElement();
                        iconElement.style.backgroundImage = new StyleBackground((Texture2D)icon);
                        iconElement.name = "Icon";
                        iconElement.style.minWidth = 20;
                        iconElement.style.minHeight = 20;
                        iconElement.style.marginRight = 5;
                        f.Q<Label>().parent.Insert(1, iconElement);
                        f.Q<Label>().parent.ElementAt(0).style.marginRight = 0;
                    }
                }
            }
            SearchedTypes.Add(f);
        }
        BottomSearchSpacer.style.minHeight = (SortedTypes.Length - (LoadedSearchPages * 20)) * 20;
        BottomSearchSpacer.BringToFront();
    }
    private void OnSearchScroll(WheelEvent evt) // if the user scrolls down load moar.
    {
        LoadVisibleSearchResults();
    }
    private void LoadVisibleSearchResults()
    {
        int curScrollPage = Mathf.CeilToInt(SearchedTypes.scrollOffset.y / 162f);
        if (curScrollPage > LoadedSearchPages)
            for (int i = 0; i < curScrollPage - LoadedSearchPages; i++)
                LoadNextSearchPage();
    }*/

    public Type[] SortedTypes;
    private static Type[] s_tps;
    public static Type[] SearchableTypes 
    {  
        get 
        {
            if (s_tps == null)
            {
                s_tps = UltNoodleExtensions.GetAllTypes();
                EditorUtility.ClearProgressBar();
            }
                
            return s_tps; 
        } 
    }
    public static Dictionary<Type, Foldout> TypeFolds = new();

    private void OnUpdate()
    {
        //if (SearchMenu.visible)
        //    LoadVisibleSearchResults();
        cog.style.rotate = new Rotate(cog.style.rotate.value.angle.value + .01f);
    }
    private void OnDestroy()
    {
        EditorApplication.update -= OnUpdate;
    }
}
#endif