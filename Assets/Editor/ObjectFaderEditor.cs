using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectFader))]
public class ObjectFaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying)
        {
            GUI.enabled = false;
            GUILayout.Button("Fade In (Runtime Only)");
            GUILayout.Button("Fade Out (Runtime Only)");
            GUI.enabled = true;
            return;
        }

        ObjectFader fader = (ObjectFader)target;

        if (GUILayout.Button("Fade In"))
        {
            fader.FadeIn();
        }

        if (GUILayout.Button("Fade Out"))
        {
            fader.FadeOut();
        }
    }
}
