using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace Smash
{
    //AI keys, returns boolean for each key
    class BotKeys : BaseKeys
    {
       
        AI bot;
        //ctor of keys
        public BotKeys(AI bot)
        {
            this.bot = bot;
        }
        //keys functions
        #region BaseKeys
        private bool up = false, down = false, right = false, left = false, attack = false, shield = false;
        public override bool Left()
        {
            return left;
        }
        public override bool Right()
        {
            return right;
        }
        public override bool Up()
        {
            return up;
        }
        public override bool Down()
        {
            return down;
        }
        public override bool Attack()
        {
            return attack;
        }
        public override bool Shield()
        {
            return shield;
        }
        #endregion
    }
    class AI:Collisions
    {
        //constructor of Ai
        public AI(Folder folder, Vector2 position, float rotation, Vector2 scale, float depth) 
            :base(position, rotation, scale, depth)
        {
        Game1.event_update += update;
        }
    }
}
