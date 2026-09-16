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
    //enum for folders
    enum Folder {Stages,mario}

    //create and fills page with data for each animation
    static class Info
    {
        //sorted least with data of each string(file)
        public static SortedList<string, List<int>> Files = new SortedList<string, List<int>>()
        {
            //list{ frame rate,
            //is an air move(0=both;1=ground;2=air)}
            { "run",new List<int>(){9,1,0} },
            { "stand",new List<int>(){6,1,0} },
            { "jump",new List<int>(){4,0,0} },
            { "fall" ,new List<int>(){7,2,0} },
            { "Stage" ,new List<int>(){15,0,0} }
        };
        //full dictionary
        public static Dictionary<Folder, Dictionary<string, Page>> DInfo;

        //fill dictionary in data
        public static void FillDictionary()
        {
            //new big Dictionary
            DInfo = new Dictionary<Folder, Dictionary<string, Page>>();
            //fills big dICTIONARY IN  DATA
            foreach (Folder Fol in Enum.GetValues(typeof(Folder)))
            {
                //small dictionary
                Dictionary<string, Page> fileData = new Dictionary<string, Page>();
                //checks if current file contains in folder
                foreach (string Fil in Files.Keys)
                {
                    //sets directory string by folder and file name
                    string path = Directory.GetCurrentDirectory();
                    path +="/Content/"+ Fol.ToString() + "/" + Fil + ".xnb";
                    //check if directory exists in content
                    if (File.Exists(path))
                    {
                        //fills small dictionary
                        fileData.Add(Fil, new Page(Fol, Fil));
                    }

                }
                //fill big dictionary with small dictionary
                DInfo.Add(Fol, fileData);
            }    
        }
    }
}
