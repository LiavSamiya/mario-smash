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
    class Map: Drawable
    {

        public int width;
        public int height;
        //constructor for map
        public Map(Texture2D texture, Vector2 scale)
            : base(texture,new Rectangle(0,0, texture.Width * (int)scale.Length(),texture.Height* (int) scale.Length()),Vector2.Zero, scale,0)
        {
            
            Game1.event_draw += draw;
            this.width = texture.Width*(int)scale.Length();
            this.height = texture.Height*(int)scale.Length();

        }
    }
}
