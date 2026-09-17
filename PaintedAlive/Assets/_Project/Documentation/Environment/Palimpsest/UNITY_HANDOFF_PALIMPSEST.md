# The Palimpsest — Final Unity Handoff

Target: Unity 6 LTS / URP  
Blender source: `Art/Environments/The_Palimpsest/Painted_Alive_The_Palimpsest.blend`  
Coordinate export: Blender metres, exported with Forward `-Z`, Up `Y`, Apply Unit Scale `1.0`.

This package preserves the validated map. The Visual, Collision, and Gameplay FBXs were staged in memory from the master and exported without saving scene edits.

## Package contents

- `Models/The_Palimpsest_Visual.fbx` — visible geometry only; interactive visual pieces remain separate.
- `Models/The_Palimpsest_Collision.fbx` — collision proxies and state families only.
- `Models/The_Palimpsest_Gameplay.fbx` — gameplay empties/helpers and pivots.
- `Textures/Paintings/` — four project-local generated paintings.
- `Materials_Source/URP_Starter/material_mapping.json` — all 30 actual Blender materials, object users, image inputs, Principled inputs, nodes, and URP classification.
- `Documentation/UNITY_BINDINGS_PALIMPSEST.json` — machine-readable bindings.
- `Documentation/BINDING_VALIDATION_REPORT.txt` — generated name/material/package validation.
- `Documentation/README_Unity_Import.txt` — import order and Unity setup.

## Scene counts

| Export group | Count |
|---|---:|
| Visual objects | 3,475 |
| Collision objects | 80 |
| Gameplay helpers | 76 |
| Mechanism pivots | 3 |
| Materials | 30 |
| Exported textures | 4 |
| Generated paintings | 4 |

## System 1 — Fold A

**Unity component:** `CanvasFoldSystem`  
**Visual:** `INT_Fold_A` plus the separate `Fold_A_*` mechanism pieces in the Visual FBX.  
**Pivot:** `GP_Fold_A_Pivot` (local X axis).  
**Open state:** `GP_Fold_A_State_Open`, `COL_Fold_A_Open`, `COL_Junction_Main_Open`.  
**Folded state:** `GP_Fold_A_State_Folded`, `COL_Fold_A_Folded`, `COL_Junction_Main_Folded`.  
**Clearance:** `GP_Fold_A_Clearance`.  
**Painter control:** `PAINTER_Fold_A_Control` (`telegraph_seconds = 2.5`).  
**Routes:** `Main` is the affected normal route; `Fold_A_SafeBypass` remains the authored counter-route.  `GP_Fold_A_Route_A` and `GP_Fold_A_Route_B` are the route references.

Painter activation must enter `OPEN → TRANSITIONING → FOLDED` after the telegraph. Rotate the canvas around `GP_Fold_A_Pivot` by the authored **65° local X** state delta. The state changes route topology: the normal canvas route becomes harder or unavailable while the exposed frame service route remains available. Figure counterplay is the moving fold, the newly exposed route, or timing during transition. Never teleport the Figure. Use `GP_Fold_A_Clearance` as a swept-volume trigger and keep one collision state active.

## System 2 — Great Fold

**Unity component:** `CanvasFoldSystem` or `GreatFoldController`  
**Primary visual:** `INT_GreatFold` plus the separate `GreatFold_*` pieces.  
**Secondary/lower leaf:** `GreatFold_LowerLeaf`.  
**Pivot:** `GP_GreatFold_Pivot` (local X axis).  
**State A:** `GP_GreatFold_State_A`, `COL_GreatFold_State_A`, `COL_Junction_Main_A`.  
**State B:** `GP_GreatFold_State_B`, `COL_GreatFold_State_B`, `COL_Junction_Main_B`.  
**Clearance:** `GP_GreatFold_Clearance`.  
**Painter control:** `PAINTER_GreatFold_Control` (`telegraph_seconds = 2.5`).  
**Routes:** `Main`, `Exposed_Shortcut`, and `Smear_B_Shortcut`, referenced by `GP_GreatFold_Route_A` and `GP_GreatFold_Route_B`.

Use the authored pivot and **65° local X** delta. Telegraph tension ropes, hinge hardware, and the fold seam before movement. The fold may block the old route, become a ramp, expose upper-frame travel, and open the route to Smear B. Keep Figure counterplay through the exposed upper frame and Smear B shortcut. No instant trap and no teleportation. Use `GP_GreatFold_Clearance` for occupancy and swept-volume safety.

## System 3 — Backside Works

