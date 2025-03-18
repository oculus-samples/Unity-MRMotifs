// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MRMotifs.SharedAssets
{
    [MetaCodeSample("MRMotifs-SharedAssets")]
    public class SceneLoader : MonoBehaviour
    {
        [Tooltip("List of buttons that load scenes.")]
        [SerializeField]
        private List<Button> sceneButtons;

        [Tooltip("List of scene names corresponding to the buttons.")]
        [SerializeField]
        private List<string> sceneNames;

        [Tooltip("List of control bars for individual scenes.")]
        [SerializeField]
        private List<GameObject> sceneControlBars;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            RegisterButtonListeners();
            DeactivateAllControlBars();
        }

        private void RegisterButtonListeners()
        {
            // Ensure each button is mapped to load the corresponding scene.
            for (var i = 0; i < sceneButtons.Count; i++)
            {
                var index = i; // Capture the index in a local variable to avoid closure issues.
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
}
