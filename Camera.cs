using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Smash
{
    interface Ifocus
    {
        Vector2 Position { get; set; }
        Vector2 checkZoom { get; set; }
    }
    class Mainfocus:Ifocus
    {
        public Vector2 Position { get; set; }
        public Vector2 checkZoom { get; set; }
        
        public Mainfocus()
        {
            this.Position = Vector2.Zero;
            this.checkZoom = Vector2.One;
            Game1.event_update += updateFocus;
        }
        public void updateFocus()
        {
            Vector2 pos = Vector2.Zero;
            for (int i = 0; i < G.allMario.Count; i++)
            {
                pos.X += G.allMario[i].Position.X;
                pos.Y += G.allMario[i].Position.Y;
            }
            
            pos.X /= G.allMario.Count;
            pos.Y /= G.allMario.Count;
            Position = pos;

            //checks if Zoom needed
            
            Vector2 zoomR = Position - new Vector2(G.currentMap.width / 2, G.currentMap.height / 2);
            if (zoomR.Length() > G.currentMap.width * 4 / 7)
            {
                checkZoom = new Vector2(1.3f);
            }
            if (zoomR.Length() < G.currentMap.width *4/7 && zoomR.Length() > G.currentMap.width *2/7)
            {

                checkZoom = Vector2.One;
            }
            if(zoomR.Length()<G.currentMap.width*2/7)
            {
                checkZoom = new Vector2(0.7f);
            }




        }

    }
    class Camera
    {
        //matrix to aplly camera changes
        public Matrix Mat { get; private set; }
        //amount of zooming in current update
        public Vector2 Zoom { get;  set; }
        //Interface for the followed object by camera
        Ifocus focus;
        //camera position
        Vector2 pos;
        //the part of the screen that the window shows
        Viewport vp;

        //constructor for camera
        public Camera(Ifocus focus, Viewport vp)
        {
            //camera starts in the middle of the map
            this.pos = new Vector2(G.currentMap.width / 2, G.currentMap.height / 2);
            this.focus = focus;
            this.vp = vp;
            Zoom = Vector2.One;
            Game1.event_update += camUpdate;
        }

        public void camUpdate()
        {
            //Zoom=Vector2.Lerp(Zoom,focus.checkZoom,0.2f);
            
            //moves cameras posiotion towards focus posiotion
            pos += (focus.Position - pos) / G.cameraSpeed;

           
            
            #region camera borders
            //right border
            if (pos.X > G.currentMap.width - G.windowWidth / 2)
            {
                pos.X = G.currentMap.width - G.windowWidth / 2;
            }
            //left border
            if (pos.X < G.windowWidth / 2 * focus.checkZoom.X)
            {
                pos.X = G.windowWidth / 2;
            }
            //up border
            if (pos.Y > G.currentMap.height - G.windowHeight / 2)
            {
                pos.Y = G.currentMap.height - G.windowHeight / 2;
            }
            //down border
            if (pos.Y < G.windowHeight / 2)
            {
                pos.Y = G.windowHeight / 2;
            }
            #endregion
            //calculate new matrix after changes
            Mat = Matrix.CreateTranslation(-pos.X, -pos.Y, 0) *
                  Matrix.CreateRotationZ(0) *
                  Matrix.CreateScale(Zoom.X,Zoom.X,1) *
                  Matrix.CreateTranslation(G.windowWidth / 2, G.windowHeight / 2, 0);
           

        }
    }
}

