using System;
using System.Collections.Generic;
using System.Linq;
using InputGlyphs.Display;
using InputGlyphs.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Oyung.Input.Unity
{
    /// <summary>
    /// Shows one input action's button for the device the player uses
    /// (LastUsedDevice), drawn by the InputGlyphs package; if a fill is
    /// given, the button fills left to right while held (Hold).
    /// Knows nothing about who shows it or when: its parent turns it on
    /// and off. Needs the InputGlyphsSetup prefab once in the scene.
    /// </summary>
    [AddComponentMenu("Oyung/Input/Input Icon")]
    public sealed class InputIcon : MonoBehaviour
    {
        [Tooltip("The action whose button is shown.")]
        [SerializeField] private InputActionReference action;

        [Tooltip("Where the button is drawn. Set its height; " +
                 "the width follows the button's shape.")]
        [SerializeField] private Image glyph;

        [Tooltip("Optional child of the glyph, stretched over it. " +
                 "Gets the glyph's sprite; its colour tints the fill.")]
        [SerializeField] private Image holdFill;

        [SerializeField] private GlyphsLayoutData layout =
            GlyphsLayoutData.Default;

        private readonly List<InputDevice> _devices = new List<InputDevice>();
        private readonly List<string> _paths = new List<string>();
        private Texture2D _texture;
        private Sprite _sprite;

        private void Awake()
        {
            if (action == null || action.action == null)
                throw new InvalidOperationException(
                    $"InputIcon on '{name}' has no action picked.");
            if (glyph == null)
                throw new InvalidOperationException(
                    $"InputIcon on '{name}' has no glyph Image.");
            _texture = new Texture2D(2, 2);
        }

#if UNITY_EDITOR
        // Width comes from the glyph's shape, so the sprite must fill it.
        private void OnValidate()
        {
            if (glyph != null) glyph.preserveAspect = false;
            if (holdFill == null) return;
            holdFill.preserveAspect = false;
            holdFill.type = Image.Type.Filled;
            holdFill.fillMethod = Image.FillMethod.Horizontal;
            holdFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
#endif

        private void OnEnable()
        {
            LastUsedDevice.Changed += Redraw;
            Redraw();
            // The fill's editor preview must not flash for a frame.
            if (holdFill != null) holdFill.enabled = false;
        }

        // Again after every Awake: InputGlyphsSetup registers its glyph
        // sets in its Awake, which may run after our first OnEnable.
        private void Start() => Redraw();

        private void OnDisable() => LastUsedDevice.Changed -= Redraw;

        private void OnDestroy()
        {
            Destroy(_sprite);
            Destroy(_texture);
        }

        // Read every frame: the fill must follow the hold smoothly.
        private void Update()
        {
            if (holdFill == null) return;
            float progress = action.action.GetTimeoutCompletionPercentage();
            holdFill.enabled = glyph.enabled && progress > 0f;
            holdFill.fillAmount = progress;
        }

        // Device -> its control scheme in our actions asset -> the
        // action's binding in that scheme -> the package draws it.
        private void Redraw()
        {
            var device = LastUsedDevice.Current;
            // SupportsDevice, not FindControlSchemeForDevice: that one
            // wants the device to fill the whole scheme, and a keyboard
            // alone doesn't fill "Keyboard&Mouse".
            var scheme = device == null ? null :
                action.action.actionMap.asset.controlSchemes
                    .Cast<InputControlScheme?>()
                    .FirstOrDefault(s => s.Value.SupportsDevice(device));

            _devices.Clear();
            bool drawn = scheme.HasValue
                && DisplayUtils.CollectDevicesForControlScheme(
                    scheme.Value, _devices)
                && InputLayoutPathUtility.TryGetActionBindingPath(
                    action.action, scheme.Value.bindingGroup, _paths)
                && DisplayGlyphTextureGenerator.GenerateGlyphTexture(
                    _texture, _devices, _paths, layout);

            glyph.enabled = drawn;
            if (!drawn) return;

            var visible = VisibleRect(_texture);
            Destroy(_sprite);
            _sprite = Sprite.Create(_texture, visible,
                new Vector2(0.5f, 0.5f), visible.height);
            glyph.sprite = _sprite;
            if (holdFill != null) holdFill.sprite = _sprite;

            // Height is the designer's; width follows the glyph's shape, so
            // "Space" is wider than "E". Centred in the parent, it grows to
            // both sides; the parent keeps its size, so labels stay put.
            glyph.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                glyph.rectTransform.rect.height
                    * visible.width / visible.height);
        }

        // The package pads every glyph into a square texture (a wide
        // "Space" floats in empty rows); crop to the drawn pixels.
        private static Rect VisibleRect(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            int w = texture.width, h = texture.height;
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a == 0) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            return maxX < 0
                ? new Rect(0, 0, w, h)
                : new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}
