using System.Collections.Generic;
using Dreamteck.Splines;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Spline interpolation to apply when the path is built. Mirrors <see cref="Spline.Type"/> under a
/// name that reads naturally in this component's own inspector.
/// </summary>
public enum ConveyorSplineType
{
    Linear = 0,
    CatmullRom = 1,
    BSpline = 2,
}

/// <summary>
/// Editor-only authoring tool that lays a hand-placed path onto the conveyor's spline and drags the
/// rest of the conveyor onto it in one press.
///
/// <para><b>What it does.</b> <c>Build Path</c> reads <see cref="_pathParent"/>'s children in
/// sibling order; for each child it takes that child's own first child as the actual path point,
/// using its world position and its world Z axis (<c>Transform.forward</c>) as the spline point's
/// normal. It replaces the spline's points with those, sets the spline's interpolation to
/// <see cref="_splineType"/>, optionally closes the loop, then hands off to
/// <see cref="ConveyorMeshUpdater"/> to refit the belt mesh. This component never touches the mesh
/// itself — that is entirely <see cref="ConveyorMeshUpdater"/>'s job, done the same way pressing its
/// own Update Mesh button would.</para>
///
/// <para><b>Why a grandchild, not the child itself.</b> Each direct child of the path parent is a
/// placement node in the scene; its own first child is the actual point transform whose position
/// and rotation should drive the spline. This mirrors how the path is typically hand-authored:
/// each waypoint is a small rig (child 0) parented under a named marker.</para>
///
/// <para><b>Editor only.</b> Every method lives inside <c>#if UNITY_EDITOR</c> and there is no
/// Awake/Start/Update, so nothing here can execute in a player build. The serialized fields
/// deliberately sit outside the guard so the component's serialized layout is identical in the
/// editor and in a build.</para>
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SplineComputer))]
[RequireComponent(typeof(ConveyorMeshUpdater))]
public sealed class ConveyorPathBuilder : MonoBehaviour
{
    // See the class summary for why these sit outside the UNITY_EDITOR guard.
#pragma warning disable CS0169, CS0414, CS0649

    [Header("References (auto-filled; drag only if this is not on the conveyor)")]
    [Tooltip("The conveyor's path. Auto-filled from this GameObject.")]
    [SerializeField] private SplineComputer _splineComputer;

    [Tooltip("Refits the belt mesh onto the path once it is built. Auto-filled from this " +
             "GameObject; added automatically if missing.")]
    [SerializeField] private ConveyorMeshUpdater _meshUpdater;

    [Header("Path")]
    [Tooltip("The parent whose children define the path. Each direct child must itself have at " +
             "least one child; that grandchild's world position becomes a spline point (in sibling " +
             "order) and its world Z axis (forward) becomes that point's normal.")]
    [SerializeField] private Transform _pathParent;

    [Tooltip("Interpolation to apply when the path is written. Linear passes exactly through every " +
             "point. CatmullRom and BSpline are smoother but do not necessarily pass through each " +
             "point exactly.")]
    [SerializeField] private ConveyorSplineType _splineType = ConveyorSplineType.BSpline;

    [Tooltip("Close the path into a loop after building it.")]
    [SerializeField] private bool _closed = true;

    [Tooltip("Flip every path point's normal to the opposite of its world Z axis (-forward instead " +
             "of forward). Use this if the belt or its channels come out facing the wrong way.")]
    [SerializeField] private bool _flipNormals;

#pragma warning restore CS0169, CS0414, CS0649

#if UNITY_EDITOR

    private const string BuildUndoName = "Build Conveyor Path";

    // Fills the references in when the component is first added, so dropping it on the generated
    // conveyor is all the setup there is.
    private void Reset()
    {
        ResolveReferences();
    }

