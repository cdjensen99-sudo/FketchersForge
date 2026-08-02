# Changelog

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
