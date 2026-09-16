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
    class Engine
    {
        public Engine()
        {
            Game1.event_update += Die;

        }
        //checks for each mario if he die
        public void Die()
        {
            for (int i = 0; i < G.allMario.Count; i++)
            {
                //check if mario out of boarders
                if (G.allMario[i].Position.X >= G.currentMap.width || G.allMario[i].Position.X < 0 ||
                    G.allMario[i].Position.Y >= G.currentMap.height || G.allMario[i].Position.Y < 0)
                {
                    //remove the all old mario pointers to destroy him 
                    Game1.event_update -= G.allMario[i].update;
                    //creates new mario instead of current mario 
                    BaseKeys keys = G.allMario[i].BaseKey;
                    G.allMario[i] = new Player(Folder.mario, new Vector2(G.currentMap.width / 2, G.currentMap.height * 3 / 7), 0, new Vector2(2), 1);
                    G.allMario[i].BaseKey = keys;

                }
            }
        }
    }
}
