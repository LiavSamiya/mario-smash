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
    class Animations:Drawable
    {

        //contains animation data
        public Page page;
        //the current part of the animation
        public int frame;
        //slow animation speed
        protected int delay;
        //current file
        protected string data;
        //next file in order
        protected string buffdata;
        //current object side true=right,false=left
        protected bool side;
        //next object side in order true=right,false=left
        protected bool buffside;
        //The type of object
        protected Folder Type;
        
     
      // construtor for any animation
        #region ctor
        public Animations( Vector2 position, float rotation,Vector2 scale, float layerDepth)
            : base(position, rotation, scale, layerDepth)
        {
            
            frame = 0;
            side = true;
            buffside = true;
            if (position.X > G.currentMap.width/2)
            {
              effects = SpriteEffects.FlipHorizontally; side = false; buffside = false;
            }
            side = buffside;

        }
        
        /// constructor for stage
        #region stage ctor
        public Animations( Vector2 position,Vector2 scale,float layerdepth)
            :base(G.content.Load<Texture2D>("Stages/Stage"),new Rectangle((int)position.X,(int)position.Y,G.stageWidth, G.stageHeight), position, scale,layerdepth)
        {
            frame = 0;
            //creates rectangle for stage
            sourceRectangle = new Rectangle((int)position.X, (int)position.Y+20, G.stageWidth,G.stageHeight);
            side = true;
            buffside = true;

        }
        #endregion

        #endregion
        //used to switch for next animation without buffring(immediately) or for staying in same frame
        public void setframe(int frm)
        {
           frame = frm;
           delay = 0;
        }
        //updates the current part in animation
        public virtual void update()
        {
            page = Info.DInfo[Type][data];//set the page by data from dictionary
            texture = page.Tex;//sets texture
            sourceRectangle = page.Recs[frame];//sets current rectangle in texture
            origin = page.Orgs[frame];//sets current origin in rectangle 
            //fix origin and flip text side
            if (!side)
            {
                effects = SpriteEffects.FlipHorizontally; origin.X = sourceRectangle.Width - origin.X;
            }
            //delay animation speed by the rate in page
            if ((++delay) % page.Rate == 0)
            {
                frame++;
                frame %= page.Orgs.Count;
            }
         
        }

      

    }
}
