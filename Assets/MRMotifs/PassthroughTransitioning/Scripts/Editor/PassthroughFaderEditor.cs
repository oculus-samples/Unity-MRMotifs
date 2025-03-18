// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;

namespace MRMotifs.PassthroughTransitioning.Editor
{
    using UnityEditor;

    [CustomEditor(typeof(PassthroughFaderEditor))]
    [MetaCodeSample("MRMotifs-PassthroughTransitioning")]
    public class PassthroughFaderEditor : Editor
    {
        private SerializedProperty m_ovrPassthroughLayerProp;
        private SerializedProperty m_viewingModeProp;
        private SerializedProperty m_selectiveDistanceProp;
        private SerializedProperty m_fadeSpeedProp;
        private SerializedProperty m_fadeDirectionProp;
        private SerializedProperty m_onStartFadeInProp;
        private SerializedProperty m_onStartFadeOutProp;
        private SerializedProperty m_onFadeInCompleteProp;
        private SerializedProperty m_onFadeOutCompleteProp;

        private void OnEnable()
        {
            m_ovrPassthroughLayerProp = serializedObject.FindProperty("oVRPassthroughLayer");
            m_viewingModeProp = serializedObject.FindProperty("passthroughViewingMode");
            m_selectiveDistanceProp = serializedObject.FindProperty("selectiveDistance");
            m_fadeSpeedProp = serializedObject.FindProperty("fadeSpeed");
            m_fadeDirectionProp = serializedObject.FindProperty("fadeDirection");
            m_onStartFadeInProp = serializedObject.FindProperty("onStartFadeIn");
            m_onStartFadeOutProp = serializedObject.FindProperty("onStartFadeOut");
            m_onFadeInCompleteProp = serializedObject.FindProperty("onFadeInComplete");
            m_onFadeOutCompleteProp = serializedObject.FindProperty("onFadeOutComplete");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_ovrPassthroughLayerProp);
            EditorGUILayout.PropertyField(m_viewingModeProp);

            // Only show selective distance if the viewing mode is set to "Selective"
            // (Assuming enum order: Underlay = 0, Selective = 1)
            if (m_viewingModeProp.enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(m_selectiveDistanceProp);
            }

            EditorGUILayout.PropertyField(m_fadeSpeedProp);
            EditorGUILayout.PropertyField(m_fadeDirectionProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fade Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_onStartFadeInProp);
            EditorGUILayout.PropertyField(m_onStartFadeOutProp);
            EditorGUILayout.PropertyField(m_onFadeInCompleteProp);
            EditorGUILayout.PropertyField(m_onFadeOutCompleteProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
