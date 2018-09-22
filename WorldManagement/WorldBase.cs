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

            groundColor = Color.Black;

            noise = new PerlinNoise("burgerBaby");

            playerLoc = new Vector2(40, noise.octavePerlin1D(0) * 250 * Chunk.tileDrawWidth);
        }

        public virtual void switchTo()
        {

        }

        public virtual void update(GameTime time)
        {
            performChunkManagement(time);
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
            totalDrawOffset += new Point(Game1.instance.graphics.PreferredBackBufferWidth / 2, (Game1.instance.graphics.PreferredBackBufferHeight / 2));


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
                chunk.generate();

                lock (deQueue)
                {
                    deQueue.Add(chunk);
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
