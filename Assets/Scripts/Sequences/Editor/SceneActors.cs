using System.Collections.Generic;
using System.Linq;
using Oyung.Actors;
using Oyung.Sequences.Unity;
using UnityEditor;
using UnityEngine;

namespace Oyung.Sequences.Editor
{
    /// <summary>
    /// Finds the actor a sequence is for in the open scenes, for the
    /// inspector tools. In Play mode the actor holds its id, so that is
    /// asked first. Searches are remembered until the hierarchy changes,
    /// so drawing the inspector stays cheap.
    /// </summary>
    internal static class SceneActors
    {
        private static readonly Dictionary<ActorId, IActor> Found =
            new Dictionary<ActorId, IActor>();

        static SceneActors() =>
            EditorApplication.hierarchyChanged += () => Found.Clear();

        /// <summary>The actor id of the sequence this property is in.
        /// </summary>
        internal static ActorId IdOf(SerializedProperty property) =>
            property.serializedObject
                .FindProperty(SequenceAsset.ActorField)
                ?.objectReferenceValue as ActorId;

        /// <summary>The actor holding the id in the open scenes; null if
        /// none.</summary>
        internal static IActor Find(ActorId id)
        {
            if (id == null) return null;
            if (id.Current != null) return id.Current;
            if (Found.TryGetValue(id, out var actor)) return actor;
            actor = Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include)
                .OfType<IActor>()
                .FirstOrDefault(a => a.Id == id);
            Found[id] = actor;
            return actor;
        }

        /// <summary>Why the tool can't work with this actor yet, or
        /// null if it can.</summary>
        internal static string Missing(ActorId id, IActor actor) =>
            id == null ? "Set the sequence's actor first."
            : actor == null ? $"Open a scene with actor '{id.name}'."
            : null;
    }
}
