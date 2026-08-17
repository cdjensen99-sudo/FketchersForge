# Changelog

## 0.2.0

- **Custom models** — arrowhead bag world drops, Fletcher's dagger mesh on the knife, and a craftable quiver.
- **Quiver** — eight slots stored on the quiver item, HUD when inventory is closed, backpack row when it is open, configurable keys.
- **Docs** — README rewritten for 0.2.0 (finished features only).

## 0.1.76

- **Inventory row** — tighten the wooden frame around the quiver slots so the gap from slot to edge matches the backpack panel.

## 0.1.75

- **Inventory row** — add the light-brown wooden inventory frame around the quiver slots while inventory is open.

## 0.1.74

- **Inventory row** — while inventory is open, the quiver slots sit on the same brown inventory panel background as the backpack. The closed HUD stays translucent like the hotbar.

## 0.1.73

- **Inventory dock** — move the quiver row down a quarter-slot so it sits just under the backpack instead of touching it.

## 0.1.72

- **Inventory dock** — quiver slots follow the 8-column backpack (hotbar column 1 down to the last backpack row), not extra equipment slots to the right.

## 0.1.71

- **Inventory dock** — quiver slots are placed from the backpack's real bottom-left cell after the inventory UI updates, so they sit under the grid instead of remaining on the HUD.
- **Cursor key** — holding the mouse-free key no longer draws the bow or drains stamina; left-click only hits the quiver UI.

## 0.1.70

