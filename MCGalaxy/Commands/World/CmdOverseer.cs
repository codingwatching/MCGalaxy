/*
    Copyright 2011 MCForge
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Collections.Generic;

namespace MCGalaxy.Commands.World {
    public sealed partial class CmdOverseer : Command {
        public override string name { get { return "Overseer"; } }
        public override string shortcut { get { return "os"; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Builder; } }
        public override bool SuperUseable { get { return false; } }
        public override CommandAlias[] Aliases {
            get { return new[] { new CommandAlias("Realm"), new CommandAlias("MyRealm") }; }
        }
        
        public override void Use(Player p, string message) {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(3);
            string cmd = args[0];
            string arg = args.Length > 1 ? args[1] : "";
            string arg2 = args.Length > 2 ? args[2] : "";
            
            bool mapOnly = !(cmd.CaselessEq("go") || cmd.CaselessEq("map"));
            if (mapOnly && !LevelInfo.IsRealmOwner(p.name, p.level.name)) {
                Player.Message(p, "You may only perform that action on your own map."); return;
            }

            // used to be /os spawn, keep alias for backwards compatibility
            if (cmd.CaselessEq("spawn")) cmd = "setspawn";
            foreach (var subCmd in subCommands) {
                if (!subCmd.Key.CaselessEq(cmd)) continue;
                subCmd.Value.Handler(p, arg, arg2);
                return;
            }
            Help(p);
        }
        
        public override void Help(Player p, string message) {
            foreach (var subCmd in subCommands) {
                if (!subCmd.Key.CaselessEq(message)) continue;
                Player.MessageLines(p, subCmd.Value.Help);
                return;
            }
            Player.Message(p, "Unrecognised command \"{0}\".", message);
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/os [command] [args]");
            Player.Message(p, "%HAllows you to modify and manage your personal realms.");
            Player.Message(p, "%HCommands: %S{0}", subCommands.Keys.Join());
            Player.Message(p, "%HType %T/Help os [command] %Hfor more details");
        }
        
        
        delegate void SubCommandHandler(Player p, string cmd, string value);
        class SubCommand {
            public SubCommandHandler Handler;
            public string[] Help;
            
            public SubCommand(SubCommandHandler handler, string[] help) {
                Handler = handler;
                Help = help;
            }
        }
        
        Dictionary<string, SubCommand> subCommands = new Dictionary<string, SubCommand>() {
            { "blockprops", new SubCommand(HandleBlockProps, blockPropsHelp) },
            { "blockproperties", new SubCommand(HandleBlockProps, blockPropsHelp) },
            { "env", new SubCommand(HandleEnv, envHelp) },
            { "go", new SubCommand(HandleGoto, gotoHelp) },
            { "kick", new SubCommand(HandleKick, kickHelp) },
            { "kickall", new SubCommand(HandleKickAll, kickAllHelp) },
            { "lb", new SubCommand(HandleLevelBlock, levelBlockHelp) },
            { "levelblock", new SubCommand(HandleLevelBlock, levelBlockHelp) },
            { "map", new SubCommand(HandleMap, mapHelp) },
            { "preset", new SubCommand(HandlePreset, presetHelp) },
            { "setspawn", new SubCommand(HandleSpawn, spawnHelp) },
            { "zone", new SubCommand(HandleZone, zoneHelp) },
            { "zones", new SubCommand(HandleZones, zonesHelp) },
        };
        
        
        static string NextLevel(Player p) {
            string level = p.name.ToLower();
            if (LevelInfo.MapExists(level) || LevelInfo.MapExists(level + "00")) {
                // subtract 1, because we accounted for it in above if statement
                for (int i = 2; i < (p.group.OverseerMaps - 1) + 2; i++) {
                    if (LevelInfo.MapExists(p.name.ToLower() + i)) continue;
                    return p.name.ToLower() + i;
                }
                
                Player.Message(p, "You have reached the limit for your overseer maps."); return null;
            }
            return level;
        }

        static string FirstMapName(Player p) {
            /* Returns the proper name of the User Level. By default the User Level will be named
             * "UserName" but was earlier named "UserName00". Therefore the Script checks if the old
             * map name exists before trying the new (and correct) name. All Operations will work with
             * both map names (UserName and UserName00)
             * I need to figure out how to add a system to do this with the players second map.
             */
            if (LevelInfo.MapExists(p.name.ToLower() + "00"))
                return p.name.ToLower() + "00";
            return p.name.ToLower();
        }

        #region Help messages

        static string[] blockPropsHelp = new string[] {
            "%T/os blockprops [id] [action] <args> %H- Manages properties for custom blocks on your map.",
            "%H  See %T/Help blockprops %Hfor a list of actions",
        };
        
        static string[] envHelp = new string[] {
            "%T/os env [fog/cloud/sky/shadow/sun] [hex color] %H- Changes env colors of your map.",
            "%T/os env level [height] %H- Sets the water height of your map.",
            "%T/os env cloudheight [height] %H-Sets cloud height of your map.",
            "%T/os env maxfog %H- Sets the max fog distance in your map.",
            "%T/os env horizon %H- Sets the \"ocean\" block outside your map.",
            "%T/os env border %H- Sets the \"bedrock\" block outside your map.",
            "%T/os env weather [sun/rain/snow] %H- Sets weather of your map.",
            " Note: Shrub, flowers, mushrooms, rope, fire cannot be used for horizon/bedrock.",
            " Note: If no hex or block is given, the default will be used.",
        };
        
        static string[] gotoHelp = new string[] {
            "%T/os goto %H- Teleports you to your first map.",
            "%T/os goto [num] %H- Teleports you to your nth map.",
        };

        static string[] kickHelp = new string[] {
            "%T/os kick [name] %H- Removes that player from your map.",
        };
        
        static string[] kickAllHelp = new string[] {
            "%T/os kickall %H- Removes all other players from your map.",
        };

        static string[] levelBlockHelp = new string[] {
            "%T/os lb [action] <args> %H- Manages custom blocks on your map.",
            "%H  See %T/Help lb %Hfor a list of actions",
        };
        
        static string[] mapHelp = new string[] {
            "%T/os map add [type - default is flat] %H- Creates your map (128x64x128)",
            "%T/os map add [width] [height] [length] [type]",
            "%H  See %T/Help newlvl types %Hfor a list of map types.",
            "%T/os map physics [level] %H- Sets the physics on your map.",
            "%T/os map delete %H- Deletes your map",
            "%T/os map restore [num] %H- Restores backup [num] of your map",
            "%T/os map resize [width] [height] [length] %H- Resizes your map",
            "%T/os map save %H- Saves your map",
            "%T/os map pervisit [rank] %H- Sets the pervisit of you map",
            "%T/os map perbuild [rank] %H- Sets the perbuild of you map",
            "%T/os map texture [url] %H- Sets terrain.png for your map",
            "%T/os map texturepack [url] %H- Sets texture pack .zip for your map",
            "%T/os map [option] <value> %H- Toggles that map option.",
            "%H  See %T/Help map %Hfor a list of map options",
        };
        
        static string[] presetHelp = new string[] {
            "%T/os preset [name] %H- Sets the env settings of your map to that preset's.",
        };
        
        static string[] spawnHelp = new string[] {
            "%T/os setspawn %H- Sets the map's spawn point to your current position.",
        };
        
        static string[] zoneHelp = new string[] {
            "%T/os zone add [name] %H- Allows them to build in your map.",
            "%T/os zone del all %H- Deletes all zones in your map.",
            "%T/os zone del [name] %H- Prevents them from building in your map.",
            "%T/os zone list %H- Shows zones affecting a particular block.",
            "%T/os zone block [name] %H- Prevents them from joining your map.",
            "%T/os zone unblock [name] %H- Allows them to join your map.",
            "%T/os zone blacklist %H- Shows currently blacklisted players.",
        };
        
        static string[] zonesHelp = new string[] {
            "%T/os zones [cmd] [args]",
            "%HManages zones in your map. See %T/Help zone",
        };
        #endregion
    }
}