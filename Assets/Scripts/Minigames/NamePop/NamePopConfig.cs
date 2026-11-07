using System;
using System.Collections.Generic;
using Oyung.SharedKernel;
using UnityEngine;

namespace Oyung.Minigames.NamePop
{
    // CLAUDE: how one run of the minigame looks and feels: shapes, sizes,
    // CLAUDE: speeds and what happens to the finished name. Another run
    // CLAUDE: with other shapes or speed is another asset.
    [CreateAssetMenu(menuName = "Oyung/Minigames/Name Pop Config",
        fileName = "NamePopConfig")]
    internal sealed class NamePopConfig : ScriptableObject
    {
        [Header("Name")]
        [SerializeField, Min(1)] private int maxLength = 12;
        [Tooltip("Runs with the finished name.")]
        [SerializeReference] private StringInstruction onNamed;

        [Header("Shapes")]
        [Tooltip("One mesh per letter, each named as its letter (A, B...).")]
        [SerializeField] private List<Mesh> letterMeshes = new List<Mesh>();
        [SerializeField] private Mesh removeMesh;
        [SerializeField] private Mesh doneMesh;
        [Tooltip("Coloured by place, so neighbours differ.")]
        [SerializeField] private Color[] letterColours =
        {
            new Color(0.91f, 0.26f, 0.24f), new Color(0.98f, 0.60f, 0.15f),
            new Color(0.98f, 0.82f, 0.20f), new Color(0.36f, 0.73f, 0.33f),
            new Color(0.20f, 0.56f, 0.90f), new Color(0.56f, 0.36f, 0.85f),
            new Color(0.94f, 0.42f, 0.67f)
        };
        [SerializeField] private Color removeColour = new Color(.9f, .3f, .3f);
        [SerializeField] private Color doneColour = new Color(.3f, .8f, .4f);

        [Header("Keyboard, in metres")]
        [SerializeField] private string[] rows =
            { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
        [SerializeField] private float keySpacing = 0.06f;
        [SerializeField] private float rowSpacing = 0.055f;
        [SerializeField] private float bubbleDiameter = 0.05f;
        [SerializeField] private float letterHeightInBubble = 0.024f;
        [Tooltip("Keys from the centre of the last row.")]
        [SerializeField] private float actionKeysOffset = 4.5f;

        [Header("Bubbles")]
        [Tooltip("Seconds a new bubble takes from the parents to its place. "
                 + "It can't be popped on the way.")]
        [SerializeField, Min(0.05f)] private float arrivalSeconds = 1.4f;
        [Tooltip("At the start, bubbles leave within this many seconds.")]
        [SerializeField, Min(0f)] private float firstWaveSeconds = 2.5f;
        [SerializeField] private float flightArc = 0.12f;
        [Tooltip("Where on a parent's body new bubbles appear, as a part of "
                 + "their height: roughly where they speak from.")]
        [SerializeField, Range(0f, 1f)] private float speakingHeight = 2f / 3f;
        [SerializeField] private float spawnScatter = 0.08f;
        [Tooltip("Remove and done grow back in place, this fast.")]
        [SerializeField, Min(0.01f)] private float actionKeyGrowSeconds = 0.25f;
        [SerializeField] private float bobHeight = 0.0015f;
        [SerializeField] private float bobSpeed = 1.3f;
        [SerializeField] private float focusScale = 1.9f;
        [Tooltip("Metres towards the eyes, so it shows over its neighbours.")]
        [SerializeField] private float focusForward = 0.03f;
        [SerializeField] private float focusSeconds = 0.1f;

        [Header("Name letters, in metres")]
        [SerializeField] private float nameLetterHeight = 0.03f;
        [SerializeField] private float nameLetterGap = 0.004f;
        [SerializeField] private float landingDrop = 0.012f;
        [SerializeField, Min(0.01f)] private float landingSeconds = 0.35f;

        [Header("Pop sound")]
        [SerializeField] private float popPitch = 0.95f;
        [Tooltip("Added for each letter of the name.")]
        [SerializeField] private float popPitchStep = 0.03f;

        [Header("Ending")]
        [Tooltip("Seconds the finished name stays before it goes.")]
        [SerializeField, Min(0f)] private float finishedHoldSeconds = 1.5f;

        internal int MaxLength => maxLength;
        internal StringInstruction OnNamed => onNamed;
        internal Mesh RemoveMesh => removeMesh;
        internal Mesh DoneMesh => doneMesh;
        internal Color RemoveColour => removeColour;
        internal Color DoneColour => doneColour;
        internal IReadOnlyList<string> Rows => rows;
        internal float KeySpacing => keySpacing;
        internal float RowSpacing => rowSpacing;
        internal float BubbleDiameter => bubbleDiameter;
        internal float LetterHeightInBubble => letterHeightInBubble;
        internal float ActionKeysOffset => actionKeysOffset;
        internal float ArrivalSeconds => arrivalSeconds;
        internal float FirstWaveSeconds => firstWaveSeconds;
        internal float FlightArc => flightArc;
        internal float SpeakingHeight => speakingHeight;
        internal float SpawnScatter => spawnScatter;
        internal float ActionKeyGrowSeconds => actionKeyGrowSeconds;
        internal float BobHeight => bobHeight;
        internal float BobSpeed => bobSpeed;
        internal float FocusScale => focusScale;
        internal float FocusForward => focusForward;
        internal float FocusSeconds => focusSeconds;
        internal float NameLetterHeight => nameLetterHeight;
        internal float NameLetterGap => nameLetterGap;
        internal float LandingDrop => landingDrop;
        internal float LandingSeconds => landingSeconds;
        internal float PopPitch => popPitch;
        internal float PopPitchStep => popPitchStep;
        internal float FinishedHoldSeconds => finishedHoldSeconds;

        internal Mesh MeshOf(char letter)
        {
            foreach (var mesh in letterMeshes)
                if (mesh != null && mesh.name == letter.ToString())
                    return mesh;
            throw new InvalidOperationException(
                $"{name}: no mesh named '{letter}'.");
        }

        /// <summary>A colour from the place on the keyboard, so that
        /// neighbours differ in every direction.</summary>
        internal Color ColourAt(int row, int column) =>
            letterColours[(row * 3 + column) % letterColours.Length];
    }
}
