# OmniDown icon assets

This project is packaged as a WinUI/MSIX app. Windows does not use one icon file everywhere; it picks different assets for the app list, pinned Start items, splash screen, Store metadata, and executable icon.

## Current storage

The app manifest is `OmniDown/Package.appxmanifest`.

Important manifest entries:

- `Properties/Logo`: `Assets\StoreLogo.png`
- `uap:VisualElements/@Square44x44Logo`: `Assets\Square44x44Logo.png`
- `uap:VisualElements/@Square150x150Logo`: `Assets\Square150x150Logo.png`
- `uap:VisualElements/@BackgroundColor`: `transparent`
- `uap:DefaultTile/@Wide310x150Logo`: `Assets\Wide310x150Logo.png`
- `uap:SplashScreen/@Image`: `Assets\SplashScreen.png`

The checked-in files use Windows scale and target-size naming, so the manifest path `Assets\Square44x44Logo.png` resolves to `Assets\Square44x44Logo.scale-200.png` when Windows needs the 200% asset.

Current asset files:

| File | Size | Main use |
| --- | ---: | --- |
| `OmniDown/Assets/Square44x44Logo.scale-200.png` | 88 x 88 | app list, small tile, task surfaces at 200% scale |
| `OmniDown/Assets/Square44x44Logo.targetsize-16_altform-unplated.png` | 16 x 16 | unplated small icon variant |
| `OmniDown/Assets/Square44x44Logo.targetsize-24_altform-unplated.png` | 24 x 24 | unplated small icon variant |
| `OmniDown/Assets/Square44x44Logo.targetsize-32_altform-unplated.png` | 32 x 32 | unplated small icon variant |
| `OmniDown/Assets/Square44x44Logo.targetsize-48_altform-unplated.png` | 48 x 48 | unplated small icon variant |
| `OmniDown/Assets/Square44x44Logo.targetsize-256_altform-unplated.png` | 256 x 256 | unplated large icon variant |
| `OmniDown/Assets/Square150x150Logo.scale-200.png` | 300 x 300 | pinned Start tile at 200% scale |
| `OmniDown/Assets/Wide310x150Logo.scale-200.png` | 620 x 300 | wide Start tile at 200% scale |
| `OmniDown/Assets/StoreLogo.png` | 50 x 50 | package/store metadata |
| `OmniDown/Assets/SplashScreen.scale-200.png` | 1240 x 600 | splash screen |
| `OmniDown/Assets/LockScreenLogo.scale-200.png` | 48 x 48 | lock-screen notification logo, if used |
| `OmniDown/Assets/OmniDown.ico` | ICO | classic executable icon |

## Why a pinned Start icon can show a dark border

Pinned Start items are rendered like tiles/plates, while the app list usually uses an unplated small icon. That means the two places can use different image files and different Windows rendering rules.

In this project the visual element background is set to `transparent`. The PNG assets also have transparent corners. On pinned Start items, Windows may draw a plate behind the icon, and a transparent or dark plate can look like a black square/border. The app list can look clean because it is more likely to use the unplated 24 px target-size icon.

To reduce this effect when replacing the icon:

- Provide unplated target-size assets for the icon sizes Windows may pick, not only `targetsize-24`.
- Keep transparent padding consistent and avoid semi-transparent dark pixels around the artwork.
- If you want a plated tile, set `BackgroundColor` to an intentional brand color instead of `transparent`.
- If you want the icon to visually blend into Start, prefer `*_altform-unplated.png` target-size assets for common sizes.

## Replacement checklist

1. Start from a source icon you own, preferably SVG or a high-resolution transparent PNG.
2. Export PNGs with transparent background for these sizes:
   - `Square44x44Logo.scale-200.png`: 88 x 88
   - `Square150x150Logo.scale-200.png`: 300 x 300
   - `Wide310x150Logo.scale-200.png`: 620 x 300
   - `StoreLogo.png`: 50 x 50
   - `SplashScreen.scale-200.png`: 1240 x 600
   - `LockScreenLogo.scale-200.png`: 48 x 48
3. Also export unplated target-size icons for Start/task surfaces. Current minimum set:
   - `Square44x44Logo.targetsize-16_altform-unplated.png`
   - `Square44x44Logo.targetsize-24_altform-unplated.png`
   - `Square44x44Logo.targetsize-32_altform-unplated.png`
   - `Square44x44Logo.targetsize-48_altform-unplated.png`
   - `Square44x44Logo.targetsize-256_altform-unplated.png`
4. Replace `OmniDown/Assets/OmniDown.ico` with a multi-size ICO containing at least 16, 24, 32, 48, and 256 px images.
5. Rebuild/repackage the app.
6. If Start still shows the old icon, uninstall the previous package or clear the pinned item and pin it again. Windows caches app icons aggressively.

## Optional manifest tweak

If the pinned tile should have a deliberate background instead of a transparent/dark plate, change this in `OmniDown/Package.appxmanifest`:

```xml
BackgroundColor="#202020"
```

Use a real brand color once the final icon exists.
