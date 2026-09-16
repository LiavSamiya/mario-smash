using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Smash
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        //graphics functions
        GraphicsDeviceManager graphics;
        public static SpriteBatch spriteBatch;
        //delegates
        public static event DLG_update event_update;
        public static event DLG_draw event_draw;
        //focus for camera
        private Mainfocus focus;
        //collision of objects
        private Engine engine;

        //constructor for game1
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            //make mouse visiable
            IsMouseVisible = true;
            //set window size
            graphics.PreferredBackBufferHeight = G.windowHeight;
            graphics.PreferredBackBufferWidth = G.windowWidth;
            graphics.ApplyChanges();
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            //intialize class G
            G.init(Content, spriteBatch,GraphicsDevice);
            //fill Dictionary in stats
            Info.FillDictionary();
            //set current map
            G.currentMap = new Map(G.content.Load<Texture2D>("Background"), new Vector2(2));
            //set current stage
            G.currStage = new Stage();
            G.stageHeight = G.currStage.sourceRectangle.Height;
            G.stageWidth = G.currStage.sourceRectangle.Width ;
            //create players
            for (int i = 0; i < G.numOfPlayers; i++)
            {
                G.allMario.Add( new Player(Folder.mario, new Vector2(G.currentMap.width * (i+3) / 7, G.currentMap.height / 2), 0, new Vector2(2), 1));
            }
            //set keys for players 
            G.allMario[0].BaseKey = new UserKeys(Keys.A, Keys.D, Keys.W, Keys.S, Keys.Tab, Keys.Q);
            G.allMario[1].BaseKey = new UserKeys(Keys.Left, Keys.Right, Keys.Up, Keys.Down, Keys.RightShift, Keys.NumPad0);
            //set focus and camera
            focus = new Mainfocus();
            focus.updateFocus();
            engine = new Engine();
            G.cam = new Camera(focus, new Viewport(0, 0, G.currentMap.width/2, G.currentMap.height/2));
            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }
        //update game 
        protected override void Update(GameTime gameTime)
        {
            //exit game
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            //update all classes in event_update
            if (event_update != null)
            {
                
                G.update();
                event_update();
                
            }
            base.Update(gameTime);
        }

        //Draw objects
        protected override void Draw(GameTime gameTime)
        {
            //background color
            GraphicsDevice.Clear(Color.CornflowerBlue);
            //camera positioning and layer order
            spriteBatch.Begin(SpriteSortMode.FrontToBack, null, null, null, null, null,G.cam.Mat);
            //draw all classes in event_draw
            if (event_draw != null)
            {
                event_draw();
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
