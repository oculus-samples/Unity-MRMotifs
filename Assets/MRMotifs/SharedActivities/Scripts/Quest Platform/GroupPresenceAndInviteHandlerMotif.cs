// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Platform;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.Samples;
using MRMotifs.SharedAssets;

namespace MRMotifs.SharedActivities.QuestPlatform
{
    /// <summary>
    /// The GroupPresenceAndInviteHandlerMotif class is responsible for managing group presence
    /// and launching the invite panel using the Oculus Platform SDK. It allows users to set
    /// their presence in a joinable state and invite friends to join them in a multiplayer session.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class GroupPresenceAndInviteHandlerMotif : MonoBehaviour
    {
        [Tooltip("Decide if you would like to use the Group Presence features for your experience, such as invites.")]
        [SerializeField]
        private bool setupGroupPresence = true;

        [Header("Destination and Session Info")]
        [Tooltip("Destination API Name, which can be found on the Developer Dashboard under Engagement > Destinations.")]
        [SerializeField]
        private string destinationApiName;

        [Tooltip("Lobby Session ID.")]
        [SerializeField]
        private string lobbySessionId;

        [Tooltip("Match Session ID.")]
        [SerializeField]
        private string matchSessionId;

        private Button m_inviteFriendsButton;

        private void Awake()
        {
            if (!setupGroupPresence) return;
            FusionBBEvents.OnConnectedToServer += SetGroupPresence;
            SetupFriendsInvite();
        }

        private void OnDestroy()
        {
            if (!setupGroupPresence) return;
            FusionBBEvents.OnConnectedToServer -= SetGroupPresence;
            m_inviteFriendsButton.onClick.RemoveListener(OpenInvitePanel);
        }

        private void SetupFriendsInvite()
        {
            m_inviteFriendsButton = FindAnyObjectByType<MenuPanel>().FriendsInviteButton;
            m_inviteFriendsButton.onClick.AddListener(OpenInvitePanel);
        }

        private void SetGroupPresence(NetworkRunner obj)
        {
            SetGroupPresence();
        }

        private void OpenInvitePanel()
        {
            LaunchInvitePanel();
        }

        /// <summary>
        /// Sets the group presence for the current user with the provided destination, lobby session ID,
        /// and match session ID. This makes the user's session joinable, allowing other users to join the game.
        /// </summary>
        public void SetGroupPresence()
        {
            var options = new GroupPresenceOptions();

            options.SetDestinationApiName(destinationApiName);
            options.SetLobbySessionId(lobbySessionId);
            options.SetMatchSessionId(matchSessionId);
            options.SetIsJoinable(true);

            GroupPresence.Set(options).OnComplete(
                message =>
                {
                    if (message.IsError)
                    {
                        Debug.LogError("Error setting group presence: " + message.GetError().Message);
                    }
                    else
                    {
                        Debug.Log("Group presence successfully set for the Chess scene!");
                    }
                });
        }

        /// <summary>
        /// Launches the invite panel, allowing the user to invite friends to join their current session.
        /// </summary>
        public void LaunchInvitePanel()
        {
            var options = new InviteOptions();

            GroupPresence.LaunchInvitePanel(options).OnComplete(
                message =>
                {
                    if (message.IsError)
                    {
                        Debug.LogError("Error launching invite panel: " + message.GetError().Message);
                    }
                    else
                    {
                        Debug.Log("Invite panel successfully launched.");
                    }
                });
        }
    }
}
#endif
