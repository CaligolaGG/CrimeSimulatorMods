using CaliMods.Core;
using UnityEngine;

namespace CaliMods.UI;

/// <summary>
/// The "Custom Mods" window: an IMGUI window drawn over the pause menu, listing every registered
/// feature with its toggle and sub-options. While it's open the pause menu's pages are hidden
/// (so clicks don't reach the buttons underneath); ESC / "Back" returns to the pause main page.
/// </summary>
internal sealed class CustomModsMenu : MonoBehaviour
{
    private const float RefHeight = 1080f;   // GUI is designed at 1080p and scaled to the screen
    private const float WindowWidth = 620f;

    private static CustomModsMenu _host;
    private static PauseMenu _menu;
    private static bool _open;
    private static bool _switchingPage;

    private Rect _window;
    private Vector2 _scroll;
    private GUIStyle _windowStyle, _titleStyle, _featureStyle, _descStyle, _optionStyle, _buttonStyle;
    private Texture2D _bg, _buttonBg, _buttonHoverBg;

    internal static void CreateHost()
    {
        if (_host != null)
            return;
        var go = new GameObject("CaliMods_CustomModsMenu");
        go.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(go);
        _host = go.AddComponent<CustomModsMenu>();
    }

    internal static void Open(PauseMenu menu)
    {
        _menu = menu;
        _switchingPage = true;
        try { menu.ActivatePage(-1); } // hide the pause pages; the blur background stays
        finally { _switchingPage = false; }
        _open = true;
    }

    internal static void Close() => _open = false;

    internal static void OnPauseMenuPageChanged()
    {
        if (!_switchingPage)
            _open = false;
    }

    private static void BackToPauseMenu()
    {
        _open = false;
        if (_menu != null && _menu.player != null && _menu.player.pauseActive)
            _menu.ActivatePage(0);
    }

    private void Update()
    {
        // Safety net: the pause menu got closed or destroyed some other way (scene change, death...).
        if (_open && (_menu == null || !_menu.isActiveAndEnabled || _menu.player == null || !_menu.player.pauseActive))
            _open = false;
    }

    private void OnGUI()
    {
        if (!_open)
            return;

        EnsureStyles();
        float scale = Screen.height / RefHeight;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        float screenW = Screen.width / scale;

        float height = Mathf.Min(RefHeight - 160f, 140f + FeaturesHeight());
        _window = new Rect((screenW - WindowWidth) / 2f, (RefHeight - height) / 2f, WindowWidth, height);
        GUI.Box(_window, GUIContent.none, _windowStyle);
        GUILayout.BeginArea(new Rect(_window.x + 24f, _window.y + 18f, _window.width - 48f, _window.height - 36f));
        DrawContents();
        GUILayout.EndArea();
    }

    private void DrawContents()
    {
        GUILayout.Label("Custom Mods", _titleStyle);
        GUILayout.Label($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}", _descStyle);
        GUILayout.Space(10f);

        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        foreach (var feature in FeatureRegistry.All)
        {
            bool on = GUILayout.Toggle(feature.Enabled.Value, "  " + feature.Name, _featureStyle);
            if (on != feature.Enabled.Value)
                feature.Enabled.Value = on;
            GUILayout.Label(feature.Description, _descStyle);

            GUI.enabled = feature.Enabled.Value;
            foreach (var option in feature.Options)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                GUILayout.BeginVertical();
                bool v = GUILayout.Toggle(option.Value.Value, "  " + option.Name, _optionStyle);
                if (v != option.Value.Value)
                    option.Value.Value = v;
                GUILayout.Label(option.Description, _descStyle);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            GUI.enabled = true;
            GUILayout.Space(14f);
        }
        GUILayout.EndScrollView();

        GUILayout.Space(6f);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Back", _buttonStyle, GUILayout.Width(160f), GUILayout.Height(40f)))
        {
            if (_menu != null) _menu.PlayClick();
            BackToPauseMenu();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private static float FeaturesHeight()
    {
        float h = 0f;
        foreach (var f in FeatureRegistry.All)
            h += 80f + f.Options.Count * 62f;
        return h + 60f;
    }

    private void EnsureStyles()
    {
        if (_windowStyle != null)
            return;

        _bg = MakeTex(new Color(0.05f, 0.07f, 0.12f, 0.94f));
        _buttonBg = MakeTex(new Color(0.15f, 0.25f, 0.45f, 1f));
        _buttonHoverBg = MakeTex(new Color(0.22f, 0.36f, 0.62f, 1f));

        _windowStyle = new GUIStyle(GUI.skin.box) { normal = { background = _bg } };
        _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        _featureStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 22, fontStyle = FontStyle.Bold };
        _featureStyle.normal.textColor = _featureStyle.onNormal.textColor = Color.white;
        _featureStyle.hover.textColor = _featureStyle.onHover.textColor = new Color(0.7f, 0.85f, 1f);
        _optionStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 19 };
        _optionStyle.normal.textColor = _optionStyle.onNormal.textColor = new Color(0.9f, 0.9f, 0.9f);
        _optionStyle.hover.textColor = _optionStyle.onHover.textColor = new Color(0.7f, 0.85f, 1f);
        _descStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = new Color(0.65f, 0.7f, 0.78f) } };
        _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
        _buttonStyle.normal.background = _buttonBg;
        _buttonStyle.hover.background = _buttonHoverBg;
        _buttonStyle.active.background = _buttonHoverBg;
        _buttonStyle.normal.textColor = _buttonStyle.hover.textColor = _buttonStyle.active.textColor = Color.white;
    }

    private static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
}
