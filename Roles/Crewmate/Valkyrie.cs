﻿using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Valkyrie : RoleBase
{

    private const int Id = 12500;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem RevengeTime;

    public static PlayerControl Killer = null;
    private static bool IsRevenge = false;
    private static int Timer = 0;
    private static bool Success = false;
    private static float tempKillTimer = 0;

    public override void SetupCustomOption()
  {

   SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Valkyrie, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(300f, 300f, 300f), 300f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Valkyrie])
            .SetValueFormat(OptionFormat.Seconds);
        RevengeTime = IntegerOptionItem.Create(Id + 11, "ValkyrieRevengeTime", new(0, 60, 1), 30, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Valkyrie])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Valkyrie]);
    }

     public override void Init()
    {
        playerIdList.Clear();
        IsRevenge = false;
        Success = false;
        Killer = null;
        tempKillTimer = 0;
    }
       public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        Timer = RevengeTime.GetInt();

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.300f;
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseSabotage(PlayerControl pc) => false;

      public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return true;
        if (killer == null) return false;
        if (target == null) return false;
        if (IsRevenge) return true;

        _ = new LateTask(() => { target.RpcRandomVentTeleport(); }, 1f, "Random Vent Teleport - Valkyrie");

        Timer = RevengeTime.GetInt();
        Countdown(Timer, target);
        IsRevenge = true;
        killer.SetKillCooldown();
        killer.Notify(GetString("ValkyrieHide"), RevengeTime.GetFloat());
        tempKillTimer = target.killTimer;
        target.SetKillCooldown(time: 3f);
        Killer = killer;

         return false;
    }
    private static void Countdown(int seconds, PlayerControl player)
    {
        var killer = Killer;
        if (Success)
        {
            Timer = RevengeTime.GetInt();
            Success = false;
            Killer = null; 
            return;
        }
        if (seconds <= 0 || GameStates.IsMeeting && player.IsAlive()) 
        { 
            player.RpcMurderPlayer(player); 
            player.SetRealKiller(killer);
            Killer = null; 
            return; 
        }
        player.Notify(string.Format(GetString("ValkyrieRevenge"), seconds, Killer.GetRealName()), 1.1f);
        Timer = seconds;

        _ = new LateTask(() => { Countdown(seconds - 1, player); }, 1.01f, "Valkyrie Countdown");
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !killer.IsAlive()) return false;

        if (!IsRevenge) return true;
        else if (target == Killer)
        {
            Success = true;
            killer.Notify(GetString("ValkyrieSuccess"));
            killer.SetKillCooldown(KillCooldown.GetFloat() + tempKillTimer);
            IsRevenge = false;
            return true;
        }
        else
        {
            killer.RpcMurderPlayer(killer);
            return false;
        }
    }
}
