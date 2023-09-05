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

        static string CutsceneName = "AYZ_SC02";
        static string LevelToAddTo = "BSP_TORRENS";

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

                string shot_number_s = data[1].Split('_')[2];
                if (shot_number_s[0] == 's') shot_number_s = shot_number_s.Substring(1);
                if (shot_number_s[0] == 'h') shot_number_s = shot_number_s.Substring(1);
                int shot_number = Convert.ToInt32(shot_number_s) / 10;

                if (!animShots.ContainsKey(shot_number))
                    animShots.Add(shot_number, new List<string[]>());
                animShots[shot_number].Add(data);
            }

            Dictionary<string, FunctionEntity> previousAnimEnts = new Dictionary<string, FunctionEntity>();
            foreach (KeyValuePair<int, List<string[]>> shot in animShots.OrderBy(x => x.Key))
            {
                foreach (string[] shotAnim in shot.Value)
                {
                    if (shotAnim[0] == "AXEL") continue; //TEMP!
                    if (shotAnim[0] == "CAT") continue; //TEMP!
                    if (shotAnim[0] == "RIPLEY") continue; //TEMP!

                    FunctionEntity animEnt = composite.AddFunction(FunctionType.CMD_PlayAnimation);
                    ((cString)animEnt.AddParameter("AnimationSet", DataType.STRING).content).value = shotAnim[0];
                    ((cString)animEnt.AddParameter("Animation", DataType.STRING).content).value = shotAnim[1];

                    ((cInteger)animEnt.AddParameter("shot_number", DataType.INTEGER).content).value = shot.Key;
                    ((cFloat)animEnt.AddParameter("ConvergenceTime", DataType.FLOAT).content).value = 0.2f; //0 on others
                    ((cBool)animEnt.AddParameter("AllowCollision", DataType.BOOL).content).value = false;
                    ((cBool)animEnt.AddParameter("AllowGravity", DataType.BOOL).content).value = false;
                    //((cBool)animEnt.AddParameter("FullCinematic", DataType.BOOL).content).value = true;
                    ((cBool)animEnt.AddParameter("LocationConvergence", DataType.BOOL).content).value = true;
                    ((cBool)animEnt.AddParameter("NoIK", DataType.BOOL).content).value = true;
                    ((cBool)animEnt.AddParameter("OrientationConvergence", DataType.BOOL).content).value = true;
                    ((cBool)animEnt.AddParameter("PlayerDrivenAnimView", DataType.BOOL).content).value = false;
                    //((cBool)animEnt.AddParameter("StartInstantly", DataType.BOOL).content).value = true;

                    FunctionEntity triggerBind = GetCharacterTrigger(shotAnim[0]);
                    triggerBind.AddParameterLink("bound_trigger", animEnt, "apply_start");

                    if (!previousAnimEnts.ContainsKey(shotAnim[0]))
                    {
                        previousAnimEnts.Add(shotAnim[0], animEnt);
                    }
                    else
                    {
                        previousAnimEnts[shotAnim[0]].AddParameterLink("finished", triggerBind, "trigger");
                        previousAnimEnts[shotAnim[0]] = animEnt;
                    }
                }
            }

            foreach (KeyValuePair<string, FunctionEntity> finalAnimFuncs in previousAnimEnts) 
            {
                finalAnimFuncs.Value.AddParameterLink("finished", GetCharacter(finalAnimFuncs.Key, out bool _), "despawn_npc");
                finalAnimFuncs.Value.AddParameterLink("finished", GetCharacter(finalAnimFuncs.Key, out bool _), "hide_npc");
                finalAnimFuncs.Value.AddParameterLink("finished", GetCharacter(finalAnimFuncs.Key, out bool _), "deleted");
            }

            commands.Entries.Add(composite);
            FunctionEntity instance = commands.EntryPoints[0].AddFunction(composite);
            cTransform instancePosition = (cTransform)instance.AddParameter("position", DataType.TRANSFORM).content;

            //GOOD POSITIONS FOR HAB_SHOPPING:
            //instancePosition.position = new Vector3(85.3651000f, 1.7223700f, -76.2859000f); //Spawn_FromTechHub
            //instancePosition.position -= new Vector3(-50.0000000f, 12.8468000f, 42.0000000f); //MISSIONS offset

            //TORRENS:
            instancePosition.position = new Vector3(7.904f, 7.6f, -14.97f); 

            commands.Save();
        }

        private static FunctionEntity GetCharacterTrigger(string AnimationSet)
        {
            FunctionEntity triggerBind = composite.AddFunction(FunctionType.TriggerBindCharacter);
            triggerBind.AddParameter("bound_trigger", DataType.INTEGER);

            FunctionEntity character = GetCharacter(AnimationSet, out bool didCreate);
            if (didCreate)
            {
                checkpoint.AddParameterLink("on_checkpoint", character, "spawn_npc");
                character.AddParameterLink("finished_spawning", triggerBind, "trigger");
            }

            triggerBind.AddParameterLink("characters", character, "npc_reference");
            return triggerBind;
        }

        private static FunctionEntity GetCharacter(string AnimationSet, out bool didCreate)
        {
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
                case "TAYLOR":
                    characterCompositePath = "ARCHETYPES\\NPCS\\ACTORS\\TAYLOR";
                    break;
                case "SAMUELS":
                    characterCompositePath = "ARCHETYPES\\NPCS\\ACTORS\\SAMUELS";
                    break;
                case "RICARDO":
                    characterCompositePath = "ARCHETYPES\\NPCS\\ACTORS\\RICARDO";
                    break;
                case "VERLAINE":
                    characterCompositePath = "ARCHETYPES\\NPCS\\ACTORS\\VERLAINE";
                    break;
                case "CONNOR":
                    characterCompositePath = "ARCHETYPES\\NPCS\\ACTORS\\CONNOR";
                    break;
                default:
                    throw new Exception("Unknown character: " + AnimationSet);
            }
            Composite characterComposite = commands.GetComposite(characterCompositePath);

            Console.WriteLine(AnimationSet);
            FunctionEntity character = composite.functions.FirstOrDefault(o => o.function == characterComposite.shortGUID);
            if (character == null)
            {
                character = composite.AddFunction(characterComposite);
                didCreate = true;
            }
            else
            {
                didCreate = false;
            }

            return character;
        }
    }
}
