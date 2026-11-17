using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class PlayersModule : PoppyModule
    {
        internal override string Name => "Players";

        private static int _hurt = 100;
        private static int _giveCount = 1;
        private static string _banArmed;

        internal override void DrawMenu()
        {
            if (!PlayerContext.InGame) { Widgets.EmptyState("Start a run first."); return; }

            var users = NetworkUser.readOnlyInstancesList;
            if (users == null || users.Count == 0) { Widgets.EmptyState("No players in the lobby."); return; }

            Widgets.SectionBegin("Options");
            if (!NetUtil.IsServer)
                Widgets.Note("You're a client, so these go to the host and only apply if the host turned on \"Allow Others To Use This\".");
            _hurt = Widgets.IntStepper("Hurt amount", _hurt, 50, 1, 1000000000);
            _giveCount = Widgets.IntStepper("Give item count", _giveCount, 1, 1, 100);
            Widgets.SectionEnd();

            foreach (NetworkUser user in users)
                if (user != null) DrawPlayer(user);
        }

        private static void DrawPlayer(NetworkUser user)
        {
            CharacterMaster master = user.master;
            bool isLocal = NetworkUser.readOnlyLocalPlayersList.Contains(user);

            Widgets.SectionBegin(user.userName + (isLocal ? "  (you)" : ""));

            CharacterBody body = master != null ? master.GetBody() : null;
            if (body != null && body.healthComponent != null)
                Widgets.Label($"{body.GetDisplayName()}   ·   {Mathf.CeilToInt(body.healthComponent.health)} / {Mathf.CeilToInt(body.healthComponent.fullCombinedHealth)} HP");
            else
                Widgets.Label("No body (dead or not spawned)");

            if (!isLocal && NetUtil.IsServer)
            {
                string banId = "players.ban." + user.userName;
                bool armed = _banArmed == banId;
                Widgets.Buttons("Moderation", armed ? "Click Confirm ban to ban this player." : null,
                    ("Kick", () => NetUtil.KickUser(user)),
                    (armed ? "Confirm ban" : "Ban", () =>
                    {
                        if (armed) { _banArmed = null; NetUtil.BanUser(user); }
                        else _banArmed = banId;
                    }));
            }

            if (master == null) { Widgets.SectionEnd(); return; }

            Widgets.Buttons("Health", null,
                ("Heal", () => NetUtil.DoFor(master, PoppyOp.HealFull)),
                ("Hurt " + _hurt, () => NetUtil.DoFor(master, PoppyOp.HurtBody, f1: _hurt)),
                ("Kill", () => NetUtil.DoFor(master, PoppyOp.TrueKillTarget)));

            if (!isLocal)
            {
                Widgets.Buttons("Teleport", null,
                    ("Bring to me", () =>
                    {
                        Vector3 p = MyPos();
                        NetUtil.DoFor(master, PoppyOp.TeleportBody, f1: p.x, f2: p.y, f3: p.z);
                    }),
                    ("Go to them", () =>
                    {
                        CharacterBody b = master.GetBody();
                        Vector3 p = b != null ? b.footPosition : MyPos();
                        NetUtil.DoFor(PlayerContext.Master, PoppyOp.TeleportBody, f1: p.x, f2: p.y, f3: p.z);
                    }));
            }

            Widgets.Buttons("Team", null,
                ("Players", () => NetUtil.DoFor(master, PoppyOp.SetTeam, i1: (int)TeamIndex.Player)),
                ("Monsters", () => NetUtil.DoFor(master, PoppyOp.SetTeam, i1: (int)TeamIndex.Monster)),
                ("Neutral", () => NetUtil.DoFor(master, PoppyOp.SetTeam, i1: (int)TeamIndex.Neutral)));

            CharacterMaster target = master;
            Widgets.Button("Give item", () =>
                ItemPicker.Open("Give item to " + user.userName, idx => NetUtil.DoFor(target, PoppyOp.GiveItem, i1: (int)idx, i2: _giveCount)));

            Widgets.SectionEnd();
        }

        private static Vector3 MyPos()
        {
            CharacterBody b = PlayerContext.Body;
            return b != null ? b.footPosition : Vector3.zero;
        }
    }
}