    /// <summary>
    /// Fills in every reference that is still null. Safe to call repeatedly; never overwrites
    /// something you assigned by hand.
    /// </summary>
    public void ResolveReferences()
    {
        if (_splineComputer == null) _splineComputer = GetComponent<SplineComputer>();
        if (_meshUpdater == null) _meshUpdater = GetComponent<ConveyorMeshUpdater>();
    }

    /// <summary>
    /// Replaces the spline's points with the path read from <see cref="_pathParent"/>'s hierarchy,
    /// then hands the rest of the conveyor over to <see cref="ConveyorMeshUpdater"/>.
    /// </summary>
    public void BuildPath()
    {
        if (!CanEdit()) return;

        if (_pathParent == null)
        {
            LogError("no Path Parent assigned. Assign the transform whose children define the path.");
            return;
        }

        if (_pathParent.childCount == 0)
        {
            LogError($"Path Parent '{_pathParent.name}' has no children.");
            return;
        }

        ResolveReferences();
        if (_splineComputer == null)
        {
            LogError("no Spline Computer. Attach this to the Conveyor's root.");
            return;
        }

        if (_meshUpdater == null)
        {
            LogError("no Conveyor Mesh Updater. Attach this to the Conveyor's root.");
            return;
        }

        if (!TryBuildPathPoints(out var points)) return;

        Undo.RecordObjects(new Object[] { _splineComputer, this }, BuildUndoName);

        // Set before SetPoints/RebuildImmediate so the rebuild evaluates using the chosen type from
        // the start.
        _splineComputer.type = ResolveSplineType();
        _splineComputer.SetPoints(points);
        if (_closed) _splineComputer.Close();
        _splineComputer.RebuildImmediate();
        MarkDirty(_splineComputer);

        // UpdateMesh reads the spline's resulting length/shape, so it runs only after the spline above
        // has fully settled. It records its own Undo targets (the belt mesh), so nothing more needs
        // recording here.
        _meshUpdater.ResolveReferences();
        _meshUpdater.UpdateMesh();

        Log($"built a {points.Length}-point {_splineType} path from '{_pathParent.name}', " +
            $"closed={_closed}, flipped={_flipNormals}.");
    }

    // ── Point generation ────────────────────────────────────────────────────────────────────

    private Spline.Type ResolveSplineType() => _splineType switch
    {
        ConveyorSplineType.Linear => Spline.Type.Linear,
        ConveyorSplineType.CatmullRom => Spline.Type.CatmullRom,
        ConveyorSplineType.BSpline => Spline.Type.BSpline,
        _ => Spline.Type.Linear,
    };

    // One spline point per direct child of the path parent, in sibling order: the child's own first
    // child is the actual path point, its world position becomes the spline point's position and its
    // world Z axis (forward) becomes the spline point's normal.
    private bool TryBuildPathPoints(out SplinePoint[] points)
    {
        points = null;
        var childCount = _pathParent.childCount;
        var result = new List<SplinePoint>(childCount);

        for (var i = 0; i < childCount; i++)
        {
            var marker = _pathParent.GetChild(i);
            if (marker.childCount == 0)
            {
                LogError($"Path Parent child '{marker.name}' (index {i}) has no first child to use " +
                         "as a path point.");
                return false;
            }

            var pointTransform = marker.GetChild(0);
            var normal = pointTransform.forward;
            if (_flipNormals) normal = -normal;

            var point = new SplinePoint(pointTransform.position) { normal = normal };
            result.Add(point);
        }

        points = result.ToArray();
        return true;
    }

    // ── Validation and plumbing ─────────────────────────────────────────────────────────────

    private bool CanEdit()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode) return true;

        LogError("cannot build the path in play mode. Exit play mode and try again.");
        return false;
    }

    private static void MarkDirty(Object target)
    {
        if (target == null) return;

        EditorUtility.SetDirty(target);

        if (target is Component component && component.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
    }

    private void Log(string message) => Debug.Log($"<b>Conveyor Path</b>: {message}", this);
    private void LogError(string message) => Debug.LogError($"<b>Conveyor Path</b>: {message}", this);

#endif
}
