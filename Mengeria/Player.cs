using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Mengeria
{
    public class Player
    {
        public int ID { get; set; }
        public string Name { get; set; }

        public byte Level { get; set; }
        public int Experience { get; set; }
        public int NextLevel { get; set; }
        public int SkillPoints { get; set; }

        public int Damage { get; set; }
        public int DamagePercent { get; set; }
        public int Defense { get; set; }
        public int DefensePercent { get; set; }
        public int HealOnHit { get; set; }
        public int LifeLeech { get; set; }
        public int Crit { get; set; }
        
        public Player(int id, string name)
        {
            ID = id;
            Name = name;
            Level = 1;
            NextLevel = 140;
           
        }

        public void OnMobHit(int dmg)
        {
            GainExperience(dmg);
        }

        public void GainExperience(int experience)
        {
            Experience += experience;
            if (Level < 99 && Experience >= NextLevel)
                LevelUp();


            if (Experience > 99999999)
                Experience = 99999999;
        }

        public void LevelUp()
        {
            if (Level++ > 99)
                Level = 99;

            if (Level == 99)
                NextLevel = 0;
            else
                NextLevel += (int) (Math.Pow(Level, 2) * 27.4);

            SkillPoints++;

            MainPlugin.Play.Player = new TSPlayer(ID);
            MainPlugin.Play.StartSong();

            TShock.Players[ID].SendMessage($"You have leveled up to {Level}! Type /level to increase stats.", Color.Lime);
        }
    }
}
