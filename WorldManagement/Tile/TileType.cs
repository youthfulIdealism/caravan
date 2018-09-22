using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using caravan.WorldManagement.Entities;
//using caravan.WorldManagement.Inventory;
using caravan.WorldManagement.Tile.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace caravan.WorldManagement.Tile
{
    public class TileType
    {
        protected static Dictionary<int, TileType> tileRegistry;
        protected static int currentID;
        public int TILEID;
        public HashSet<TileTag> tags { get; set; }
        public Texture2D texture { get; set; }
        public float friction { get; set; }
        public int harvestTicks;
        public string blockBreakSound;


        public static TileType getTileFromID(int id)
        {
            return tileRegistry[id];
        }

        public TileType(TileTag[] tags, Texture2D tex, bool permanent)
        {
            if(tileRegistry == null)
            {
                tileRegistry = new Dictionary<int, TileType>();
                currentID = 0;
            }

            TILEID = ++currentID;
            this.tags = new HashSet<TileTag>();
            this.texture = tex;
            friction = .02f;
            harvestTicks = 25;
            blockBreakSound = "block-break";
            foreach (TileTag tag in tags)
            {
                this.tags.Add(tag);
            }

            if(permanent)
            {
                tileRegistry.Add(TILEID, this);
            }
        }

        public virtual float getFrictionMultiplier()
        {
            return 1f - friction;
        }

        public virtual void draw(SpriteBatch batch, Point place, Color color)
        {
            //IF YOU CHANGE HOW THIS WORKS, BE SURE TO CHANGE HOW RANDOMIMAGETILEFROMSPRITESHEET WORKS, TOO!
            if(texture != null)
            {
                if(tags.Contains(TagReferencer.DRAWOUTSIDEOFBOUNDS))
                {
                    batch.Draw(texture, new Rectangle(place.X * Chunk.tileDrawWidth - (texture.Width / 2 - Chunk.tileDrawWidth / 2), place.Y * Chunk.tileDrawWidth - (texture.Height / 2 - Chunk.tileDrawWidth / 2), texture.Width, texture.Height), color);
                }
                else
                {
                    batch.Draw(texture, new Rectangle(place.X * Chunk.tileDrawWidth, place.Y * Chunk.tileDrawWidth, Chunk.tileDrawWidth, Chunk.tileDrawWidth), color);
                }
                
            }
            
        }

        /*public virtual void harvest(TileType tileType, Item harvestTool, Vector2 location, WorldBase world)
        {
            foreach (ItemDropper dropper in HarvestDictionary.getHarvestsForTile(tileType))
            {
                dropper.drop(world, harvestTool, location);
            }
            
        }*/

        public virtual void Dispose()
        {
            texture.Dispose();
        }


        public static void replaceTileInDictionary(int id, TileType newEntry)
        {
            TileType oldType = tileRegistry[id];
            tileRegistry.Remove(id);
            tileRegistry.Add(id, newEntry);
            newEntry.TILEID = oldType.TILEID;
        }

        public override bool Equals(object obj)
        {
            return obj is TileType && TILEID == ((TileType)obj).TILEID;
        }
    }
}
