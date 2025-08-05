using UnityEditor;
using UnityEngine;
using MRMotifs.PassthroughTransitioning; // Make sure this matches your namespace

[CustomEditor(typeof(PassthroughDissolver))]
public class PassthroughDissolverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw all default inspector fields
        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("Dissolve Test Controls", EditorStyles.boldLabel);

        PassthroughDissolver dissolver = (PassthroughDissolver)target;

        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("▶ Start Dissolve Loop"))
        {
            dissolver.StartDissolveLoop();
        }

        if (GUILayout.Button("■ Stop Dissolve Loop"))
        {
            dissolver.StopDissolveLoop();
        }

        GUI.enabled = true;
    }
}