**Unity components:** `CanvasBacksideRegion`, `CanvasBacksideTraceSystem`  
**Front canvas:** `Backside_MainCanvas`.  
**Secondary canvas:** `Backside_Secondary`.  
**Backside geometry:** the `Backside_*` visual objects in the Visual FBX.  
**Entry:** `GP_Backside_Entry`.  
**Exit:** `GP_Backside_Exit`.  
**Region trigger:** `GP_Backside_Region` (`is_trigger = true`, occupancy plus coarse trace).  
**Trace helpers:** `GP_Backside_Trace_A`, `GP_Backside_Trace_B`, `GP_Backside_Trace_C`; each has `uncertainty_radius_m = 3`.  
**Collision:** the `COL_Backside_*` objects in the Collision FBX and the static route proxies used by `Backside_Service`.  
**Approximate route length:** 46.2485 m.  
**Route:** `Backside_Service`.

When a Figure enters `GP_Backside_Region`, do not expose its exact position to Painter. Keep occupancy state synchronized and feed only a coarse trace or deformation cue to the front surface. The Figure can mislead Painter through route changes behind the canvas. This is approximate positional information, not full invisibility.

## Systems 4 and 5 — Underpainting A/B

**Unity component:** `UnderpaintingRevealSystem`  

### Underpainting A

- Root: `INT_Underpaint_A`
- Current visual: `CURRENT_LAYER_A` and `CURRENT_LAYER_A_*`
- Revealed visual: `UNDERPAINT_LAYER_A_Arch*` and `UNDERPAINT_LAYER_A_Jamb*`
- Current collision: `COL_Underpaint_A_Current`
- Revealed collision: `COL_Underpaint_A_Revealed`
- Helpers: `GP_Underpaint_A_Current`, `GP_Underpaint_A_Revealed`, `GP_Underpaint_A_Reveal`
- Painter control: `PAINTER_Underpaint_A_Control`

### Underpainting B

- Root: `INT_Underpaint_B`
- Current visual: `CURRENT_LAYER_B` and `CURRENT_LAYER_B_*`
- Revealed visual: `UNDERPAINT_LAYER_B_Arch*` and `UNDERPAINT_LAYER_B_Jamb*`
- Current collision: `COL_Underpaint_B_Current`
- Revealed collision: `COL_Underpaint_B_Revealed`
- Helpers: `GP_Underpaint_B_Current`, `GP_Underpaint_B_Revealed`, `GP_Underpaint_B_Reveal`
- Painter control: `PAINTER_Underpaint_B_Control`

Both systems use `CURRENT → TRANSITIONING → REVEALED`, with the authored reveal translation `[0, 0, 5]` metres. Activate the corresponding old arch/stair geometry and exactly one collision state; never leave overlapping current and revealed blockers active. Routes are `Main` and `Underpaint_Defensive`. Synchronize state for late join.

## Systems 6 and 7 — Smear A/B

**Unity component:** `PaintSmearSurface`  

### Smear A

- Visual: `Smear_A_WetLayer`, with `Smear_A_SupportTrack*`, scrape hardware, and `Smear_A_ImpastoRidge*`
- Root/helper reference: `INT_Smear_A`
- Extended/retracted helpers: `GP_Smear_A_Extended`, `GP_Smear_A_Retracted`
- Clearance: `GP_Smear_A_Clearance`
- Collision: `COL_Smear_A_Extended`, `COL_Smear_A_Retracted`
- Painter control: `PAINTER_Smear_A_Control`
- Material: `MAT_Canvas_Wet`
- Shape key: `Smear_A_WetLayer.Retracted`; `0 = extended`, `1 = retracted`
- Authored travel: 7 m along the helper delta
- Affected route: `Main`

### Smear B

- Visual: `Smear_B_WetLayer`, with `Smear_B_SupportTrack*`, scrape hardware, `Smear_B_Junction_0/1`, and `Smear_B_ImpastoRidge*`
- Root/helper reference: `INT_Smear_B`
- Extended/retracted helpers: `GP_Smear_B_Extended`, `GP_Smear_B_Retracted`
- Clearance: `GP_Smear_B_Clearance`
- Collision: `COL_Smear_B_Extended`, `COL_Smear_B_Retracted`
- Painter control: `PAINTER_Smear_B_Control`
- Material: `MAT_Canvas_Wet`
- Shape key: `Smear_B_WetLayer.Retracted`; `0 = extended`, `1 = retracted`
- Authored travel: 7 m along the helper delta
- Affected routes: `Smear_B_Shortcut`, `Smear_B_Exit`

Use `RETRACTED → MOVING → EXTENDED` and animate the wet layer, impasto ridges, hardware, and state collision together. This is thick paint being dragged, not an ordinary platform. The Figure may ride or time the movement; never teleport. Use clearance helpers and server-authoritative transitions.

## System 8 — Perspective Exit

