# Minimal Unity UI asset pack

This deliberately small pack covers the menu and HUD icons required for the kitchen-game test. It avoids paid packs and third-party attribution requirements.

## Contents

| File | Intended UI use | Official source |
| --- | --- | --- |
| `icons/play.svg` | Start / resume | https://github.com/google/material-design-icons/blob/master/src/av/play_arrow/materialicons/24px.svg |
| `icons/pause.svg` | Pause button | https://github.com/google/material-design-icons/blob/master/src/av/pause/materialicons/24px.svg |
| `icons/restart.svg` | Restart round | https://github.com/google/material-design-icons/blob/master/src/av/replay/materialicons/24px.svg |
| `icons/quit.svg` | Quit / return to title | https://github.com/google/material-design-icons/blob/master/src/action/exit_to_app/materialicons/24px.svg |
| `icons/timer.svg` | Round timer HUD | https://github.com/google/material-design-icons/blob/master/src/image/timer/materialicons/24px.svg |
| `icons/score.svg` | Score / high-score HUD | https://github.com/google/material-design-icons/blob/master/src/toggle/star/materialicons/24px.svg |

## Licence and use

All six icons are Google Material Icons and are licensed under Apache License 2.0. The full licence text is included as `LICENSE-APACHE-2.0.txt`. Attribution is appreciated by Google but is not required; retain this folder and licence with the project submission.

The originals use a 24 × 24 viewBox and inherit a black fill. In Unity, import SVGs through the **Vector Graphics** package, or rasterize them to PNG at the target UI size. Tint the resulting UI Image in the editor; no separate colour variants are needed.

## Intentionally not included

- **Font:** use Unity TextMeshPro's bundled default font for this assessment. It is sufficient for functional UI and avoids a needless external font dependency.
- **Panels, progress bars, order cards, and score popups:** make these from standard Unity UI Image/TextMeshPro components using rounded rectangles and colour. No image assets are required.
- **Audio and decorative art:** not required by the test brief.
