# Changelog

## 0.2.6

- **Quiver row** — while inventory is open, drag the left grip to move the eight slots (clicking a slot still moves items). Right-click the grip to reset.
- **Fletcher's knife** — unequipping the knife closes the bench; inventory stays open.
- **Fletcher's bench** — compact panel at normal slot, button, and text size, with Reforge and Split at the bottom. Chest extras (weight tab, Reclaim all) stay hidden on the bench.
- **Docs** — in-game screenshots of the arrowhead bag, knife, quiver, HUD, and bench.

## 0.2.5

- **Quiver unload** — stacks can be dragged back out of the quiver into the backpack. Vanilla was cancelling any drag that did not start in the backpack unless a chest was open.
- **HUD dump** — with inventory closed, hold `~` and right-click (or Shift/Ctrl-click) a quiver slot to send that stack to your inventory. Open-inventory **Ctrl-click** does the same.

## 0.2.4

- **Quiver and knife** — fixed issues with the Fletcher's knife and quiver.

## 0.2.0

- **Custom models** — arrowhead bag world drops, Fletcher's dagger mesh on the knife, and a craftable quiver.
- **Quiver** — eight slots stored on the quiver item, HUD when inventory is closed, backpack row when it is open, configurable keys.
- **Docs** — README rewritten for 0.2.0 (finished features only).

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
