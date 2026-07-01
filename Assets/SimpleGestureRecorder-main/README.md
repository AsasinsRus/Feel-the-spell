# Simple Gesture Recorder 
### XR Hand pose snapshot tool for Unity Editor (windows)
![Simple Gesture Recorder Screenshot](https://github.com/Phlegmati/SimpleGestureRecorder/assets/56511043/c5e37115-de22-40cf-a173-55a7c79314b8)

This repository contains a utility tool for Unity developers, which works on top of the [Gesture Samples](https://docs.unity3d.com/Packages/com.unity.xr.hands@1.4/manual/index.html#samples).
The tool facilitates the capturing of Unity's XRHandShape, enabling developers to create snapshots easily. These snapshots can then be exported as assets for convenient reuse within the Unity engine.

## How to Install

### Installing Dependencies

1. Create a new Unity project (version 2022.3.15f1 or later) with 3D Core.

2. Go to the Package Manager and manually install:
    - [TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.2/manual/index.html)
    - [XRHands v1.4.0-pre1](https://docs.unity3d.com/Packages/com.unity.xr.hands@1.4/manual/project-setup/install-xrhands.html)
      - Click on the '+' symbol.
      - Choose "Add package by name."
      - For name, insert: `com.unity.xr.hands`.
      - For version number, use: `1.4.0-pre.1`.
      - Let Unity restart if prompted.

3. Go to "Project Settings" and tap XR Plug-in Management.
    - Set the check for "OpenXR."
    - Let Unity install the packages.

4. Go to Project Validation and fix any errors that may occur.
   
5. Go to OpenXR and choose your Play Mode OpenXR Runtime (e.g., Oculus).
   
6. Under Features, check "Hand Tracking Subsystem" and "Meta Hand Tracking Aim."
   
7. Now, go to the Package Manager > XRHands.
    - Go to the "Samples" tab on the right
    - Install Gestures and HandVisualizer.
    

### Installing Simple Gesture Recorder

Make sure you completed all the steps above before proceeding to the next section.

- Go to the Assetstore [here]() or
- Clone this repository in your project

## How to Use

### Recording a Gesture

![Simple Gesture Recorder Usage](https://github.com/Phlegmati/SimpleGestureRecorder/assets/56511043/34c72b6b-0f11-4d56-a772-89befc77ef5f)

1. Start the recording scene under `SimpleGestureRecorder/Scenes/Recording Scene.unity`.
   
2. To create a handshape, begin the recording by making a thumb pose with both hands.
   
3. Hold this pose until a countdown timer appears.
   
4. Aim your hands within the frame.
   
5. After the timer reaches 0, the recording starts.
    
6. Make your custom pose and hold it until the recording stops.
    
7. Test your pose in the scene; if everything works, the indicators will light up.
    
8. Assets were created under `SimpleGestureRecorder/Snapshots`:
   - Screenshot of Pose
   - (optional) LeftHandPose.asset
   - (optional) RightHandPose.asset


## Contribute

Feel free to use and contribute in any way, making this tool awesome and usable for everyone.

## ToDos:

- Support Mac and Linux (paths etc.)
- Record XRHandPoses instead of XRHandShapes
- Record hand motion
- Edit approximity calculations for tolerance
- Improve UI/UX

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

Copyright (c) 2024 Nico Mahler
