# Project Overview
- Game Title: Good Knight VR Game
- High-Level Concept: A VR experience where a knight can ride a horse.
- Players: Single player (VR).
- Target Platform: Standalone Windows (VR).
- Render Pipeline: URP.

# Game Mechanics
## Core Gameplay Loop
The player explores the environment as a Knight. They can interact with a horse to mount it, ride around using VR controller inputs, and dismount.

## Controls and Input Methods
- **Mount**: Interact with the `MountButton` on the horse (using XR Select action, typically grip/trigger).
- **Movement (Riding)**: Use the **Left Thumbstick** to move the horse forward/backward and turn.
- **Dismount**: Press the **Jump** button (Right controller) or **Select** (Right controller) or **Escape** on the keyboard.
- **Full VR Follow**: The Knight character model follows the player's physical movements (head position, rotation, and height) while staying correctly positioned on the horse's saddle.

# UI
- No specific UI elements are requested. Interaction is handled via XR Interaction Toolkit (XRI) interactables.

# Key Asset & Context
- **Knight**: The character GameObject (`Knight`) with `VRAvatarFollow` and an `Animator`.
- **Horse**: The horse GameObject (`DetailedHorsePrefab`) with `HorseController`.
- **SaddlePoint**: The anchor point on the horse for the rider.
- **KnightAnimator**: `Assets/Fantasy knight/Animations/KnightAnimator.controller`.

# Implementation Steps

## Step 1: Update VRAvatarFollow Script
- **Description**: Modify `VRAvatarFollow.cs` to support "Riding Mode". This involves adding a toggle to adjust the vertical offset and scaling behavior when the player is on the horse.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 2: Update HorseController Script
- **Description**: Enhance `HorseController.cs` to correctly handle the Knight character during mounting/dismounting.
    - Find the `Knight` object and its `VRAvatarFollow` component.
    - Set the `VRAvatarFollow` state (adjust offsets) in `Mount()` and `Dismount()`.
    - Ensure the `XR Origin` is parented to the `SaddlePoint` for correct VR camera movement.
    - Update animator parameters on the Knight.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

## Step 3: Setup Knight Animator
- **Description**: Assign the `KnightAnimator` to the `Knight` GameObject. Add an `IsRiding` bool parameter to the animator controller to allow for riding-specific poses/animations in the future.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 4: Scene Verification and Wiring
- **Description**: Ensure the `MountButton` on the horse is correctly linked to the `HorseController` and that the `XR Origin` and `Main Camera` references are correctly set up in `VRAvatarFollow`.
- **Assigned role**: developer
- **Dependencies**: Steps 1, 2, 3
- **Parallelizable**: No

# Verification & Testing
- **Manual Test**: Enter Play Mode, walk up to the horse, and interact with the `MountButton`.
- **Check Movement**: Verify the horse moves with the left thumbstick and the player's view follows correctly.
- **Check Avatar**: Verify the Knight model stays on the saddle and follows the player's head rotation/movement.
- **Check Dismount**: Verify the player can dismount and return to normal walking mode.
