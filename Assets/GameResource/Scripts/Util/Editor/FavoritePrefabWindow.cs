using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using System.Linq;

public class FavoritePrefabWindow : EditorWindow
{
    private List<GameObject> prefabs;
    private VisualElement prefabListContainer;
    private const string EditorPrefsKey = "FavoritePrefabWindow_PrefabGUIDs";

    [MenuItem("Tools/Util/Favorite Prefab")]
    public static void ShowWindow()
    {
        GetWindow<FavoritePrefabWindow>("Favorite Prefab");
    }

    private void OnEnable()
    {
        LoadPrefabList();
    }

    private void OnDisable()
    {
        SavePrefabList();
    }

    private void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.paddingTop = root.style.paddingBottom = root.style.paddingLeft = root.style.paddingRight = 10;

        // 드래그 앤 드롭 영역
        var dropArea = new VisualElement
        {
            style = { height = 50, backgroundColor = new Color(0.2f, 0.2f, 0.2f), justifyContent = Justify.Center }
        };
        var dropLabel = new Label("프리팹을 여기에 놓으세요")
        {
            style = { alignSelf = Align.Center, color = Color.white }
        };
        dropArea.Add(dropLabel);
        root.Add(dropArea);

        // 프리팹 리스트
        prefabListContainer = new VisualElement();
        root.Add(prefabListContainer);

        // 드래그 앤 드롭
        dropArea.RegisterCallback<DragUpdatedEvent>(evt =>
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
        });

        dropArea.RegisterCallback<DragPerformEvent>(evt =>
        {
            DragAndDrop.AcceptDrag();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go))
                {
                    if (prefabs == null)
                    {
                        prefabs = new List<GameObject>();
                    }
                    if (!prefabs.Contains(go))
                    {
                        prefabs.Add(go);
                        AddPrefabElement(go);
                        SavePrefabList();
                    }
                }
            }
        });

        // UI 갱신
        RefreshPrefabListUI();
    }

    private void AddPrefabElement(GameObject prefab)
    {
        var element = new VisualElement
        {
            style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingTop = 5, paddingBottom = 5, paddingLeft = 5, paddingRight = 5, marginBottom = 2, backgroundColor = new Color(0.15f, 0.15f, 0.15f) }
        };

        // 아이콘
        var icon = new Image
        {
            image = AssetPreview.GetAssetPreview(prefab),
            style = { width = 32, height = 32, marginRight = 5 }
        };
        element.Add(icon);

        // 프리팹 이름
        var nameLabel = new Label(prefab.name)
        {
            style = { flexGrow = 1, color = Color.white }
        };
        element.Add(nameLabel);

        // 편집 버튼
        var editButton = new Button(() =>
        {
            PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(prefab));
        })
        {
            text = "편집",
            style = { width = 60, marginRight = 5 }
        };
        element.Add(editButton);

        // 삭제 버튼
        var deleteButton = new Button(() =>
        {
            prefabs.Remove(prefab);
            prefabListContainer.Remove(element);
            SavePrefabList();
        })
        {
            text = "삭제",
            style = { width = 60 },
        };
        element.Add(deleteButton);

        // 드래그 앤 드롭으로 순서 변경
        element.AddManipulator(new DragManipulator(element, prefabListContainer, prefabs, prefab, this));

        prefabListContainer.Add(element);
    }

    private void RefreshPrefabListUI()
    {
        prefabListContainer.Clear();
        if (prefabs == null || prefabs.Count == 0)
        {
            return; // 빈 리스트 또는 null이면 UI에 아무것도 표시하지 않음
        }

        foreach (var prefab in prefabs)
        {
            if (prefab != null) // null 체크
            {
                AddPrefabElement(prefab);
            }
        }
    }

    private void SavePrefabList()
    {
        if (prefabs == null || prefabs.Count == 0)
        {
            EditorPrefs.DeleteKey(EditorPrefsKey);
            return;
        }

        var guidList = prefabs
            .Where(p => p != null)
            .Select(p => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p)))
            .ToList();
        EditorPrefs.SetString(EditorPrefsKey, string.Join(",", guidList));
    }

    private void LoadPrefabList()
    {
        if (prefabs != null)
        {
            return; // 이미 초기화된 경우 무시
        }

        if (EditorPrefs.HasKey(EditorPrefsKey))
        {
            var guidString = EditorPrefs.GetString(EditorPrefsKey);
            if (!string.IsNullOrEmpty(guidString))
            {
                var guids = guidString.Split(',');
                prefabs = new List<GameObject>();
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        prefabs.Add(prefab);
                    }
                }
            }
        }

        if (prefabs == null)
        {
            prefabs = new List<GameObject>(); // 로드할 데이터가 없으면 빈 리스트
        }
    }

    private class DragManipulator : Manipulator
    {
        private VisualElement element;
        private VisualElement container;
        private List<GameObject> prefabs;
        private GameObject prefab;
        private FavoritePrefabWindow window;
        private bool isDragging;

        public DragManipulator(VisualElement element, VisualElement container, List<GameObject> prefabs, GameObject prefab, FavoritePrefabWindow window)
        {
            this.element = element;
            this.container = container;
            this.prefabs = prefabs;
            this.prefab = prefab;
            this.window = window;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                isDragging = true;
                target.CaptureMouse();
                evt.StopPropagation();
            }
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!isDragging) return;

            var pos = evt.mousePosition;
            int newIndex = -1;

            for (int i = 0; i < container.childCount; i++)
            {
                var child = container[i];
                if (child.worldBound.Contains(pos))
                {
                    newIndex = i;
                    break;
                }
            }

            if (newIndex >= 0 && newIndex < container.childCount)
            {
                container.Remove(element);
                container.Insert(newIndex, element);
                prefabs.Remove(prefab);
                prefabs.Insert(newIndex, prefab);
                window.SavePrefabList();
            }

            evt.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (isDragging)
            {
                isDragging = false;
                target.ReleaseMouse();
                evt.StopPropagation();
            }
        }
    }
}