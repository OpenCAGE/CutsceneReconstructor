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

        [STAThread]
        static void Main()
        {
            //CombineTextures();
            //CombineModels();
            CombineCommands();

            //TODO: sort out reds
        }

        private static void CombineTextures()
        {
            List<string> files = Directory.GetFiles(pathToAI + "/DATA/ENV/PRODUCTION/", "LEVEL_TEXTURES.ALL.PAK", SearchOption.AllDirectories).ToList<string>();
            //files.Add(pathToAI + "/DATA/ENV/GLOBAL/WORLD/GLOBAL_TEXTURES.ALL.PAK");
            Textures texturesNew = new Textures("LEVEL_TEXTURES.ALL.PAK");
            foreach (string file in files)
            {
                Textures textures = new Textures(file);
                for (int i = 0; i < textures.Entries.Count; i++)
                {
                    Textures.TEX4 texture = texturesNew.Entries.FirstOrDefault(o => o.Name == textures.Entries[i].Name);
                    if (texture != null) continue;
                    texturesNew.Entries.Add(textures.Entries[i]);
                }
            }
            texturesNew.Save();
        }

        private static void CombineModels()
        {
            List<string> files = Directory.GetFiles(pathToAI + "/DATA/ENV/PRODUCTION/", "LEVEL_MODELS.PAK", SearchOption.AllDirectories).ToList<string>();
            Models modelsNew = new Models("LEVEL_MODELS.PAK");
            foreach (string file in files)
            {
                Models models = new Models(file);
                for (int i = 0; i < models.Entries.Count; i++)
                {
                    Models.CS2 model = modelsNew.Entries.FirstOrDefault(o => o.Name == models.Entries[i].Name);
                    if (model != null) continue;
                    modelsNew.Entries.Add(models.Entries[i]);
                }
            }
            modelsNew.Save();
        }

        private static void CombineCommands()
        {
            List<string> files = Directory.GetFiles(pathToAI + "/DATA/ENV/PRODUCTION/", "COMMANDS.PAK", SearchOption.AllDirectories).ToList<string>();
            Commands commandsNew = new Commands("COMMANDS.PAK");
            foreach (string file in files)
            {
                Commands commands = new Commands(file);
                foreach (Composite comp in commands.Entries)
                {
                    Composite refComp = commandsNew.Entries.FirstOrDefault(o => o.name == comp.name);
                    if (refComp == null)
                    {
                        commandsNew.Entries.Add(comp);
                    }
                    else
                    {
                        //TODO:
                        // Some things to solve here, E.G. PROXIES will resolve incorrectly as the root will have changed.
                        // Also, resource references will be wrong. We should store info about the model/material they point to, then recalculate the indexes.
                        // ... what do we do about MVR?


                        continue;

                        foreach (FunctionEntity func in comp.functions)
                        {
                            FunctionEntity refFunc = refComp.functions.FirstOrDefault(o => o.shortGUID == func.shortGUID);
                            if (refFunc == null)
                            {
                                refComp.functions.Add(func);
                            }
                            else
                            {
                                foreach (Parameter param in func.parameters)
                                {
                                    //add missing params
                                }
                                foreach (EntityLink link in func.childLinks)
                                {
                                    //add missing links
                                }

                                //TODO: add extra data for trigger seq and cageanim
                            }
                        }
                        foreach (VariableEntity var in comp.variables)
                        {
                            VariableEntity refVar = refComp.variables.FirstOrDefault(o => o.shortGUID == var.shortGUID);
                            if (refVar == null)
                            {
                                refComp.variables.Add(var);
                            }
                            else
                            {
                                foreach (Parameter param in var.parameters)
                                {
                                    //add missing params
                                }
                                foreach (EntityLink link in var.childLinks)
                                {
                                    //add missing links
                                }
                            }
                        }
                        foreach (ProxyEntity prox in comp.proxies)
                        {
                            ProxyEntity refProx = refComp.proxies.FirstOrDefault(o => o.shortGUID == prox.shortGUID);
                            if (refProx == null)
                            {
                                refComp.proxies.Add(prox);
                            }
                            else
                            {
                                foreach (Parameter param in prox.parameters)
                                {
                                    //add missing params
                                }
                                foreach (EntityLink link in prox.childLinks)
                                {
                                    //add missing links
                                }
                            }
                        }
                        foreach (OverrideEntity ovrr in comp.overrides)
                        {
                            OverrideEntity refOvrr = refComp.overrides.FirstOrDefault(o => o.shortGUID == ovrr.shortGUID);
                            if (refOvrr == null)
                            {
                                refComp.overrides.Add(ovrr);
                            }
                            else
                            {
                                foreach (Parameter param in ovrr.parameters)
                                {
                                    //add missing params
                                }
                                foreach (EntityLink link in ovrr.childLinks)
                                {
                                    //add missing links
                                }
                            }
                        }
                    }
                }
            }
            commandsNew.Save();
        }
    }
}
