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
    class Drawable: Ifocus
    {

        #region data
        //the place of the object
        public Vector2 Position { get; set; }
        //check for changes in camera zoom
        public Vector2 checkZoom { get; set; }
        //texture object
        protected Texture2D texture;
        //rectangle of object
        public Rectangle sourceRectangle { get; set; }
        //the handles for position
        protected Vector2 origin;
        //rotation of object
        protected float rotation;
        //color change of object
        protected Color color;
        //size multiplayer of obect
        protected Vector2 scale;
        //flips image 
        protected SpriteEffects effects;
        //layer of object, bigger=in front
        float layerDepth;
        #endregion
        #region ctor
        //constructor for any moving object
        public Drawable(Vector2 position, float rotation,Vector2 scale,float layerDepth)
        {
            this.effects = SpriteEffects.None;
            this.Position = position;
            this.color = Color.White;
            this.rotation = rotation;
            this.scale = scale;
            this.layerDepth = layerDepth;
            Game1.event_draw += draw;
        }
        
        //ctor for map and stage:
        public Drawable( Texture2D texture,Rectangle rectangle, Vector2 position, Vector2 scale, float layerdepth)
        {
            this.texture = texture;
            this.scale = scale;
            this.Position = position;
            this.sourceRectangle=rectangle;
            this.color = Color.White;
            this.rotation = 0;
            this.origin = Vector2.Zero;
            this.effects = SpriteEffects.None;
            this.layerDepth = layerdepth;
            Game1.event_draw += draw;
        }

        
        #endregion
       //draws the object
        public virtual void draw()
        {
            //use spritebatch to draw 
            G.sb.Draw(this.texture, this.Position, this.sourceRectangle,
            this.color, this.rotation, this.origin, this.scale, this.effects, this.layerDepth);
            //sets side of object to right
            this.effects = SpriteEffects.None;
            
        }

    }
}