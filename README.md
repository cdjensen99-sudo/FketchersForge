# Fletchers Forge

Disassemble vanilla arrows into **shafts** and **arrowheads**, craft components at vanilla stations, **reforge ammunition in the field** with the **Fletcher's knife**, and carry ammo in a **Fletcher's quiver**.

**Current version:** 0.2.9  
**Requires:** BepInEx + Jötunn

**Links**
- **[Team Extreme Discord](https://discord.gg/cCNG8xKXMn)** — setup help, bug reports, and updates
- **[GitHub — Fletchers Forge](https://github.com/cdjensen99-sudo/FketchersForge)** — source code and issues

---

## Screenshots

![Fletcher's knife and quiver in inventory](https://raw.githubusercontent.com/cdjensen99-sudo/FketchersForge/main/Images/FF-Items.png)

![Arrowhead bag world drop](https://raw.githubusercontent.com/cdjensen99-sudo/FketchersForge/main/Images/FF-Arrowheadbag.png)

![Quiver slots while inventory is open](https://raw.githubusercontent.com/cdjensen99-sudo/FketchersForge/main/Images/FF-Quiver.png)

![Quiver HUD while inventory is closed (left grip to move)](https://raw.githubusercontent.com/cdjensen99-sudo/FketchersForge/main/Images/FF-QuiverHotbar.png)

![Fletcher's bench](https://raw.githubusercontent.com/cdjensen99-sudo/FketchersForge/main/Images/FF-FletchersBench.png)

---

## What this mod does

| You can… | Where |
|----------|--------|
| Craft arrow **shafts** and **heads** (batches of 20) | Workbench / Forge / Black forge |
| **Split** arrows into shaft + head | Field — Fletcher's bench |
| **Reforge** shaft + head into arrows | Field — Fletcher's bench |
| **Rehead** arrows (swap head type) | Field — Fletcher's bench |
| Keep several ammo types in a **quiver** and pick which the bow/crossbow uses | Fletcher's quiver |
| Store **crossbow bolts** in the quiver (as well as arrows) | Fletcher's quiver |
| See the quiver on your **back** while it is equipped | Cosmetic only |
| Carry finished ammo as normal **vanilla arrows / bolts** | Inventory / bow / crossbow |

There is **no material salvage** beyond shafts and heads (no wood/ore back from splitting).

---

## Custom models

This release adds custom 3D models:

| Model | In game |
|-------|---------|
| **Arrowhead bag** | Dropped arrowheads appear as a leather pouch, not a crate. |
| **Fletcher's dagger** | The Fletcher's knife uses this dagger mesh when held or dropped. |
| **Quiver** | The Fletcher's quiver uses this model when held, dropped, or shown on the back while equipped. |

---

## Quick start

1. Install **BepInEx** and **Jötunn**.
2. Install **Fletchers Forge** into `BepInEx/plugins/Hardwire99-FletchersForge/`.
3. Craft a **Fletcher's knife** at the **Forge** (level 1).
4. Hold the knife in your **hand** — the **Fletcher's bench** opens automatically.
5. Use **Reforge** or **Split**. Results go **directly into your inventory**.
6. Craft a **Fletcher's quiver**, **right-click** it in inventory to equip, then drag arrows or crossbow bolts into its eight slots.

---

## Fletcher's knife

| | |
|--|--|
| **Recipe** | Forge level 1 — Fine wood ×1, Copper ×1, Leather scraps ×1 |
| **Look** | Custom dagger model |
| **Role** | Opens the field bench; used for reforge / split |
| **Combat** | No damage; does not wear out from normal use |
| **Bench** | Auto-opens when the knife is **in your hand**; stays open while equipped |

---

## Fletcher's quiver

Craft a **Fletcher's quiver** and **right-click** it in your inventory to equip or unequip. It does **not** use the cape/cloak slot. While equipped it unlocks the eight fletcher slots and shows a **cosmetic quiver on your back**. You can carry **multiple quivers**; only **one** can be equipped at a time (contents stay on each quiver).

| | |
|--|--|
| **Recipe** | Forge level 1 — Deer hide ×4, Bronze ×2, Fine wood ×4 |
| **Equip** | Right-click the quiver in the backpack (toggle) |
| **Slots** | **8**, stored **on the quiver item** (they travel with that quiver) |
| **Accepts** | Vanilla arrows, crossbow bolts, shafts, arrowheads, and the Fletcher's knife |

### Loading the slots

With the quiver **equipped**, open your **inventory**. The eight quiver slots sit **under the backpack**. Drag arrows (or other accepted items) in and out like any other inventory. **Ctrl-click** a quiver slot to send that stack straight to the backpack.

### Choosing ammo

The bow or crossbow shoots from the **selected** quiver slot.

- If that slot has matching ammo (arrows for bows, bolts for crossbows), those are used.
- If it is empty or the wrong ammo type, the next matching quiver slot is used (and becomes selected).
- If no quiver slot matches, the weapon uses ammo from your **normal inventory** as usual.
- Melee weapons and staffs do **not** use quiver ammo.

**While inventory is closed**, the quiver shows as a HUD bar under the hotbar.

| How | Default |
|-----|---------|
| Hold **[+1** … **[+8** | Hold **[** and press **1**–**8** |
| Click a HUD slot | Hold **~** (the key left of 1) to free the mouse, then click |
| Unload a HUD slot | Hold **~**, then **right-click** (or Shift/Ctrl-click) to dump that stack into the backpack |

Selecting a slot that holds the **Fletcher's knife** equips the knife.

### Moving the HUD bar

The closed-inventory quiver bar can be moved.

1. Hold **~** to free the mouse.
2. Drag the **left grip** (the small handle on the left of the bar) to a new spot. The position is saved.
3. **Right-click** that grip (still holding **~**) to put the bar back in the default place.

### Moving the inventory row

While inventory is open, the quiver row starts under the backpack. Drag the **left grip** to move it (clicking a slot still moves items). That spot is saved until you drag it again. **Right-click** the grip to put it back under the backpack.

---

## Arrow components

### Shafts (inventory stack **100**)

| Item | Craft station | Notes |
|------|---------------|--------|
| Arrow shaft | Workbench 1 | Wood ×8, Feathers ×2 → **20** shafts |
| Needle arrow shaft | Workbench 4 | Feathers ×2 → **20** shafts |
| Ashwood arrow shaft | Workbench 3 | Black wood ×8, Feathers ×2 → **20** shafts |

### Arrowheads (inventory stack **200**)

Craft output is always **20** per recipe batch. Dropped heads use the **arrowhead bag** model.

| Head | Station | Materials |
|------|---------|-----------|
| Fire | Workbench 2 | Resin ×8 |
| Flint | Workbench 2 | Flint ×2 |
| Bronze | Forge 1 | Bronze ×1 |
| Iron | Forge 2 | Iron ×1 |
| Silver | Forge 3 | Silver ×1 |
| Obsidian | Workbench 3 | Obsidian ×4 |
| Poison | Workbench 3 | Obsidian ×4, Ooze ×2 |
| Frost | Workbench 4 | Obsidian ×4, Freeze gland ×1 |
| Needle | Workbench 4 | Needle ×4 |
| Carapace | Black forge 1 | Carapace ×4 |
| Charred | Black forge 3 | Charred bone ×4 |

---

## Field bench (Fletcher's bench)

Opens automatically when the **Fletcher's knife** is in your hand.

### Slots

| Slot | Accepts |
|------|---------|
| **Left** | Arrow **shaft** or full **arrow** |
| **Right** | **Arrowhead** only |

### Actions

Results go into your **inventory** immediately.

| Action | Button | Default key | Effect |
|--------|--------|-------------|--------|
| **Reforge** | Reforge | *(none — use the button)* | Shaft + head → **20** arrows; or arrow + new head → rehead |
| **Split** | Split | *(none — use the button)* | **20** arrows → shaft + head (wood arrows → shaft only) |

---

## Vanilla arrow mapping

| Vanilla arrow | Shaft | Head |
|---------------|-------|------|
| Wood | Standard | *(none — split gives shaft only)* |
| Fire | Standard | Fire |
| Flint | Standard | Flint |
| Bronze | Standard | Bronze |
| Iron | Standard | Iron |
| Silver | Standard | Silver |
| Obsidian | Standard | Obsidian |
| Poison | Standard | Poison |
| Frost | Standard | Frost |
| Needle | Needle | Needle |
| Carapace | Standard | Carapace |
| Charred | Ashwood | Charred |

---

## Configuration

File: `BepInEx/config/hardwire99.fletchersforge.cfg`

Restart Valheim (or reload the profile) after changing keys.

| Section | Key | Default | Description |
|---------|-----|---------|-------------|
| **General** | `Enabled` | `true` | Turn the entire mod off without uninstalling. |
| **Controls** | `Reforge` | `None` | Reforge hotkey while the bench is open (`None` = button only). |
| **Controls** | `Split` | `None` | Split hotkey while the bench is open (`None` = button only). |
| **Quiver** | `CursorKey` | `BackQuote` (**~**) | Hold to free the mouse: click HUD slots and drag the bar. |
| **Quiver** | `SelectModifier` | `LeftBracket` (**[**) | Hold with a slot key to choose ammo. `None` disables keyboard select (click still works). |
| **Quiver** | `Slot1` … `Slot8` | `Alpha1` … `Alpha8` | Keys used with SelectModifier for each quiver slot. |
| **Quiver** | `HudOffsetY` | `0` | Extra vertical shift for the **default** HUD spot. Negative moves it down. Ignored after you drag the bar. |

There is **no key to open the bench** — it opens when the knife is **in your hand**.

HUD position after you drag the bar is saved in the same Quiver section. Right-click the left grip while holding the cursor key to restore the default spot. The open-inventory row has its own saved position after you drag its grip.

---

## Credits

- **Author:** Hardwire99  
- **Framework:** Jötunn / Valheim modding community
