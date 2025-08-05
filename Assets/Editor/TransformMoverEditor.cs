using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TransformMover))]
public class TransformMoverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("▶ Move To Target"))
        {
            ((TransformMover)target).MoveToTarget();
        }

        if (GUILayout.Button("◀ Move To Start"))
        {
            ((TransformMover)target).MoveToStart();
        }

        GUI.enabled = true; // Re-enable GUI after disabling it
    }
}
