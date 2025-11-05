## ForestVRWalk — Project Index

**Unity version**: 6000.0.51f1

### Top-level structure
- **Assets/**: game content (scenes, prefabs, materials, scripts, settings)
- **Packages/**: package manifest/lock
- **ProjectSettings/**: project configuration
- **UserSettings/**: user-local settings
- **Library/**, **Temp/**, **Logs/**, **obj/**: generated/cache (can be ignored in VCS)

### Scenes (Assets)
- Assets/Scenes/BasicScene.unity
- Assets/Scenes/SampleScene.unity
- Assets/Samples/XR Hands/1.6.1/HandVisualizer/HandVisualizer.unity
- Assets/Samples/XR Interaction Toolkit/3.2.1/Hands Interaction Demo/HandsDemoScene.unity
- Assets/Samples/XR Interaction Toolkit/3.2.1/Starter Assets/DemoScene.unity
- Assets/LittleFriends-CartoonAnimals-Lite/Demo/DemoScene_LittleCat.unity
- Assets/LittleFriends-CartoonAnimals-Lite/Demo/DemoScene_LittleSquirrel.unity
- Assets/ithappy/Animals_FREE/Scenes/Built-In_Scenes/Demonstration.unity
- Assets/ithappy/Animals_FREE/Scenes/URP_Scenes/Demonstration.unity
- Assets/ithappy/Animals_FREE/Scenes/HDRP_Scenes/Demonstration.unity
- Assets/Zacxophone/Birds/Built-In/DemoScenes/LowPolyBirdsExampleSceneBuiltIn.unity
- Assets/Zacxophone/Birds/URP/DemoScenes/LowPolyBirdsExampleSceneURP.unity
- Assets/Zacxophone/Birds/HDRP/DemoScenes/BirdsExampleSceneHDRP.unity
- Assets/Bublisher/3D Stylized Animated Dogs Kit/DemoScene/SampleScene.unity

### Scripts (Assets)
- Assets/RandomMoveAroundSetAnchorPoint.cs

### Prefabs (Assets)
Note: Large set present. Examples:
- Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab
- Assets/VRTemplateAssets/Prefabs/Teleport/Teleport Anchor.prefab
- Assets/VRTemplateAssets/Prefabs/TutorialPlayer/Tutorial Player.prefab
- Assets/ithappy/Animals_FREE/Prefabs/Dog_001.prefab (and other animals)
- Assets/Zacxophone/Birds/*/Prefabs/... (low poly birds)
- Assets/Samples/XR Interaction Toolkit/3.2.1/... (starter/hands demo prefabs)

### Shaders (Assets)
- Assets/VRTemplateAssets/Shaders/Grid.shader
- Assets/VRTemplateAssets/Shaders/FauxBlurURP.shader
- Assets/VRTemplateAssets/Materials/Skybox/Horizontal Skybox.shader
- Assets/TextMesh Pro/Shaders/TMP_*.shader
- Assets/Samples/XR Interaction Toolkit/3.2.1/Starter Assets/Shaders/UI-NoZTest.shader

### Materials (Assets)
Examples (many more present):
- Assets/Scenes/BasicScene/Grid.mat
- Assets/VRTemplateAssets/Materials/Environment/Concrete.mat
- Assets/VRTemplateAssets/Materials/UI/Blue.mat
- Assets/ithappy/Animals_FREE/Materials/Material.mat
- Assets/LittleFriends-CartoonAnimals-Lite/Materials/Cat_Body_Idle.mat

### Animations (Assets)
Examples:
- Assets/VRTemplateAssets/Models/Anchor/AnchorFadeScale.anim
- Assets/ithappy/Animals_FREE/Animations/Other_Animations/Dog_001_run.anim
- Assets/LittleFriends-CartoonAnimals-Lite/Animations/04_Run_Cat_Copy.anim

### Animator Controllers (Assets)
- Assets/ithappy/Animals_FREE/Animations/Animation_Controllers/Dog.controller
- Assets/ithappy/Animals_FREE/Animations/Animation_Controllers/Kitty.controller
- Assets/Zacxophone/Birds/Animators/01BirdAnimator.controller
- Assets/Bublisher/3D Stylized Animated Dogs Kit/Animator Controllers/Dog_Animator_Controler.controller

### Settings and Configuration
- Packages/manifest.json, Packages/packages-lock.json
- ProjectSettings/*.asset (graphics, quality, XR, input, etc.)
- Assets/Settings/Project Configuration/* (URP/quality presets)
- Assets/XR/Settings/* (OpenXR/Oculus/XR settings)

### Notes
- Many files under Library/ and Library/PackageCache/ are generated or package contents and are typically excluded from version control.
- For quick navigation to gameplay code, start with scripts under Assets/ (currently 1 custom C# file listed above).


