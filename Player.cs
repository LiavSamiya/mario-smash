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
    class UserKeys : BaseKeys
    {
        //constructor for Userkeys
        Keys left, right, up, down, attack, shield;
        public UserKeys(Keys left, Keys right, Keys up, Keys down, Keys attack, Keys shield)
        {
            this.left = left;
            this.right = right;
            this.up = up;
            this.down = down;
            this.attack = attack;
            this.shield = shield;
        }
        //keys functions
        public override bool Left()
        {
            return G.currKey.IsKeyDown(left); 
        }
        public override bool Right()
        {
            return G.currKey.IsKeyDown(right);
        }
        public override bool Up()
        {
            return G.currKey.IsKeyDown(up);
        }
        public override bool Down()
        {
            return G.currKey.IsKeyDown(down);
        }
        public override bool Attack()
        {
            return G.Clicked(attack);
        }
        public override bool Shield()
        {
            return G.currKey.IsKeyDown(shield);
        }

    }
    class Player: Collisions
    {
        //constructor for player
        public Player(Folder folder, Vector2 position, float rotation,Vector2 scale, float depth) 
            :base(position,rotation,scale,depth)
        {
            Game1.event_update += update;
        }
       
    }
}
