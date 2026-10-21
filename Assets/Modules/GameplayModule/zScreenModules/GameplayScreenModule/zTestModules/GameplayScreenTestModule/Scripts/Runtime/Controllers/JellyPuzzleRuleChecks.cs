#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Controller;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Data.ValueObjects;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.GameplayScreenModule.GameplayScreenTestModule.Controllers
{
    public sealed class VerifyJellyRulesCommand : Command
    {
        public override void Execute() => FlowLogger.Log(new JellyPuzzleRuleChecks().Run());
    }

    // Runs in the module's playable test scene, without a second test assembly or live game state.
    public sealed class JellyPuzzleRuleChecks
    {
        private int _checks;
        public string Run()
        {
            _checks = 0;
            RunModelChecks();
            RunLevelChecks();
            return "Jelly Fuse: " + _checks + " rule checks passed.";
        }

        // ---------- the rules, on level snapshots built in code ----------

        private void RunModelChecks()
        {
            var model = new JellyPuzzleModel();
            model.Start(new ExampleJellyLevelFactory().Create());
            model.Drop(new Vector2Int(1, 1), new Vector2Int(1, 5));
            Check(model.Snapshot().Moves == 0 && !model.Snapshot().CanUndo, "Wall rejects drop without history");
            model.Drop(new Vector2Int(1, 1), new Vector2Int(-1, 1));
            Check(model.Snapshot().Moves == 0, "Outside rejects drop");
            model.Drop(new Vector2Int(1, 1), new Vector2Int(4, 4));
            Check(model.Snapshot().Pieces.Count == 4 && model.Snapshot().Moves == 0, "Same colour overlap rejected");
            model.Drop(new Vector2Int(1, 1), new Vector2Int(2, 0));
            Check(model.Snapshot().Moves == 0, "Other colour overlap rejected");
            model.Drop(new Vector2Int(1, 1), new Vector2Int(1, 1));
            Check(model.Snapshot().Moves == 0, "Tap does not spend move");
            model.Drop(new Vector2Int(1, 1), new Vector2Int(3, 4));
            Check(model.Snapshot().Pieces.Count == 3 && model.Snapshot().Pieces.Single(p => p.Id == 1).Cells.Count == 3,
                "Diagonal drag over obstacles merges only at destination");
            var before = model.Snapshot();
            model.Tick(10);
            model.Drag(new Vector2Int(3, 4), new Vector2Int(7, 4), true);
            Check(model.Snapshot().Exited == 1 && model.Snapshot().DoorProgress[0] == 1, "The exact L leaves through its door");
            Check(model.Undo() && model.Snapshot().Pieces.Count == 3 && model.Snapshot().Exited == 0 && model.Snapshot().DoorProgress[0] == 0, "Undo brings the jelly back and reopens the door");
            Check(model.RemainingSeconds == 110, "Undo does not restore time");
            Check(before.Pieces.Single(p => p.Id == 1).Cells.Contains(new Vector2Int(3, 4)), "Snapshots immutable");
            model.Drag(new Vector2Int(3, 4), new Vector2Int(7, 4), true);
            // The blue square fuses one free cell from its door, so letting go draws it in: three moves win.
            model.Drag(new Vector2Int(5, 0), new Vector2Int(3, 0), true);
            Check(model.Status == PuzzleStatus.Won && model.Snapshot().Moves == 3, "A jelly fused in front of its door is drawn in, and the example wins in three moves");
            model.Tick(999);
            Check(model.RemainingSeconds == 110, "Win freezes clock");
            Check(model.Undo() && model.IsTicking, "Undo win reopens board");
            model.Tick(110);
            int moves = model.Snapshot().Moves;
            model.Drop(new Vector2Int(2, 0), new Vector2Int(1, 0));
            Check(model.Status == PuzzleStatus.Lost && model.Snapshot().Moves == moves && !model.Undo(), "Timeout blocks move and undo");
            model.Start(new ExampleJellyLevelFactory().Create());
            Check(model.RemainingSeconds == 120 && !model.Snapshot().CanUndo, "Restart resets");
            model.Start(Level(new[] { Piece(1, JellyColour.Pink, 0, 0), Piece(2, JellyColour.Pink, 2, 2) }));
            model.Drop(Vector2Int.zero, new Vector2Int(1, 1));
            Check(model.Snapshot().Pieces.Count == 2, "Diagonal contact does not fuse");
            model.Drop(new Vector2Int(1, 1), new Vector2Int(5, 5));
            Check(model.Snapshot().Pieces.Count == 2, "Passing over another piece does not fuse");
            model.Start(Level(new[] { Piece(1, JellyColour.Pink, 0, 0), Piece(2, JellyColour.Pink, 2, 2), Piece(3, JellyColour.Pink, 3, 2) }));
            model.Drop(Vector2Int.zero, new Vector2Int(1, 2));
            Check(model.Snapshot().Pieces.Count == 1 && model.Snapshot().Pieces[0].Cells.Count == 3, "Transitive fusion");
            Check(model.Undo() && model.Snapshot().Pieces.Count == 3, "Undo restores fusion");
            var wide = new JellyPieceVO(1, JellyColour.Pink, new[] { new Vector2Int(1, 1), new Vector2Int(2, 1) });
            model.Start(Level(new[] { wide }, new[] { new Vector2Int(3, 3) }));
            model.Drop(new Vector2Int(1, 1), new Vector2Int(2, 3));
            Check(model.Snapshot().Moves == 0, "Every cell participates in collision");
            model.Drop(new Vector2Int(1, 1), new Vector2Int(2, 1));
            Check(model.Snapshot().Moves == 1, "Own original cells may overlap destination");

            // A door on the right, two rows high, waiting for a pink L.
            var door = Door(JellySide.Right, 1, 2, Shape(JellyColour.Pink, (0, 0), (1, 0), (1, 1)));
            var turned = new JellyPieceVO(1, JellyColour.Pink, new[] { new Vector2Int(3, 1), new Vector2Int(3, 2), new Vector2Int(4, 1) });
            model.Start(Level(new[] { turned }, gates: new[] { door }));
            model.Drag(new Vector2Int(3, 1), new Vector2Int(8, 1), true);
            Check(model.Snapshot().Exited == 0 && model.Snapshot().Warning.Length > 0, "No automatic rotation");
            model.Start(Level(new[] { Piece(1, JellyColour.Pink, 0, 1) }, gates: new[] { door }));
            model.Drag(new Vector2Int(0, 1), new Vector2Int(8, 1), true);
            Check(model.Snapshot().Exited == 0, "Part of the shape cannot leave");
            var blue = new JellyPieceVO(1, JellyColour.Blue, new[] { new Vector2Int(2, 1), new Vector2Int(3, 1), new Vector2Int(3, 2) });
            model.Start(Level(new[] { blue }, gates: new[] { door }));
            model.Drag(new Vector2Int(2, 1), new Vector2Int(8, 1), true);
            Check(model.Snapshot().Exited == 0, "Wrong colour cannot leave");
            var tall = new JellyPieceVO(1, JellyColour.Pink, new[] { new Vector2Int(4, 1), new Vector2Int(4, 2) });
            model.Start(Level(new[] { tall }, gates: new[] { Door(JellySide.Right, 1, 1, Shape(JellyColour.Pink, (0, 0), (0, 1))) }));
            model.Drag(new Vector2Int(4, 1), new Vector2Int(8, 1), true);
            Check(model.Snapshot().Exited == 0 && model.Snapshot().Pieces[0].Cells[0].x == 5, "A jelly wider than its door cannot pass, and the door blocks it like a wall");
            model.Start(Level(new[] { Piece(1, JellyColour.Pink, 4, 3) }, gates: new[] { Door(JellySide.Right, 1, 1, Shape(JellyColour.Pink, (0, 0))) }));
            model.Drag(new Vector2Int(4, 3), new Vector2Int(8, 3), true);
            Check(model.Snapshot().Exited == 0, "Only the rows a door covers lead out through it");

            model.Start(new ExampleJellyLevelFactory().Create());
            model.Drag(new Vector2Int(1, 1), new Vector2Int(1, 1), false);
            var detour = model.Drag(new Vector2Int(1, 1), new Vector2Int(1, 6), false);
            Check(model.Snapshot().Pieces.Single(p => p.Id == 1).Cells[0] == new Vector2Int(1, 6)
                && detour.Frames.All(f => !f.Pieces.Single(p => p.Id == 1).Cells.Contains(new Vector2Int(1, 5))), "Drag goes round a wall, never through it");
            Check(detour.Frames.Count > 1, "A detour reports every step so the board can walk it");
            model.Drag(new Vector2Int(1, 1), new Vector2Int(3, 4), false);
            Check(model.Snapshot().Pieces.Single(p => p.Id == 1).Cells.Count == 1 && model.Snapshot().FuseReady.SequenceEqual(new[] { 2 }),
                "A held jelly does not fuse yet, and hints the fuse it would make");
            model.Drag(new Vector2Int(1, 1), new Vector2Int(5, 6), true);
            Check(model.Snapshot().Pieces.Count == 4 && model.Snapshot().Exited == 0 && model.Snapshot().Moves == 1,
                "Passing a jelly of its own colour without releasing beside it does not fuse");
            Check(model.Undo() && model.Snapshot().Pieces.Count == 4 && model.Snapshot().Moves == 0, "Undo restores entire drag");
            // The L it fuses into sits one free cell from its door, so letting go also draws it in.
            var fused = model.Drag(new Vector2Int(1, 1), new Vector2Int(3, 4), true);
            Check(model.Snapshot().Exited == 1 && model.Snapshot().Moves == 1, "Release fuses and draws the fused shape into its door, as one move");
            Check(fused.Frames.Any(f => f.Pieces.Any(p => p.Id == 1 && p.Cells.Count == 3)), "The fused shape is seen whole before the door takes it");
            Check(model.Undo() && model.Snapshot().Pieces.Count == 4 && model.Snapshot().Moves == 0, "Undo brings back both jellies");
            model.Drag(new Vector2Int(1, 1), new Vector2Int(2, 1), false);
            Check(model.Snapshot().Moves == 0, "Other colour blocks live drag");
            model.Drag(new Vector2Int(1, 1), new Vector2Int(2, 1), true);
            Check(!model.Snapshot().CanUndo, "Blocked drag creates no history");
            model.Drag(new Vector2Int(1, 1), new Vector2Int(3, 4), true);
            model.Drag(new Vector2Int(3, 4), new Vector2Int(7, 4), true);
            model.Drag(new Vector2Int(5, 0), new Vector2Int(3, 0), true);
            model.Drag(new Vector2Int(2, 0), new Vector2Int(-1, 0), true);
            Check(model.Status == PuzzleStatus.Won && model.Snapshot().Pieces.Count == 0, "Fused wide piece keeps moving on grid");
            model.Start(Level(new[] { wide }, new[] { new Vector2Int(2, 3) }));
            var lane = model.Drag(new Vector2Int(1, 1), new Vector2Int(1, 6), true);
            Check(model.Snapshot().Pieces[0].Cells.SequenceEqual(new[] { new Vector2Int(1, 6), new Vector2Int(2, 6) })
                && lane.Frames.All(f => !f.Pieces[0].Cells.Contains(new Vector2Int(2, 3))), "A full shape goes round a narrow blocked lane");
            var line = Door(JellySide.Right, 1, 1, Shape(JellyColour.Pink, (0, 0), (1, 0)));
            model.Start(Level(new[] { Piece(1, JellyColour.Pink, 0, 0), Piece(2, JellyColour.Pink, 2, 1) }, gates: new[] { line }));
            model.Drag(Vector2Int.zero, new Vector2Int(2, 0), false);
            Check(model.Snapshot().FuseReady.Count == 0, "A fuse that would spoil the shape is not hinted");
            model.Drag(Vector2Int.zero, new Vector2Int(1, 1), false);
            Check(model.Snapshot().FuseReady.SequenceEqual(new[] { 2 }), "A fuse that fits a door's jelly is hinted");
            model.Start(new ExampleJellyLevelFactory().Create());
            model.Drag(new Vector2Int(1, 1), new Vector2Int(1, 3), false);
            model.Tick(120);
            Check(model.Status == PuzzleStatus.Lost && model.Snapshot().Moves == 1, "Timeout finalizes active drag");

            var single = new JellyGateCVO(JellySide.Right, 1, 1, 3, 3, new[] { Shape(JellyColour.Pink, (0, 0)) });
            model.Start(new JellyLevelCVO(3, 3, 60, Array.Empty<Vector2Int>(), new[] { Piece(1, JellyColour.Pink, 0, 1) }, new[] { single }));
            model.Drag(new Vector2Int(0, 1), new Vector2Int(1, 1), true);
            Check(model.Status == PuzzleStatus.Won && model.Snapshot().DoorProgress[0] == 1 && model.Snapshot().WaitingStage(0) == null, "Let go of in front of its door, a jelly is drawn in, once");
            Check(model.Undo() && model.Snapshot().DoorProgress[0] == 0 && model.Snapshot().Pieces.Count == 1, "Undo brings back the delivered jelly");
            model.Start(new JellyLevelCVO(3, 3, 60, Array.Empty<Vector2Int>(), new[] { Piece(1, JellyColour.Pink, 0, 1) }, new[] { single }));
            model.Drag(new Vector2Int(0, 1), new Vector2Int(2, 1), true);
            Check(model.Status == PuzzleStatus.Won && model.Snapshot().DoorProgress[0] == 1, "An outward step leaves through the door");
            // Each door draws a jelly in from as far as its own PullReach.
            JellyLevelCVO Far(int reach) => new JellyLevelCVO(5, 3, 60, Array.Empty<Vector2Int>(), new[] { Piece(1, JellyColour.Pink, 0, 1) },
                new[] { new JellyGateCVO(JellySide.Right, 1, 1, 5, 3, new[] { Shape(JellyColour.Pink, (0, 0)) }, "Door", false, reach) });
            model.Start(Far(2));
            model.Drag(new Vector2Int(0, 1), new Vector2Int(0, 1), true);
            Check(model.Snapshot().Exited == 0, "A jelly further from its door than the door's pull reach is not drawn in");
            model.Start(Far(3));
            model.Drag(new Vector2Int(0, 1), new Vector2Int(0, 1), true);
            Check(model.Snapshot().Exited == 1, "Within the door's pull reach it is");
            model.Start(Far(-1));
            model.DefaultPullReach = 3;
            model.Drag(new Vector2Int(0, 1), new Vector2Int(0, 1), true);
            Check(model.Snapshot().Exited == 1, "A door with no pull reach of its own uses the default");
            model.Start(Far(-1));
            model.DefaultPullReach = 0;
            model.Drag(new Vector2Int(0, 1), new Vector2Int(1, 1), true);
            Check(model.Snapshot().Exited == 0, "A default of 0 draws nothing in");
            model.DefaultPullReach = 1;
            model.Start(new JellyLevelCVO(3, 3, 60, Array.Empty<Vector2Int>(), new[] { Piece(1, JellyColour.Blue, 1, 1) }, new[] { single }));
            model.Drag(new Vector2Int(1, 1), new Vector2Int(3, 1), true);
            Check(model.Snapshot().Moves == 0 && model.Snapshot().Exited == 0 && model.Snapshot().Pieces[0].Cells[0] == new Vector2Int(1, 1),
                "A wrong jelly cannot enter a door: it stops in front of it as at a wall");
            model.Start(new JellyLevelCVO(3, 3, 60, Array.Empty<Vector2Int>(), new[] { Piece(1, JellyColour.Blue, 0, 1) }, new[] { single }));
            model.Drag(new Vector2Int(0, 1), new Vector2Int(2, 2), true);
            Check(model.Snapshot().Pieces[0].Cells.All(c => c != new Vector2Int(2, 1)), "A drag never routes through a door");
            model.Start(new JellyLevelCVO(3, 3, 60, Array.Empty<Vector2Int>(), new[] { Piece(1, JellyColour.Pink, 0, 1) }, new[] { single }, new[] { new Vector2Int(1, 1) }));
            model.Drag(new Vector2Int(0, 1), new Vector2Int(3, 1), true);
            Check(model.Snapshot().Moves == 0, "Holes block movement rather than being traversable empty cells");

            // Frozen: a door that takes nothing and a jelly that cannot move, until jellies go through doors.
            var icy = new JellyLevelCVO(6, 4, 60, Array.Empty<Vector2Int>(), new[]
            {
                Piece(1, JellyColour.Pink, 4, 1), Piece(2, JellyColour.Blue, 1, 1), Piece(3, JellyColour.Yellow, 2, 2),
            }, new[]
            {
                new JellyGateCVO(JellySide.Right, 1, 1, 6, 4, new[] { Shape(JellyColour.Pink, (0, 0)) }, "Frozen", false, -1, 1),
                new JellyGateCVO(JellySide.Left, 1, 1, 6, 4, new[] { Shape(JellyColour.Blue, (0, 0)) }),
            }, null, "Icy", "", null, new Dictionary<int, int> { { 3, 1 } });
            model.Start(icy);
            Check(model.Snapshot().DoorFrozen[0] == 1 && model.Snapshot().JellyFrozen(3) == 1, "A level starts with its frozen doors and jellies");
            model.Drag(new Vector2Int(4, 1), new Vector2Int(6, 1), true);
            Check(model.Snapshot().Exited == 0, "A frozen door takes nothing");
            model.Drag(new Vector2Int(2, 2), new Vector2Int(3, 2), true);
            Check(model.Snapshot().Pieces.Single(p => p.Id == 3).Cells[0] == new Vector2Int(2, 2), "A frozen jelly cannot be moved");
            int beforeThaw = model.Snapshot().Moves;
            model.Drag(new Vector2Int(1, 1), new Vector2Int(-1, 1), true);
            Check(model.Snapshot().Exited == 1 && model.Snapshot().DoorFrozen[0] == 0 && model.Snapshot().JellyFrozen(3) == 0,
                "A jelly through another door thaws every frozen door and jelly by one");
            Check(model.Undo() && model.Snapshot().DoorFrozen[0] == 1 && model.Snapshot().JellyFrozen(3) == 1 && model.Snapshot().Moves == beforeThaw, "Undo freezes them again");
            model.Drag(new Vector2Int(1, 1), new Vector2Int(-1, 1), true);
            model.Drag(new Vector2Int(4, 1), new Vector2Int(6, 1), true);
            Check(model.Snapshot().Exited == 2, "A thawed door takes its jelly");
            model.Drag(new Vector2Int(2, 2), new Vector2Int(3, 2), true);
            Check(model.Snapshot().Pieces.Single(p => p.Id == 3).Cells[0] == new Vector2Int(3, 2), "A thawed jelly moves");

            // Arrow blocks: a jelly that moves only one way.
            JellyLevelCVO Arrows(JellyPieceVO[] pieces, Dictionary<int, JellyAxis> arrows, bool freeAfterFusion = false) =>
                new JellyLevelCVO(7, 7, 120, Array.Empty<Vector2Int>(), pieces, Array.Empty<JellyGateCVO>(), null, "Arrows", "", null, null, arrows, freeAfterFusion);
            model.Start(Arrows(new[] { Piece(1, JellyColour.Pink, 2, 2) }, new Dictionary<int, JellyAxis> { { 1, JellyAxis.Horizontal } }));
            Check(model.Snapshot().MoveWays(1) == PuzzleSnapshotRVO.Horizontal, "A level starts with its arrow blocks");
            model.Drag(new Vector2Int(2, 2), new Vector2Int(2, 4), true);
            Check(model.Snapshot().Pieces[0].Cells[0] == new Vector2Int(2, 2) && model.Snapshot().Moves == 0, "An arrow block does not move across its way");
            model.Drag(new Vector2Int(2, 2), new Vector2Int(4, 5), true);
            Check(model.Snapshot().Pieces[0].Cells[0] == new Vector2Int(4, 2), "Dragged at a slant, an arrow block goes only along its way");
            model.Start(Arrows(new[] { Piece(1, JellyColour.Pink, 1, 1), Piece(2, JellyColour.Pink, 3, 1) }, new Dictionary<int, JellyAxis> { { 1, JellyAxis.Horizontal } }));
            model.Drag(new Vector2Int(3, 1), new Vector2Int(2, 1), true);
            Check(model.Snapshot().Pieces.Count == 1 && model.Snapshot().MoveWays(model.Snapshot().Pieces[0].Id) == PuzzleSnapshotRVO.Horizontal,
                "A jelly fused with an arrow block keeps its way");
            Check(model.Undo() && model.Snapshot().MoveWays(2) == PuzzleSnapshotRVO.AnyWay && model.Snapshot().MoveWays(1) == PuzzleSnapshotRVO.Horizontal, "Undo parts them, each with its own ways");
            model.Start(Arrows(new[] { Piece(1, JellyColour.Pink, 1, 1), Piece(2, JellyColour.Pink, 1, 3) },
                new Dictionary<int, JellyAxis> { { 1, JellyAxis.Horizontal }, { 2, JellyAxis.Vertical } }));
            model.Drag(new Vector2Int(1, 3), new Vector2Int(1, 2), true);
            int locked = model.Snapshot().Pieces[0].Id;
            Check(model.Snapshot().Pieces.Count == 1 && model.Snapshot().MoveWays(locked) == 0, "Arrow blocks of both ways fused cannot move at all");
            int movesBefore = model.Snapshot().Moves;
            model.Drag(new Vector2Int(1, 1), new Vector2Int(4, 1), true);
            Check(model.Snapshot().Moves == movesBefore, "A locked jelly stays put");
            model.Start(Arrows(new[] { Piece(1, JellyColour.Pink, 1, 1), Piece(2, JellyColour.Pink, 3, 1) },
                new Dictionary<int, JellyAxis> { { 1, JellyAxis.Horizontal } }, true));
            model.Drag(new Vector2Int(3, 1), new Vector2Int(2, 1), true);
            int freed = model.Snapshot().Pieces[0].Id;
            Check(model.Snapshot().Pieces.Count == 1 && model.Snapshot().MoveWays(freed) == PuzzleSnapshotRVO.AnyWay,
                "Enabled option frees an arrow jelly after fusion");
            model.Drag(new Vector2Int(1, 1), new Vector2Int(1, 3), true);
            Check(model.Snapshot().Pieces[0].Cells.Contains(new Vector2Int(1, 3)), "Freed fused jelly moves across its old arrow way");
            Check(model.Undo() && model.Snapshot().MoveWays(freed) == PuzzleSnapshotRVO.AnyWay, "Undo movement keeps the already-freed fusion free");
            Check(model.Undo() && model.Snapshot().MoveWays(1) == PuzzleSnapshotRVO.Horizontal && model.Snapshot().MoveWays(2) == PuzzleSnapshotRVO.AnyWay,
                "Undo fusion restores the original arrow restriction");

            // A door taking two jellies in turn, and another taking one.
            var queued = new[]
            {
                new JellyGateCVO(JellySide.Right, 1, 2, 4, 4, new[] { Shape(JellyColour.Pink, (0, 0)), Shape(JellyColour.Blue, (0, 0), (0, 1)) }),
                new JellyGateCVO(JellySide.Top, 0, 1, 4, 4, new[] { Shape(JellyColour.Yellow, (0, 0)) }),
            };
            var queuedLevel = new JellyLevelCVO(4, 4, 60, Array.Empty<Vector2Int>(), new[]
            {
                Piece(1, JellyColour.Pink, 3, 1),
                new JellyPieceVO(2, JellyColour.Blue, new[] { new Vector2Int(1, 1), new Vector2Int(1, 2) }),
                Piece(3, JellyColour.Yellow, 0, 3),
            }, queued);
            model.Start(queuedLevel);
            Check(model.Snapshot().WaitingStage(0).Colour == JellyColour.Pink && model.Snapshot().QueuedAfter(0) == 1 && model.Snapshot().QueuedAfter(1) == 0,
                "A door waits for its first jelly and counts the ones after it");
            Check(model.Snapshot().Warning.Length == 0, "A jelly a door takes later is not reported as impossible");
            model.Drag(new(0, 3), new(0, 4), true);
            Check(model.Snapshot().DoorProgress.SequenceEqual(new[] { 0, 1 }), "Each door keeps its own queue");
            model.Drag(new(3, 1), new(4, 1), true);
            Check(model.Snapshot().WaitingStage(0).Colour == JellyColour.Blue && model.Snapshot().QueuedAfter(0) == 0, "Passing the first jelly makes the door wait for the next");
            Check(model.Undo() && model.Snapshot().WaitingStage(0).Colour == JellyColour.Pink && model.Snapshot().Pieces.Count == 2, "Undo restores the door's queue and the jelly");
            model.Drag(new(3, 1), new(4, 1), true);
            model.Drag(new(1, 1), new(4, 1), true);
            Check(model.Status == PuzzleStatus.Won && model.Snapshot().WaitingStage(0) == null, "The last jelly closes the door and wins");
            Check(model.Undo() && model.Snapshot().WaitingStage(0).Colour == JellyColour.Blue, "Undoing the win reopens the last jelly");
            model.Start(queuedLevel);
            Check(model.Snapshot().DoorProgress.All(p => p == 0), "Restart resets every door's queue");
            var early = new JellyLevelCVO(4, 4, 60, Array.Empty<Vector2Int>(),
                new[] { new JellyPieceVO(1, JellyColour.Blue, new[] { new Vector2Int(3, 1), new Vector2Int(3, 2) }) }, queued);
            model.Start(early); model.Drag(new(3, 1), new(4, 1), true);
            Check(model.Snapshot().Exited == 0 && model.Snapshot().Moves == 0, "A door does not take a jelly before its turn");

            // A door that fits its jelly opens only as wide as the jelly it waits for, from its first cell.
            var fitStages = new[] { Shape(JellyColour.Pink, (0, 0)), Shape(JellyColour.Blue, (0, 0), (0, 1)) };
            var fit = new JellyGateCVO(JellySide.Right, 0, 2, 4, 4, fitStages, "Door", true);
            Check(fit.OpenCells(0).Count == 1 && fit.OpenCells(1).Count == 2 && fit.OpenCells(2).Count == 2, "A fitting door opens as wide as each jelly it waits for");
            model.Start(new JellyLevelCVO(4, 4, 60, Array.Empty<Vector2Int>(), new[] { Piece(1, JellyColour.Pink, 2, 1) }, new[] { fit }));
            model.Drag(new(2, 1), new(4, 1), true);
            Check(model.Snapshot().Exited == 0, "A fitting door is wall beside the part it opens");
            model.Start(new JellyLevelCVO(4, 4, 60, Array.Empty<Vector2Int>(), new[] { Piece(1, JellyColour.Pink, 2, 0) }, new[] { fit }));
            model.Drag(new(2, 0), new(4, 0), true);
            Check(model.Snapshot().Exited == 1 && model.Snapshot().WaitingStage(0).Colour == JellyColour.Blue, "A fitting door takes its jelly through the part it opens");
            var whole = new JellyGateCVO(JellySide.Right, 0, 2, 4, 4, fitStages);
            model.Start(new JellyLevelCVO(4, 4, 60, Array.Empty<Vector2Int>(), new[] { Piece(1, JellyColour.Pink, 2, 1) }, new[] { whole }));
            model.Drag(new(2, 1), new(4, 1), true);
            Check(model.Snapshot().Exited == 1, "A door that does not fit its jelly stays open across its whole size");
        }

        // ---------- level assets: what the designer authors ----------

        private void RunLevelChecks()
        {
            var level = ScriptableObject.CreateInstance<CD_JellyLevel>();
            try
            {
                level.Width = 3; level.Height = 3;
                level.Shapes.Add(new JellyShapeCVO { Cells = new List<Vector2Int> { new(1, 1) }, Colour = JellyColour.Pink });
                var door = new JellyDoorCVO { Side = JellySide.Right, Start = 1, Size = 1 };
                door.Queue.Add(new JellyShapeCVO { Colour = JellyColour.Pink, Cells = new List<Vector2Int> { new(0, 0) } });
                level.Doors.Add(door);
                Check(level.Validate().Count == 0 && level.CreateLevel().Gates[0].Cells.SequenceEqual(new[] { new Vector2Int(2, 1) }), "A door opens from the board cells along its side");
                Check(level.CreateLevel().Gates[0].PullReach == -1, "A door with no pull reach of its own leaves it to the game's default");
                door.OwnPullReach = 0;
                Check(level.CreateLevel().Gates[0].PullReach == 0, "A door's own pull reach is kept");
                door.OwnPullReach = -1;
                var frame = level.CreateLevel().Walls;
                Check(frame.Count == 7 && !frame.Contains(new Vector2Int(2, 1)) && !frame.Contains(new Vector2Int(1, 1)), "The outermost cells are wall, except where a door opens");
                level.Shapes[0].Cells[0] = new(0, 1); Check(level.Validate().Any(e => e.Contains("wall round the board")), "Designer rejects a jelly in the wall round the board"); level.Shapes[0].Cells[0] = new(1, 1);
                door.Start = 0; Check(level.Validate().Count > 0, "Designer rejects a door in a corner"); door.Start = 1;
                level.Holes.Add(new(2, 1)); Check(level.Validate().Count > 0, "Designer rejects an empty cell in front of a door"); level.Holes.Clear();
                level.Shapes[0].Cells.Add(new(3, 3)); Check(level.Validate().Count > 0, "Designer rejects disconnected shape and mismatched totals"); level.Shapes[0].Cells.RemoveAt(1);
                door.Start = 3; Check(level.Validate().Count > 0, "Designer rejects a door past the end of its side"); door.Start = 1;
                door.Size = 5; Check(level.Validate().Count > 0, "Designer rejects a door wider than 4"); door.Size = 1;
                door.Queue[0].Cells = new List<Vector2Int> { new(0, 0), new(0, 1) };
                Check(level.Validate().Any(e => e.Contains("cannot pass")), "Designer rejects a jelly wider than its door"); door.Queue[0].Cells = new List<Vector2Int> { new(0, 0) };
                level.Doors.Add(new JellyDoorCVO { Side = JellySide.Right, Start = 1, Size = 1, Queue = { new JellyShapeCVO { Cells = new List<Vector2Int> { new(0, 0) } } } });
                Check(level.Validate().Any(e => e.Contains("overlap")), "Designer rejects overlapping doors"); level.Doors.RemoveAt(1);
                level.Walls.Add(new(1, 1)); Check(level.Validate().Count > 0, "Designer rejects shape on wall"); level.Walls.Clear();
                var played = level.CreateLevel();
                door.Queue[0].Colour = JellyColour.Blue;
                Check(played.Gates[0].Stages[0].Colour == JellyColour.Pink, "Runtime does not follow later edits of the asset");
            }
            finally { UnityEngine.Object.DestroyImmediate(level); }

            var old = ScriptableObject.CreateInstance<CD_JellyLevel>();
            try
            {
                old.Width = 4; old.Height = 4;
                old.Shapes.Add(new JellyShapeCVO { Colour = JellyColour.Pink, Cells = new List<Vector2Int> { new(1, 1), new(1, 2) } });
                old.Shapes.Add(new JellyShapeCVO { Colour = JellyColour.Blue, Cells = new List<Vector2Int> { new(2, 1) } });
                var exit = new JellyTargetCVO { Colour = JellyColour.Pink, Cells = new List<Vector2Int> { new(5, 1), new(5, 2) } };
                exit.NextGates.Add(new JellyShapeCVO { Colour = JellyColour.Blue, Cells = new List<Vector2Int> { new(0, 0) } });
                old.Targets.Add(exit);
                Check(old.Validate().Count == 0 && old.CreateLevel().Gates.Count == 1, "A level with old exits still plays, as doors");
                old.ConvertOldExits();
                var converted = old.Doors.Single();
                Check(!old.HasOldExits && converted.Side == JellySide.Right && converted.Start == 1 && converted.Size == 2
                    && converted.Queue.Select(s => s.Colour).SequenceEqual(new[] { JellyColour.Pink, JellyColour.Blue }), "Old exits convert to a door on the same side, rows and queue");
                Check(converted.Queue[0].Cells.SequenceEqual(new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) }), "Converted jellies keep their shape");
                old.Doors.Clear();
                old.Targets.Add(new JellyTargetCVO { Colour = JellyColour.Pink, Cells = new List<Vector2Int> { new(5, 1), new(5, 2) } });
                old.Targets.Add(new JellyTargetCVO { Colour = JellyColour.Blue, Cells = new List<Vector2Int> { new(6, 2) } });
                old.ConvertOldExits();
                Check(old.Doors.Count == 1 && old.Doors[0].Size == 2 && old.Doors[0].Queue.Count == 2, "Old exits sharing rows become one door taking both in turn");
            }
            finally { UnityEngine.Object.DestroyImmediate(old); }
        }

        private static JellyPieceVO Piece(int id, JellyColour colour, int x, int y) => new JellyPieceVO(id, colour, new[] { new Vector2Int(x, y) });
        private static JellyPieceVO Shape(JellyColour colour, params (int X, int Y)[] cells) => new JellyPieceVO(0, colour, cells.Select(c => new Vector2Int(c.X, c.Y)));
        private static JellyGateCVO Door(JellySide side, int start, int size, JellyPieceVO stage) => new JellyGateCVO(side, start, size, 7, 7, new[] { stage });
        private static JellyLevelCVO Level(IEnumerable<JellyPieceVO> pieces, IEnumerable<Vector2Int> walls = null, IEnumerable<JellyGateCVO> gates = null)
            => new JellyLevelCVO(7, 7, 120, walls ?? Array.Empty<Vector2Int>(), pieces, gates ?? Array.Empty<JellyGateCVO>());
        private void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("Jelly Fuse rule check failed: " + name);
            _checks++;
        }
    }
}
#endif
