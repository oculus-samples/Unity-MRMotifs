/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEditor;

[CustomEditor(typeof(PassthroughFaderEditor))]
public class PassthroughFaderEditor : Editor
{
    private SerializedProperty _ovrPassthroughLayerProp;
    private SerializedProperty _viewingModeProp;
    private SerializedProperty _selectiveDistanceProp;
    private SerializedProperty _fadeSpeedProp;
    private SerializedProperty _fadeDirectionProp;
    private SerializedProperty _onStartFadeInProp;
    private SerializedProperty _onStartFadeOutProp;
    private SerializedProperty _onFadeInCompleteProp;
    private SerializedProperty _onFadeOutCompleteProp;

    private void OnEnable()
    {
        _ovrPassthroughLayerProp = serializedObject.FindProperty("oVRPassthroughLayer");
        _viewingModeProp = serializedObject.FindProperty("passthroughViewingMode");
        _selectiveDistanceProp = serializedObject.FindProperty("selectiveDistance");
        _fadeSpeedProp = serializedObject.FindProperty("fadeSpeed");
        _fadeDirectionProp = serializedObject.FindProperty("fadeDirection");
        _onStartFadeInProp = serializedObject.FindProperty("onStartFadeIn");
        _onStartFadeOutProp = serializedObject.FindProperty("onStartFadeOut");
        _onFadeInCompleteProp = serializedObject.FindProperty("onFadeInComplete");
        _onFadeOutCompleteProp = serializedObject.FindProperty("onFadeOutComplete");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_ovrPassthroughLayerProp);
        EditorGUILayout.PropertyField(_viewingModeProp);

        // Only show selective distance if the viewing mode is set to "Selective"
        // (Assuming enum order: Underlay = 0, Selective = 1)
        if (_viewingModeProp.enumValueIndex == 1)
        {
            EditorGUILayout.PropertyField(_selectiveDistanceProp);
        }

        EditorGUILayout.PropertyField(_fadeSpeedProp);
        EditorGUILayout.PropertyField(_fadeDirectionProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fade Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_onStartFadeInProp);
        EditorGUILayout.PropertyField(_onStartFadeOutProp);
        EditorGUILayout.PropertyField(_onFadeInCompleteProp);
        EditorGUILayout.PropertyField(_onFadeOutCompleteProp);

        serializedObject.ApplyModifiedProperties();
    }
}
