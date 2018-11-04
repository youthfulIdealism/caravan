using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using caravan.Worldgen;
using caravan.WorldManagement.Tile;
using caravan.WorldManagement.Tile.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArmadilloLib.Animation;
using caravan.WorldManagement.Entities;

namespace caravan.WorldManagement
{
    public class Chunk : IDisposable
    {
        public const int tilesPerChunk = 20;
        public const int tileDrawWidth = 60;
        public bool needsReDraw { get; set; }
        public bool needsReBuildCollisions { get; set; }
        public static SpriteBatch chunkRenderBatch { get; set; }


        //0 = air.
        //1 = ground.
        //2 = cave
        //3 = rope
        //TODO: replace this with an array of tile objects.
        private TileType[,] tiles;
        private TileType[,] backgroundTiles;
        private List<AnimatedTree> trees;
        public Point location;
        WorldBase world;
        RenderTarget2D texture;
        public List<Rectangle> collisionBoxes;
        public List<Rectangle> collisionBoxWorkBuffer;
        public Rectangle totalBox { get; private set; }
        public Rectangle tileBox { get; private set; }
        Random random;

        bool generated = false;

        public Chunk(Point loc, WorldBase world)
        {
            if (chunkRenderBatch == null)
            {
                chunkRenderBatch = new SpriteBatch(Game1.instance.GraphicsDevice);
            }


            this.location = loc;
            this.world = world;
            tiles = new TileType[tilesPerChunk, tilesPerChunk];
            backgroundTiles = new TileType[tilesPerChunk, tilesPerChunk];
            needsReDraw = true;
            needsReBuildCollisions = true;

            collisionBoxes = new List<Rectangle>();
            collisionBoxWorkBuffer = new List<Rectangle>();
            totalBox = new Rectangle(location.X * tilesPerChunk * tileDrawWidth, location.Y * tilesPerChunk * tileDrawWidth, tilesPerChunk * tileDrawWidth, tilesPerChunk * tileDrawWidth);
            tileBox = new Rectangle(location.X * tilesPerChunk, location.Y * tilesPerChunk, tilesPerChunk, tilesPerChunk);

            trees = new List<AnimatedTree>();
        }

        public List<Entity> generate()
        {
            List<Entity> returnable;
            lock (tiles)
            {
                returnable = generateProcedural();
            }
            return returnable;
        }

        //returns a list of entities that need to be spawned into the world
        public List<Entity> generateProcedural()
        {
            List<Entity> spawnedEntities = new List<Entity>();

            //TODO: remove cast
            WorldBase world = (WorldBase)this.world;
            generated = true;

            random = new Random(location.X * 500 + location.Y);
            int terrainMultipler = 250;
            float caveThreshold = .585f;

            PerlinNoise perlin = world.noise;
            for (int x = 0; x < tilesPerChunk; x++)
            {
                float groundLevel = perlin.octavePerlin1D((float)(location.X * tilesPerChunk + x) / 25) * terrainMultipler;
                for (int y = 0; y < tilesPerChunk; y++)
                {
                    float height = (location.Y * tilesPerChunk + y);
                    if (height > groundLevel)
                    {
                        tiles[x, y] = TileTypeReferencer.DIRT;
                    }
                    else
                    {
                        
                        if(height < groundLevel && height >= groundLevel - 1)
                        {
                            PlantWrapper wrapper = new PlantWrapper(Game1.grass.getTree(), world);

                            int nx = location.X * tilesPerChunk * tileDrawWidth + x * tileDrawWidth/* - tileDrawWidth / 2 - 10*/;
                            int ny = location.Y * tilesPerChunk * tileDrawWidth + y * tileDrawWidth/* - tileDrawWidth / 2*/;
                            wrapper.location = new Vector2(nx, ny);
                            spawnedEntities.Add(wrapper);

                            PlantWrapper wrapper_background = new PlantWrapper(Game1.grass.getTree(), world);
                            wrapper_background.location = new Vector2(nx, ny);
                            wrapper_background.isBackground = true;
                            spawnedEntities.Add(wrapper_background);
                        }

                        tiles[x, y] = TileTypeReferencer.AIR;
                    }
                }
            }
            return spawnedEntities;
        }
        
        

        public void update(GameTime time)
        {
            lock (collisionBoxWorkBuffer)
            {
                if (collisionBoxWorkBuffer.Count > 0)
                {
                    collisionBoxes = collisionBoxWorkBuffer;
                    collisionBoxWorkBuffer = new List<Rectangle>();
                }

            }
        }

        public void reBakeTexture()
        {
            //ensure that we perform ONLY a redraw OR a rebackdraw
            if (needsReDraw)
            {
                lock (tiles)
                {
                    if (texture != null) { texture.Dispose(); }
                    texture = new RenderTarget2D(
                        Game1.instance.GraphicsDevice,
                        tileDrawWidth * tilesPerChunk,
                        tileDrawWidth * tilesPerChunk,
                        false,
                        Game1.instance.GraphicsDevice.PresentationParameters.BackBufferFormat,
                        DepthFormat.Depth24);

                    Game1.instance.GraphicsDevice.SetRenderTarget(texture);

                    // Draw the scene
                    Game1.instance.GraphicsDevice.Clear(Color.Transparent);

                    chunkRenderBatch.Begin();

                    for (int x = 0; x < tilesPerChunk; x++)
                    {
                        for (int y = 0; y < tilesPerChunk; y++)
                        {
                            if (backgroundTiles[x, y] != null)
                            {
                                backgroundTiles[x, y].draw(chunkRenderBatch, new Point(x, y), Color.White);
                            }
                            tiles[x, y].draw(chunkRenderBatch, new Point(x, y), Color.White);
                        }
                    }

                    chunkRenderBatch.End();

                    Game1.instance.GraphicsDevice.SetRenderTarget(null);

                    needsReDraw = false;
                }
            }
        }

        public void setTile(Point loc, TileType type)
        {
            lock (tiles)
            {
                tiles[loc.X, loc.Y] = type;
            }
        }

        public TileType getTile(Point loc)
        {
            lock (tiles)
            {
                return tiles[loc.X, loc.Y];
            }
        }

        public void setBackgroundTile(Point loc, TileType type)
        {
            lock (backgroundTiles)
            {
                backgroundTiles[loc.X, loc.Y] = type;
            }
        }

        public TileType getBackgroundTile(Point loc)
        {
            lock (backgroundTiles)
            {
                return backgroundTiles[loc.X, loc.Y];
            }
        }

        public void draw(SpriteBatch batch, GameTime time, Point offset, Color groundColor)
        {
            if (texture != null)
            {
                batch.Draw(texture, new Rectangle(location.X * tileDrawWidth * tilesPerChunk + offset.X, location.Y * tileDrawWidth * tilesPerChunk + offset.Y, tileDrawWidth * tilesPerChunk, tileDrawWidth * tilesPerChunk), groundColor);
            }



        }

        public void Dispose()
        {
            if (texture != null)
            {
                texture.Dispose();
            }

        }
    }
}
