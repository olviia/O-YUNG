using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Oyung.SharedKernel.Editor
{
    /// <summary>
    /// Inspector for [SerializeReference] fields of the kernel contracts
    /// listed below (Condition, Instruction), in any module: a dropdown
    /// to pick the concrete type, then its fields. Applies only to those
    /// contracts, not to every [SerializeReference] field.
    /// Knows: every non-abstract subclass of the field's type (TypeCache).
    /// Does: creates / replaces / clears the inline instance.
    /// Used by: Unity, automatically; deciders only write
    /// [SerializeReference] and never reference this assembly.
    /// </summary>
    [CustomPropertyDrawer(typeof(Condition), true)]
    [CustomPropertyDrawer(typeof(Instruction), true)]
    internal sealed class TypePickerDrawer : PropertyDrawer
    {
        private const string NoneLabel = "None";

        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var line = position;
            line.height = EditorGUIUtility.singleLineHeight;
            var labelRect = line;
            labelRect.width = EditorGUIUtility.labelWidth;
            var buttonRect = line;
            buttonRect.xMin += EditorGUIUtility.labelWidth;

            // CLAUDE: foldout only over the label, so clicking the
            // CLAUDE: dropdown doesn't also toggle it.
            if (property.hasVisibleChildren)
                property.isExpanded = EditorGUI.Foldout(labelRect,
                    property.isExpanded, label, true);
            else
                EditorGUI.LabelField(labelRect, label);

            var current = property.managedReferenceValue?.GetType();
            var buttonText = new GUIContent(
                current == null ? NoneLabel : current.Name);
            if (EditorGUI.DropdownButton(buttonRect, buttonText,
                    FocusType.Keyboard))
                ShowTypeMenu(property, current);

            if (property.hasVisibleChildren && property.isExpanded)
            {
                EditorGUI.indentLevel++;
                var y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
                foreach (var child in VisibleChildren(property))
                {
                    var h = EditorGUI.GetPropertyHeight(child, true);
                    EditorGUI.PropertyField(
                        new Rect(position.x, y, position.width, h),
                        child, true);
                    y += h + EditorGUIUtility.standardVerticalSpacing;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (!property.hasVisibleChildren || !property.isExpanded)
                return height;
            foreach (var child in VisibleChildren(property))
                height += EditorGUI.GetPropertyHeight(child, true) +
                          EditorGUIUtility.standardVerticalSpacing;
            return height;
        }

        // CLAUDE: children are drawn one by one instead of calling
        // CLAUDE: PropertyField on the property itself, which would
        // CLAUDE: land back in this drawer.
        private static IEnumerable<SerializedProperty> VisibleChildren(
            SerializedProperty property)
        {
            var child = property.Copy();
            var end = property.GetEndProperty();
            if (!child.NextVisible(true)) yield break;
            while (!SerializedProperty.EqualContents(child, end))
            {
                yield return child;
                if (!child.NextVisible(false)) yield break;
            }
        }

        private void ShowTypeMenu(SerializedProperty property, Type current)
        {
            // CLAUDE: the menu answers later, when this property object
            // CLAUDE: may be stale; keep the object and path instead.
            var target = property.serializedObject;
            var path = property.propertyPath;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(NoneLabel), current == null,
                () => Assign(target, path, null));
            menu.AddSeparator(string.Empty);
            foreach (var type in ConcreteTypes(BaseType()))
                menu.AddItem(new GUIContent(MenuPath(type)), type == current,
                    () => Assign(target, path, type));
            menu.ShowAsContext();
        }

        private static void Assign(SerializedObject target, string path,
            Type type)
        {
            target.Update();
            var property = target.FindProperty(path);
            property.managedReferenceValue =
                type == null ? null : Activator.CreateInstance(type);
            property.isExpanded = true;
            target.ApplyModifiedProperties();
        }

        // CLAUDE: for List<...> fields (e.g. AllOf / AnyOf children),
        // CLAUDE: fieldInfo is the list; the picker needs its element type.
        private Type BaseType()
        {
            var type = fieldInfo.FieldType;
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(List<>))
                return type.GetGenericArguments()[0];
            return type;
        }

        private static IEnumerable<Type> ConcreteTypes(Type baseType) =>
            TypeCache.GetTypesDerivedFrom(baseType)
                .Append(baseType)
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition &&
                            t.IsSerializable &&
                            !typeof(UnityEngine.Object).IsAssignableFrom(t) &&
                            t.GetConstructor(Type.EmptyTypes) != null)
                .Distinct()
                .OrderBy(MenuPath);

        // CLAUDE: groups the menu by module: Oyung.Globals.Unity.GlobalIs
        // CLAUDE: shows as "Globals/GlobalIs".
        private static string MenuPath(Type type)
        {
            var parts = (type.Namespace ?? string.Empty).Split('.');
            var module = parts.Length > 1 && parts[0] == "Oyung"
                ? parts[1]
                : parts[0];
            return string.IsNullOrEmpty(module)
                ? type.Name
                : module + "/" + type.Name;
        }
    }
}
