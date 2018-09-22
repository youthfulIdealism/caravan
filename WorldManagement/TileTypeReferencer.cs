using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using caravan.WorldManagement.Tile;
using caravan.WorldManagement.Tile.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace caravan.WorldManagement
{
    public static class TileTypeReferencer
    {
        public static TileType DIRT;
        public static TileType AIR;

        public static void Load(ContentManager Content)
        {
            //DO NOT MODIFY WITHOUT ORDER HELP
            //DO NOT MODIFY WITHOUT ORDER HELP
            //DO NOT MODIFY WITHOUT ORDER HELP
            DIRT = new TileType(new TileTag[] { TagReferencer.SOLID }, Content.Load<Texture2D>("tile_dirt"), true);
            //DIRT.friction = .2f;
            DIRT.friction = .17f;
            DIRT.harvestTicks *= 4;
            DIRT.blockBreakSound = "rock-break";
            AIR = new TileType(new TileTag[] { TagReferencer.AIR }, Content.Load<Texture2D>("tile_air"), true);
            AIR.friction = .027f;
        }
    }
}
