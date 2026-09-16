using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.IO;


namespace Smash
{
    //delegate for all updates in game1
    public delegate void DLG_update();
    //delgate for all drawing in game1
    public delegate void DLG_draw();

    //abstract keys for key override
    abstract class BaseKeys
    {
        public abstract bool Left();
        public abstract bool Right();       
        public abstract bool Up();
        public abstract bool Down();
        public abstract bool Attack();
        public abstract bool Shield();

    }
    //all static parameters
    static class G
    {
        //list of all players
        public static List<Mario> allMario = new List<Mario>();
        //camera
        public static Camera cam;
        //content manager
        public static ContentManager content;
        //window width
        public static int windowWidth = 1200;
        //window height
        public static int windowHeight = 700;
        //Stage width
        public static int stageWidth = 840;
        //Stage height
        public static int stageHeight = 40;
        //current map
        public static Map currentMap;
        //curent stage
        public static Stage currStage;
        //spritebatch function for drawing
        public static SpriteBatch sb;
        //graphic device function
        public static GraphicsDevice graphics;
        //curren and previous key
        public static KeyboardState currKey, prvKey;
        //current and previous mouse state
        public static MouseState currMouse, prvMouse;
        //number of players
        public static int numOfPlayers=2;
        //speed of camera movment' bigger=slower
        public static int cameraSpeed = 30;
        




        #region Press and Release

        //check if key is clicked
        public static bool Clicked(Keys key)
        {
            return G.currKey.IsKeyDown(key) && G.prvKey.IsKeyUp(key);
        }
        //check if key is released
        public static bool Released(Keys key)
        {
            return G.currKey.IsKeyUp(key) && G.prvKey.IsKeyDown(key);
        }
        //check if mouse left button is clicked
        public static bool LeftMouse()
        {
            return G.currMouse.LeftButton == ButtonState.Pressed && G.prvMouse.LeftButton == ButtonState.Released;
        }
        //check if mouse right button is clicked
        public static bool RightMouse()
        {
            return G.currMouse.RightButton == ButtonState.Pressed && G.prvMouse.RightButton == ButtonState.Released;
        }
        #endregion

        #region update
        public static void update()
        {
            #region update keyboard and mouse
            //keys update
            G.prvKey = G.currKey;
            G.currKey = Keyboard.GetState();
            //mouse update
            G.prvMouse = G.currMouse;
            G.currMouse = Mouse.GetState();
            #endregion
        }
        #endregion
        //sets spritebatch and content manager 
        public static void init(ContentManager cm,SpriteBatch sb,GraphicsDevice gd)
        {
            G.sb = sb;
            content = cm;
            graphics = gd;
        }
    }
}
