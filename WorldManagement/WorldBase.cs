using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
//using caravan.WorldManagement.ContentProcessors;
using caravan.WorldManagement.Entities;
/*using caravan.WorldManagement.Entities.Particles;
using caravan.WorldManagement.Inventory;
using caravan.WorldManagement.Procedurals;*/
using caravan.WorldManagement.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using caravan.Worldgen;
using ArmadilloLib.Animation;
using ArmadilloTree.Simulation;
using ArmadilloTree;

namespace caravan.WorldManagement
{
    public class WorldBase : IDisposable
    {
        public int tileGenRadious = 2;

        public Dictionary<Point, Chunk> chunks;

        public HashSet<Point> chunksQueuedForWorldGen;
        public HashSet<Point> chunksinWorldGen;
        protected List<Chunk> deQueuedChunk;
        protected List<Chunk> removeChunks;

        public HashSet<Point> chunksQueuedForPhysicsGen;
        public HashSet<Point> chunksinPhysicsGen;
        protected List<Chunk> deQueuedChunkPhysicsGen;

        public Vector2 playerLoc;
        public Point drawOffset;
        public Random rand;
        public bool hasRedrawnChunkThisUpdate;

        public Color groundColor;

        public PerlinNoise noise { get; private set; }

        public HashSet<Entity> queuedEntities;
        public HashSet<Entity> entities;

        float windOffset;
        float windWaveWidth = 100;

        public BasicEffect[] quadEffect;
        //public BasicEffect quadEffectBackground;

        public WorldBase(int difficulty)
        {
            chunks = new Dictionary<Point, Chunk>();

            //set up threading for world generation
            chunksQueuedForWorldGen = new HashSet<Point>();
            chunksinWorldGen = new HashSet<Point>();
            deQueuedChunk = new List<Chunk>();

            //set up threading for physics baking
            chunksQueuedForPhysicsGen = new HashSet<Point>();
            chunksinPhysicsGen = new HashSet<Point>();
            deQueuedChunkPhysicsGen = new List<Chunk>();

            removeChunks = new List<Chunk>();

            //groundColor = Color.Black;
            groundColor = Color.Lerp(Color.SaddleBrown, Color.Black, .7f);

            noise = new PerlinNoise("burgerBaby");

            playerLoc = new Vector2(40, noise.octavePerlin1D(0) * 250 * Chunk.tileDrawWidth);

            entities = new HashSet<Entity>();
            queuedEntities = new HashSet<Entity>();

            windOffset = 0;
            rand = new Random();

            Rectangle screenBounds = Game1.viewDimensions;



            /*quadEffect = new BasicEffect(Game1.graphics.GraphicsDevice);
            quadEffect.World = Matrix.CreateTranslation(-screenBounds.Width / 2, -screenBounds.Height / 2, 0);
            quadEffect.View = Matrix.CreateLookAt(new Vector3(0, 0, -1), new Vector3(0, 0, 0), new Vector3(0, -1, 0));
            quadEffect.Projection = Matrix.CreateOrthographic((float)screenBounds.Width, (float)screenBounds.Height, 1.0f, 100.0f);
            quadEffect.VertexColorEnabled = true;
            quadEffect.TextureEnabled = true;
            quadEffect.Texture = Game1.grass_img;

            quadEffectBackground = new BasicEffect(Game1.graphics.GraphicsDevice);
            quadEffectBackground.World = Matrix.CreateTranslation(-screenBounds.Width / 2, -screenBounds.Height / 2, 0);
            quadEffectBackground.View = Matrix.CreateLookAt(new Vector3(0, 0, -1), new Vector3(0, 0, 0), new Vector3(0, -1, 0));
            quadEffectBackground.Projection = Matrix.CreateOrthographic((float)screenBounds.Width, (float)screenBounds.Height, 1.0f, 100.0f);
            quadEffectBackground.VertexColorEnabled = true;
            quadEffectBackground.TextureEnabled = true;
            quadEffectBackground.Texture = Game1.grass_img_background;*/

            AnimationCapturer capturer = new AnimationCapturer(1500, 16, () =>
            {
                Root plant = new Root();
                buildGrass(plant, 0, 5);
                return plant;
            });
            capturer.capture();
            Texture2D[] blades = capturer.generateAnimation(Game1.instance.spriteBatch, Game1.graphics.GraphicsDevice);

            quadEffect = new BasicEffect[blades.Length];
            for (int i = 0; i < blades.Length; i++)
            {
                quadEffect[i] = new BasicEffect(Game1.graphics.GraphicsDevice);
                quadEffect[i].World = Matrix.CreateTranslation(-screenBounds.Width / 2, -screenBounds.Height / 2, 0);
                quadEffect[i].View = Matrix.CreateLookAt(new Vector3(0, 0, -1), new Vector3(0, 0, 0), new Vector3(0, -1, 0));
                quadEffect[i].Projection = Matrix.CreateOrthographic((float)screenBounds.Width, (float)screenBounds.Height, 1.0f, 100.0f);
                quadEffect[i].VertexColorEnabled = true;
                quadEffect[i].TextureEnabled = true;
                quadEffect[i].Texture = blades[i];
            }

        }

