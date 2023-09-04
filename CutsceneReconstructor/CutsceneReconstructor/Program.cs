using CATHODE;
using CATHODE.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;
using Newtonsoft.Json.Linq;
using CathodeLib;

namespace CombineLevels
{
    internal static class Program
    {
        static string pathToAI = "G:\\SteamLibrary\\steamapps\\common\\Alien Isolation";

        static string CutsceneName = "";

        [STAThread]
        static void Main()
        {
            string animStringPath = pathToAI + "/DATA/GLOBAL/ANIM_STRING_DB_DEBUG.BIN";
            if (!File.Exists(animStringPath))
            {
                PAK2 animPAK = new PAK2(pathToAI + "/DATA/GLOBAL/ANIMATION.PAK");
                PAK2.File animFile = animPAK.Entries.FirstOrDefault(o => o.Filename.Contains("ANIM_STRING_DB_DEBUG.BIN"));
                File.WriteAllBytes(animStringPath, animFile.Content);
            }

            AnimationStrings animStrings = new AnimationStrings(animStringPath);
            foreach (KeyValuePair<uint, string> str in animStrings.Entries)
            {

            }
        }
    }
}
