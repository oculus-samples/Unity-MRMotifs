# MR Motifs - Create delightful Mixed Reality experiences

![MR Motifs Banner](./Media/MRMotifsBanner.png 'MR Motifs')

# Project Overview

Motifs are blueprints for recurring ideas which we expect, and have observed, our community to build. They are not full applications, but rather recurring aspects of applications, which may require a collection of technical features and APIs to be achieved. With MR Motifs we would like to teach MR best practices, inspire developers and spark new ideas. Our goal is to stop developers from having to reinvent the wheel by providing them with a solid baseline for popular mechanics, which we frequently observe.

You can find even more tips and tricks on [how to design Mixed Reality experiences](https://developer.oculus.com/resources/mr-overview/) in our [Developer Resources](https://developer.oculus.com/resources/)!

# Requirements

- [Unity 2022.3.38](https://unity.com/releases/editor/whats-new/2022.3.38) (Recommended) or later
- URP (Recommended) or BiRP
- [Meta XR Core SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-core-sdk-269169) (71.0.0) - com.meta.xr.sdk.core
- [Meta XR Interaction SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-interaction-sdk-265014) (71.0.0) - com.meta.xr.sdk.interaction.ovr
- [Meta XR Interaction SDK Essentials](https://assetstore.unity.com/packages/tools/integration/meta-xr-interaction-sdk-essentials-264559) (71.0.0) - com.meta.xr.sdk.interaction

# MR Motifs Library

1) [Passthrough Transitioning](#passthrough-transitioning) - Seamlessly fade between Passthrough and VR
2) [Shared Activities in Mixed Reality](#shared-activities-in-mixed-reality) - Make people feel truly physically present with each other

> [!IMPORTANT]
> All scenes can be loaded from the [MRMotifsHome](./Assets/_MRMotifs/MRMotifsHome.unity) scene which contains the Menu Panel [prefab](./Assets/_MRMotifs/_Shared/Prefabs/Menu%20Panel.prefab) and [script](./Assets/_MRMotifs/_Shared/Scripts/MenuPanel.cs), which holds a list of all the other scenes and which displays scene controls for each scene. The menu panel can be toggled by using the menu (start) button/gesture using hands and controllers. The menu panels are hidden in the Shared Activities scenes by default to not interfere with the object of interest.

# Passthrough Transitioning

[![Video Thumbnail](./Media/Motif1/MR_Motif1_Thumbnail.png)](https://www.youtube.com/watch?v=C9PFg-XfQcA)

Make sure to read through our [**Developer Documentation**](https://developers.meta.com/horizon/documentation/unity/unity-mrmotifs-passthrough-transitioning) for additional information!

With this Motif we would like to show the transition from fully immersive VR experiences, to passthrough mixed reality experiences, using the [Passthrough API](https://developer.oculus.com/documentation/unity/unity-passthrough/). We also want to address what passthrough is, and where and how it can and should be used. This project will allow users to adjust the visibility of their surroundings by manipulating a slider, which regulates the level of passthrough, or directly switch from one mode to another by the press of a button.

> [!TIP]
> This MR Motif also teaches how to use the [Boundary API](https://developer.oculus.com/documentation/unity/unity-boundaryless/), to disable the guardian while in passthrough mode for a seamless MR experience!

## How it works

We fade between VR and passthrough by attaching a sphere to the main camera and manipulating it with a shader controlled by a custom fader class. This enables adjustable fade speed, direction, distance, and effects like dissolving in a random pattern. To set this up, use the custom [PassthroughFader shader](./Assets/_MRMotifs/PassthroughTransitioning/Shaders/PassthroughFader.shader) and the [PassthroughFader class](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Scripts/PassthroughFader.cs) or a similar class to modify the shader's properties.

Ensure an OVR Camera Rig with OVR Manager is set up with Passthrough enabled. Enable Insight Passthrough on OVR Manager, use an OVR Passthrough Layer in Underlay mode, and add the Passthrough Fader prefab to the CenterEyeCamera. Reference the OVR Passthrough Layer in the [Passthrough Fader component](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Scripts/PassthroughFader.cs). Finally, set up a way to call the `TogglePassthrough` method of the PassthroughFader class using the Controller Buttons Mapper Building Block or the [Menu Panel prefab](./Assets/_MRMotifs/_Shared/Prefabs/Menu%20Panel.prefab).

## Contextual Passthrough

[Contextual passthrough](https://developers.meta.com/horizon/documentation/unity/unity-customize-passthrough-loading-screens/#configuring-system-splash-screen) determines if passthrough should be enabled based on the [system recommendation](https://developers.meta.com/horizon/documentation/unity/unity-passthrough-gs/#enable-based-on-system-recommendation). If the user is in passthrough mode at home, the system can detect this and display the splash screen and Unity scene in passthrough. In VR mode, the splash screen appears with a black background as usual.

> [!IMPORTANT]
> If Passthrough (Contextual) is enabled but the effect can still not be observed at startuo, make sure to update the AndroidManifest.xml. Go to Meta > Tools > Update the Android Manifest xml. Also, currently Passthrough (Contextual) for the system splash screen can only be enabled with a Unity Pro license.

|                  Splash Screen (Black)                       |               Splash Screen (Passthrough Contextual)         |
| :----------------------------------------------------------: | :----------------------------------------------------------: |
| ![Black](./Media/Motif1/SplashScreenBlack.gif 'Black')       | ![PT](./Media/Motif1/SplashScreenPassthrough.gif 'PT')       |

## Conditional Passthrough

Passthrough can switch between MR and VR modes or reveal parts of the environment, like menus or scene changes, enhancing immersion and extending play sessions. Since enabling passthrough is asynchronous, system resources like cameras take milliseconds to activate, causing a black flicker. Prevent this by using the [passthroughLayerResumed](https://developer.oculus.com/documentation/unity/unity-passthrough-gs/#wait-until-passthrough-is-ready) event, which signals when passthrough is fully initialized. To ensure a smooth transition, use a [shader](./Assets/_MRMotifs/PassthroughTransitioning/Shaders/PassthroughFader.shader) instead of switching instantly.

## Passthrough Transitioning Sample Scenes

Both scenes come with a PassthroughFader prefab, which is located on the centerEyeAnchor. It contains the PassthroughFader class. The prefab also contains an audio source, that is used to play audio clips whenever we fade in or out.

|                  PassthroughFader Underlay                   |                  PassthroughFaderDissolve                    |
| :----------------------------------------------------------: | :----------------------------------------------------------: |
|![PT](./Media/Motif1/PassthroughFaderUnderlay.gif 'PT')       |![PT](./Media/Motif1/PassthroughFaderDissolveSG.gif 'PT')     |

The passthrough fader slider scene includes the `PassthroughFaderSlider` prefab on `centerEyeAnchor`, containing the `PassthroughFaderSlider` component. The passthrough dissolver scene has the `PassthroughDissolver` prefab **outside** `centerEyeAnchor` so the dissolution pattern stays anchored in the scene. It contains the `PassthroughDissolver` class. The default `PassthroughDissolver` shader applies to a sphere, requiring the `PerlinNoiseTexture` script to generate a modifiable texture. If using `PassthroughDissolverSG ShaderGraph`, remove `PerlinNoiseTexture` since the texture is generated within ShaderGraph, with everything else functioning the same.

## Main components in the Passthrough Transitioning MR Motif

### PassthroughFader class

This script shows you how to check if [passthrough is recommended](https://developers.meta.com/horizon/documentation/unity/unity-passthrough-gs/#enable-based-on-system-recommendation), [wait until passthrough is ready](https://developers.meta.com/horizon/documentation/unity/unity-passthrough-gs/#wait-until-passthrough-is-ready), and how to toggle between VR and passthrough smoothly in the [PassthroughToggle method](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Scripts/PassthroughFader.cs#L196). The [PassthroughFader](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Scripts/PassthroughFader.cs) script is a component that contains both Underlay and Selective passthrough modes and lets the user decide which to use in the inspector. You will be fine just using this one script and setting your desired mode in the dropdown, instead of using the separate [PassthroughFaderUnderlay](./Assets/_MRMotifs/PassthroughTransitioning/Scripts/PassthroughFaderUnderlay.cs) and [PassthroughFaderSelective](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Scripts/PassthroughFaderSelective.cs.meta) scripts.

|                  PassthroughFader Underlay                   |                  PassthroughFader Selective                  |
| :----------------------------------------------------------: | :----------------------------------------------------------: |
|![PT](./Media/Motif1/PassthroughFaderUnderlayScript.png 'PT') |![PT](./Media/Motif1/PassthroughFaderSelectiveScript.png 'PT')|

We can adjust Fade Speed and Fade Direction, as well as **Selective Distance** in Selective Passthrough mode, which limits virtual content visibility within a set range. This is useful for tabletop games or interactions requiring focus on surroundings. The `PassthroughFader` class includes four Unity Events that notify when fade-in and fade-out start and complete, useful for triggering actions like playing an audio clip.

### PassthroughFaderSlider and PassthroughDissolver classes

We provide a **[PassthroughFaderSlider](./Assets/_MRMotifs/PassthroughTransitioning/Scripts/PassthroughFaderSlider.cs)** script, similar to `PassthroughFader`, but allowing manual fading via a slider. It also demonstrates turning off the guardian when a threshold is crossed. The **[PassthroughDissolver](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Scripts/PassthroughDissolver.cs)** works similarly but adjusts the dissolve level instead of the inverted alpha value.

Other scripts in the Passthrough Transitioning samples:
- **[AudioController](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Scripts/AudioController.cs)** reads the inverted alpha value and adjusts volume.
- **[PerlinNoiseTexture](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Scripts/PerlinNoiseTexture.cs)** generates textures for the PassthroughDissolver shader.
- **[Perlin Noise](https://docs.unity3d.com/ScriptReference/Mathf.PerlinNoise.html)** settings allow unique dissolve effects at runtime.

## Shaders

### PassthroughFader HLSL

Passthrough Transitioning uses a shader for smooth fading between VR and passthrough. The **[PassthroughFader](./Assets/_MRMotifs/PassthroughTransitioning/Shaders/PassthroughFader.shader)** HLSL shader handles fading in the fragment shader by adjusting the alpha channel based on fade direction and inverted alpha value.

- `_InvertedAlpha` inverts transparency for the fading effect.
- `_FadeDirection` controls fade direction:
  - `0`: Uses the red channel
  - `1`: Right to left
  - `2`: Top to bottom
  - `3`: Center outwards

All transitions are smoothed with `smoothstep`.

> [!IMPORTANT]
> Since this shader is applied inside a sphere, **Culling** must be turned off (`Cull Off`). Also, set the **Render Queue** to `Transparent-1 (2999)` to ensure it renders behind transparent and opaque materials, preventing **z-fighting** (flickering).

### PassthroughDissolver HLSL & ShaderGraph

To add a stylish effect when fading between VR and passthrough, use the **[PassthroughFaderDissolve](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Scenes/PassthroughFaderDissolve.unity)** scene. It utilizes the **[PassthroughDissolver](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Scripts/PassthroughDissolver.cs)** class to manipulate either the **[PassthroughDissolver](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Shaders/PassthroughDissolver.shader)** or **[PassthroughDissolverSG](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Shaders/PassthroughDissolverSG.shadergraph)** shader, adjusting the dissolve level to reveal passthrough in a pattern.

- **[PassthroughDissolver](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Shaders/PassthroughDissolver.shader)** uses the **[PerlinNoiseTexture](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Scripts/PerlinNoiseTexture.cs)** script to generate the pattern.
- **[PassthroughDissolverSG](./Assets/_MRMotifs/PassthroughTransitioning/_Samples/Shaders/PassthroughDissolverSG.shadergraph)** generates the texture directly inside ShaderGraph.

# Shared Activities in Mixed Reality

[![Video Thumbnail](./Media/Motif2/SharedActivites.png)](https://www.youtube.com/watch?v=ZaW47wZJb0k)

Make sure to read through our [**Developer Documentation**](https://developers.meta.com/horizon/documentation/unity/unity-mrmotifs-shared-activities) for additional information!

Create convincing shared activities in MR that encourage authentic, intuitive interactions with the Shared Activities in Mixed Reality motif. This project uses the [Multiplayer Building Blocks](https://developers.meta.com/horizon/documentation/unity/bb-multiplayer-blocks) to quickly and effortlessly set up a networked experience using the [networked Meta Avatars](https://developers.meta.com/horizon/documentation/unity/meta-avatars-networking). The goal of this motif is then to show developers, how to easily extend the Building Blocks and build custom shared experiences, such as chess and movie co-watching, on top of them.

## Additional Requirements

When using the **Shared Activities** MR Motif, there are several additional requirements that need to be met in order to use the full functionality of this sample. The Multiplayer Building Blocks provide integration with two popular multiplayer frameworks: [Unity Netcode for Game Objects](https://docs-multiplayer.unity3d.com/netcode/current/about/) and [Photon Fusion 2](https://doc.photonengine.com/fusion/current/fusion-intro).

> [!NOTE]
> Both multiplayer frameworks are supported at parity with the exception of the Player Voice Chat block that is only available for Photon Fusion, which is the main reason why **this MR Motif will be based on Photon Fusion 2**. The underlying concept of this sample should be easily transferable to Unity Netcode.

- [Meta Avatars SDK](https://assetstore.unity.com/packages/tools/integration/meta-avatars-sdk-271958) (31.0.0) - com.meta.xr.sdk.avatars

- [Meta Avatars SDK Sample Assets](https://assetstore.unity.com/packages/tools/integration/meta-avatars-sdk-sample-assets-272863) (31.0.0) - com.meta.xr.sdk.avatars.sample.assets

  Required by Networked Avatar block to show a set of pre-set Meta Avatars in the editor when testing.

- [Meta XR Platform SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-platform-sdk-262366) (71.0.0) - com.meta.xr.sdk.platform

  Required by Player Name Tag and Networked Avatar blocks. Also required to retrieve data such as the player's avatar and name, as well as check the entitlement and connect to create group presence to use the friends invite feature.

- [Meta XR Simulator](https://assetstore.unity.com/packages/tools/integration/meta-xr-simulator-266732) (71.0.0) - com.meta.xr.simulator

  (Optional) For multiplayer testing without the need for many headsets.

- [Photon Fusion](https://assetstore.unity.com/packages/tools/network/photon-fusion-267958) (2.0.3)
- [Photon Voice](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) (2.55)

> [!NOTE]
> Make sure to go through the **[Import Photon Voice](https://doc.photonengine.com/voice/current/getting-started/voice-for-fusion)** setup guide.

- [ParrelSync](https://github.com/VeriorPies/ParrelSync) (1.5.2)

  (Optional) Creates and maintains multiple Unity editor instances of the same project for easier multiplayer testing.

## Shared Activities Sample Scenes

The [MRMotifsHome](./Assets/_MRMotifs/MRMotifsHome.unity) scene now includes "[MR Motif] Quest Platform Setup," containing an Entitlement Check (from Multiplayer Building Blocks) and **[InvitationAcceptanceHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Quest%20Platform/InvitationAcceptanceHandlerMotif.cs)**. This handles when a [friend is invited](https://developers.meta.com/horizon/documentation/unity/ps-invite-overview/) to your multiplayer scene, checks their entitlement, and determines the destination scene. It currently supports **chess** and **movie cowatching** but should be updated with your API and scene names. Create [destinations](https://developers.meta.com/horizon/documentation/unity/ps-destinations-overview) via the Developer Dashboard under Engagement > Destinations. Ensure the Data Use Check Up is set correctly by following [this section](#how-the-multiplayer-setup-works).

|                        Chess Sample                          |                   Movie Cowatching Sample                    |
| :----------------------------------------------------------: | :----------------------------------------------------------: |
|          ![Chess](./Media/Motif2/Chess.gif 'Chess')          |         ![Movie](./Media/Motif2/Movie.gif 'Movie')           |


The **chess sample** scene updates chess piece positions and rotations like the **[AvatarMovementHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Avatars/AvatarMovementHandlerMotif.cs)**. The **[ChessBoardHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Chess%20Sample/ChessBoardHandlerMotif.cs)** assigns State Authority to players moving pieces and syncs their networked transforms. It toggles Rigidbody between physics (for the authority) and kinematic (for others). Using Photon Fusion's **IStateAuthorityChanged** interface, it waits for authority transfer before allowing movement. The board has four spawn points, which can be increased by adding more and assigning the **[SpawnPointMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Spawning/SpawnPointMotif.cs)** class.

![Spawn Locations](./Media/Motif2/SpawnLocations.png)

The **movie cowatching** logic in **[MovieControlsHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Movie%20Sample/MovieControlsHandlerMotif.cs)** differs from the previous sample, as it synchronizes UI elements (button/toggle states) instead of transforms. It uses Networked Properties like NetworkBools and NetworkedFloats to track slider values and toggle states. The **IStateAuthorityChanged** interface ensures actions are executed by the correct player. Currently, there are 4 spawn locations set in front of the chess board.

## Multiplayer setup & troubleshooting

Find a detailed setup and troubleshooting guide in our [Developer Documentation](https://developers.meta.com/horizon/documentation/unity/unity-mrmotifs-shared-activities).

## Main components in the Shared Activities MR Motif

This MR Motif's [scripts folder](./Assets/_MRMotifs/SharedActivities/Scripts/) is subdivided into 5 folders, each hosting concise and easy-to-follow classes:
- **Avatars**
  - **[AvatarMovementHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Avatars/AvatarMovementHandlerMotif.cs)**: Manages the synchronization of networked avatar positions and rotations. It childs the remote avatars to the object of interest to make them move with the object. The class ensures that both local and remote avatars are correctly positioned relative to a central "object of interest" by updating their transforms across clients whenever the object is moved or interacted with, maintaining consistency in the multiplayer environment.
  - **[AvatarNameTagHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Avatars/AvatarNameTagHandlerMotif.cs)**: Manages the attachment of name tags to the heads of remote avatars. It waits for the avatars to be initialized and then dynamically parents the name tag to each avatar's head, ensuring that the name tags correctly follow the avatars' movements in the MR environment.
  - **[AvatarSpawnerHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Avatars/AvatarSpawnerHandlerMotif.cs)**: Manages the spawning and positioning of avatars. It utilizes [SpawnManagerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Spawning/SpawnManagerMotif.cs) to assign spawn locations, releases these locations when players exit, and optionally handles group presence features like friend invites for a more interactive multiplayer experience.
  - **[AvatarSpeakerHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Avatars/AvatarSpeakerHandlerMotif.cs)**: Manages the assignment of voice speakers to remote avatars using Photon Voice. It waits for avatars to be initialized and then dynamically attaches a speaker component to each remote avatar's head, ensuring that voice is correctly positioned and synchronized with the avatars in the MR environment.
- **Chess Sample**
  - **[ChessBoardHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Chess%20Sample/ChessBoardHandlerMotif.cs)**: Manages the synchronization of networked chess piece positions and rotations very similarly to the [AvatarMovementHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Avatars/AvatarMovementHandlerMotif.cs). It handles player interactions with the chess pieces such as selecting and moving them and updates their states across all clients, providing networked audio feedback to ensure consistent and interactive gameplay in a multiplayer environment. It also supports physics which is the reason why, as opposed to the [AvatarMovementHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Avatars/AvatarMovementHandlerMotif.cs), we send updates of the positions and rotations every frame instead of only when the pieces are moved, to account for falling or moving pieces due to physics.
- **Helpers**
  - **[ConstraintInjectorMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Helpers/ConstraintInjectorMotif.cs)**: Dynamically injects rotation constraints into the GrabFreeTransformer component of a GameObject. It is used to limit the rotation of interactive objects like the chessboard and movie screen in sample scenes, ensuring they rotate only within specified bounds during user interaction.
  - **[HandleAnimationMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Helpers/HandleAnimationMotif.cs)**: Controls the smooth scaling and transparency transitions of a GameObject during hover interactions. It listens for hover and unhover events using an InteractableUnityEventWrapper and employs coroutines to animate the object's scale and material alpha over a set duration, enhancing visual feedback during user interaction. This is used by the movie panel handle.
- **Movie Sample**
  - **[MovieControlsHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Movie%20Sample/MovieControlsHandlerMotif.cs)**: Manages networked user interactions with a video player. It synchronizes playback controls like play/pause, volume, settings, and timeline adjustments across all connected clients, ensuring consistent video playback and UI states in a multiplayer environment.
- **Quest Platform**
  - **[GroupPresenceAndInviteHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Quest%20Platform/GroupPresenceAndInviteHandlerMotif.cs)**: Manages [group presence](https://developers.meta.com/horizon/documentation/unity/ps-group-presence-overview) and [friend invitations](https://developers.meta.com/horizon/documentation/unity/ps-invite-overview) using the Oculus Platform SDK. It allows users to set their session as joinable with specific destination and session IDs, and provides functionality to launch the invite panel so users can invite friends to join their multiplayer session.
  - **[InvitationAcceptanceHandlerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Quest%20Platform/InvitationAcceptanceHandlerMotif.cs)**: Manages [deep link invitations](https://developers.meta.com/horizon/documentation/unity/ps-deep-linking/) using the Oculus Platform SDK. When the app is launched via a deep link (e.g., from a friend's invitation), it checks the launch details to map the provided destination API name to a scene and automatically loads that scene, directing the user to the appropriate multiplayer session.
  ![Chess Testing](./Media/Motif2/InvitePanel.gif)
- **Spawning**
  - **[SpawnManagerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Spawning/SpawnManagerMotif.cs)**: Manages player spawn locations. It controls a queuing system for players waiting for available spawn points, ensuring avatars are correctly positioned at these locations, and prevents conflicts by assigning unique spawn positions to each player as they join the session.
  - **[SpawnPointMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Spawning/SpawnPointMotif.cs)**: Serves as a marker component in your scene to designate player spawn points. It is used by the [SpawnManagerMotif](./Assets/_MRMotifs/SharedActivities/Scripts/Spawning/SpawnManagerMotif.cs) to identify and manage these spawn locations but contains no additional logic beyond being attached to GameObjects as an identifier.

# Health and safety guidelines

When building mixed reality experiences, we highly recommend evaluating your
content from a health and safety perspective to offer your users a comfortable
and safe experience. Please read the
[Mixed Reality H&S Guidelines](https://developer.oculus.com/resources/mr-health-safety-guideline/)
before designing and developing your app using this sample project, or any of
our Presence Platform features.

Developers should avoid improper occlusion, which occurs when virtual content
does not respect the physicality of the user’s environment. Improper Occlusion
can result in a misperception of actionable space.

- See
  [Occlusions with Virtual Content](https://developer.oculus.com/resources/mr-health-safety-guideline/#passthrough)

- To avoid improper occlusion, developers should ensure that users have (1)
  completed Space Setup and (2) granted Spatial Data permission (setup design)
  to allow proper occlusion in content placement, mesh collisions, and air
  navigation.

Using semi-transparent content lets the user have a better view of their
physical space and reduces the occlusion of objects or people that are not part
of the scanned mesh.

- Spatial data won’t incorporate dynamic elements of a user’s living space (for
  example, a chair that was moved after capture or a moving person/pet in the
  space).

- Uncaptured dynamic elements may be occluded by virtual content, making it more
  difficult for a user to safely avoid such hazards while engaged in the mixed
  reality experience.

Respect the user’s personal space. Avoid having virtual content pass through
their body or loom close to their face. When content crosses into a user’s
personal space they may experience a psychological or visual discomfort, or take
actions to avoid the virtual content that may increase the risk of injury or
damage (for example, backing up into a wall or chair). Dynamic virtual content
may also distract the user from their surroundings.

# License

This codebase is available as both a reference and a template for mixed reality
projects. The [Meta License](./LICENSE) applies to the SDK and supporting
material. The MIT License applies to only certain, clearly marked documents. If
an individual file does not indicate which license it is subject to, then the
Oculus License applies.

See the [CONTRIBUTING](./CONTRIBUTING.md) file for how to help out.
