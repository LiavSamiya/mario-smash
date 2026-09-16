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
    //check collision between objects
    class Collisions:Mario
    {
        
       
        //constructor for collision
        public Collisions(Vector2 position, float rotation, Vector2 scale, float layerDepth):
            base(position, rotation, scale, layerDepth)
        {
            Game1.event_update += Collide;

        }
        //check for each mario if he collide
        public void Collide()
        {
            //checks all opponents
            for (int i = 0; i < G.allMario.Count; i++)
            {
                if(G.allMario[i].Position!=Position)
                { 
                    //checks all opponent's hurtbox
                    for (int j = 0; j < G.allMario[i].page.hurtbox.Count; j++)
                    {
                        //checks hurtbox
                        for (int k = 0; k < page.hurtbox.Count; k++)
                        {
                            float centersDifference;
                            Page opponent = G.allMario[i].page;
                            centersDifference = Vector2.Distance(opponent.hurtbox[G.allMario[i].frame][j].center, page.hurtbox[frame][k].center);

                            if (centersDifference <= (opponent.hurtbox[G.allMario[i].frame][j].radius + page.hurtbox[frame][k].radius))
                            {
                                velocity = Vector2.Zero;
                            }
                        }
                    }
                    //checks all opponent's hitbox
                    for (int j = 0; j < G.allMario[i].page.hitbox.Count; j++)
                    {
                        //checks all circles
                        for (int k = 0; k < page.allCircles.Count; k++)
                        {
                            float centersDifference;
                            Page opponent = G.allMario[i].page;
                            centersDifference = Vector2.Distance(opponent.hitbox[G.allMario[i].frame][j].center, page.allCircles[frame][k].center);
                           
                                if (centersDifference <= (opponent.hitbox[G.allMario[i].frame][j].radius + page.allCircles[frame][k].radius))
                                {
                                knockback = new Vector2(opponent.Collide);
                                }
                   
                        }
                    }
                   
                }
              
                
                       
                
            }
                    
            
        }
       
       

    }
}

