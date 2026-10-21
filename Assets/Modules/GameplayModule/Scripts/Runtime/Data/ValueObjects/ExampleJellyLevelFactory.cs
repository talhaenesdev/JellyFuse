using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
namespace Modules.GameplayModule.Data.ValueObjects
{
    public class ExampleJellyLevelFactory
    {
        // Rule-check fixture. Production levels are authored in CD_JellyLevel.
        // Pink: fuse the single into the pair to make an L, and take it out the right-hand door.
        // Blue: fuse the two pairs into a square, and take it out the left-hand door.
        public JellyLevelCVO Create() => new JellyLevelCVO(7, 7, 120,
            new[] { new Vector2Int(1, 5), new Vector2Int(2, 3), new Vector2Int(3, 3), new Vector2Int(4, 3) },
            new[]
            {
                new JellyPieceVO(1, JellyColour.Pink, new[] { new Vector2Int(1, 1) }),
                new JellyPieceVO(2, JellyColour.Pink, new[] { new Vector2Int(4, 4), new Vector2Int(4, 5) }),
                new JellyPieceVO(3, JellyColour.Blue, new[] { new Vector2Int(2, 0), new Vector2Int(2, 1) }),
                new JellyPieceVO(4, JellyColour.Blue, new[] { new Vector2Int(5, 0), new Vector2Int(5, 1) })
            },
            new[]
            {
                new JellyGateCVO(JellySide.Right, 4, 2, 7, 7,
                    new[] { new JellyPieceVO(0, JellyColour.Pink, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) }) }, "L"),
                new JellyGateCVO(JellySide.Left, 0, 2, 7, 7,
                    new[] { new JellyPieceVO(0, JellyColour.Blue, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }) }, "2×2")
            });
    }
}
