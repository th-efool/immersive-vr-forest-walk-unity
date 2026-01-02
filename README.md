## ForestVRWalk — Immersive VR Forest Experience (Unity 6000.0.51f1)
![Status](https://img.shields.io/badge/status-experimental%20VR-blue.svg)
![Platform](https://img.shields.io/badge/platform-Unity%20XR-black.svg)
![Domain](https://img.shields.io/badge/domain-VR%20%7C%20AI%20Behaviors%20%7C%20XR%20UI-blueviolet.svg)
![Engine](https://img.shields.io/badge/engine-Unity%206000.0.51f1-lightgrey.svg)
![XR](https://img.shields.io/badge/XR-OpenXR%20%7C%20XR%20Interaction%20Toolkit-success.svg)
![Performance](https://img.shields.io/badge/performance-VR--optimized-success.svg)

<div align="center" style="display:flex; gap:10px; justify-content:center;">

  <video src="https://github.com/th-efool/ForestVRWalk/docs/Clips/Actual%20FINAL.mp4" controls width="360" height="200" style="border-radius:8px; box-shadow:0 0 8px rgba(0,0,0,0.2);"></video>

  <video src="https://github.com/th-efool/ForestVRWalk/docs/Clips/XFINAL.mp4" controls width="360" height="200" style="border-radius:8px; box-shadow:0 0 8px rgba(0,0,0,0.2);"></video>

</div>

This repository contains an XR-ready Unity project for a VR forest scene with autonomous wildlife, spatial audio, and world-space UI. The implementation focuses on clarity, extensibility, and performance for headset builds.

VR performance challenge
- VR renders stereo views and pays high alpha/overdraw cost in foliage-heavy scenes. We designed for aggressive cost control from the start: billboarding, distance culling, LOD crossfades, occlusion, and conservative post. PCVR recommended.

### Contents (Stepwise Deep-Dive)
1. Terrain Generation (heightmap, splats, trees, details)
2. Rendering Optimization for VR Forests (billboarding, culling, URP/XR)
3. Movement & Animation Graphs (animals)
4. AI Systems (wander, activation, flee)
5. VR UI (world-space info panels and interactable triggers)
6. Ambient Audio (looping beds + procedural one-shots)
7. XR & Input Setup (XRI integration)
8. Performance Strategy (distance activation, pooling)
9. Project Structure & Getting Started
10. Contributing & Notes

---

## 1) Terrain Generation (heightmap, splats, trees, details)

We treated terrain as a first-class system and standardized a heightmap-driven workflow:

Step 1 — Heightmap authoring/import
- Imported 16-bit RAW heightmaps and configured Terrain resolution to match the detail budget.
- Tuned Terrain Pixel Error to balance vertex count vs. silhouette fidelity (VR comfort priority: 5–12 depending on distance).

Step 2 — Splatmaps & textures
- Authored control splatmaps to paint albedo/normal sets (e.g., dirt, moss, rock, path). For URP, used Terrain Layers with packed texture sets.
- Calibrated tiling and normal intensity per material for headset-scale realism.

Step 3 — Trees (LODs + billboards)
- Placed trees via Terrain trees or baked prefab placement for precise control.
- Each tree uses `LODGroup` with:
  - LOD0/LOD1 mesh simplifications, and
  - Billboard/impostor at far range (see Section 2 for billboarding fades).
- Disabled per-tree colliders where not needed; retained only for interactable specimens.

Step 4 — Details/grass
- Used Terrain Details with GPU instancing, density scaled by performance budget.
- Grass shader configured for wind animation with cheap vertex sway and distance-based fade out.
- Clustered detail density near player routes; sparse in peripheral areas.

Step 5 — Paths & decals
- Painted path splats and added decal mesh strips for high-frequency detail where players look closely.

Step 6 — Baking & probes
- Baked lightmaps/reflection probes across sectors; probe density reduced in heavy foliage to reduce memory.

Key terrain settings we standardized
- Terrain: Pixel Error 5–12 (scene-dependent), Draw Instanced on, Basemap Distance reduced for VR.
- Trees/Details: Billboard Start close, Billboard Fade crossfade on, density scaled by device.

---

## 2) Rendering Optimization for VR Forests (billboarding, culling, URP/XR)

Forests in VR are notoriously expensive. We executed an aggressive, layered optimization plan to cut cost without killing atmosphere.

Foliage billboarding & impostors
- LODGroups on trees/bushes with crossfade between LODs to avoid popping.
- Billboard/impostor last LOD with per-asset fade factor; alpha-clip dithering used for smooth transitions.
- Impostor atlas baked for hero species to reduce draw calls at distance.

Distance-based culling & density scaling
- Hard stop distance for grass/details beyond which nothing is rendered.
- Species-level density falloff curves (dense near path, quickly sparse off-path).
- Per-quality-level foliage density multipliers (PCVR/Standalone vs. Mobile XR).

URP pipeline tuning (applicable also to HDRP analogs)
- SRP Batcher enabled; Shader variants audited to ensure compatibility.
- Forward+ disabled if overdraw increased; MSAA 2x–4x (no TAA in VR for comfort) — verified per device.
- Shadow cascades reduced (2) with tuned distances; soft shadows off for mobile XR, on for PCVR only nearby.
- Per-object limit reduced; mixed lighting with baked GI where possible.

XR rendering mode & foveation
- Single Pass Instanced rendering for stereo (platform permitting).
- Fixed Foveated Rendering (FFR) on devices that support it; conservative level to avoid artifacting on UI.
- Dynamic Resolution optional, minimum scale clamped to keep UI legible.

Camera & culling
- Far plane pulled in aggressively (e.g., 200–400m depending on scene scale), relying on atmospheric perspective in skybox/gradient.
- Occlusion Culling baked per-forest sector; portals used for huts/caves to zero interior overdraw.

Overdraw and alpha testing
- Grass/leaf shaders use alpha-clipping with ordered dithering; bias tuned to minimize pixel kill cost.
- Where feasible, switched to cutout over transparent; tightened geometry to reduce overdraw footprint.

Batching & instancing
- GPU Instancing enabled on foliage/props; verified material property blocks for color tints.
- Static Batching for static rocks/logs; material atlases to collapse draw calls.

Shader level-of-detail (LOD)
- Variant stripping for unused features; keyword counts minimized.
- Per-quality keyword toggles to disable expensive effects (parallax, subsurface) on low.

Post-processing minimalism
- No screen-space AO in VR forest (too costly); relied on baked/light probes for grounding.
- Lightweight color grading; bloom disabled or very subtle to avoid halo in stereo.

Made-for-VR hard cuts
- Turned off shadows on far LODs for trees; shadow caster culled beyond mid distance.
- Terrain holes avoided; tessellation disabled.
- Camera stacking avoided; UI uses world-space canvases.

Performance verification loop
- GPU/CPU captures across hotspots; iterated density/LODs until frame time target was met.
- Per-platform quality tiers ship with preset densities, far-plane, shadow settings, and FFR levels.

---

## 3) Movement & Animation Graphs

We implemented a data-driven motion stack for animals using `CharacterController` physics and Animator state blending.

- `Assets/ithappy/Animals_FREE/Scripts/CreatureMover.cs`
  - Component requires `CharacterController` and `Animator`.
  - Exposes `SetInput(Vector2 axis, Vector3 target, bool isRun, bool isJump)`.
  - Internal MovementHandler:
    - Converts input space (self/world) into displacement.
    - Gravity integration and grounded checks.
    - Smooth turn-in-place using target-forward and angular velocity.
  - Internal AnimationHandler:
    - Blends locomotion state with smoothed inputs.
    - Supports IK look-at using a target and tunable look weight.

Animator Graphs & Controllers
- Each animal relies on a valid Animator Controller (e.g., `Assets/ithappy/Animals_FREE/Animations/Animation_Controllers/...`).
- Parameters used by `CreatureMover`:
  - `Vert` (float): magnitude of motion applied to locomotion blend tree.
  - `State` (float): coarse blend for walk/run tiers.
- Graphs were validated against the included demo controllers and extended for smooth transitions, ensuring consistent stride at different speeds.

Key Results
- Natural walk/run with rotation leading motion.
- Animator transitions remain smooth under variable input magnitudes.



---

## 2) AI Systems (wander, activation, flee)

Autonomous movement is built on top of `CreatureMover`, keeping gameplay logic decoupled from core physics/animation.

- `Assets/ithappy/Animals_FREE/Scripts/AIMover.cs`
  - Wander behavior:
    - Picks random destinations within a configurable radius.
    - Alternates move/pause cycles for organic pacing.
    - Optional run bursts via probability.
  - Distance Activation:
    - Auto-detects `XROrigin` camera; falls back to `Camera.main`/`tag=Player`.
    - Sleeps animation/motion beyond a range with hysteresis; re-enables when near.
  - Flee/Run-Away Trigger:
    - Public API: `TriggerRunAway(Transform/Vector3 from, duration, distance)`.
    - Computes an away vector and forces a timed sprint.

Design Principles
- Controller-agnostic: AI only calls `CreatureMover.SetInput(...)`.
- Headset-friendly: motion deactivates far from the user to save CPU/GPU.



---

## 3) VR UI (world-space info + interactable triggers)

We added dynamic, billboarded world-space UI that feels anchored in the scene and responds to XR rays/pokes.

- `Assets/VRAnimalInfoUI.cs`
  - Builds a world-space Canvas at an offset; billboards to the user.
  - Title + multi-line description using TextMeshPro.
  - Toggle button defaults to “ShowInfo” => reveals the panel (hidden by default).
  - Auto-resizes panel height to fit content (min/max clamps); no clipping.
  - XR-ready (TrackedDeviceGraphicRaycaster) for hover/press feedback.

- `Assets/VRPokeRunAway.cs`
  - Compact world-space button at an offset; billboards to the user.
  - On click, invokes `AIMover.TriggerRunAway(...)` so animals sprint away from the interactor’s position.
  - XR-ready UI with proper hover/press states.

UX Details
- Both canvases explicitly rotate 180° after billboarding to respect the scene’s facing conventions.
- Offset/size/scale are inspector-tunable for per-animal placement.



---

## 4) Ambient Audio (bed + one-shots)

Spatialized ambiances that scale with player proximity and avoid repetition.

- `Assets/AmbientAudioManager.cs`
  - Looping Bed:
    - Two AudioSources crossfading long-format loops (wind/forest bed).
    - Subtle LFO volume modulation + pitch jitter to avoid static feel.
  - Procedural One-Shots:
    - Random bird/insect/foliage SFX spawned in a ring around the listener.
    - Volume/pitch variance, concurrency caps, and pooling for performance.
  - Distance Activation:
    - Mutes sources beyond a threshold to save CPU/voices.
  - VR Integration:
    - Auto-detects XR camera; leave Output Mixer empty if not using an AudioMixer.

Recommended Clip Imports
- Long loops: Streaming or Compressed In Memory; OGG/WAV loop-safe ideal.
- One-shots: Compressed In Memory; Spatial Blend 3D.



---

## 5) XR & Input Setup

The project uses Unity XR Interaction Toolkit.

Core Requirements
- XR Rig (XROrigin) with ray/poke interactors for UI.
- EventSystem in scene configured with XR UI input (XRUIInputModule).
- Ensure UI layers are included in raycaster masks.

Auto-Detection
- Systems that need the player reference first look for `XROrigin.Camera`, then `Camera.main`, then `tag=Player`.



---

## 6) Performance Strategy

We prioritized fluid VR performance via:

- Distance-based Activation
  - AI movers disable cost-heavy components when far; resume when near.
  - Ambient audio mutes beyond range; one-shots limited by concurrency.

- Pooling & Batching
  - Reuse AudioSources for one-shots; avoid runtime allocations.

- Animator/Physics Hygiene
  - `CreatureMover` performs light-weight CharacterController updates.
  - Animator parameters updated with smoothed flows to avoid spikes.



---

## 7) Project Structure & Getting Started

Key Paths
- `Assets/ithappy/Animals_FREE/Scripts/CreatureMover.cs` — core locomotion & animation driver.
- `Assets/ithappy/Animals_FREE/Scripts/AIMover.cs` — autonomous wander/activation/flee.
- `Assets/VRAnimalInfoUI.cs` — world-space info panel for animals.
- `Assets/VRPokeRunAway.cs` — world-space button to trigger run-away behavior.
- `Assets/AmbientAudioManager.cs` — ambient loops + one-shots.
- `Assets/Scenes/BasicScene.unity` / `Assets/Scenes/SampleScene.unity` — example scenes.

Setup Steps
1. Open with Unity 6000.0.51f1.
2. Ensure XR packages (XR Interaction Toolkit, OpenXR) are resolved (Packages/manifest.json).
3. Add an XR Rig (XROrigin) with an EventSystem using XR UI input.
4. Drop animals into scene:
   - Ensure each has `CharacterController`, `Animator`, `CreatureMover`.
   - Add `AIMover` for autonomous behavior (optionally set run chance and bounds).
5. Add world-space UI:
   - `VRAnimalInfoUI` to show info; `VRPokeRunAway` for a “Run!” button.
6. Add ambient audio:
   - Place `AmbientAudioManager` in scene and assign Loop/One-shot clips.

---

## 8) Contributing & Notes

Coding Standards
- Plain, readable code with descriptive identifiers.
- Avoid deep nesting; prefer guard clauses.
- Only essential comments (non-obvious rationale and caveats).

Extensibility Ideas
- NavMesh-based pathing fallback for complex terrains.
- Flocking/herding behaviors (steering) for groups.
- Zone-based audio controllers (rivers, wind corridors).
- Save/load system for animal states and player progress.

Attribution
- Asset packs and sample controllers within `Assets/ithappy/Animals_FREE` and others were integrated, tuned, and validated within this architecture for VR.

---

 
