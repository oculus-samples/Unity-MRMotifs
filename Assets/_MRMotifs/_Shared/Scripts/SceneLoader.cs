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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Tooltip("List of buttons that load scenes.")]
    [SerializeField] private List<Button> sceneButtons;

    [Tooltip("List of scene names corresponding to the buttons.")]
    [SerializeField] private List<string> sceneNames;

    [Tooltip("List of control bars for individual scenes.")]
    [SerializeField] private List<GameObject> sceneControlBars;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        RegisterButtonListeners();
        DeactivateAllControlBars();
    }

    private void RegisterButtonListeners()
    {
        // Ensure each button is mapped to load the corresponding scene.
        for (int i = 0; i < sceneButtons.Count; i++)
        {
            int index = i; // Capture the index in a local variable to avoid closure issues.
            sceneButtons[index].onClick.AddListener(() => LoadScene(index));
        }
    }

    private void LoadScene(int sceneIndex)
    {
        if (sceneIndex >= 0 && sceneIndex < sceneNames.Count)
        {
            StartCoroutine(LoadSceneAsync(sceneNames[sceneIndex], sceneIndex));
        }
    }

    private IEnumerator LoadSceneAsync(string sceneName, int sceneIndex)
    {
        var asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        EnableControlBar(sceneIndex);
    }

    private void EnableControlBar(int sceneIndex)
    {
        DeactivateAllControlBars();
        if (sceneIndex >= 0 && sceneIndex < sceneControlBars.Count)
        {
            sceneControlBars[sceneIndex].SetActive(true);
        }
    }

    private void DeactivateAllControlBars()
    {
        foreach (var controlBar in sceneControlBars)
        {
            controlBar.SetActive(false);
        }
    }
}
