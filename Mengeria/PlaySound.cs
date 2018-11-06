using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Mengeria
{
    public class PlaySound : TerrariaPlugin
    {
        private int _delta = 0;
        private DateTime _lastUpdate = DateTime.Now;
        private bool _isPlaying = false;
        private int _tempo;
        public TSPlayer Player;
        private bool isHooked = false;
        private List<float> _notesToPlay;
        public List<float> NotesToPlay = new List<float>();
        public static readonly Dictionary<string, float> Note = new Dictionary<string, float>()
        {
            {"C4", -1},
            {@"C#4", -.916667f},
            {"D4", -.833333f},
            {@"D#4", -.75f},
            {"E4", -.666666667f},
            {"F4", -.583333333f},
            {@"F#4", -.5f},
            {"G4", -.416666667f},
            {@"G#4", -.333333333f},
            {"A4", -.25f},
            {@"A#4", -.166666667f},
            {"B4", -.083333333f},
            {"C5", 0},
            {@"C#5", .083333333f},
            {"D5", .166666667f},
            {@"D#5", .25f},
            {"E5", .333333333f},
            {"F5", .416666667f},
            {@"F#5", .5f},
            {"G5", .583333333f},
            {@"G#5", .666666667f},
            {"A5", .75f},
            {@"A#5", .833333f},
            {"B5", .916667f},
            {"C6", 1}
        };
        public PlaySound(Main game) : base(game)
        {
           
        }
        public void Update(EventArgs args)
        {
            if (!_isPlaying)
                return;

            if (NotesToPlay.Count <= 0)
            {
                EndSong();
                return;
            }

            _delta += (int)((DateTime.Now - _lastUpdate).TotalMilliseconds);
            if (_delta > _tempo)
            {
                float notes = NotesToPlay[0];
                NotesToPlay.RemoveAt(0);
                    if (notes >= -1 && notes <= 1)
                    {
                        PlayNote(notes);
                        //Player.SendMessage($"PlayNote({note})",Color.Red);
                    }
                _delta -= _tempo;
            }
            _lastUpdate = DateTime.Now;
        }
        public void StartSong()
        {
            _notesToPlay = new List<float>()
            {
                127,
                Note["C4"],
                Note["C4"],
                Note["C4"],
                Note["C4"],
                Note["G#4"],
                Note["A#4"],
                Note["C4"],
                Note["D5"],
                Note["C4"]
            };
            NotesToPlay = new List<float>();
            NotesToPlay = _notesToPlay;
            _tempo = (int)_notesToPlay[0];
            _isPlaying = true;
            _lastUpdate = DateTime.Now;
            if(!isHooked)
                ServerApi.Hooks.GamePostUpdate.Register(this, Update);
        }

        public void EndSong()
        {
            _isPlaying = false;
           
        }

        private void PlayNote(float note)
        {
            Player.SendDataFromPlayer(PacketTypes.PlayHarp, Player.Index,"", note);
        }

        public override void Initialize()
        {
         
        }

        public void End()
        {
            ServerApi.Hooks.GamePostUpdate.Deregister(this, Update);
            isHooked = false;
        }
    }
}
