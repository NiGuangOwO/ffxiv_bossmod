﻿using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ImGuiNET;

namespace BossMod.AI;

sealed class AIManager : IDisposable
{
    private readonly Autorotation _autorot;
    private readonly AIController _controller;
    private readonly AIConfig _config;
    private int _masterSlot = PartyState.PlayerSlot; // non-zero means corresponding player is master
    private AIBehaviour? _beh;
    private readonly UISimpleWindow _ui;

    public AIManager(Autorotation autorot)
    {
        _autorot = autorot;
        _controller = new();
        _config = Service.Config.Get<AIConfig>();
        _ui = new("AI", DrawOverlay, false, new(100, 100), ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoFocusOnAppearing) { RespectCloseHotkey = false };
        Service.ChatGui.ChatMessage += OnChatMessage;
        Service.CommandManager.AddHandler("/vbmai", new Dalamud.Game.Command.CommandInfo(OnCommand) { HelpMessage = "Toggle AI mode" });
    }

    public void Dispose()
    {
        SwitchToIdle();
        _ui.Dispose();
        Service.ChatGui.ChatMessage -= OnChatMessage;
        Service.CommandManager.RemoveHandler("/vbmai");
    }

    public void Update()
    {
        if (_autorot.WorldState.Party.ContentIDs[_masterSlot] == 0)
            SwitchToIdle();

        if (!_config.Enabled && _beh != null)
            SwitchToIdle();

        var player = _autorot.WorldState.Party.Player();
        var master = _autorot.WorldState.Party[_masterSlot];
        if (_beh != null && player != null && master != null)
        {
            _beh.Execute(player, master);
        }
        else
        {
            _controller.Clear();
        }
        _controller.Update(player);

        _ui.IsOpen = _config.Enabled && player != null && _config.DrawUI;
    }

    private void DrawOverlay()
    {
        ImGui.TextUnformatted($"AI: {(_beh != null ? "开" : "关")}, 队长={_autorot.WorldState.Party[_masterSlot]?.Name}");
        ImGui.TextUnformatted($"Navi={_controller.NaviTargetPos} / {_controller.NaviTargetRot}{(_controller.ForceFacing ? " 强制" : "")}");
        _beh?.DrawDebug();
        if (ImGui.Button("重设"))
            SwitchToIdle();
        ImGui.SameLine();
        if (ImGui.Button("AI 启用 - 跟随队长"))
        {
            if (_config.FollowLeader)
            {
                var leader = Service.PartyList[(int)Service.PartyList.PartyLeaderIndex];
                int leaderSlot = leader != null ? _autorot.WorldState.Party.ContentIDs.IndexOf((ulong)leader.ContentId) : -1;
                SwitchToFollow(leaderSlot >= 0 ? leaderSlot : PartyState.PlayerSlot);
            }
            else
            {
                SwitchToFollow(PartyState.PlayerSlot);
            }
        }
    }

    private void SwitchToIdle()
    {
        _beh?.Dispose();
        _beh = null;

        _masterSlot = PartyState.PlayerSlot;
        _controller.Clear();
    }

    private void SwitchToFollow(int masterSlot)
    {
        SwitchToIdle();
        _masterSlot = masterSlot;
        _beh = new AIBehaviour(_controller, _autorot);
    }

    private int FindPartyMemberSlotFromSender(SeString sender)
    {
        if (sender.Payloads.FirstOrDefault() is not PlayerPayload source)
            return -1;
        var pm = Service.PartyList.FirstOrDefault(pm => pm.Name.TextValue == source.PlayerName && pm.World.Id == source.World.RowId);
        return pm != null ? _autorot.WorldState.Party.ContentIDs.IndexOf((ulong)pm.ContentId) : -1;
    }

    private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (!_config.Enabled || type != XivChatType.Party)
            return;

        var messagePrefix = message.Payloads.FirstOrDefault() as TextPayload;
        if (messagePrefix?.Text == null || !messagePrefix.Text.StartsWith("vbmai ", StringComparison.Ordinal))
            return;

        var messageData = messagePrefix.Text.Split(' ');
        if (messageData.Length < 2)
            return;

        switch (messageData[1])
        {
            case "follow":
                var master = FindPartyMemberSlotFromSender(sender);
                if (master >= 0)
                    SwitchToFollow(master);
                break;
            case "cancel":
                SwitchToIdle();
                break;
            default:
                Service.Log($"[AI] Unknown command: {messageData[1]}");
                break;
        }
    }

    private void OnCommand(string cmd, string message)
    {
        var messageData = message.Split(' ');
        switch (messageData[0])
        {
            case "on":
                SwitchToFollow(PartyState.PlayerSlot);
                break;
            case "off":
                SwitchToIdle();
                break;
            case "toggle":
                if (_beh == null)
                    SwitchToFollow(PartyState.PlayerSlot);
                else
                    SwitchToIdle();
                break;
            default:
                Service.Log($"[AI] Unknown command: {messageData[0]}");
                break;
        }
    }
}
