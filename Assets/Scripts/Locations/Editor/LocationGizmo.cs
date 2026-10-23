using UnityEditor;
using UnityEngine;

namespace Oyung.Locations.Editor
{
    /// <summary>
    /// Draws every Location in the Scene view as an icon lying on the
    /// floor, its arrow along the location's forward (+Z), with the id
    /// written flat behind it (along -Z). A gizmo, so the "Location" row
    /// of the Gizmos dropdown turns it on and off. Clicking it selects
    /// the location.
    /// </summary>
    internal static class LocationGizmo
    {
        private const string IconPath =
            "Assets/Scripts/Locations/Editor/LocationIcon.png";
        private const float IconSize = 0.4f;
        private const float TextHeight = 0.09f;
        // Above the floor, so the floor doesn't hide it.
        private const float Lift = 0.01f;
        private static readonly Color TextColor = new Color(1f, 0.6f, 0.2f);

        private static Material _icon;
        private static Material _text;
        private static Mesh _quad;
        private static Font _font;

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected |
                   GizmoType.Pickable)]
        private static void Draw(Location location, GizmoType type)
        {
            if (!Prepare()) return;
            var onFloor = Matrix4x4.TRS(
                location.Position + Vector3.up * Lift,
                location.Facing * Quaternion.Euler(90f, 0f, 0f),
                Vector3.one);

            _icon.SetPass(0);
            Graphics.DrawMeshNow(_quad,
                onFloor * Matrix4x4.Scale(Vector3.one * IconSize));

            var id = location.Id != null ? location.Id.name : location.name;
            DrawText(id, onFloor *
                Matrix4x4.Translate(new Vector3(0f, -IconSize * 0.6f, 0f)));

            // Invisible, only so a click on the icon selects it.
            Gizmos.matrix = onFloor;
            Gizmos.color = Color.clear;
            Gizmos.DrawCube(Vector3.zero,
                new Vector3(IconSize, IconSize, 0.001f));
            Gizmos.matrix = Matrix4x4.identity;
        }

        // CLAUDE: text as glyph quads from a built-in font, so it can lie
        // CLAUDE: flat like the icon (Handles.Label always faces the view).
        // CLAUDE: Centred at the given matrix, growing down along its -Y,
        // CLAUDE: which on the floor is the location's -Z.
        private static void DrawText(string text, Matrix4x4 at)
        {
            const int fontSize = 64;
            _font.RequestCharactersInTexture(text, fontSize);
            var scale = TextHeight / fontSize;
            var width = 0f;
            foreach (var ch in text)
                if (_font.GetCharacterInfo(ch, out var info, fontSize))
                    width += info.advance;

            var vertices = new Vector3[text.Length * 4];
            var uvs = new Vector2[text.Length * 4];
            var triangles = new int[text.Length * 6];
            var x = -width * 0.5f;
            var top = -fontSize * 0.8f;
            for (var i = 0; i < text.Length; i++)
            {
                if (!_font.GetCharacterInfo(text[i], out var info, fontSize))
                    continue;
                var v = i * 4;
                vertices[v] = new Vector3(x + info.minX, top + info.minY) * scale;
                vertices[v + 1] = new Vector3(x + info.maxX, top + info.minY) * scale;
                vertices[v + 2] = new Vector3(x + info.maxX, top + info.maxY) * scale;
                vertices[v + 3] = new Vector3(x + info.minX, top + info.maxY) * scale;
                uvs[v] = info.uvBottomLeft;
                uvs[v + 1] = info.uvBottomRight;
                uvs[v + 2] = info.uvTopRight;
                uvs[v + 3] = info.uvTopLeft;
                var t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 2;
                x += info.advance;
            }

            var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            _text.SetPass(0);
            Graphics.DrawMeshNow(mesh, at);
            Object.DestroyImmediate(mesh);
        }

        // Built once per domain; false while the icon isn't imported yet.
        private static bool Prepare()
        {
            if (_icon != null) return true;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (texture == null) return false;

            _icon = new Material(Shader.Find("Unlit/Transparent"))
            {
                mainTexture = texture,
                hideFlags = HideFlags.HideAndDontSave
            };
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text = new Material(_font.material)
            {
                color = TextColor,
                hideFlags = HideFlags.HideAndDontSave
            };
            _quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            return true;
        }
    }
}
