# Jelly Fuse — Level Designer Guide

Make and edit levels without writing code.

## Editor language

The editor can be shown in Turkish: **Floria → Localization**, set *Editor language* to *Türkçe*. It
changes only your own editor. The translations are in `GameplayModule/Scriptables/Localization/CD_EditorTexts`.

## Open the editor

**Floria → Gameplay → Levels**

The window has two sides:

- **Left – the level list.** Every level in the game, in play order.
- **Right – the level designer.** The level you opened from the list.

Drag the line between them to make the list wider or narrower.

## Words you will see

| Word | Meaning |
|---|---|
| **Jelly** | A coloured piece on the board. Pieces of the same colour fuse for good when they touch side by side. |
| **Door** | An opening in the wall round the board, 1 to 4 cells wide. It shows the jelly it waits for. |
| **Queue** | The jellies a door takes, in order. When one passes, the door waits for the next. The **+N** on a door is how many more come after the one shown. |
| **Wall** | A block nothing can pass. The board's outermost cells are always wall, except where a door opens; you paint extra walls inside. |
| **Empty** | A missing floor cell. Nothing can pass it either. |

## The level list (left)

| Control | What it does |
|---|---|
| **Start level** | The level the game opens when you press Play. Use it to test a later level. |
| **Open** | Shows the level on the right. |
| **↑ ↓** | Moves the level earlier or later. The position is the level number. |
| **⧉** | Copies the level. The copy is placed right after it. |
| **✕** | Removes the level from the list. The file is **not** deleted. |
| **+ Add new level** | Creates an empty level at the end of the list. |
| **Add an existing level** | Drop a level file here to put it (back) into the list. |

A **⚠** in front of a level means it has errors. Open it and read the warnings at the bottom.

A level that is not in the list is never played.

## The level designer (right)

### 1. Level settings

Open **Level settings** at the top to set the name, the hint, the board **Width** and **Height** (3–20) and the **Time** in seconds.

**Free arrows after fusion** controls the Arrow Block sub-mechanic for this level:

- **On:** after an arrow jelly successfully fuses, the resulting jelly loses its arrow and can move in every direction.
- **Off:** fusion keeps the resulting jelly's arrow restriction, as before.

Undoing that fusion restores the original arrow state of every separated jelly.

### 2. Pick a tool

| Tool | Use |
|---|---|
| **Jelly** | Click empty floor to place a jelly. |
| **Door** | Click the board's outermost cells (not a corner) to place a door. Pick its width with **1x 2x 3x 4x**. |
| **Wall** | Drag over the board to paint walls. |
| **Empty** | Drag over the board to paint empty cells. |
| **Erase** | Click or drag to delete what is underneath. |

With any tool:

- **Click** a jelly or door to select it.
- **Drag** it to move it. Green means it fits, red means it does not.
- **Right-click** deletes.
- **Esc** clears the selection.

### 3. Colour and shape

- **Colour** row: the colour of the next thing you place.
- **Shape** row: the shape of the next thing you place. **↻** rotates it.

When something is selected, these rows change **that** jelly, or the door's selected jelly, instead.

Need more than pink, blue and yellow? Open **Level settings → Extra colours**.

### 4. Doors and their queues

Select a door. The jellies it takes are shown in order, each as a small picture.

- Click one to set its colour and shape with the rows above.
- **+ Add next jelly** adds one after the last.
- **← Earlier / Later →** changes the order. **Remove this jelly** takes it out of the queue.
- **1x 2x 3x 4x** change the door's width.

A jelly passes a door only if it has the **same colour and the same shape** (it is never turned),
and it fits through the door's width. The door shows the jelly it waits for, with **+N** for the
ones after it.

Levels made before doors show a **Convert old exits to doors** button. Press it to edit them as
doors. **Floria → Gameplay → Convert Old Exits To Doors** converts every level at once.

### 5. Check the balance

At the bottom, each colour shows how many jelly cells there are and how many cells its doors take. They must be equal: a **✓** means they are.

The warnings under it list anything else that is wrong. When the level is valid you will see *Level is valid*.

### 6. Save and test

1. Press **Save**.
2. Set **Start level** to this level's number.
3. Press **Play**.

While playing, press **Restart** in the game to load edits you made. **Ctrl+Z** undoes any edit.

## Quick recipe for a new level

1. Press **⧉** on a level that is close to what you want, or **+ Add new level**.
2. Set the size and time.
3. Place the doors and set the jellies each one takes.
4. Place the jellies.
5. Add walls and empty cells.
6. Make every colour show ✓.
7. Save, and play it.
