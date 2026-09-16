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
    //creates a circle data 
    class Circle
    {
        public Vector2 center;
        public float radius;
        public Circle(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
    }
    class Page
    {
        #region data
        //circles that deals damage sorted by frames
        public SortedList<int,List<Circle>> hitbox = new SortedList<int, List<Circle>>();
        //circles that takes damage sorted by frames
        public SortedList<int, List<Circle>> hurtbox = new SortedList<int, List<Circle>>();
        //all circles sorted by frames
        public SortedList<int, List<Circle>> allCircles = new SortedList<int, List<Circle>>();
        //texture
        public Texture2D Tex { get; private set; }
        //texture of mask
        public Texture2D maskTex { get; private set; }
        //list of handles of texture postion
        public List<Vector2> Orgs { get; private set; } = new List<Vector2>();
        //list of rectangles of anmation(frames)
        public List<Rectangle> Recs { get; private set; } = new List<Rectangle>();
        //animation speed
        public int Rate { get; private set; }
        //checks if air move
        public int IsAirMove { get; private set; }
        //checks if attack move
        public int Collide { get; private set; }
        #endregion
        //constructor for page
        public Page(Folder Folder, string file)
        {
            //set rate
            Rate = Info.Files[file][0];
            //set move airtype
            IsAirMove = Info.Files[file][1];
            //set move collision type
            Collide = Info.Files[file][2]/10;
            
            #region tex and color ary
            //set texture and put last line in color array
            Tex = G.content.Load<Texture2D>(Folder.ToString() + "/" + file.ToString());
            Color[] c = new Color[Tex.Width];
            Tex.GetData<Color>(0,new Rectangle(0, Tex.Height - 1, Tex.Width, 1), c, 0, c.Length);
            #endregion

            #region Dividers
            //find black points in animation
            List<int> divider = new List<int>();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == Color.Black)
                {
                    divider.Add(i);
                }
            }
            #endregion
            #region Rectangles
            //set rectangles from black poonts
            for (int i = 0; i < divider.Count - 2; i += 2)
            {
                Rectangle r = new Rectangle(divider[i], 0,divider[i + 2] - divider[i], Tex.Height - 2);
                Recs.Add(r);
            }
            #endregion
            #region Origins
            //Set origins from black points
            for (int i = 1; i < divider.Count - 1; i += 2)
            {
                Vector2 v = new Vector2(divider[i] - divider[i - 1], Tex.Height - 2);
                Orgs.Add(v);
            }

            #endregion

            if(Folder.ToString()!="Stages")
            {
                maskTex = G.content.Load<Texture2D>(Folder.ToString() + ".mask/" + file.ToString());
                Color[] maskcol = new Color[maskTex.Width * maskTex.Height];
                maskTex.GetData<Color>(maskcol);
                Color hurtColor = maskcol[0];
                List<Vector2> hurtpnt = new List<Vector2>();
                Color hitColor = maskcol[1];
                List<Vector2> hitpnt = new List<Vector2>();

                //indetify points by colors and sort them
                int index;
                for (int row = 0; row < maskTex.Height; row++)
                {
                    for (int col = 2; col < maskTex.Width; col++)
                    {
                    
                        index = col + row * maskTex.Width;
                        if (maskcol[index] == hurtColor)
                        {
                            hurtpnt.Add(new Vector2(col, row));
                            
                        }
                        if (maskcol[index] == hitColor && hitColor != Color.White)
                        {
                            hitpnt.Add(new Vector2(col, row));
                        }
                    }
                }
                //fills hurtbox and hitbox
                for (int i = 0; i < Recs.Count; i++)
                {
                    for (int j = 0; j < hurtpnt.Count; j += 2)
                    {

                        hurtbox[i].Add(new Circle(hurtpnt[j], (hurtpnt[j + 1] - hurtpnt[j]).Length()));
                        allCircles[i].Add(new Circle(hurtpnt[j], (hurtpnt[j + 1] - hurtpnt[j]).Length()));

                    }
                    if (hitpnt.Count != 0)
                    {
                        for (int j = 0; j < hitpnt.Count; j += 2)
                        {

                            hitbox[i].Add(new Circle(hitpnt[j], (hitpnt[j + 1] - hitpnt[j]).Length()));
                            allCircles[i].Add(new Circle(hitpnt[j], (hitpnt[j + 1] - hitpnt[j]).Length()));

                        }
                    }
                }
            }
          
            makeTranBg();

            #region Mask
           
            #endregion
        }

        //makes background of animation transparent
        void makeTranBg()
        {
            //create array doe texture
            Color[] c = new Color[Tex.Width * Tex.Height];
            Tex.GetData<Color>(c);
            Color trans = c[0];
            //checks if color is background
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == trans)
                {
                    c[i] = Color.Transparent;
                }
            }
            Tex.SetData<Color>(c);
        }
        
    }
}
