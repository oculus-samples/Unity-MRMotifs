using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectScaler))]
public class ObjectScalerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();

        GUI.enabled = Application.isPlaying;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scale In"))
        {
            ((ObjectScaler)target).ScaleIn();
        }

        if (GUILayout.Button("Scale Out"))
        {
            ((ObjectScaler)target).ScaleOut();
        }
        EditorGUILayout.EndHorizontal();

        GUI.enabled = true; // Reset GUI state
    }
}
