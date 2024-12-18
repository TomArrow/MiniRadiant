﻿using PCRE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace MiniRadiant
{
    // TriggerCleanup isn't a class. I'm merely collecting stuff here that's meant for that function
    // so it's not all in a single file


    public class EntityProperties : Dictionary<string, string> , INotifyPropertyChanged
    {
        public EntityProperties() : base(StringComparer.InvariantCultureIgnoreCase)
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in this)
            {
                sb.Append($"\"{(kvp.Key+'\"').PadRight(10)} \"{kvp.Value}\"\n");
            }
            return sb.ToString();
        }

        static Regex singleEntityParseRegex = new Regex(@"(\s*""([^""]+)""[ \t]+""([^""]+)"")+\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static EntityProperties FromString(string propertiesString)
        {
            MatchCollection matches = singleEntityParseRegex.Matches(propertiesString);
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    EntityProperties props = new EntityProperties();

                    int lineCount = match.Groups[2].Captures.Count;
                    for (int c = 0; c < lineCount; c++)
                    {
                        //Trace.WriteLine($"{match.Groups[2].Captures[c].Value}:{match.Groups[3].Captures[c].Value}");
                        props[match.Groups[2].Captures[c].Value] = match.Groups[3].Captures[c].Value;
                    }
                    return props;
                }
            }
            return null;
        }

        public string String => this.ToString();


        public override bool Equals(object obj)
        {
            EntityProperties other = obj as EntityProperties;
            if (!(other is null))
            {
                if (this.Count == other.Count)
                {
                    foreach (var kvp in this)
                    {
                        if (!other.ContainsKey(kvp.Key) || other[kvp.Key] != kvp.Value)
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            bool firstDone = false;

            string[] keys = this.Keys.ToArray();
            Array.Sort(keys,StringComparer.InvariantCultureIgnoreCase);

            foreach (var key in keys)
            {

                int hereHash = HashCode.Combine(key.GetHashCode(StringComparison.InvariantCultureIgnoreCase), this[key].GetHashCode(StringComparison.InvariantCultureIgnoreCase));
                if (!firstDone)
                {
                    hash = hereHash;
                } else
                {
                    hash = HashCode.Combine(hereHash, hash);
                    firstDone = true;
                }
            }
            return hash;
        }
    }

    public class EntityGroup:INotifyPropertyChanged
    {
        public EntityProperties props { get; set; } = new EntityProperties();
        public List<string> brushTexts { get; set; } = new List<string>();
        public bool moreThanOne => brushTexts.Count > 1;
        public bool merge { get; set; } = false;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class TriggerType
    {
        public bool moveToEnd { get; set; }
        public string name { get; set; }

    }

    public partial class MainWindow { 
        

        public ObservableCollection<EntityGroup> entityGroups = new ObservableCollection<EntityGroup>();


        private bool classIsTrigger(string name)
        {
            return name.StartsWith("df_trigger_", StringComparison.InvariantCultureIgnoreCase) || name.StartsWith("trigger_", StringComparison.InvariantCultureIgnoreCase);
        }

        string triggersMatchRegex = @"\{(?<properties>[^\{\}]+)(?<brushes>(?:\{(?:[^\{\}]+|(?R))*\}(?:[^\{\}]+))*)\s*\}";

        private void ParseEntities()
        {
            entityGroups.Clear();

            var entityMatches = PcreRegex.Matches(mapFileData, triggersMatchRegex);
            foreach (var entityMatch in entityMatches)
            {
                string propertiesText = entityMatch.Groups["properties"].Value;
                string brushesText = entityMatch.Groups["brushes"].Value;

                EntityProperties props = EntityProperties.FromString(propertiesText);

                if (props.ContainsKey("classname") && classIsTrigger(props["classname"]))
                {
                    EntityGroup newGroup = null;
                    bool found = false;
                    foreach (var group in entityGroups)
                    {
                        if (group.props.Equals(props))
                        {
                            group.brushTexts.Add(brushesText);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        newGroup = new EntityGroup();
                        newGroup.props = props;
                        newGroup.brushTexts.Add(brushesText);
                        entityGroups.Add(newGroup);
                    }

                }
            }

            triggersList.ItemsSource = entityGroups;
        }

        class EntityGroupProcessingData
        {
            public bool alreadyDone;
        }

        private string ProcessTriggerChanges(string mapText)
        {

            SortedDictionary<string,string> defragCourseDataForSorting = new SortedDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            Dictionary<EntityGroup, EntityGroupProcessingData> procData = new Dictionary<EntityGroup, EntityGroupProcessingData>();

            StringBuilder endMapSB = new StringBuilder();
            StringBuilder afterEndMapSB = new StringBuilder();

            mapText = PcreRegex.Replace(mapText,triggersMatchRegex,(entityMatch) => {

                string propertiesText = entityMatch.Groups["properties"].Value;
                string brushesText = entityMatch.Groups["brushes"].Value;

                EntityProperties props = EntityProperties.FromString(propertiesText);

                if (props.ContainsKey("classname") && classIsTrigger(props["classname"]))
                {
                    EntityGroup grp = null;
                    bool found = false;
                    foreach (var group in entityGroups)
                    {
                        if (group.props.Equals(props))
                        {
                            found = true;
                            grp = group;
                            break;
                        }
                    }
                    if (!found)
                    {
                        MessageBox.Show($"{props.ToString()} not found in registered triggers while processing! Weird!");
                        return entityMatch.Value;
                    }

                    if (!procData.ContainsKey(grp))
                    {
                        procData[grp] = new EntityGroupProcessingData();
                    }
                    EntityGroupProcessingData procDataHere = procData[grp];

                    string retVal = entityMatch.Value;
                    if (grp.merge)
                    {
                        if (procDataHere.alreadyDone)
                        {
                            retVal= "";
                        } else
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("\n{");
                            sb.Append($"\n{propertiesText}\n");
                            foreach(var brushText in grp.brushTexts)
                            {
                                sb.Append($"\n{brushText}\n");
                            }
                            sb.Append("}\n");
                            procDataHere.alreadyDone = true;
                            retVal= sb.ToString();
                        }
                    }
                    if (sortDefragCourses && props["classname"].Equals("df_trigger_finish", StringComparison.InvariantCultureIgnoreCase) && props.ContainsKey("message"))
                    {
                        defragCourseDataForSorting[props["message"]] = retVal;
                        retVal = "";
                    }
                    else if (defragFinishTriggerLast && props["classname"].Equals("df_trigger_finish", StringComparison.InvariantCultureIgnoreCase))
                    {
                        afterEndMapSB.Append($"\n{retVal}\n");
                        retVal = "";
                    }
                    else 
                    {

                        // Put at end?
                        foreach (TriggerType triggerType in triggerTypes)
                        {
                            if (triggerType.name.Equals(props["classname"], StringComparison.InvariantCultureIgnoreCase) && triggerType.moveToEnd)
                            {
                                endMapSB.Append($"\n{retVal}\n");
                                retVal = "";
                                break;
                            }
                        }
                    }
                    return retVal;

                }

                return entityMatch.Value;
            });

            if(defragCourseDataForSorting.Count > 0)
            {
                foreach(var item in defragCourseDataForSorting)
                {
                    if (defragFinishTriggerLast)
                    {
                        afterEndMapSB.Append($"\n{item.Value}\n");
                    } else
                    {
                        endMapSB.Append($"\n{item.Value}\n");
                    }
                }
            }

            if(endMapSB.Length > 0 || afterEndMapSB.Length > 0)
            {
                return $"{mapText}{endMapSB.ToString()}{afterEndMapSB.ToString()}";
            } else
            {
                return mapText;
            }

        }

        HashSet<string> spOnlyEnts = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { "func_train", "misc_ns_turret", "misc_panel_turret", "misc_sentry_turret", "misc_turret", "path_corner","point_combat","ref_tag","target_scriptrunner", "target_activate", "target_autosave","target_secret", "target_interest", "target_position", "target_relay", "trigger_location","target_deactivate" };
        Dictionary<string,string> spMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
            { "func_static" ,"func_group"},
            { "func_breakable" ,"func_group"},
            { "func_usable" ,"func_group"},
            { "func_door" ,"func_group"},
        };

        Regex commonwtfregex = new Regex(@"\scommon/WTF\s",RegexOptions.Compiled | RegexOptions.IgnoreCase);
        Regex commonoriginregex = new Regex(@"\scommon/origin\s",RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private string ProcessSPtoMPCleanup(string mapText)
        {

            SortedDictionary<string,string> defragCourseDataForSorting = new SortedDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            Dictionary<EntityGroup, EntityGroupProcessingData> procData = new Dictionary<EntityGroup, EntityGroupProcessingData>();

            StringBuilder endMapSB = new StringBuilder();
            StringBuilder afterEndMapSB = new StringBuilder();

            mapText = PcreRegex.Replace(mapText,triggersMatchRegex,(entityMatch) => {

                string propertiesText = entityMatch.Groups["properties"].Value;
                string brushesText = entityMatch.Groups["brushes"].Value;

                EntityProperties props = EntityProperties.FromString(propertiesText);

                bool modded = false;

                brushesText = commonwtfregex.Replace(brushesText, (a) => { // disgusting way of doing this but oh well!
                    modded = true;
                    return " system/caulk "; 
                });
                brushesText = commonoriginregex.Replace(brushesText, (a) => { // disgusting way of doing this but oh well!
                    modded = true;
                    return " system/nodraw "; 
                });

                if (props.ContainsKey("classname"))
                {
                    string classname = props["classname"];
                    if (classname.StartsWith("NPC_", StringComparison.OrdinalIgnoreCase) || classname.StartsWith("waypoint", StringComparison.OrdinalIgnoreCase) || classname.StartsWith("trigger_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!classname.Equals("trigger_hurt", StringComparison.OrdinalIgnoreCase) && !classname.Equals("trigger_push", StringComparison.OrdinalIgnoreCase))
                        {

                            return "";
                        }
                    } else if (classname.StartsWith("misc_model", StringComparison.OrdinalIgnoreCase))
                    {
                        if (props.ContainsKey("model"))
                        {
                            props["old_classname"] = classname;
                            props["classname"] = classname = "misc_model";
                            string spawnflagsString = props.ContainsKey("spawnflags") ? props["spawnflags"] : "0";
                            int spawnflags = 0;
                            if(!int.TryParse(spawnflagsString,out spawnflags)){
                                spawnflags = 0;
                            }
                            spawnflags |= 2; // clip it
                            props["spawnflags"] = spawnflags.ToString();
                            modded = true;
                        }
                        else
                        {
                            return "";
                        }
                    }else if (classname.StartsWith("target_speaker", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!props.ContainsKey("noise"))
                        {
                            return "";
                        }
                    } else if (spOnlyEnts.Contains(classname))
                    {
                        return "";
                    } else if (spMappings.ContainsKey(classname))
                    {
                        props["old_classname"] = classname;
                        props["classname"] = classname = spMappings[classname];
                        modded = true;
                    }
                }

                if (modded)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("\n{");
                    sb.Append($"\n{props.ToString()}\n");
                    sb.Append($"\n{brushesText}\n");
                    sb.Append("}\n");
                    return sb.ToString();
                }

                return entityMatch.Value;
            });


            return mapText;

        }

    }

}
