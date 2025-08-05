using UnityEditor;
using UnityEngine;
using MRMotifs.PassthroughTransitioning; // Match the namespace of your component

[CustomEditor(typeof(PassthroughSlider))]
public class PassthroughSliderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw all normal Inspector fields
        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("Fade Test Controls", EditorStyles.boldLabel);

        PassthroughSlider slider = (PassthroughSlider)target;

        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("▶ Start Fade Loop"))
        {
            slider.StartFadeLoop();
        }

        if (GUILayout.Button("■ Stop Fade Loop"))
        {
            slider.StopFadeLoop();
        }

        GUI.enabled = true;

        GUILayout.Space(5);
        GUILayout.Label("One-Shot Fades", EditorStyles.boldLabel);

        if (GUILayout.Button("⤴ Fade In Once"))
        {
            slider.FadeInOnce();
        }

        if (GUILayout.Button("⤵ Fade Out Once"))
        {
            slider.FadeOutOnce();
        }

    }
}
