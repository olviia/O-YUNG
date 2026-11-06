using System;
using System.Collections.Generic;
using Oyung.Actors;
using UnityEngine;
using UnityEngine.Localization;

namespace Oyung.Dialogues.Unity
{
    // CLAUDE: TEMPORARY: the tool for the first communication, until the
    // CLAUDE: full dialogue tool exists (docs/ToDo.md item 13). Delete it
    // CLAUDE: then, with ChatterPlayer and its assets.
    // CLAUDE: class comment (your words). Draft: small talk: who takes
    // CLAUDE: part and a pool of short exchanges. ChatterPlayer plays it
    // CLAUDE: and decides when. A line is one string table entry; the
    // CLAUDE: voice always speaks its English.
    [CreateAssetMenu(menuName = "Oyung/Dialogues/Chatter (temporary)",
        fileName = "NewChatter")]
    internal sealed class ChatterAsset : ScriptableObject
    {
        [Serializable]
        internal sealed class Line
        {
            [CastMember]
            [SerializeField] private int speaker;
            [SerializeField] private LocalizedString text;

            internal int Speaker => speaker;
            internal LocalizedString Text => text;
        }

        [Serializable]
        internal sealed class Exchange
        {
            [SerializeField] private Line[] lines = Array.Empty<Line>();

            internal IReadOnlyList<Line> Lines => lines;
        }

        [Tooltip("Who takes part. Lines pick their speaker from this list.")]
        [SerializeField] private ActorId[] cast = Array.Empty<ActorId>();
        [Tooltip("Shortest silence between two exchanges, in seconds.")]
        [Min(0f)]
        [SerializeField] private float shortestPause = 1.5f;
        [Tooltip("Longest silence between two exchanges, in seconds.")]
        [Min(0f)]
        [SerializeField] private float longestPause = 5f;
        [Tooltip("Silence between two lines of an exchange, in seconds.")]
        [Min(0f)]
        [SerializeField] private float lineGap = 0.35f;
        [SerializeField] private Exchange[] exchanges =
            Array.Empty<Exchange>();

        internal IReadOnlyList<Exchange> Exchanges => exchanges;
        internal float LineGap => lineGap;

        internal float RandomPause() =>
            UnityEngine.Random.Range(shortestPause, longestPause);

        internal ActorId SpeakerOf(Line line) => cast[line.Speaker];

        // Marks a line's speaker field: picked by name from the cast.
        private sealed class CastMemberAttribute : PropertyAttribute { }

#if UNITY_EDITOR
        [UnityEditor.CustomPropertyDrawer(typeof(CastMemberAttribute))]
        private sealed class CastMemberDrawer : UnityEditor.PropertyDrawer
        {
            public override void OnGUI(Rect position,
                UnityEditor.SerializedProperty property, GUIContent label)
            {
                var members = property.serializedObject
                    .FindProperty(nameof(cast));
                var names = new GUIContent[members.arraySize];
                for (var i = 0; i < names.Length; i++)
                {
                    var actor = members.GetArrayElementAtIndex(i)
                        .objectReferenceValue;
                    names[i] = new GUIContent(
                        actor == null ? "(empty)" : actor.name);
                }
                UnityEditor.EditorGUI.BeginProperty(position, label, property);
                property.intValue = UnityEditor.EditorGUI.Popup(position, label,
                    property.intValue, names);
                UnityEditor.EditorGUI.EndProperty();
            }
        }
#endif
    }
}