        private Node buildGrass(Node current, int currentDepth, int targetDepth)
        {
            for (int i = 0; i < 25; i++)
            {
                Root root = new Root();
                root.origin = current.origin + new Vector2((rand.nextFloat() - .5f) * 60, 0);

                current.children.Add(root);

                Color[] mixColors = new Color[] { Color.Salmon, Color.DarkSeaGreen, Color.Red, Color.Purple };

                buildGrassblade(root, currentDepth, targetDepth + rand.Next(6), Color.Lerp(mixColors[rand.Next(mixColors.Length)], Color.Yellow, .4f + rand.nextFloat() * .2f));
            }
            return current;
        }

        private void buildGrassblade(Node current, int currentDepth, int targetDepth, Color mixColor)
        {
            Limb nextSegment = new Limb(6 + rand.Next(5), (rand.nextFloat() - .5f) * .1f, (int)Math.Max((.75f * (targetDepth - currentDepth + 1)), 1), current);
            nextSegment.squashiness = 1f;
            nextSegment.stiffness = .2f;
            //nextSegment.mass = .8f;
            nextSegment.color = Color.Lerp(mixColor, Color.Wheat, ((float)currentDepth / (float)targetDepth + 1) * .5f + rand.nextFloat() * .1f);
            current.children.Add(nextSegment);

            if (currentDepth <= targetDepth)
            {
                nextSegment.windForceMultiplier = .1f;
                nextSegment.childForceMultiplier = 1;
                nextSegment.gravityForceMultiplier = 1;
                nextSegment.mass = 0;
                buildGrassblade(nextSegment, currentDepth + 1, targetDepth, mixColor);
            }
            else
            {
                nextSegment.windForceMultiplier = 6;
            }
        }

        public virtual void switchTo()
        {

        }

        public virtual void update(GameTime time)
        {
            performChunkManagement(time);
            windOffset += 1f;

            foreach (Entity entity in entities)
            {
                entity.update(1);
                if(entity is PlantWrapper)
                {
                    PlantWrapper tree = (PlantWrapper)entity;
                    float effectiveWindOffset = windOffset + (tree.isBackground ? 0 : 15);

                    if ((tree.location.X + effectiveWindOffset) % windWaveWidth <= 1 && (tree.location.X + effectiveWindOffset) % windWaveWidth > 0)
                    {
                         tree.bendModifiers.Add(new SineAndHalfTimeModifier(220, .1f, .025f));
                    }
                }
            }

            lock(queuedEntities)
            {
                foreach(Entity entity in queuedEntities)
                {
                    entities.Add(entity);
                }
                queuedEntities.Clear();
            }
        }

        public void setGraphicForQuad()
        {
            //Game1.graphics.GraphicsDevice.BlendState = BlendState.Opaque;
            Game1.graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            //Game1.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            Game1.graphics.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
        }

        public virtual void onQueueChunkForWorldGen(Point where)
        {

        }

        public virtual void onDeQueueChunk(Chunk chunk)
        {
            
        }
        

