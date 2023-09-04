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

        static Commands commands;
        static Composite composite;
        static FunctionEntity checkpoint;

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

            commands = new Commands(pathToAI + "/DATA/ENV/PRODUCTION/" + LevelToAddTo + "/WORLD/COMMANDS.PAK");
            composite = commands.GetComposite(CutsceneName);
            if (composite != null)
                commands.Entries.Remove(composite);
            composite = new Composite(CutsceneName);

            checkpoint = composite.AddFunction(FunctionType.DebugCheckpoint);
            ((cString)checkpoint.AddParameter("section", DataType.STRING).content).value = "Start " + CutsceneName;

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

            List<string> initialisedCharacters = new List<string>();

            CAGEAnimation prevAnimEnt = null;
            foreach (KeyValuePair<int, List<string[]>> shot in animShots.OrderBy(x => x.Key))
            {
                CAGEAnimation animEnt = null;

                foreach (string[] shotAnim in shot.Value)
                {
                    animEnt = new CAGEAnimation();
                    ((cString)animEnt.AddParameter("AnimationSet", DataType.STRING).content).value = shotAnim[0];
                    ((cString)animEnt.AddParameter("Animation", DataType.STRING).content).value = shotAnim[1];

                    /*
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
                    */

                    if (!initialisedCharacters.Contains(shotAnim[0]))
                    {
                        FunctionEntity triggerBind = GetCharacterTrigger(shotAnim[0]);
                        triggerBind.AddParameterLink("bound_trigger", animEnt, "apply_start");
                        initialisedCharacters.Add(shotAnim[0]);
                    }

                    composite.functions.Add(animEnt);
                }

                if (prevAnimEnt != null)
                    prevAnimEnt.AddParameterLink("finished", animEnt, "apply_start");

                prevAnimEnt = animEnt;
            }

            commands.Entries.Add(composite);
            FunctionEntity instance = commands.EntryPoints[0].AddFunction(composite);
            cTransform instancePosition = (cTransform)instance.AddParameter("position", DataType.TRANSFORM).content;
            instancePosition.position = new Vector3(86.3651000f, 1.7223700f, -76.2859000f); //Spawn_FromTechHub
            instancePosition.position -= new Vector3(-50.0000000f, 12.8468000f, 42.0000000f); //MISSIONS offset
            commands.Save();
        }

        private static FunctionEntity GetCharacterTrigger(string AnimationSet)
        {
            FunctionEntity triggerBind = new FunctionEntity(FunctionType.TriggerBindCharacter);
            triggerBind.AddParameter("bound_trigger", DataType.INTEGER);

            string characterCompositePath = "";
            switch (AnimationSet)
            {
                case "MARLOW":
                    characterCompositePath = "ARCHETYPES\\NPCS\\ACTORS\\MARLOW";
                    break;
                case "WAIT":
                    characterCompositePath = "ARCHETYPES\\NPCS\\ACTORS\\WAITS";
                    break;
                case "RIPLEY":
                    characterCompositePath = "ARCHETYPES\\CUTSCENE_ACTORS\\RIPLEY_3P_V4";
                    break;
                default:
                    throw new Exception("Unknown character: " + AnimationSet);
            }
            Composite characterComposite = commands.GetComposite(characterCompositePath);

            FunctionEntity character = composite.functions.FirstOrDefault(o => o.function == characterComposite.shortGUID);
            if (character != null)
                throw new Exception("Expected there to be no character already.");

            character = composite.AddFunction(characterComposite);
            checkpoint.AddParameterLink("on_checkpoint", character, "spawn_npc");
            character.AddParameterLink("finished_spawning", triggerBind, "trigger");

            triggerBind.AddParameterLink("characters", character, "npc_reference");
            return triggerBind;
        }
    }
}