- **Inventory dock** — while inventory is open, the eight quiver slots sit under the backpack by measuring the backpack slots themselves (not clipped inside the grid).
- **Quiver keys** — all quiver binds live in the Quiver config section. Defaults: hold **~** for the mouse, **[+1**–**[+8** to pick a slot (no longer Ctrl, which crouches).

## 0.1.69

- **HUD default** — closed-inventory quiver bar sits two rows below the hotbar so it clears the AzuEPI food slots. Drag it after holding the cursor key.
- **Cursor key** — hold **~** (configurable; try **[** if that key is taken) to free the mouse, drag the bar, and click a slot to choose ammo. Release to look and aim again.
- **Inventory** — the eight slots dock under the backpack grid while inventory is open.

## 0.1.68

- **Quiver slots in inventory** — opening inventory shows the eight slots under the backpack so you can drag arrows in. The HUD bar is for when inventory is closed.
- **HUD alignment** — default row lines up with hotbar slot 1, below AzuEPI food slots. Drag the left grip to move it; right-click the grip to reset. Labels are **Ctrl 1**–**Ctrl 8** on one line.

## 0.1.67

- **Quiver ammo** — the eight HUD slots are stored on the quiver item. The bow draws from the selected slot. Ctrl+1–8 or click a slot (inventory closed) switches arrows; the knife in a slot still equips.

## 0.1.66

- **Quiver size** — ground drop is 1.25× larger (scale 1.1 → 1.375).

## 0.1.65

- **Quiver size** — doubled the ground-drop scale (0.55 → 1.1).

## 0.1.64

- **Quiver leather** — keep the quiver mesh; apply the arrowhead pouch leather material to the icon and ground drop.

## 0.1.63

- **Quiver visual** — restore the quiver prefab for the inventory icon and ground drop (no longer the arrowhead pouch mesh).

## 0.1.62

- **Quiver look** — inventory icon and ground drop use the leather pouch mesh (same bag as arrowhead drops), not the thin ranger quiver.

## 0.1.61

- **Quiver HUD** — stop the per-frame inventory-grid crash (missing UI group).
- **Quiver icon** — render the quiver mesh with a leather material so it no longer reuses the knife icon.
- **Quiver recipe** — craft at Forge 1 (4 Deer Hide, 2 Bronze, 4 Fine Wood).

## 0.1.60

- **Fletcher's quiver** — craft at Workbench 2 (4 Deer Hide, 2 Bronze, 4 Fine Wood). Carrying it unlocks eight HUD slots under the hotbar for the knife, shafts, heads, and arrows. Those slots stay visible with inventory closed and can be dragged like the hotbar when inventory is open. They are a separate panel, not a fifth backpack row.

## 0.1.59

- **Fletcher's knife drop** — keep a ground collider after the custom mesh hides the vanilla blade, so throwing it no longer sends it through the floor.

## 0.1.58

- **Fletcher's knife tooltip** — hide knockback, backstab, block, and parry lines (still a one-handed hold so the grip stays correct). Combat values on the item are zeroed as well.

## 0.1.57

- **Knife grip** — slide the mesh so the palm sits on the wood handle (was gripping the blade base / guard).

## 0.1.56

- **Knife position** — keep the 0.1.55 rotation; fix grip offset so it uses local mesh bounds (world renderer bounds on the hidden prefab had parked the dagger at the knee).

## 0.1.55

- **Knife alignment** — rotate the custom mesh so its longest axis matches vanilla `KnifeBlackMetal` `attach/mesh`, then seat the handle on that grip point.

## 0.1.54

- **Knife hold** — item type is `OneHandedWeapon` like vanilla daggers (still 0 damage). Custom mesh copies `attach/mesh` position/rotation from `KnifeBlackMetal` instead of guessed Euler angles.

## 0.1.53

- **Knife in hand** — extra 90° Z rotation; visual is shifted so the wood handle sits on the grip attach (mesh pivot was the middle of the dagger).

## 0.1.52

- **Knife grip rotation** — added 90° Y. Scale unchanged at 0.85.

## 0.1.51

- **Knife scale and grip** — scale 0.85. Rotation set to Unity FBX import (−90° X) so the blade is not forced through the hand.

## 0.1.50

- **Knife scale** — character select was still oversized at 8. Extra scale is now 1 so the held/preview mesh matches the Unity prefab size.

## 0.1.49

- **Knife scale** — 65 was far too large on the character select / world model. Reduced to 8. Hand mesh and inventory icon still share this one scale.

## 0.1.48

- **Knife size** — split FBX imported at a tiny scale; hand/inventory visual scale increased so the copper blade and wood handle are visible.

## 0.1.47

- **Knife Unity materials** — uses the rebuilt `FF_FletchersKnife` prefab (separate blade/handle meshes with copper and wood Standard materials). The mod no longer overwrites those materials at runtime.

## 0.1.46

- **Knife size and color** — larger hand scale, 90° rotation so the blade faces the camera (not a thin black line), copper tint via KnifeCopper material template.

## 0.1.45

- **Knife bundle always on** — ignores the saved `UseBundledKnifeVisual = false` config from 0.1.43; always applies `FF_FletchersKnife` when the bundle loads (kitbash only if the mesh is missing).

## 0.1.44

- **Knife AssetBundle attach fix** — custom dagger parents to the hand attach like kitbash (hides vanilla blade/handle only, not the whole item). Bundle mesh enabled by default again with scale tuning; kitbash remains fallback.

## 0.1.43

- **Knife visual restored** — AssetBundle dagger is off by default again (`UseBundledKnifeVisual = false`). The working copper kitbash blade and inventory icon return until the Unity mesh/material export is ready.

## 0.1.42

- **Knife invisible fix** — bundle mesh no longer strips vanilla renderers until the custom mesh actually draws; fixes empty material slots from the AssetBundle; falls back to the copper kitbash blade if the bundle still fails.

## 0.1.41

- **Knife visual fix** — AssetBundle dagger was oversized and white in-game. Added hand scale tuning and remaps materials from the vanilla blackmetal knife so the custom mesh renders correctly.

## 0.1.40

- **Quiver work paused** — removed the extra inventory row and any quiver item/recipe. The `FF_Quiver` mesh stays in the AssetBundle for when the item is designed.

## 0.1.39

- **Fix startup crash** — quiver inventory patches now target the Valheim `Inventory.AddItem` overloads that actually exist.

## 0.1.38

- **Quiver slots** — opening inventory adds an extra row of 8 slots. Those slots only accept the Fletcher's knife, arrowheads, shafts, and arrows (cape/cloak is unchanged; no wearable quiver item yet).

## 0.1.37

- **Custom knife mesh** — Fletcher's knife uses the `FF_FletchersKnife` AssetBundle model (kitbash fallback if the bundle is missing).
- **Fletcher's quiver** — cosmetic back-slot quiver crafted at the workbench; uses the `FF_Quiver` bundle mesh.
- **AssetBundle** — embedded bundle now ships all three prefabs: pouch, knife, and quiver.

## 0.1.36

- **Pouch drop physics** — box collider + freeze rotation + higher drag so leather-pouch arrowheads slide briefly and stop (no endless rolling).

## 0.1.35

- **Pouch drop size / stability** — larger leather pouch scale; disable visual colliders and skip ground-align that could bury the mesh; stronger drop collider so heads stop disappearing into the terrain.

## 0.1.34

- **Leather pouch arrowhead drops** — world drops use the custom `FF_HeadPouch` mesh from an embedded AssetBundle (replaces the scaled cargo crate). Falls back to the crate if the bundle is missing.

## 0.1.33

- **Fix live-world load crash** — defer shaft/knife icon mesh rigs until world load; skip ZNetView registration while building temporary icon rigs (fixes `NullReferenceException` in `ZDOMan.CreateNewZDO` during Fejd startup).
- **Store package** — no longer ships `Icons/` PNGs to players (gallery images load from GitHub; in-game art is embedded in the DLL).

## 0.1.32

- **Fletcher's knife fix** — no durability loss or combat damage; bench stays open while the knife remains equipped.
- **Store README** — GitHub and Team Extreme Discord links; gallery images load from the GitHub repo.
- **Release package** — includes `Icons/` gallery assets and listing `icon.png` for Thunderstore and Hexium.

## 0.1.31

- **Fletcher's knife no longer breaks while using the bench** — durability disabled; attack damage multiplier set to 0.
- **Bench stays open** while the knife remains equipped (closing when sheathed was too aggressive).

## 0.1.30

- **Fletcher's knife finalized** — field-tool description, 1 pierce damage, 1 durability (breaks after one swing), no skill gain on attack.
- **ChestSnap stability** — sanitize null entries in ChestSnap object arrays; suppress residual null-ref noise during world scans after legacy cleanup.

## 0.1.29

- **ChestSnap patches apply correctly** — manual Harmony registration (optional `string` parameter broke automatic patching).
- **Null ZNetView guard** — ChestSnap lambda no longer throws when phantom views remain.

## 0.1.24

- **Embedded arrowhead icons** — PNG art ships inside `FletchersForge.dll`; no required `Icons` folder for players.
- **Legacy ZDO cleanup** — broader ZDOMan scan; runs on world load, player spawn, and via `fletcher.cleanup`.
- **Deploy** — `build.ps1 -Deploy` no longer overwrites profile `Icons/` (embedded art is authoritative by default).

## 0.1.23

- Thunderstore README, icon gallery, and release metadata.
- Reforge / Split default keys set to `None` (button-only workflow).

## 0.1.1–0.1.22

- Initial v1: arrow shafts and heads, vanilla-station recipes, field Fletcher's bench, reforge / split / rehead (batch 20).
- Fletcher's knife kitbash, cargo-crate head drops, shaft mesh drops.
- Phantom bench ZDO cleanup for saves affected by early builds.
