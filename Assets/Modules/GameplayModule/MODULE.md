# GameplayModule

## Purpose
Owns the Jelly Fuse board, grid-constrained dragging, permanent same-colour fusion, doors that take
exact shapes in turn, undo history and the countdown. GameplaySystemContext declares the input and timer command flows.

## Concepts
gameplay, jelly, fusion, sliding puzzle, door, queue, exit, wall, undo, timer

## Level authoring and play
CD_JellyLevels is filed once on GameplaySystemRoot (Shared Scriptables): the levels in play order,
the start level and the CD_JellyPalette every level draws its colours from. StartPuzzleCommand
builds the level at LevelIndex with that palette; empty or invalid entries are skipped (logged).
ExampleJellyLevelFactory is only a rule-check fixture.

Floria/Gameplay/Levels opens the level window: the list on the left (CD_JellyLevelsEditor), the
selected level's designer on the right (JellyLevelEditor). One toolbar - Jelly, Door, Wall, Empty,
Erase - with twelve shape presets; click to place, click to select, drag to move, right-click to
delete. LEVEL_DESIGNER.md is the designer's guide. Level snapshots are immutable: edits take
effect on Restart. The editor caches its asset lookups and level checks, redone only after an edit.

CD_JellyPalette holds the shared colours (IDs 0-2 legacy, 100+ new); a level's ExtraColours add
its own (3-99).

The board's outermost row and column are the wall round it: CreateLevel plays every one of those
cells as wall except where a door opens, painted or not, and no jelly may start there. Doors
(CD_JellyLevel.Doors) are the gaps in that wall: a side, a first cell along it (never a corner), a
width of 1-4 cells, and a queue of jellies (colour and shape, local cells). Levels made
before doors keep their old exits in a hidden Targets list; the game converts them as it loads, and
the level editor's Convert button or Floria/Gameplay/Convert Old Exits To Doors rewrites the asset.
A converted door spans every row its jellies did; old exits sharing rows on one side become one
door taking the first exit's jellies, then the next's.

Editor-only play shortcuts (#if UNITY_EDITOR, compiled out of builds): N / B next / previous
level, R restart, U undo, W win, L lose.

## Rules
- A drag moves one cardinal step at a time along the shortest free route to the target
  (breadth-first over the piece's offsets), so it goes round anything in its way and never steps
  back on itself. With no free route (the target is off the board or taken) it steps straight
  toward the target while it can, which is also how it leaves through a door. Each step is one
  frame of the turn, so the board walks the route.
- Orthogonal same-colour contact merges permanently; diagonal contact does not merge. A held jelly
  fuses only when released. While held, the snapshot's FuseReady lists the pieces it would fuse
  with - only when the fused shape still fits a jelly some door has yet to take -
  and HeldFreeSides says which way it can step, so the board never decides either.
- FreeArrowAfterFusion is authored per level. When enabled, any successful fusion involving an
  arrow-constrained result removes that result's arrow restriction; when disabled, the existing
  intersection-of-directions rule remains. Undo restores the restriction state from that turn.
- A door's cells block like a wall: nothing moves into them or routes through them. A door takes
  the jelly at the front of its queue - same colour, same shape (moved, never turned) - when it
  stands right in front of the door (or already in it) with every row (or column) inside the door,
  on an outward step. Then it
  waits for the next. The snapshot's DoorProgress, WaitingStage and QueuedAfter say where each
  door's queue stands. The level is won when no jelly is left.
- One drag is one move and one undo entry. Undo also restores door queues, but never time.
- Timeout locks play; victory can be undone. Impossible remaining shapes produce a warning.
- Connector owns all cross-module signal wiring; the timer ticks through IUpdateProvider.

## Haptic feedback
The Drag sequence captures before state, runs DragJellyCommand unchanged, then PlayPuzzleFeedbackCommand selects one generic IHapticService preset. Gameplay references the service assembly directly; the service knows nothing about the board. IsDragging only exposes the existing gesture lifetime. Timeout announces its one transition internally and binds the service Play step. Undo and Restart do not trigger feedback.

## Performance
The model reuses its route and fuse buffers and hands out the same snapshot until the state
changes; a drag step allocates only that new snapshot (~1 KB). The board allocates nothing per
frame and skips the UI entirely while nothing moves.

## Validation
JellyPuzzleRuleChecks (44 model checks, plus config checks that need the Editor) covers moves,
routing round walls, fuse on release and its hint, doors (shape, colour, width, rows), holes,
door queues, undo and timeout, then the level asset rules: door placement and width, and old-exit
conversion.

<!-- FLOWIOC:BEGIN version=1 hash=94b5dcdb | generated by Tools/FlowIoC/Module Scanner - do not edit inside this block -->
**Kind** Main · **Assemblies** Modules.Gameplay, Modules.Gameplay.Shared, Modules.Gameplay.Signals
**Root** GameplaySystemRoot → GameplaySystemContext
**Incoming** BuyLives() · Drag(Vector2Int, Vector2Int, bool) · ForceResult(PuzzleStatus) · GrantShopProduct(string) · NextLevel() · OpenLivesOffer() · Quit() · RefreshLives() · RefreshLivesOffer() · ResetBoosters() · ResetLives() · Resume() · SkipLevel(int) · Start() · Stop() · Undo() · UseBooster(BoosterType, int)
**Outgoing** BoardChanged(PuzzleTurnVO) · BoosterUsed(BoosterType, int) · BoostersChanged(IReadOnlyList<BoosterStateRVO>) · ClockFrozen(float) · CoinsChanged(int) · CoinsEarned(int) · CurrentLevelChanged(int) · InfoRequested(string) · LevelChanged(int, bool) · LivesChanged(LivesStateRVO) · LivesOfferChanged(PurchaseOfferVO) · LivesOfferFinished() · LivesOfferOpened(PurchaseOfferVO) · OutOfLives(float) · TimeChanged(float) · TutorialChanged(JellyTutorialRVO)
**Publishes** BoosterCVO, BoosterSVO, BoosterStateRVO, BoosterType, CD_Boosters, CD_JellyLevel, CD_JellyLevels, CD_JellyPalette, CD_Lives, CD_ShopProducts, GameplayOffers, GameplayScreenDevToggles, JellyAxis, JellyColour, JellyColourCVO, JellyDoorCVO, JellyGateCVO, JellyLevelCVO, JellyPaletteCVO, JellyPieceVO, JellyShapeCVO, JellySide, JellyTargetCVO, JellyTutorialRVO, JellyTutorialStepCVO, LevelDifficulty, LivesStateRVO, PuzzleSnapshotRVO, PuzzleStatus, PuzzleTurnVO, SD_JellyProgress, SettingsConstants, SettingsOption, ShopProductCVO
**Sub modules** GameplayScreenModule (Screen) · PauseScreenModule (Screen) · SettingsScreenModule (Screen)
<!-- FLOWIOC:END -->
