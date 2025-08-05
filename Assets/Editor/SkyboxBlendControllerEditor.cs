using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkyboxBlendController))]
public class SkyboxBlendControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("Skybox Blend Test", EditorStyles.boldLabel);

        SkyboxBlendController controller = (SkyboxBlendController)target;

        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("🌅 Blend to Skybox B"))
        {
            controller.BlendToSkyboxB();
        }

        if (GUILayout.Button("☀️ Blend to Skybox A"))
        {
            controller.BlendToSkyboxA();
        }

        GUI.enabled = true;
    }
}
