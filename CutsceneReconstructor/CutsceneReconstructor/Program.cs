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
using CATHODE.Scripting.Internal;
using System.Collections;

namespace CombineLevels
{
    internal static class Program
    {
        static string pathToAI = "G:\\SteamLibrary\\steamapps\\common\\Alien Isolation";

        static string CutsceneName = "AYZ_SC24";
        static string LevelToAddTo = "HAB_SHOPPINGCENTRE";

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

            Composite composite = new Composite(CutsceneName);

            AnimationStrings animStrings = new AnimationStrings(animStringPath);
            Dictionary<int, List<string[]>> animShots = new Dictionary<int, List<string[]>>();
            foreach (KeyValuePair<uint, string> str in animStrings.Entries)
            {
                if (!str.Value.Contains(CutsceneName)) continue;

                string[] data = str.Value.Split('\\');
                if (data.Length != 2) continue;
                data[1] = data[1].ToLower();

                int shot_number = Convert.ToInt32(data[1].Split(new[] { "sh" }, StringSplitOptions.None)[1]) / 10;
                if (!animShots.ContainsKey(shot_number))
                    animShots.Add(shot_number, new List<string[]>());
                animShots[shot_number].Add(data);
            }

            foreach (KeyValuePair<int, List<string[]>> shot in animShots.OrderBy(x => x.Key))
            {
                foreach (string[] shotAnim in shot.Value)
                {
                    CAGEAnimation animEnt = new CAGEAnimation();
                    ((cString)animEnt.AddParameter("AnimationSet", DataType.STRING).content).value = shotAnim[0];
                    ((cString)animEnt.AddParameter("Animation", DataType.STRING).content).value = shotAnim[1];
                    ((cInteger)animEnt.AddParameter("shot_number", DataType.INTEGER).content).value = shot.Key;
                    ((cInteger)animEnt.AddParameter("ConvergenceTime", DataType.INTEGER).content).value = 0;
                    ((cBool)animEnt.AddParameter("AllowCollision", DataType.BOOL).content).value = false;
                    ((cBool)animEnt.AddParameter("AllowGravity", DataType.BOOL).content).value = false;
                    ((cBool)animEnt.AddParameter("FullCinematic", DataType.BOOL).content).value = true;
                    ((cBool)animEnt.AddParameter("LocationConvergence", DataType.BOOL).content).value = true;
                    ((cBool)animEnt.AddParameter("NoIK", DataType.BOOL).content).value = true;
                    ((cBool)animEnt.AddParameter("OrientationConvergence", DataType.BOOL).content).value = true;
                    ((cBool)animEnt.AddParameter("PlayerDrivenAnimView", DataType.BOOL).content).value = false;
                    ((cBool)animEnt.AddParameter("StartInstantly", DataType.BOOL).content).value = true;
                    composite.functions.Add(animEnt);
                }
            }

            Commands commands = new Commands(pathToAI + "/DATA/ENV/PRODUCTION/" + LevelToAddTo + "/WORLD/COMMANDS.PAK");
            commands.Entries.Add(composite);
            commands.Save();
        }
    }
}