        public virtual void trackPlayerMovementsWithCamera()
        {
            int playerTileLocX = (int)Math.Floor(playerLoc.X);
            int playerTileLocY = (int)Math.Floor(playerLoc.Y);
            drawOffset = new Point(-playerTileLocX, -playerTileLocY);
        }

        public void performChunkManagement(GameTime time)
        {
            //find the player's location and the draw offset
            int playerTileLocX = (int)Math.Floor(playerLoc.X);
            int playerTileLocY = (int)Math.Floor(playerLoc.Y);
            trackPlayerMovementsWithCamera();

            //find which chunk the player is in, ...
            int playerChunkLocX = (int)Math.Floor(playerLoc.X / (Chunk.tilesPerChunk * Chunk.tileDrawWidth));
            int playerChunkLocY = (int)Math.Floor(playerLoc.Y / (Chunk.tilesPerChunk * Chunk.tileDrawWidth));

            //...and use that information to queue any un-generated surrounding chunks.
            for (int x = playerChunkLocX - tileGenRadious; x <= playerChunkLocX + tileGenRadious; x++)
            {
                for (int y = playerChunkLocY - tileGenRadious; y <= playerChunkLocY + tileGenRadious; y++)
                {
                    if (!chunks.ContainsKey(new Point(x, y)) && !chunksQueuedForWorldGen.Contains(new Point(x, y)) && !chunksinWorldGen.Contains(new Point(x, y)))
                    {
                        Point queuePoint = new Point(x, y);
                        chunksQueuedForWorldGen.Add(queuePoint);
                        onQueueChunkForWorldGen(queuePoint);
                    }
                }
            }

            //try to acquire chunk lock. When it's free, add the contents of the chunk dequeue to the chunk dictionary and clean it out.
            if (Monitor.TryEnter(deQueuedChunk))
            {
                try
                {
                    foreach (Chunk chunk in deQueuedChunk)
                    {
                        chunks.Add(chunk.location, chunk);
                        chunksinWorldGen.Remove(chunk.location);
                        chunksQueuedForWorldGen.Remove(chunk.location);

                        onDeQueueChunk(chunk);
                    }
                    deQueuedChunk.Clear();
                }
                finally
                {
                    Monitor.Exit(deQueuedChunk);
                }
            }

            //iterate through the chunks queued for world generation. If one of them is not yet being generated, queue it!
            foreach (Point point in chunksQueuedForWorldGen)
            {
                if (!chunksinWorldGen.Contains(point))
                {
                    ChunkMultithreadingHelper chunkGenerator = new ChunkMultithreadingHelper(point, deQueuedChunk, this);
                    ThreadPool.QueueUserWorkItem(chunkGenerator.runWorldGenerate);
                    chunksinWorldGen.Add(point);
                    break;
                }
            }


            //Reset chunk draw flag. We only want to redraw one chunk per update.
            hasRedrawnChunkThisUpdate = false;
            bool hasRemovedChunkThisUpdate = false;

            //update chunks.
            foreach (Chunk chunk in chunks.Values)
            {
                //if a chunk is within disposal distance, add it to the disposal queue
                if (Math.Abs(playerChunkLocX - chunk.location.X) > tileGenRadious * 2 || Math.Abs(playerChunkLocY - chunk.location.Y) > tileGenRadious * 2)
                {
                    if (!hasRemovedChunkThisUpdate)
                    {
                        hasRemovedChunkThisUpdate = true;
                        removeChunks.Add(chunk);
                    }

                }
                else//don't bother updating unless the chunk is within an appropriate distance
                {
                    chunk.update(time);

                    //if the chunk needs any variety of redraw, redraw it, ...
                    if (chunk.needsReDraw && !hasRedrawnChunkThisUpdate)
                    {
                        chunk.reBakeTexture();
                        //... and set the flag so that we don't redraw more than one chunk.
                        hasRedrawnChunkThisUpdate = true;
                    }

                    //if the chunk needs to have rebuilt collisions and it is not currently being rebuilt, queue it for rebuilding.
                    if (chunk.needsReBuildCollisions && !chunksQueuedForPhysicsGen.Contains(chunk.location) && !chunksinPhysicsGen.Contains(chunk.location))
                    {
                        chunksQueuedForPhysicsGen.Add(chunk.location);
                    }
                }
            }

            Point chunkPushedIntoPhysicsGen = new Point(int.MinValue, int.MinValue);

            //queue a chunk for physics generation if it's not already queued.
            foreach (Point point in chunksQueuedForPhysicsGen)
            {
                if (!chunksinPhysicsGen.Contains(point))
                {
                    Chunk workingOn = chunks[point];
                    ChunkMultithreadingHelper chunkGenerator = new ChunkMultithreadingHelper(point, deQueuedChunkPhysicsGen, this);
                    ThreadPool.QueueUserWorkItem(chunkGenerator.runPhysicsUpdate, workingOn);
                    chunksinPhysicsGen.Add(point);
                    chunkPushedIntoPhysicsGen = point;
                    break;
                }
            }

            if (!chunkPushedIntoPhysicsGen.Equals(new Point(int.MinValue, int.MinValue)))
            {
                chunksQueuedForPhysicsGen.Remove(chunkPushedIntoPhysicsGen);
            }

            lock (deQueuedChunkPhysicsGen)
            {
                foreach (Chunk chunk in deQueuedChunkPhysicsGen)
                {
                    chunksinPhysicsGen.Remove(chunk.location);
                }
            }


            //delete any disposed chunks.
            foreach (Chunk chunk in removeChunks)
            {
                onRemoveChunk(chunk);
                chunks.Remove(chunk.location);
                chunk.Dispose();
            }
            removeChunks.Clear();
        }