**Unity components:** `PerspectiveFrameSystem`, `PerspectiveTraversalBridge`  
**Frame helper:** `INT_Perspective_Frame`.  
**Frame pivot:** `GP_Perspective_Frame_Pivot`.  
**Misaligned state:** `GP_Perspective_State_Misaligned` (12° Z rotation; bridge offset `[5,0,0]`).  
**Aligned state:** `GP_Perspective_State_Aligned` (bridge offset `[0,0,0]`).  
**View reference:** `GP_Perspective_ViewReference`.  
**Targets:** `GP_Perspective_Target_A`, `GP_Perspective_Target_B`.  
**Traversal visual:** `Perspective_Aligned_Connection`.  
**Traversal collision:** `COL_Perspective_Aligned`, `COL_Perspective_Misaligned`.  
**Painter control:** `PAINTER_Perspective_Control`.  
**Exit:** `GP_EXIT_MAIN`.  
**Routes:** `Smear_B_Exit` and the final `Main` continuation.

Use state `MISALIGNED → ALIGNING → ALIGNED`. Solve the authored projection using the frame state, `GP_Perspective_ViewReference`, targets A/B, and the 1.5° alignment tolerance. Enable the physical traversal connection only when solved. This is not a teleporter: no fade, no player teleport, and no origin-only validation. Preserve the frame pivot and verify world geometry alignment.

## System 9 — Painter interaction points

The actual Painter helpers are:

- Fold: `PAINTER_Fold_A_Control`
- Great Fold: `PAINTER_GreatFold_Control`
- Underpaint: `PAINTER_Underpaint_Control`, `PAINTER_Underpaint_A_Control`, `PAINTER_Underpaint_B_Control`
- Smear: `PAINTER_Smear_A_Control`, `PAINTER_Smear_B_Control`
- Perspective: `PAINTER_Perspective_Control`

Bind each to the existing Painted Alive interaction architecture as a `PainterWorldInteraction`. Each record has a system reference, telegraph state, activation state, and server-validated persistent state.

## System 10 — Fall, respawn, entry, exit, and route helpers

Global fall trigger: `GP_FallZone_Global`. Regional triggers: `GP_FallZone_01` through `GP_FallZone_06`. Regional respawn targets: `GP_Respawn_01` through `GP_Respawn_06`. Regions are `GP_Region_01_Stretched_Entrance`, `GP_Region_02_Backside_Works`, `GP_Region_03_Great_Fold`, `GP_Region_04_Underpainting`, `GP_Region_05_Smear_Works`, and `GP_Region_06_Perspective_Exit`.

Use `FallZone`, `RespawnVolume`, and `RespawnPoint` components. Each regional FallZone custom property points to its matching `GP_Respawn_0N`. Validate that a respawn point is outside moving fold geometry, smear sweep, active collision, and its own fall trigger. Main entry/exit are `GP_ENTRY_MAIN` and `GP_EXIT_MAIN`. Region route references are `GP_ROUTE_01` through `GP_ROUTE_06`; sampled route points are included in the binding JSON.

## Palimpsest networked state

Use the existing Painted Alive networking architecture with server-authoritative environment state and late-join synchronization for every persistent system:

| System | State enum | State data |
|---|---|---|
| Fold A | `OPEN`, `TRANSITIONING`, `FOLDED` | pivot, 65° local X target, clearance, active collider |
| Great Fold | `A`, `TRANSITIONING`, `B` | pivot, 65° local X target, lower leaf, active collider |
| Underpaint A/B | `CURRENT`, `TRANSITIONING`, `REVEALED` | +5 m reveal transform, layer visibility, active collider |
| Smear A/B | `RETRACTED`, `MOVING`, `EXTENDED` | shape-key value, helper travel, active collider |
| Perspective | `MISALIGNED`, `ALIGNING`, `ALIGNED` | frame state, target solve, bridge offset, active collider |
| Backside | `FRONT`, `BACKSIDE_OCCUPIED` | occupancy and coarse trace only |

Painter requests must be validated by the existing network authority. Late joiners must receive the current persistent state before Figure movement resumes.

## Unity implementation checklist

Reuse existing Painted Alive equivalents where they exist. Suggested components are:

- `CanvasFoldSystem.cs`
- `CanvasBacksideRegion.cs`
- `CanvasBacksideTraceSystem.cs`
- `UnderpaintingRevealSystem.cs`
- `PaintSmearSurface.cs`
- `PerspectiveFrameSystem.cs`
- `PerspectiveTraversalBridge.cs`
- `PainterWorldInteraction.cs`
- `FallZone.cs`
- `RespawnVolume.cs`
- `PalimpsestGameplayBinder.cs`
- `PalimpsestNetworkState.cs`

The machine-readable binding file is authoritative for exhaustive object names, transforms, materials, collision states, route samples, and network metadata.
