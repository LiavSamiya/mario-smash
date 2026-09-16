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

    class Mario : Animations
    {
        //true=jumped
        bool jumped;
        //true=air
        bool air;
        
        //added position movment 
        protected Vector2 velocity = Vector2.Zero;
        //opponents position influence
        protected Vector2 knockback = Vector2.Zero;
        //keys(overrided by Userkeys or BotKeys)
        public BaseKeys BaseKey { get; set; }
        //constructor fo mario
        #region ctor
        public Mario( Vector2 position, float rotation, Vector2 scale, float depth)
        : base(position, rotation, scale, depth)
        {
            setframe(0);
            Type = Folder.mario;
            data ="fall";
            texture = Info.DInfo[Type][data].Tex;
            jumped = true;
            air = true;
            Game1.event_update += update;
            Game1.event_update += updateCircles;
        }
        #endregion
        public void updateCircles()
        {
            for (int i = 0; i < page.allCircles[frame].Count; i++)
            {
                page.allCircles[frame][i].radius = page.allCircles[frame][i].radius * scale.Y;
                page.allCircles[frame][i].center = this.Position + page.allCircles[frame][i].center * scale.Y;
            }
          

            
        }
        //movment if player in air
        public void airMoves()
        {
            //set fall frame to 0(1 frame animation)
            setframe(0);
            //sets buffer to fall
            buffdata = "fall";
            //gravity
            if (velocity.Y < 8)
            {
                velocity.Y += 0.2f;
            }
            //sets horizontal move to zero(if no key is pressed)
            velocity.X = 0;
            //move right
            if (BaseKey.Right())
            {
                velocity.X += 3;
            }
            //move left
            if (BaseKey.Left())
            {
                velocity.X -= 3;
            }
            //jump if not jumed
            if (BaseKey.Up() && (!jumped))
            {
                buffdata = "jump";
            }

        }
        //check if buffered move and current move is both air move or both not an air move
        public bool buffCheck()
        {
            return Info.DInfo [Type][data].IsAirMove * Info.DInfo [Type][buffdata].IsAirMove != 2;
        }
        //movment if player on stage
        public void groundMoves()
        {
            //buffer to fall
            buffdata = "stand"; 
            //move left
            if (BaseKey.Left())
            {
                buffdata = "run";
                buffside = false;
                }
            //move right
            if (BaseKey.Right())
            {
                buffdata = "run";
                buffside = true;
            }
            //jump if not jumped
            if (BaseKey.Up() && (!jumped))
            {
                buffdata = "jump";
            }
            //checks if mario isnt on stage
            if ((Position.X <= (G.currentMap.width - G.stageWidth) / 2 || Position.X >= (G.currentMap.width + G.stageWidth) / 2))
            {
                //sets mario ata to air
                air = true;
                setframe(0);
                velocity = Vector2.Zero;
            }
            //change data without buffering for stand and run and change side in this data
            if ((data == "stand" || data == "run") && (data != buffdata || side != buffside))
            {
                data = buffdata;
                setframe(0);
            }

        }
        //update all mario data
        public override void update()
        {
            //check where is mario(air or on stage) and buffer data
            switch (air)
            {
                case (true):
                    {
                        airMoves();
                        //check if mario is about to land on stage 
                        if (velocity.Y + Position.Y > G.currentMap.height / 2 && Position.Y < G.currentMap.height / 2 && 
                           (Position.X > (G.currentMap.width - G.stageWidth) / 2 && Position.X < (G.currentMap.width + G.stageWidth) / 2))
                        {
                            velocity = new Vector2(0, G.currentMap.height / 2 - Position.Y);
                        }
                        //check id mario land and change data to landing(the end of jump animation)
                        if((Position.X > (G.currentMap.width - G.stageWidth) / 2 && Position.X < (G.currentMap.width + G.stageWidth) / 2) &&
                           (Position.Y == G.currentMap.height / 2))
                        {
                            data = "jump";
                            setframe(2);
                            velocity = Vector2.Zero;
                            air = false;
                        }
                        break;
                    }
                case (false):
                    {
                        groundMoves();
                        break;
                    }
            }
            //change mario side buffer
            if (BaseKey.Left()) { buffside = false; }
            if (BaseKey.Right()){ buffside = true; }

            //checks marios current data
            switch (data)
            {
                case ("run"):
                    {
                        //movment to sides
                        if (!side)
                        {
                            velocity = -Vector2.UnitX*2 ;
                        }
                        if (side)
                        {
                            velocity = Vector2.UnitX *2;
                        }
                        jumped = false;
                        break;
                    }
                case ("jump"):
                    {
                        //check if jumped (the other option to be in jump is while landing)
                        if (!jumped)
                        {
                            //jump
                            velocity =  -Vector2.UnitY * 7;
                            jumped = true;
                            air = true;
                        }
                     
                        break;
                    }
                
                case ("stand"):
                    {
                        //position.Y on stage and change data to standing
                        Position = new Vector2(Position.X, G.currentMap.height / 2);
                        velocity = Vector2.Zero;
                        jumped = false;
                        air = false;
                        break;
                    }
              
            }

            //add position to velocity
            Position += velocity;

            //change buffered data and buffer side
            if (frame == 0)
            {
                data = buffdata;
                side = buffside;
            }
   
            base.update();
        }
        public void collisionCheck(Mario obj)
        {
            switch (obj.page.Collide)
            {
                case (0): { break; }
                case (1):
                    {
                        velocity.X =0;
                        break;
                    }
                case (2):
                    {
                        setframe(0);
                        data = "hit";
                        break;
                    }
            }
           


        }


    }

}