        public virtual void onRemoveChunk(Chunk chunk)
        {

        }

        /**
            Convert a world location to a global tile location.
        */
        public Point worldLocToTileLoc(Vector2 position)
        {
            int tileLocX = (int)Math.Floor(position.X / Chunk.tileDrawWidth);
            int tileLocY = (int)Math.Floor(position.Y / Chunk.tileDrawWidth);
            return new Point(tileLocX, tileLocY);
        }

        public void placeTile(TileType block, Vector2 position)
        {

            int chunkLocX = (int)Math.Floor(position.X / (Chunk.tilesPerChunk * Chunk.tileDrawWidth));
            int chunkLocY = (int)Math.Floor(position.Y / (Chunk.tilesPerChunk * Chunk.tileDrawWidth));

            int tileLocX = (int)Math.Floor((position.X - (chunkLocX * Chunk.tilesPerChunk * Chunk.tileDrawWidth)) / Chunk.tileDrawWidth);
            int tileLocY = (int)Math.Floor((position.Y - (chunkLocY * Chunk.tilesPerChunk * Chunk.tileDrawWidth)) / Chunk.tileDrawWidth);

            Point chunkLoc = new Point(chunkLocX, chunkLocY);

            if (chunks.ContainsKey(chunkLoc))
            {
                Chunk chunk = chunks[chunkLoc];
                chunk.setTile(new Point(tileLocX, tileLocY), block);
                chunk.needsReBuildCollisions = true;
                chunk.needsReDraw = true;
            }
        }

        public void placeBackgroundTile(TileType block, Vector2 position)
        {

            int chunkLocX = (int)Math.Floor(position.X / (Chunk.tilesPerChunk * Chunk.tileDrawWidth));
            int chunkLocY = (int)Math.Floor(position.Y / (Chunk.tilesPerChunk * Chunk.tileDrawWidth));

            int tileLocX = (int)Math.Floor((position.X - (chunkLocX * Chunk.tilesPerChunk * Chunk.tileDrawWidth)) / Chunk.tileDrawWidth);
            int tileLocY = (int)Math.Floor((position.Y - (chunkLocY * Chunk.tilesPerChunk * Chunk.tileDrawWidth)) / Chunk.tileDrawWidth);

            Point chunkLoc = new Point(chunkLocX, chunkLocY);

            if (chunks.ContainsKey(chunkLoc))
            {
                Chunk chunk = chunks[chunkLoc];
                chunk.setBackgroundTile(new Point(tileLocX, tileLocY), block);
                chunk.needsReBuildCollisions = true;
                chunk.needsReDraw = true;
            }
        }

