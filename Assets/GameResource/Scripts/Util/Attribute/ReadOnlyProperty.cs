using UnityEngine;
using System;
#if UNITY_EDITOR
namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(ReadOnlyProperty), true)]
    public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        // Necessary since some properties tend to collapse smaller than their content
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        // Draw a disabled property field
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = !Application.isPlaying && ((ReadOnlyProperty)attribute).runtimeOnly;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
#endif
[AttributeUsage(AttributeTargets.Field)]
public class ReadOnlyProperty : PropertyAttribute
{
    public readonly bool runtimeOnly;
    public ReadOnlyProperty(bool runtimeOnly = false)
    {
        this.runtimeOnly = runtimeOnly;
    }
}