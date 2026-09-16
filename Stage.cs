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
    class Stage:Animations
    {
        private int width;
        private int height;

        //constructor for Stage
        public Stage():base(new Vector2(G.currentMap.width / 2, (G.currentMap.height / 2) + 420), new Vector2(2), 0)
        {
            setframe(0);
            Type = Folder.Stages;
            data = "Stage";
            buffdata = "Stage";
            width = texture.Width * (int)scale.Length();
            height = texture.Height * (int)scale.Length();
            Game1.event_update += update;
        }
       //update stage
       public override void update()
        {
            base.update();

        }
    }
}