        public TileType getBlock(Vector2 position)
        {
            int chunkLocX = (int)Math.Floor(position.X / (Chunk.tilesPerChunk * Chunk.tileDrawWidth));
            int chunkLocY = (int)Math.Floor(position.Y / (Chunk.tilesPerChunk * Chunk.tileDrawWidth));

            int tileLocX = (int)Math.Floor((position.X - (chunkLocX * Chunk.tilesPerChunk * Chunk.tileDrawWidth)) / Chunk.tileDrawWidth);
            int tileLocY = (int)Math.Floor((position.Y - (chunkLocY * Chunk.tilesPerChunk * Chunk.tileDrawWidth)) / Chunk.tileDrawWidth);

            Point chunkLoc = new Point(chunkLocX, chunkLocY);
            if (chunks.ContainsKey(chunkLoc))
            {
                Chunk chunk = chunks[chunkLoc];
                //TileType tile = chunk.tiles[tileLocX, tileLocY];
                TileType tile = chunk.getTile(new Point(tileLocX, tileLocY));

                return tile;
            }
            return null;
        }

        public TileType getBlock(Point position)
        {
            return getBlock(position.ToVector2() * Chunk.tileDrawWidth);
        }

        public TileType getBackgroundBlock(Vector2 position)
        {
            int chunkLocX = (int)Math.Floor(position.X / (Chunk.tilesPerChunk * Chunk.tileDrawWidth));
            int chunkLocY = (int)Math.Floor(position.Y / (Chunk.tilesPerChunk * Chunk.tileDrawWidth));

            int tileLocX = (int)Math.Floor((position.X - (chunkLocX * Chunk.tilesPerChunk * Chunk.tileDrawWidth)) / Chunk.tileDrawWidth);
            int tileLocY = (int)Math.Floor((position.Y - (chunkLocY * Chunk.tilesPerChunk * Chunk.tileDrawWidth)) / Chunk.tileDrawWidth);

            Point chunkLoc = new Point(chunkLocX, chunkLocY);
            if (chunks.ContainsKey(chunkLoc))
            {
                Chunk chunk = chunks[chunkLoc];
                //TileType tile = chunk.tiles[tileLocX, tileLocY];
                TileType tile = chunk.getBackgroundTile(new Point(tileLocX, tileLocY));

                return tile;
            }
            return null;
        }

        public TileType getBackgroundBlock(Point position)
        {
            return getBackgroundBlock(position.ToVector2() * Chunk.tileDrawWidth);
        }

        public virtual void Dispose()
        {
            foreach (Chunk c in chunks.Values)
            {
                c.Dispose();
            }
        }

        protected Point totalDrawOffset;
        public virtual void draw(SpriteBatch batch, GameTime time)
        {
            totalDrawOffset = drawOffset;
            totalDrawOffset += new Point(Game1.viewDimensions.Width / 2, (Game1.viewDimensions.Height / 2));

            foreach (Entity entity in entities)
            {
                entity.draw(batch, totalDrawOffset);
            }

            foreach (Chunk chunk in chunks.Values)
            {
                chunk.draw(batch, time, totalDrawOffset, groundColor);
            }
        }
        
        public class ChunkMultithreadingHelper
        {
            List<Chunk> deQueue;
            public Point point;
            WorldBase world;

            public ChunkMultithreadingHelper(Point point, List<Chunk> deQueue, WorldBase world)
            {
                this.deQueue = deQueue;
                this.point = point;
                this.world = world;
            }

            public void runWorldGenerate(Object context)
            {
                Chunk chunk = new Chunk(point, world);
                List<Entity> generatedEntities = chunk.generate();

                lock (deQueue)
                {
                    deQueue.Add(chunk);
                }

                lock(world.queuedEntities)
                {
                    foreach(Entity entity in generatedEntities)
                    {
                        world.queuedEntities.Add(entity);
                    }
                    
                }
            }

            public void runPhysicsUpdate(Object context)
            {
                Chunk chunk = (Chunk)context;

                lock (deQueue)
                {
                    deQueue.Add(chunk);
                }
            }

        }
    }
}
