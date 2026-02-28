using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum NoteType { Info, Warning, Error, Todo }

public class NoteComponent : MonoBehaviour
{
    public NoteType type = NoteType.Info;
    [TextArea(3, 20)]
    public string note = "";
    [HideInInspector] public float noteHeight;   // persisted display height
    [HideInInspector] public bool  noteLocked;   // persisted lock state
}

#if UNITY_EDITOR

[CustomEditor(typeof(NoteComponent))]
public class NoteComponentEditor : Editor
{
    private struct NoteStyle
    {
        public Color  header;
        public Color  border;
        public string label;
        public string icon;
    }

    private static readonly NoteStyle[] _styles =
    {
        new NoteStyle { header = new Color(0.20f, 0.40f, 0.65f), border = new Color(0.15f, 0.30f, 0.55f), label = "Info",    icon = "i" },
        new NoteStyle { header = new Color(0.65f, 0.50f, 0.10f), border = new Color(0.50f, 0.35f, 0.05f), label = "Warning", icon = "!" },
        new NoteStyle { header = new Color(0.60f, 0.15f, 0.15f), border = new Color(0.45f, 0.08f, 0.08f), label = "Error",   icon = "✕" },
        new NoteStyle { header = new Color(0.25f, 0.50f, 0.25f), border = new Color(0.15f, 0.38f, 0.15f), label = "Todo",    icon = "✓" },
    };

    private static readonly Color BgColor      = new Color(0.18f, 0.18f, 0.18f);
    private static readonly Color HandleColor   = new Color(0.13f, 0.13f, 0.13f);
    private static readonly Color HandleHover   = new Color(0.25f, 0.25f, 0.25f);
    private static readonly Color HandleLocked  = new Color(0.22f, 0.16f, 0.08f);

    private const float MinHeight    = 42f;
    private const float MaxHeight    = 600f;
    private const float HandleHeight = 10f;

    private GUIStyle _textAreaStyle;
    private GUIStyle _tabStyle;
    private GUIStyle _tabActiveStyle;
    private GUIStyle _lockLabelStyle;

    private SerializedProperty _noteProp;
    private SerializedProperty _typeProp;
    private SerializedProperty _heightProp;
    private SerializedProperty _lockedProp;

    // Transient drag state (not persisted)
    private float _dragAnchorY;
    private float _dragAnchorHeight;

    private void OnEnable()
    {
        _noteProp   = serializedObject.FindProperty("note");
        _typeProp   = serializedObject.FindProperty("type");
        _heightProp = serializedObject.FindProperty("noteHeight");
        _lockedProp = serializedObject.FindProperty("noteLocked");
    }

