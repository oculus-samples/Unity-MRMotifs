// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Oculus.Interaction;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

#if FUSION2
using Fusion;
#endif

namespace MRMotifs.SharedAssets
{
    public class MenuPanel : MonoBehaviour
    {
        [Header("MR Motifs - Library: Sample Scenes")]
        [Tooltip("List of buttons that load the scenes.")]
        [SerializeField]
        private List<Button> sceneButtons;

        [Tooltip("List of scene names.")]
        [SerializeField]
        private List<string> sceneNames;

        [Header("Menu Controls")]
        [Tooltip("Root object containing the menu components.")]
        [SerializeField]
        private GameObject menuRoot;

        [Tooltip("Ray Interactable of the canvas.")]
        [SerializeField]
        private RayInteractable rayInteractable;

        [Tooltip("Poke Interactable of the canvas.")]
        [SerializeField]
        private PokeInteractable pokeInteractable;

        [Tooltip("Parent that contains the viewport.")]
        [SerializeField]
        private GameObject menuContent;

        [Tooltip("The button to close the menu.")]
        [SerializeField]
        private Button panelCloseButton;

        [Header("Motif #1 - Passthrough Transitioning")]
        [Tooltip("The button used in the passthrough fader scenes to toggle passthrough on and off.")]
        [SerializeField]
        private Button passthroughFaderButton;

        [Tooltip("The slider used in the passthrough fader slider scene to slowly change visibility.")]
        [SerializeField]
        private Slider passthroughFaderSlider;

        [Header("Motif #2 - Shared Activities")]
        [Tooltip("The slider used in the passthrough fader slider scene to slowly change visibility.")]
        [SerializeField]
        private Button friendsInviteButton;

        public Button PassthroughFaderButton => passthroughFaderButton;
        public Slider PassthroughFaderSlider => passthroughFaderSlider;
        public Button FriendsInviteButton => friendsInviteButton;

        private void Awake()
        {
            panelCloseButton.onClick.AddListener(CloseMenuPanel);
            RegisterSceneButtonListeners();
        }

        private void OnDestroy()
        {
            panelCloseButton.onClick.RemoveListener(CloseMenuPanel);
            DeregisterSceneButtonListeners();
        }

        private void RegisterSceneButtonListeners()
        {
            for (var i = 0; i < sceneButtons.Count; i++)
            {
                var index = i;
                sceneButtons[index].onClick.AddListener(() => LoadScene(index));
            }
        }

        private void DeregisterSceneButtonListeners()
        {
            for (var i = 0; i < sceneButtons.Count; i++)
            {
                var index = i;
                sceneButtons[index].onClick.RemoveListener(() => LoadScene(index));
            }
        }

        private void LoadScene(int sceneIndex)
        {
#if FUSION2
        var networkRunner = FindAnyObjectByType<NetworkRunner>();
        if (networkRunner != null && networkRunner.IsSceneAuthority)
        {
            Debug.LogError($"Unloading multiplayer scene with active NetworkRunner");
            networkRunner.UnloadScene(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));
            networkRunner.Shutdown();
        }
#endif
            if (sceneIndex >= 0 && sceneIndex < sceneNames.Count)
            {
                StartCoroutine(LoadSceneAsync(sceneNames[sceneIndex]));
            }
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            var asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (asyncLoad is { isDone: false })
            {
                yield return null;
            }
        }

        public void ToggleMenu()
        {
            var isMenuActive = menuRoot.activeSelf;
            pokeInteractable.enabled = !isMenuActive;
            rayInteractable.enabled = !isMenuActive;
            menuRoot.SetActive(!isMenuActive);
        }


        private void CloseMenuPanel()
        {
            pokeInteractable.enabled = false;
            rayInteractable.enabled = false;
            menuRoot.SetActive(false);
        }
    }
}
