using Dreamteck.Splines;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector for <see cref="ConveyorMeshUpdater"/>.
///
/// Hand-written rather than Odin-driven — same status-line-then-button layout as
/// <see cref="ConveyorPathBuilderEditor"/>, since this is the same kind of one-shot authoring tool.
/// </summary>
[CustomEditor(typeof(ConveyorMeshUpdater))]
public sealed class ConveyorMeshUpdaterEditor : Editor
{
    private ConveyorMeshUpdater Updater => (ConveyorMeshUpdater)target;

    public override void OnInspectorGUI()
    {
        DrawFields();

        EditorGUILayout.Space(10);
        DrawStatus();
        EditorGUILayout.Space(10);
        DrawUpdateButton();
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

        var spline = Updater.GetComponent<SplineComputer>();
        if (spline == null)
        {
            EditorGUILayout.HelpBox(
                "No SplineComputer on this GameObject. Attach this component to the object " +
                "carrying SplineComputer and SplineMesh.",
                MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Points", spline.pointCount.ToString());
        EditorGUILayout.LabelField("Length", spline.CalculateLength().ToString("F2"));
        EditorGUILayout.LabelField("Closed", spline.isClosed ? "yes" : "no");

        var belt = Updater.GetComponent<SplineMesh>();
        if (belt == null)
        {
            EditorGUILayout.HelpBox("No SplineMesh (belt) on this GameObject.", MessageType.Error);
            return;
        }

        // Tile count is the smoothness control, so it is the one number worth reading back: a belt
        // sitting at its authored count against a much longer spline is exactly the "faceted until I
        // press Play" case.
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Tiles", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Belt", DescribeCounts(belt));
    }

    private static string DescribeCounts(SplineMesh mesh)
    {
        var channelCount = mesh.GetChannelCount();
        if (channelCount == 0) return "no channels";

        var counts = new string[channelCount];
        for (var i = 0; i < channelCount; i++) counts[i] = mesh.GetChannel(i).count.ToString();

        return string.Join(", ", counts);
    }

    // ---------------------------------------------------------------- update

    private void DrawUpdateButton()
    {
        EditorGUILayout.LabelField("Update", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Update Mesh", GUILayout.Height(32)))
                Updater.UpdateMesh();

            if (GUILayout.Button("Resolve References"))
            {
                Undo.RecordObject(Updater, "Resolve References");
                Updater.ResolveReferences();
                EditorUtility.SetDirty(Updater);
            }
        }

        EditorGUILayout.HelpBox(
            "Update Mesh rebuilds the spline, applies the current Thickness Modifier to every " +
            "spline point's size, and refits the belt mesh to the current Width Modifier and Mesh " +
            "Density. It never adds, removes, or reshapes points; it only makes the belt match " +
            "whatever shape is already on the spline.",
            MessageType.None);

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            EditorGUILayout.HelpBox("Not available in play mode.", MessageType.Warning);
    }
}
