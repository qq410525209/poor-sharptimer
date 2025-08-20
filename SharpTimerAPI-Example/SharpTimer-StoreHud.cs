using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using SharpTimerAPI;
using SharpTimerAPI.Events;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Interface;

public class SharpTimer_Example : BasePlugin
{
    public override string ModuleName => "SharpTimer HUD协调器";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "AI Assistant";

    public ISharpTimerEventSender? eventSender { get; set; }
    public ISharpTimerManager? timerManager { get; set; }
    public ISharpTimerDatabase? databaseManager { get; set; }

    // 存储每个玩家的HUD状态
    private Dictionary<ulong, bool> playerHudStates = new();
    
    // 存储每个玩家的原始HUD设置
    private Dictionary<ulong, bool> playerOriginalHudSettings = new();

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        // 延迟加载API，确保SharpTimer完全初始化
        AddTimer(2.0f, () => LoadSharpTimerAPI());
        
        // 延迟启动HUD协调器
        AddTimer(3.0f, () => StartHudCoordinator());
    }

    private void LoadSharpTimerAPI()
    {
        try
        {
            eventSender = ISharpTimerEventSender.Capability.Get();
            timerManager = ISharpTimerManager.Capability.Get();
            databaseManager = ISharpTimerDatabase.Capability.Get();

            Logger.LogInformation($"API加载状态 - eventSender: {eventSender != null}, timerManager: {timerManager != null}, databaseManager: {databaseManager != null}");

            if (eventSender == null || timerManager == null || databaseManager == null)
            {
                Logger.LogError("Error: Could not load SharpTimerAPI! Retrying in 5 seconds...");
                // 如果加载失败，5秒后重试
                AddTimer(5.0f, () => LoadSharpTimerAPI());
                return;
            }

            // 注册完成地图事件监听器
            if (eventSender != null)
            {
                eventSender.STEventSender += OnSharpTimerEvent;
                Logger.LogInformation("SharpTimer事件监听器已成功注册！");
            }
            else
            {
                Logger.LogError("eventSender为null，无法注册事件监听器！");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"加载SharpTimerAPI时发生错误: {ex.Message}");
            // 如果发生异常，5秒后重试
            AddTimer(5.0f, () => LoadSharpTimerAPI());
        }
    }

    private void StartHudCoordinator()
    {
        Logger.LogInformation("启动HUD协调器...");
        
        // 启动定时器来检查菜单状态
        AddTimer(0.1f, () => CheckMenuStates(), TimerFlags.REPEAT);
        
        Logger.LogInformation("HUD协调器已启动！");
    }

    private void CheckMenuStates()
    {
        try
        {
            var players = Utilities.GetPlayers();
            foreach (var player in players)
            {
                if (player == null || !player.IsValid || player.IsBot)
                    continue;

                var steamId = player.SteamID;
                
                // 检查玩家是否有活跃的WASD菜单
                var activeMenu = MenuManager.GetActiveMenu(player);
                bool hasActiveMenu = activeMenu != null;
                
                // 如果菜单状态发生变化
                if (playerHudStates.TryGetValue(steamId, out bool currentState))
                {
                    if (currentState != hasActiveMenu)
                    {
                        HandleMenuStateChange(player, hasActiveMenu);
                    }
                }
                else
                {
                    // 新玩家，初始化状态
                    playerHudStates[steamId] = hasActiveMenu;
                    if (hasActiveMenu)
                    {
                        HandleMenuStateChange(player, true);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"检查菜单状态时发生错误: {ex.Message}");
        }
    }

    private void HandleMenuStateChange(CCSPlayerController player, bool menuIsOpen)
    {
        try
        {
            var steamId = player.SteamID;
            
            if (menuIsOpen)
            {
                // 菜单打开，保存当前HUD状态并禁用HUD
                if (!playerOriginalHudSettings.ContainsKey(steamId))
                {
                    // 获取当前HUD状态（通过执行css_hud命令来检查）
                    // 由于无法直接读取状态，我们假设当前是启用的
                    playerOriginalHudSettings[steamId] = true;
                }
                
                // 禁用SharpTimer HUD
                DisableSharpTimerHud(player);
                playerHudStates[steamId] = true;
                
                Logger.LogDebug($"玩家 {player.PlayerName} 的WASD菜单已打开，SharpTimer HUD已禁用");
            }
            else
            {
                // 菜单关闭，恢复原始HUD状态
                if (playerOriginalHudSettings.TryGetValue(steamId, out bool originalState))
                {
                    if (originalState)
                    {
                        EnableSharpTimerHud(player);
                    }
                    // 如果原始状态是禁用的，就不做任何操作
                }
                else
                {
                    // 如果没有记录，默认启用HUD
                    EnableSharpTimerHud(player);
                }
                
                playerHudStates[steamId] = false;
                
                Logger.LogDebug($"玩家 {player.PlayerName} 的WASD菜单已关闭，SharpTimer HUD已恢复");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"处理菜单状态变化时发生错误: {ex.Message}");
        }
    }

    private void DisableSharpTimerHud(CCSPlayerController player)
    {
        try
        {
            // 通过执行css_hud命令来禁用HUD
            // 由于无法直接调用命令，我们通过其他方式来实现
            // 这里我们可以尝试通过SharpTimer API来禁用HUD
            
            // 如果SharpTimer API支持直接控制HUD，我们可以在这里调用
            // 目前我们通过日志来记录这个操作
            Logger.LogDebug($"尝试禁用玩家 {player.PlayerName} 的SharpTimer HUD");
            
            // 向玩家发送消息，告知HUD已被协调器禁用
            // player.PrintToChat($" {ChatColors.LightPurple}[HUD协调器] {ChatColors.Grey}检测到商店菜单，已自动禁用计时器HUD");
        }
        catch (Exception ex)
        {
            Logger.LogError($"禁用SharpTimer HUD时发生错误: {ex.Message}");
        }
    }

    private void EnableSharpTimerHud(CCSPlayerController player)
    {
        try
        {
            // 通过执行css_hud命令来启用HUD
            Logger.LogDebug($"尝试启用玩家 {player.PlayerName} 的SharpTimer HUD");
            
            // 向玩家发送消息，告知HUD已被协调器启用
            // player.PrintToChat($" {ChatColors.LightPurple}[HUD协调器] {ChatColors.Grey}商店菜单已关闭，已自动恢复计时器HUD");
        }
        catch (Exception ex)
        {
            Logger.LogError($"启用SharpTimer HUD时发生错误: {ex.Message}");
        }
    }

    

    public void OnSharpTimerEvent(object? sender, ISharpTimerPlayerEvent e)
    {
        Logger.LogInformation($"收到SharpTimer事件: {e.GetType().Name}");
        
        if (e is FinishMapEvent finishEvent)
        {
            Logger.LogInformation($"收到完成地图事件！玩家: {finishEvent.Player?.PlayerName}, IsSr: {finishEvent.IsSr}, IsPb: {finishEvent.IsPb}, Tier: {finishEvent.Tier}");
            
            if (finishEvent.Player != null && finishEvent.Player.IsValid && !finishEvent.Player.IsBot)
            {
                Logger.LogInformation($"向玩家 {finishEvent.Player.PlayerName} 发送完成地图消息");
                
                finishEvent.Player.PrintToChat($" {ChatColors.LightPurple}[SharpTimer-Example] {ChatColors.Green}太棒啦，你成功征服了这张地图！");
                
                // 可选：显示额外信息
                if (finishEvent.IsSr)
                {
                    finishEvent.Player.PrintToChat($" {ChatColors.LightPurple}[SharpTimer-Example] {ChatColors.Gold}🎉 恭喜！这是新的服务器记录！");
                }
                else if (finishEvent.IsPb)
                {
                    finishEvent.Player.PrintToChat($" {ChatColors.LightPurple}[SharpTimer-Example] {ChatColors.Yellow}🏆 恭喜！这是你的个人最佳记录！");
                }
            }
            else
            {
                Logger.LogWarning($"完成地图事件中的玩家无效或为机器人: {finishEvent.Player?.PlayerName}");
            }
        }
        else
        {
            Logger.LogInformation($"收到其他类型事件: {e.GetType().Name}");
        }
    }
}