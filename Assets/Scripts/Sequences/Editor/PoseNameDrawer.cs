using System.Linq;
using Oyung.Actors;
using Oyung.Sequences.Unity;
using UnityEditor;
using UnityEngine;

namespace Oyung.Sequences.Editor
{
    /// <summary>
    /// A dropdown of the poses the sequence's actor has. Under it, in
    /// red, what is wrong: the actor can't pose, or the stored pose is
    /// gone from its controller. A grey hint when no actor is found.
    /// </summary>
    [CustomPropertyDrawer(typeof(PoseNameAttribute))]
    internal sealed class PoseNameDrawer : PropertyDrawer
    {
        private static readonly Color Error = new Color(1f, 0.35f, 0.3f);

        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var line = position;
            line.height = EditorGUIUtility.singleLineHeight;

            var id = SceneActors.IdOf(property);
            var actor = SceneActors.Find(id);
            var field = EditorGUI.PrefixLabel(line, label);
            var text = string.IsNullOrEmpty(property.stringValue)
                ? "Pick a pose"
                : property.stringValue;
            if (EditorGUI.DropdownButton(field, new GUIContent(text),
                    FocusType.Keyboard))
                ShowMenu(property, actor as IAnimatedActor);

            var (note, isError) = Note(id, actor, property.stringValue);
            if (note != null)
            {
                var noteLine = field;
                noteLine.y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
                var style = new GUIStyle(EditorStyles.miniLabel);
                if (isError) style.normal.textColor = Error;
                EditorGUI.LabelField(noteLine, note, style);
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label)
        {
            var id = SceneActors.IdOf(property);
            var (note, _) = Note(id, SceneActors.Find(id),
                property.stringValue);
            var line = EditorGUIUtility.singleLineHeight;
            return note == null
                ? line
                : line * 2 + EditorGUIUtility.standardVerticalSpacing;
        }

        private static (string, bool) Note(ActorId id, IActor actor,
            string pose)
        {
            var missing = SceneActors.Missing(id, actor);
            if (missing != null) return (missing, false);
            if (actor is not IAnimatedActor animated)
                return ($"'{id.name}' can't pose.", true);
            if (!string.IsNullOrEmpty(pose) && !animated.Poses.Contains(pose))
                return ($"'{id.name}' has no pose '{pose}'.", true);
            return (null, false);
        }

        private static void ShowMenu(SerializedProperty property,
            IAnimatedActor actor)
        {
            // The menu answers later, when this property may be stale.
            var target = property.serializedObject;
            var path = property.propertyPath;
            var current = property.stringValue;

            var menu = new GenericMenu();
            if (actor == null || actor.Poses.Count == 0)
                menu.AddDisabledItem(new GUIContent("No poses found"));
            else
                foreach (var pose in actor.Poses)
                    menu.AddItem(new GUIContent(pose), pose == current,
                        () => Assign(target, path, pose));
            menu.ShowAsContext();
        }

        private static void Assign(SerializedObject target, string path,
            string pose)
        {
            target.Update();
            target.FindProperty(path).stringValue = pose;
            target.ApplyModifiedProperties();
        }
    }
}
