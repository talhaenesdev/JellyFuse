<p align="center">
  <img src="docs/images/icon.png" width="128" alt="Jelly Fuse icon">
</p>

<h1 align="center">Jelly Fuse</h1>

<p align="center">
  <b>Slide, merge and fuse colourful jellies to clear the board.</b><br>
  A mobile puzzle game made in Unity – a solo personal project, from first idea to Google Play.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.3-black?logo=unity" alt="Unity 6">
  <img src="https://img.shields.io/badge/C%23-IL2CPP-512BD4?logo=csharp" alt="C# IL2CPP">
  <img src="https://img.shields.io/badge/Platform-Android-3DDC84?logo=android&logoColor=white" alt="Android">
  <img src="https://img.shields.io/badge/Status-Closed%20testing-orange" alt="Closed testing">
</p>

<p align="center">
  <a href="https://horseman7.itch.io/jelly-fuse"><img src="https://img.shields.io/badge/Download%20APK-itch.io-FA5C5C?style=for-the-badge&logo=itchdotio&logoColor=white" alt="Download on itch.io"></a>
</p>

<p align="center">
  <img src="docs/images/banner.jpg" width="720" alt="Jelly Fuse feature graphic">
</p>

## About

Jelly Fuse is a hybrid-casual puzzle game. Every level is a board of jellies and a ring of doors, and each
door waits for a jelly of one colour and one shape. Drag jellies around the board, fuse same-coloured
pieces into bigger ones, and send each one out through the door that fits it before the clock runs out.

<p align="center">
  <img src="docs/images/menu.jpg" width="260" alt="Main menu">
  &nbsp;&nbsp;
  <img src="docs/images/gameplay.jpg" width="260" alt="Gameplay">
</p>

## Features

- **Fuse and exit puzzles:** jellies slide, merge into bigger shapes, and leave only through a door that matches their colour and shape.
- **Boosters:**
  - **Rocket** clears a door with the jellies that fit it.
  - **UFO** lifts a whole colour off the board.
  - **Freeze** stops the clock.
- **Tutorial flow:** guided first levels, plus a short lesson the first time each booster appears.
- **Lives and coins:** lives refill over time, and coins buy boosters and extra lives.
- **Player profile:** unlockable avatars and frames, plus a player name.
- **Accounts and cloud save:** Unity Gaming Services sign-in, Google Play Games linking, and progress restored on a new device.
- **Settings:** sound, music, haptics, notifications and language.

## Architecture

The project is built on **FlowIoC**, a signal-driven inversion-of-control framework for Unity.

- The game is split into **modules** (Gameplay, Profile, Purchase, Account, Cloud Save…). Each module owns its state, logic and presentation.
- Modules never reference each other. They are wired together declaratively by **Connectors**, which join one module's outgoing signals to another's incoming ones.
- Logic lives in **Commands** bound into readable sequences. **Models** own state, and **Views and Mediators** drive the UI.
- Shared services, such as screens, pooling, save, audio, haptics and assets, are reached through interfaces.

```
Signal ──▶ Command sequence ──▶ Model ──▶ Outgoing signal ──▶ Connector ──▶ another module
```

## Tech stack

| Area | Used |
|---|---|
| Engine | Unity 6 (6000.3), URP, IL2CPP, ARM64 |
| Language | C# |
| Architecture | FlowIoC – signals, commands, connectors, MVC screens |
| Content | Addressables, sprite atlases (ASTC) |
| Backend | Unity Gaming Services (Authentication, Cloud Save), Google Play Games Services |
| UI and feel | TextMeshPro, DOTween, Input System |
| Tooling | Git LFS, custom editor panels, a developer panel behind a secret gesture |

## Performance and build size

- UI art is imported at about twice its on-screen size and compressed with ASTC.
- Mipmaps are off for UI.
- Sprites are packed into atlases.
- IL2CPP code generation is set to *Optimize Size*.

Together these cut the release bundle from 73 MB to 64.5 MB.

## Running the project

1. Clone with Git LFS installed: `git lfs install`, then `git clone`.
2. Open the project in **Unity 6000.3.26f1**.
3. Open the boot scene and press Play. Unity Gaming Services sign-in needs a linked UGS project.

## Status

Jelly Fuse is in **closed testing on Google Play**, and a public release is planned. You can already
try the Android build on [itch.io](https://horseman7.itch.io/jelly-fuse).

## Author

Designed and developed by **Talha Enes İslamoğlu** as a personal project.

© 2026 Talha Enes İslamoğlu. All rights reserved.
