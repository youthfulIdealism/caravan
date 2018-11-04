using ArmadilloLib;
using caravan.WorldManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace caravan
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        public static GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        public static Texture2D block;
        public static Game1 instance;
        public WorldBase world;
        public static Rectangle viewDimensions { get; private set; }
        public static RenderTarget2D pixelBox { get; private set; }

        public static TreeProvider grass;
        public static Texture2D grass_img;
        public static Texture2D grass_img_background;



        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            instance = this;

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                graphics.PreferredBackBufferWidth = int.Parse(Environment.GetCommandLineArgs()[1]);
                graphics.PreferredBackBufferHeight = int.Parse(Environment.GetCommandLineArgs()[2]);
                viewDimensions = new Rectangle(0, 0, (int)(graphics.PreferredBackBufferWidth * .5f), (int)(graphics.PreferredBackBufferHeight * .5f));
            }
            else
            {
                graphics.PreferredBackBufferWidth = 1900;
                graphics.PreferredBackBufferHeight = 1000;
                viewDimensions = new Rectangle(0, 0, (int)(graphics.PreferredBackBufferWidth * .5f), (int)(graphics.PreferredBackBufferHeight * .5f));
            }
        }

        protected override void UnloadContent()
        {
            //generatedTexture.Dispose();
        }

        protected override void Initialize()
        {
            

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            pixelBox = new RenderTarget2D(GraphicsDevice, viewDimensions.Width, viewDimensions.Height);

            //grass = Content.Load<TreeProvider>("./animationMap");
            grass = new TreeProvider(Content.Load<Texture2D>("./animationMap_tree"), 146, 106, 63);

            grass_img = Content.Load<Texture2D>("./grass_0");
            grass_img_background = Content.Load<Texture2D>("./grass_1");

            block = Content.Load<Texture2D>("./Block");
            TileTypeReferencer.Load(Content);
        }


        


        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if(world == null)
            {
                world = new WorldBase(0);
            }

            world.update(gameTime);

            Vector2 playerMoveDir = new Vector2();

            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                playerMoveDir += new Vector2(-1, 0);
            }else if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                playerMoveDir += new Vector2(1, 0);
            }

            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                playerMoveDir += new Vector2(0, -1);
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.S))
            {
                playerMoveDir += new Vector2(0, 1);
            }

            world.playerLoc += playerMoveDir * 5;

            base.Update(gameTime);
        }

        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(pixelBox);
            GraphicsDevice.Clear(Color.LightCyan);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);

            world.draw(spriteBatch, gameTime);


            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);




            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);

            spriteBatch.Draw(pixelBox, new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight), Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
