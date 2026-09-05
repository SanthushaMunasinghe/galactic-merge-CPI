using Dreamteck.Splines;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector for <see cref="ConveyorPathBuilder"/>.
///
/// Hand-written rather than Odin-driven, to match <see cref="ConveyorMeshUpdaterEditor"/> — same
/// status-line-then-button layout, since this is the same kind of one-shot authoring tool.
/// </summary>
[CustomEditor(typeof(ConveyorPathBuilder))]
public sealed class ConveyorPathBuilderEditor : Editor
{
    private ConveyorPathBuilder Builder => (ConveyorPathBuilder)target;

    public override void OnInspectorGUI()
    {
        DrawFields();

        EditorGUILayout.Space(10);
        DrawStatus();
        EditorGUILayout.Space(10);
        DrawBuildButton();
    }

    // ----------------------------------------------------------------- fields

    private void DrawFields()
    {
        serializedObject.Update();

        var property = serializedObject.GetIterator();
        for (var enterChildren = true; property.NextVisible(enterChildren); enterChildren = false)
        {
            if (property.propertyPath == "m_Script") continue;
            EditorGUILayout.PropertyField(property, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ----------------------------------------------------------------- status

    private void DrawStatus()
    {
        EditorGUILayout.LabelField("Spline", EditorStyles.boldLabel);

        var spline = Builder.GetComponent<SplineComputer>();
        if (spline == null)
        {
            EditorGUILayout.HelpBox(
                "No SplineComputer on this GameObject. Attach this component to the Conveyor's " +
                "root.",
                MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Points", spline.pointCount.ToString());
        EditorGUILayout.LabelField("Length", spline.CalculateLength().ToString("F2"));
        EditorGUILayout.LabelField("Closed", spline.isClosed ? "yes" : "no");
        EditorGUILayout.LabelField("Type", spline.type.ToString());

        var pathParentProperty = serializedObject.FindProperty("_pathParent");
        var pathParent = pathParentProperty.objectReferenceValue as Transform;
        if (pathParent == null)
        {
            EditorGUILayout.HelpBox(
                "No Path Parent assigned. Assign a transform whose children each have their own " +
                "first child acting as a path point.",
                MessageType.Error);
        }
        else if (pathParent.childCount == 0)
        {
            EditorGUILayout.HelpBox($"Path Parent '{pathParent.name}' has no children.",
                MessageType.Error);
        }

        if (Builder.GetComponent<ConveyorMeshUpdater>() == null)
        {
            EditorGUILayout.HelpBox(
                "No ConveyorMeshUpdater on this GameObject. It refits the belt mesh once the path " +
                "is built.",
                MessageType.Error);
        }
    }

    // ----------------------------------------------------------------- build

    private void DrawBuildButton()
    {
        EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Build Path", GUILayout.Height(32)))
                Builder.BuildPath();

            if (GUILayout.Button("Resolve References"))
            {
                Undo.RecordObject(Builder, "Resolve References");
                Builder.ResolveReferences();
                EditorUtility.SetDirty(Builder);
            }
        }

        EditorGUILayout.HelpBox(
            "Build Path reads Path Parent's children in order — using each child's own first " +
            "child as the actual path point (world position + world Z as normal) — replaces the " +
            "spline's points with them at the chosen Spline Type, optionally closes the loop, then " +
            "refits the belt mesh via ConveyorMeshUpdater.",
            MessageType.None);

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            EditorGUILayout.HelpBox("Not available in play mode.", MessageType.Warning);
    }
}
