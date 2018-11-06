using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerrariaApi.Server;
using System.Reflection;
using Terraria;
using TShockAPI;
using System.IO;
using Microsoft.Xna.Framework;
using System.Threading;
using Newtonsoft.Json;
using Terraria.Localization;

namespace Mengeria
{
    [ApiVersion(2, 1)]
    public class RpgLevels : TerrariaPlugin
    {
        Random rnd = new Random();
        List<Player> players = new List<Player>();
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public override string Name
        {
            get { return "RPG Levels"; }
        }

        public override string Author
        {
            get { return "Meth"; }
        }

        public override string Description
        {
            get { return "Allows experience to accumulate for mob kills and leveling up."; }
        }

        public RpgLevels(Main game) : base(game)
        {

        }

        protected override void Dispose(bool disposing)
        {
            Save();
            if (disposing)
            {
                ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                
            }
        }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(ExperienceCommand, "xp", "exp"));
            Commands.ChatCommands.Add(new Command(LevelCommand, "level","lvl","lv"));
            Commands.ChatCommands.Add(new Command(GetExperienceCommand, "xpget"));
            Commands.ChatCommands.Add(new Command(StatsCommand, "stats"));
            Commands.ChatCommands.Add(new Command(PlayCommand, "play"));
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
            Load();
        }

        void OnServerJoin(JoinEventArgs e)
        {
            try
            {
                // Get players by name to keep stats. Different slots will be assigned when rejoining.
                Player player = players.FirstOrDefault(p => p.Name == Main.player[e.Who].name);
                if (player == null)
                {
                    players.Add(new Player(e.Who, Main.player[e.Who].name));
                }
                else
                {
                    player.ID = e.Who;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        void OnGetData(GetDataEventArgs e)
        {
            if (!e.Handled)
            {
                int plr = e.Msg.whoAmI;
                using (var reader = new BinaryReader(new MemoryStream(e.Msg.readBuffer, e.Index, e.Length)))
                {
                    Player player = players.FirstOrDefault(p => p.ID == e.Msg.whoAmI);
                    switch (e.MsgID)
                    {
                        
                        case PacketTypes.PlayerDamage:
                            DoPlayerDamage(reader, player);
                            break;
                        case PacketTypes.EffectHeal:
                            DoPlayerHeal(reader, player);
                            break;
                        case PacketTypes.EffectMana:
                            DoPlayerMana(reader, player);
                            break;
                        case PacketTypes.NpcStrike:
                            DoStrikeNPC(reader, player);
                            e.Handled = true;
                            break;
                    }
                }
            }
        }

        private void DoStrikeNPC(BinaryReader reader, Player player)
        {
            Int16 npcId = reader.ReadInt16();
            Int16 dmg = reader.ReadInt16();
            float knockback = reader.ReadSingle();
            byte direction = reader.ReadByte();
            bool critical = reader.ReadBoolean();
            bool secondCrit = rnd.Next(1, 101) <= player.Crit;
            NPC npc = Main.npc[npcId];

            // Negate defense for +dmg
            int addDmg = player.Damage > 0 ? player.Damage + npc.defense / 2 : 0;
            // Make up additional damage for additional crit
            if (!critical && secondCrit)
                addDmg += dmg;
            double aDmg = Main.CalculateDamage((int)addDmg + dmg, npc.ichor ? npc.defense - 20 : npc.defense);
            //double actualDmg = Main.npc[npcId].StrikeNPC((int)idmg, knockback, direction, critical | second true, false);
            
            if (player.HealOnHit > 0)
                TShock.Players[player.ID].Heal(player.HealOnHit);

            npc.StrikeNPC((int)aDmg, knockback, direction, critical | secondCrit ? true: false, false, false);
            if (Main.netMode != 0)
                NetMessage.SendData(28, -1, -1, (NetworkText)null, npc.whoAmI, (float)aDmg, knockback, (float)direction, critical | secondCrit ? 1 : 0, 0, 0);
            TSPlayer.All.SendData(PacketTypes.NpcUpdate,"",npcId);
            //TShock.Players[player.ID].SendMessage($"You Did {(int)aDmg} Damage!",Color.Lime);
            player.GainExperience((int)aDmg);
        }

        private static void DoPlayerHeal(BinaryReader reader, Player player)
        {
            byte playerId = reader.ReadByte();
            Int16 healAmount = reader.ReadInt16();

        }

        private static void DoPlayerMana(BinaryReader reader, Player player)
        {
            byte playerId = reader.ReadByte();
            Int16 manaAmount = reader.ReadInt16();

        }

        private static void DoPlayerDamage(BinaryReader reader, Player player)
        {
            byte playerId = reader.ReadByte();
            byte hitDirection = reader.ReadByte();
            Int16 damage = reader.ReadInt16();
            bool pvp = reader.ReadBoolean();
            bool crit = reader.ReadBoolean();

            // Since we can't override defense, we'll have to mitigate through healing :(
            if (player.Defense > 0)
                TShock.Players[player.ID].Heal(player.Defense);

            //Main.player[player.ID].Hurt(damage, hitDirection, pvp, true, null, crit);
            //NetMessage.SendData(26, -1, player.ID, string.Empty, player.ID, (float)hitDirection, (float)damage, pvp ? 1f : 0f, crit ? 1 : 0);

        }

        private void GetExperienceCommand(CommandArgs args)
        {
            Player player = players.FirstOrDefault(p => p.ID == args.Player.Index);
            int nextLevelXp = player.NextLevel - player.Experience;
            player.GainExperience(nextLevelXp);
        }

        private void ExperienceCommand(CommandArgs args)
        {
            Player player = players.FirstOrDefault(p => p.ID == args.Player.Index);
            args.Player.SendInfoMessage($"{player?.Name} Lv{player?.Level} Points: {player?.SkillPoints}");
            args.Player.SendInfoMessage($"Experience: {player?.Experience:n0}/{player?.NextLevel:n0} ({player?.Experience * 100 / player?.NextLevel:n0}%)");
        }
        private void LevelCommand(CommandArgs args)
        {
            Player player = players.FirstOrDefault(p => p.ID == args.Player.Index);
            if (args.Parameters.Count > 0)
            {
                byte points = 1;
                try
                {
                    if (args.Parameters.Count > 1)
                        points = byte.Parse(args.Parameters[1]);
                }
                catch
                {
                    // ignored
                }

                switch (args.Parameters[0].ToLower())
                {
                    case "dmg":
                    case "damage":
                        player.Damage += CalculateSkillPoints(player, points, "Damage", args);
                        break;
                    case "def":
                    case "defense":
                        player.Defense += CalculateSkillPoints(player, points, "Defense", args);
                        break;
                    case "heal":
                        player.HealOnHit += CalculateSkillPoints(player, points, "Heal", args);
                        break;
                    case "crit":
                        player.Crit += CalculateSkillPoints(player, points, "Critical Chance", args);
                        break;
                    default:
                        args.Player.SendErrorMessage("Available level up choices: dmg, def, heal");
                        break;
                }
            }
            else
            {
                args.Player.SendInfoMessage($"Available level up bonuses: {player?.SkillPoints}");
                args.Player.SendInfoMessage("Type /lvl [choice] - dmg, def, heal");
            }
        }

        private int CalculateSkillPoints(Player player, byte points, string stat, CommandArgs args)
        {
            int copySkillPoints = player.SkillPoints;
            copySkillPoints -= points;
            int newPoints = points + copySkillPoints;
            if (copySkillPoints < 0)
            {
                player.SkillPoints -= newPoints;
                args.Player.SendMessage($"Your {stat} has increased by {newPoints}!", Color.Lime);
                return newPoints;
            }
            else
            {
                player.SkillPoints -= points;
                args.Player.SendMessage($"Your {stat} has increased by {points}!", Color.Lime);
                return points;
            }
        }

        private void StatsCommand(CommandArgs args)
        {
            Player player = players.FirstOrDefault(p => p.ID == args.Player.Index);
            args.Player.SendMessage($"Name: {player.Name} Level: {player.Level}", Color.Lime);
            args.Player.SendMessage($"Heal: {player.HealOnHit} Damage: {player.Damage}", Color.Lime);
            args.Player.SendMessage($"Defense: {player.Defense} Critical: {player.Crit}", Color.Lime);
            args.Player.SendMessage($"Experience: {player.Experience:n0}/{player.NextLevel:n0}", Color.Lime);
        }

        private void PlayCommand(CommandArgs args)
        {
            Player player = players.FirstOrDefault(p => p.ID == args.Player.Index);
            if (args.Parameters.Count > 0)
            {
                int Note = 0;
                try
                {
                    if(args.Parameters.Count > 1)
                        Note = Int32.Parse(args.Parameters[1]);

                    TSPlayer.Server.SendData(PacketTypes.PlayHarp,"", Note);
                }
                catch
                {
                    //ignored
                }

                
            }
        }
        void SaveTick()
        {
            Thread.Sleep(300000);
            Save();
        }

        void Save()
        {
            File.WriteAllText("players.json", JsonConvert.SerializeObject(players));
        }

        void Load()
        {
            if (File.Exists("players.json"))
                players = JsonConvert.DeserializeObject<List<Player>>(File.ReadAllText("players.json"));
        }
    }
}
