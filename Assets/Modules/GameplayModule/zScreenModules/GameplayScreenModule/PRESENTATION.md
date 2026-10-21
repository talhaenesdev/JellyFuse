# Jelly Fuse — Presentation settings

The board is drawn with UI sprites inside the gameplay screen. Its settings are split by concern,
all in `Scriptables/`, and every change shows immediately in Play Mode:

| Asset | Holds |
|---|---|
| `CD_JellyFeel` | How the jellies move: dragging, lifting, springs, fuse hint, going into a door |
| `CD_JellyBoardLook` | How the board looks: view angle, jelly and floor art, shading, colours, the palette and the wall skin |
| `CD_JellyWallSkin` | The walls and doors |
| `CD_GameplayHud` | Map placement, the timer warning and the panel delays. The HUD's look is in the GameplayScreen prefab |

`CD_JellyPresentation` is what the screen holds: it only links the Feel, Look and Hud assets, and
its inspector shows all three, each under its slot. Swap one of them to try another feel or look.

## Sprites
Art lives in `Assets/Art/Textures/Gameplay/` and is packed by `Atlas_Gameplay` (ASTC 6x6 on
Android and iOS). Drop a new sprite into the `Grid` or `Jellys` folder and the atlas picks it up.

| Setting | What it does |
|---|---|
| Tile Sprite / Tile Sprite Alt | The floor. With both set they alternate light and dark. |
| Jelly Sprite | The jelly, tinted with the palette colour. |
| Jelly Sprite Is Block | On when the jelly art already has its top, side and shine painted in. |
| Jelly Sprite Offset | Nudges the jelly art on its cell, in cells. |
| Palette | The jelly colours. Each row has a name, a tint and an optional painted sprite. |

Empty sprite slots fall back to a generated rounded square.

## Walls and doors
The wall art is in `Scriptables/CD_JellyWallSkin.asset` (set as **Wall Skin** on `CD_JellyBoardLook`).
The wall sprites live in this module's `Sprites/Walls/` (`SPR_Wall_*`), copies of the art in
`Assets/Art/Textures/Gameplay/Walls/` named and imported as sprites. Drag a new sprite onto its slot
to change a piece. Every slot is optional: an empty one is drawn as a generated block in **Wall Colour**.

| Setting | What it does |
|---|---|
| Top / Bottom / Left / Right | The board's outermost row or column: the straight wall round it, one sprite per side. |
| Corner Top Left … Corner Bottom Right | The board's four corner cells. |
| Inner | Walls painted on the board, for any piece below left empty. |
| Single, End Up/Down/Left/Right, Horizontal, Vertical | A wall on the board, picked by which neighbours are walls too. |
| Corner Up Right … Corner Down Left, Tee Up/Down/Left/Right, Cross | The joins of walls on the board. |
| Door Top / Bottom / Left / Right | One sprite per door width: element 0 is the 1x door, element 3 the 4x. Empty: a generated door in the colour of the jelly it waits for. |
| Door Inset / Thickness / Overhang | Where the door sits on the wall and how big it is. The wall runs on under every door; size the door to cover it. |
| Piece Size / Piece Offset | Size and nudge of painted wall pieces and doors, in cells. |
| Frame Inset | Pulls the painted wall round the board toward the middle, in cells. The `SPR_Wall_*` art has its wall on the outer edge of the square, so near 1 sets it against the floor (0.8 leaves a little room). |
| Icon Size / Icon Offset | The jelly drawn on a door: the one it waits for. |
| Counter Colour / Badge / Size / Offset | The +N at a door's corner: how many more jellies it takes. |

A painted piece is drawn as it is, so paint its side and shine in.

## Motion
| Setting | What it does |
|---|---|
| Grid Drag | On: a held jelly moves cell by cell. Off: it glides with the finger. |
| Drag Follow | How far a gliding jelly leads its cell toward the finger. |
| Slide Speed | How fast jellies slide along their route, and back on undo. |
| Grab Lift / Lift Time | How high a held jelly rises, and how long rising takes. |
| Animate Jellies | Off: no wobble, stretch, pop, breathing or shatter. Sliding and lifting stay smooth. |

## Fuse hint
While a jelly is held, the jellies it would fuse with on release glow, pulse and rise.
**Fuse Hint Glow**, **Fuse Hint Pulse**, **Fuse Hint Speed** and **Fuse Ready Lift** tune it.

## Delivery
A jelly a door takes stops in front of the door, then slowly slides in, its back cells flowing after
its front ones, narrowing and fading under the door. It plays even with Animate Jellies off.
When a door has taken its last jelly it fades out once that jelly is in, and the wall shows.
**Door Enter Time** and **Door Enter Squeeze** tune it.
