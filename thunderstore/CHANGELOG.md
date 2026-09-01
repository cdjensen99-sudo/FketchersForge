# Changelog

## 0.2.10

- **Fixed:** Rare tombstone crashes when dying with an equipped quiver (reported with AzuExtendedPlayerInventory). Take All could interact badly with Fletcher equip state and packed quiver contents while extended-inventory mods auto-equipped items from equipment cells.
- **Changed:** On death, equipped quivers are unequipped before items move to the tombstone. After looting the grave, the quiver stays **unequipped** until you right-click it again — the eight-slot row and HUD do not open during Take All.

## 0.2.9

- **Fixed:** Melee weapons and staffs no longer consume arrows from an equipped quiver.
- **Fixed:** When the selected quiver slot is empty, the bow or crossbow uses the next matching quiver slot instead of stopping.

## 0.2.8

- **Fixed:** Weight of the quiver and its contents now counts toward total carry weight when the quiver is unequipped but still in inventory.
- **Added:** Quiver can hold crossbow bolts (bone, iron, black metal, carapace, charred) and use them when that slot is selected.
- **Added:** Quiver appears on your back when equipped and is removed when unequipped (visual enhancement; does not use the cape slot). Toggle with `QuiverBack.ShowOnBack`.

## 0.2.7

- **Fixed:** Initial quiver creation bug. Quiver slots did not work when first created and needed a game restart. Fixed by requiring the quiver to be **equipped** (right-click / RMB in inventory) before the quiver inventory space opens.
- **Equip:** Right-click the quiver in the backpack to equip or unequip (does not use the cape/cloak slot). You can carry multiple quivers if desired; only one can be equipped at a time. Contents stay on each quiver item.
- **Slots:** HUD and inventory quiver row only appear while a quiver is equipped.

## 0.2.6

- **Quiver row** - while inventory is open, drag the left grip to move the eight slots (clicking a slot still moves items). Right-click the grip to reset.
- **Fletcher's knife** - unequipping the knife closes the bench; inventory stays open.
- **Fletcher's bench** - compact panel at normal slot, button, and text size, with Reforge and Split at the bottom. Chest extras (weight tab, Reclaim all) stay hidden on the bench.
- **Docs** - in-game screenshots of the arrowhead bag, knife, quiver, HUD, and bench.

## 0.2.5

- **Quiver unload** - stacks can be dragged back out of the quiver into the backpack. Vanilla was cancelling any drag that did not start in the backpack unless a chest was open.
- **HUD dump** - with inventory closed, hold `~` and right-click (or Shift/Ctrl-click) a quiver slot to send that stack to your inventory. Open-inventory **Ctrl-click** does the same.

## 0.2.4

- **Quiver and knife** - fixed issues with the Fletcher's knife and quiver.

## 0.2.0

- **Custom models** - arrowhead bag world drops, Fletcher's dagger mesh on the knife, and a craftable quiver.
- **Quiver** - eight slots stored on the quiver item, HUD when inventory is closed, backpack row when it is open, configurable keys.
- **Docs** - README rewritten for 0.2.0 (finished features only).

## 0.1.33

- **Fix live-world load crash** - defer shaft/knife icon mesh rigs until world load; skip ZNetView registration while building temporary icon rigs (fixes `NullReferenceException` in `ZDOMan.CreateNewZDO` during Fejd startup).
- **Store package** - no longer ships `Icons/` PNGs to players (gallery images load from GitHub; in-game art is embedded in the DLL).

## 0.1.32

- **Fletcher's knife fix** - no durability loss or combat damage; bench stays open while the knife remains equipped.
- **Store README** - GitHub and Team Extreme Discord links; gallery images load from the GitHub repo.
- **Release package** - includes `Icons/` gallery assets and listing `icon.png` for Thunderstore and Hexium.

## 0.1.31

- **Store README** - Thunderstore / Hexium listing copy with gallery image links.

## 0.1.30

- Packaging and store listing polish.

## 0.1.0

- Initial release: shafts, arrowheads, Fletcher's knife field bench (reforge / split).