    public override void OnInspectorGUI()
    {
        EnsureStyles();
        serializedObject.Update();

        int       currentType = _typeProp.enumValueIndex;
        NoteStyle style       = _styles[currentType];

        float userSetHeight = _heightProp.floatValue;
        bool  isLocked      = _lockedProp.boolValue;

        // ── Header ───────────────────────────────────────────────────────────
        Rect headerRect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(headerRect, style.header);
        GUI.Label(new Rect(headerRect.x + 6,  headerRect.y + 4, 16, 16), style.icon, EditorStyles.boldLabel);
        GUI.Label(new Rect(headerRect.x + 26, headerRect.y,     50, headerRect.height), "Notes", EditorStyles.boldLabel);

        float tabW = 54f, tabH = 18f;
        float tabY = headerRect.y + (headerRect.height - tabH) * 0.5f;
        float tabX = headerRect.xMax - _styles.Length * tabW - 4;
        for (int i = 0; i < _styles.Length; i++)
        {
            Rect tabRect  = new Rect(tabX + i * tabW, tabY, tabW - 2, tabH);
            bool isActive = i == currentType;
            EditorGUI.DrawRect(tabRect, isActive ? _styles[i].header : new Color(0.15f, 0.15f, 0.15f));
            EditorGUI.DrawRect(new Rect(tabRect.x, tabRect.yMax, tabRect.width, 2),
                               isActive ? _styles[i].border : new Color(0.1f, 0.1f, 0.1f));
            if (GUI.Button(tabRect, _styles[i].label, isActive ? _tabActiveStyle : _tabStyle))
            {
                _typeProp.enumValueIndex = i;
                currentType = i;
                style       = _styles[i];
                GUI.FocusControl(null);
            }
        }

        // ── Top border ───────────────────────────────────────────────────────
        EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), style.border);

        // ── Text area ────────────────────────────────────────────────────────
        float contentH = Mathf.Max(1, _noteProp.stringValue.Split('\n').Length) * _textAreaStyle.lineHeight
                         + _textAreaStyle.padding.vertical;

        if (isLocked)
        {
            userSetHeight = Mathf.Clamp(contentH, MinHeight, MaxHeight);
        }
        else if (userSetHeight < MinHeight)
        {
            userSetHeight = Mathf.Clamp(contentH, MinHeight, 200f);
        }

        float displayH = Mathf.Clamp(userSetHeight, MinHeight, MaxHeight);

        Rect bgRect = GUILayoutUtility.GetRect(0, displayH + 4, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(bgRect, BgColor);

        EditorGUI.BeginChangeCheck();
        string newValue = EditorGUI.TextArea(
            new Rect(bgRect.x + 4, bgRect.y + 2, bgRect.width - 8, bgRect.height - 4),
            _noteProp.stringValue, _textAreaStyle);
        if (EditorGUI.EndChangeCheck())
            _noteProp.stringValue = newValue;

        // ── Resize handle ────────────────────────────────────────────────────
        int  controlID  = GUIUtility.GetControlID(FocusType.Passive);
        Rect handleRect = GUILayoutUtility.GetRect(0, HandleHeight, GUILayout.ExpandWidth(true));
        bool isDragging = GUIUtility.hotControl == controlID;
        bool isHovered  = handleRect.Contains(Event.current.mousePosition);

        Color handleBg = isLocked         ? HandleLocked :
                         isDragging || isHovered ? HandleHover : HandleColor;
        EditorGUI.DrawRect(handleRect, handleBg);

        float cx = handleRect.center.x, cy = handleRect.center.y;
        if (isLocked)
        {
            Rect lockOnHandle = new Rect(cx - 7, cy - 7, 14, 14);
            GUI.Label(lockOnHandle, EditorGUIUtility.IconContent("LockIcon-On"), _lockLabelStyle);
        }
        else
        {
            for (int d = -2; d <= 2; d++)
                EditorGUI.DrawRect(new Rect(cx + d * 6 - 1, cy - 1, 2, 2), new Color(0.45f, 0.45f, 0.45f));
        }

        EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeVertical);

        Event e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown when handleRect.Contains(e.mousePosition) && e.button == 0:
                if (e.clickCount == 2)
                {
                    isLocked = !isLocked;
                    _lockedProp.boolValue = isLocked;
                    GUIUtility.hotControl = 0;
                    Repaint();
                    e.Use();
                }
                else
                {
                    _dragAnchorY      = e.mousePosition.y;
                    _dragAnchorHeight = displayH;
                    GUIUtility.hotControl = controlID;
                    e.Use();
                }
                break;

            case EventType.MouseDrag when GUIUtility.hotControl == controlID:
                if (isLocked)
                {
                    isLocked = false;
                    _lockedProp.boolValue = false;
                }
                userSetHeight = Mathf.Clamp(
                    _dragAnchorHeight + (e.mousePosition.y - _dragAnchorY),
                    MinHeight, MaxHeight);
                _heightProp.floatValue = userSetHeight;
                Repaint();
                e.Use();
                break;

            case EventType.MouseUp when GUIUtility.hotControl == controlID:
                GUIUtility.hotControl = 0;
                e.Use();
                break;
        }

        // Write back height (covers locked content-tracking and auto-init)
        _heightProp.floatValue = userSetHeight;

        if (isHovered || isDragging) Repaint();

        // ── Bottom border ────────────────────────────────────────────────────
        EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), style.border);
        GUILayout.Space(2);

        serializedObject.ApplyModifiedProperties();
    }

    private void EnsureStyles()
    {
        if (_textAreaStyle != null) return;

        _textAreaStyle = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true,
            richText = false,
            fontSize = 12,
            padding  = new RectOffset(4, 4, 4, 4),
        };
        _textAreaStyle.normal.background  = null;
        _textAreaStyle.normal.textColor   = new Color(0.9f, 0.9f, 0.9f);
        _textAreaStyle.focused.background = null;
        _textAreaStyle.focused.textColor  = Color.white;

        _tabStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 10,
        };
        _tabStyle.normal.textColor = new Color(0.65f, 0.65f, 0.65f);

        _tabActiveStyle = new GUIStyle(_tabStyle) { fontStyle = FontStyle.Bold };
        _tabActiveStyle.normal.textColor = Color.white;

        _lockLabelStyle = new GUIStyle(GUIStyle.none)
        {
            alignment = TextAnchor.MiddleCenter,
            padding   = new RectOffset(0, 0, 0, 0),
        };
    }
}

#endif
