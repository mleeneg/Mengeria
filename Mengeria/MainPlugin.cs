using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Mengeria
{
    [ApiVersion(2, 1)]
    public class MainPlugin : TerrariaPlugin
    {
        public override string Author => "Mengis Khan";
        public override string Description => "It is Mengeria. KEVIN!";
        public override string Name => "Mengeria";
        public override Version Version => new Version(2, 0, 0, 0);
        public RpgLevels RPG;
        public static PlaySound Play;
        public override void Initialize()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Deregister hooks here
            }
            RPG.Dispose();
            Play.End();
            base.Dispose(disposing);
        }

        public MainPlugin(Main game) : base(game)
        {
            RPG = new RpgLevels(game);
            Play = new PlaySound(game);
        }
    }
}
