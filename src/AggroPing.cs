using HarmonyLib;
using UnityEngine;

namespace PingHeim
{
    /// <summary>
    /// The aggro ping: every hostile creature close enough is told about you and comes for you. It's a deliberate way to pull a
    /// camp or a nest to where you want to fight.
    /// </summary>
    internal static class AggroPing
    {
        public static void Use(Player player)
        {
            // The same recharge as a normal ping: using either starts it.
            if (!PingItem.TryUse(player, start: true)) return;

            float radius = PingHeimPlugin.AggroRadius.Value;
            int called = 0;
            foreach (var character in Character.GetAllCharacters())
            {
                if (character == null || character.IsPlayer() || character.IsDead() || character.IsTamed()) continue;
                if (character.IsBoss() && !PingHeimPlugin.AggroBosses.Value) continue;
                if ((character.transform.position - player.transform.position).sqrMagnitude > radius * radius) continue;

                var ai = character.GetComponent<MonsterAI>();
                if (ai == null || !BaseAI.IsEnemy(character, player)) continue;

                // Whoever's game is running the creature has to be the one to change what it does, so take it over.
                if (character.m_nview != null && character.m_nview.IsValid() && !character.m_nview.IsOwner()) character.m_nview.ClaimOwnership();
                ai.SetAlerted(true);
                ai.m_targetCreature = player;
                ai.SetHuntPlayer(true);
                called++;
            }

            PingSound.Play();
            PingNet.ShareAggro(player, called);
            player.Message(MessageHud.MessageType.Center, called == 0 ? "Aggro ping: nothing came" : $"Aggro ping! {called} creature{(called == 1 ? "" : "s")} heard you");
        }
    }

    /// <summary>With the Ping Stone in your hand, a click opens its options instead of swinging.</summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    internal static class StoneAttackPatch
    {
        private static bool Prefix(Humanoid __instance, ref bool __result)
        {
            if (__instance != Player.m_localPlayer || !PingItem.IsPingStone(__instance.GetRightItem())) return true;
            __result = false;
            if (!PingOptions.PopupOpen) PingOptions.OpenPopup();
            return false;
        }
    }
}
