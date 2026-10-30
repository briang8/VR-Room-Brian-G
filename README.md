# Enhanced Create with VR

A room-scale VR project built in Unity. It includes the completed **Create with VR** course project and a second interactive scene built from the **VR Room** assets. A start screen and an in-room menu link the scenes.

## Features

- **Two rooms and a start screen.** You can travel between them in both directions without restarting.
- **Physics door.** Grab the handle and push or pull. The door turns on a Hinge Joint and follows your hand at a natural speed. It uses no animation.
- **Sockets.** A flashlight dock, a rope hook and a 64-square chess board. Each one accepts only its own objects, filtered by interaction layers.
- **Grabbing.** Physics-based grab interactables throughout both rooms.
- **Teleportation.** Teleport anchors (mats) in both rooms, plus snap or continuous turn. Continuous movement is optional.
- **Analog wall clock.** Its hands are driven by the system time in code, and a digital display sits underneath.
- **Animated hands.** Stylized hand models whose fingers curl with the controller's grip and trigger.
- **UI.** A start screen, an in-room menu (travel, reset objects, objectives, settings, controls) and a settings panel.
- **Extras:**
  - wrist objectives that tick off as you complete them;
  - fade transitions between scenes;
  - spatial interaction sounds;
  - haptics;
  - hint labels;
  - a reset button;
  - a bouncy tennis ball.

## Requirements

- Unity 6 with the Universal Render Pipeline
- XR Interaction Toolkit 3.1.3, XR Plugin Management, OpenXR, Input System, TextMeshPro
- A Meta Quest (via Link / Air Link, or as an Android build), or the XR Interaction Simulator to test without a headset

## Getting started

1. Clone the repository and open the folder in Unity Hub with Unity 6.
2. Open `Assets/Scenes/Start.unity` and press **Play**.
3. Pick a room from the start screen.

**Scene order** for builds (File > Build Profiles > Scene List):

| # | Scene | Shown as |
|---|---|---|
| 0 | `Assets/Scenes/Start` | Start screen |
| 1 | `Assets/Scenes/VR Room - Brian G` | Brian Room (course project) |
| 2 | `Assets/Unity - VR Room/Assets/Scenes/VR Room` | VR Room |


## Controls

| Action | Brian Room | VR Room |
|---|---|---|
| Grab / release | Grip | Grip |
| Press UI | Point the ray, pull the trigger | Point the ray, pull the trigger |
| Teleport | Press X / A or touch the left stick, aim at a mat, press grip | Push the left stick forward, aim at a mat, let go |
| Turn | Right thumbstick | Right thumbstick |
| Walk | Left thumbstick (turn on in Settings or the room menu) | Left thumbstick (turn on in the room menu) |
| Room menu | Y or B | Y or B |

## Building

- **Windows (Link / Air Link):**
  1. Switch the platform to Windows.
  2. Tick OpenXR in XR Plug-in Management and add the Oculus Touch Controller Profile.
  3. Click **Build**.
- **Quest standalone (Android):**
  1. Switch the platform to Android.
  2. Tick OpenXR with **Meta Quest Support** and add the Oculus Touch Controller Profile.
  3. In Player settings, set the Scripting Backend to IL2CPP and Target Architectures to ARM64.
  4. Turn on Developer Mode on the headset, connect it by USB, then click **Build and Run**.

## Project structure

```
Assets/
  Scenes/                       Start scene and the course project scene
  Unity - VR Room/              VR Room models, materials and scene
  _Course Library/              Create with VR course prefabs, scripts, audio
  VRAssignmentKit/Runtime/      Project scripts (door, clock, hands, menus, navigation, audio)
  VRAssignmentKit/Generated/    Materials for socket stands and the start floor
  SimpleHands/                  Stylized hand models
  Samples/XR Interaction Toolkit/   XRI Starter Assets and Interaction Simulator
  XRI/Settings/                 Interaction layer names
```

### Main scripts

| Script | Purpose |
|---|---|
| `PhysicsDoor` | Turns the hinged door towards the hand that holds its handle; latch, creak, haptics |
| `WallClock` | Analog and digital clock from `DateTime.Now` |
| `AnimatedHand` | Grip and trigger values drive the finger bones |
| `SceneNavigator` | Fade, scene load, re-enables XR input after each transition |
| `RoomMenu` / `StartMenu` | In-room menu and start screen |
| `ChessBoard` / `SocketFeedback` | Chess sockets; socket colour and sound feedback |
| `ObjectiveTracker` / `Tooltip` | Wrist objectives; floating hint labels |
| `AudioOutputFollower` | Keeps audio on the current output device |

## Third-party assets

- Create with VR course library – Unity Learn
- VR Room assets
- XR Interaction Toolkit Starter Assets – Unity
- Stylized - Simple Hands – sametsylmz3d (Unity Asset Store)
