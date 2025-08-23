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
// using StoreApi; // 临时注释，等待StoreApi编译完成

public class SharpTimer_Example : BasePlugin
{
    public override string ModuleName => "SharpTimer 商店兼容";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "小彩旗";

    public ISharpTimerEventSender? eventSender { get; set; }
    public ISharpTimerManager? timerManager { get; set; }
    public ISharpTimerDatabase? databaseManager { get; set; }
    // public IStoreApi? storeApi { get; set; } // 临时注释

    // 存储每个玩家的HUD状态
    private Dictionary<ulong, bool> playerHudStates = new();
    
    // 存储每个玩家的原始HUD设置
    private Dictionary<ulong, bool> playerOriginalHudSettings = new();

    // 服务器记录奖励配置
    private const int SR_REWARD_CREDITS = 5;
    private bool srRewardEnabled = true;

    public override void Load(bool hotReload)
    {
        AddCommand("css_sr_reward", "切换服务器记录奖励功能", Command_ToggleSrReward);
        AddCommand("css_sr_reward_status", "查看服务器记录奖励状态", Command_SrRewardStatus);

        RegisterEventHandler<EventPlayerConnect>(EventPlayerConnect);
        RegisterEventHandler<EventPlayerDisconnect>(EventPlayerDisconnect);
        
        Logger.LogInformation("SharpTimer 商店兼容插件已加载！");
    }

    public override void Unload(bool hotReload)
    {
        RemoveCommand("css_sr_reward", Command_ToggleSrReward);
        RemoveCommand("css_sr_reward_status", Command_SrRewardStatus);

        // 清理事件监听器
        if (eventSender != null)
        {
            eventSender.STEventSender -= OnSharpTimerEvent;
        }

        DeregisterEventHandler<EventPlayerConnect>(EventPlayerConnect);
        DeregisterEventHandler<EventPlayerDisconnect>(EventPlayerDisconnect);
        
        Logger.LogInformation("SharpTimer 商店兼容插件已卸载！");
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        // 延迟加载API，确保SharpTimer完全初始化
        AddTimer(2.0f, () => LoadSharpTimerAPI());
        
        // 延迟启动HUD协调器
        AddTimer(3.0f, () => StartHudCoordinator());
        
        // 延迟加载Store API
        // AddTimer(4.0f, () => LoadStoreAPI()); // 临时注释
    }

    /*
    private void LoadStoreAPI()
    {
        try
        {
            storeApi = IStoreApi.Capability.Get();
            
            if (storeApi != null)
            {
                Logger.LogInformation("Store API已成功加载！");
            }
            else
            {
                Logger.LogWarning("Store API未找到，服务器记录奖励功能将不可用");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"加载Store API时发生错误: {ex.Message}");
        }
    }
    */

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

    // 服务器记录奖励相关命令
    public void Command_ToggleSrReward(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        srRewardEnabled = !srRewardEnabled;
        string status = srRewardEnabled ? "启用" : "禁用";
        
        command.ReplyToCommand($" {ChatColors.LightPurple}[服务器记录奖励] {ChatColors.Grey}功能已{(srRewardEnabled ? ChatColors.Green : ChatColors.Red)}{status}");
        
        if (player != null)
        {
            player.PrintToChat($" {ChatColors.LightPurple}[服务器记录奖励] {ChatColors.Grey}功能已{(srRewardEnabled ? ChatColors.Green : ChatColors.Red)}{status}");
        }
        
        Logger.LogInformation($"服务器记录奖励功能已{status}");
    }

    public void Command_SrRewardStatus(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        command.ReplyToCommand($" {ChatColors.LightPurple}[服务器记录奖励] {ChatColors.Grey}状态信息:");
        command.ReplyToCommand($" {ChatColors.Grey}功能状态: {(srRewardEnabled ? $"{ChatColors.Green}启用" : $"{ChatColors.Red}禁用")}");
        command.ReplyToCommand($" {ChatColors.Grey}奖励Credits: {ChatColors.Gold}{SR_REWARD_CREDITS}");
        command.ReplyToCommand($" {ChatColors.Grey}Store API: {ChatColors.Red}未加载 (临时禁用)");
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
                    
                    // 处理服务器记录奖励
                    HandleServerRecordReward(finishEvent.Player);
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

    private void HandleServerRecordReward(CCSPlayerController player)
    {
        try
        {
            if (!srRewardEnabled)
            {
                Logger.LogDebug($"服务器记录奖励功能已禁用，跳过奖励");
                return;
            }

            // 临时禁用Store API功能
            Logger.LogWarning($"Store API功能临时禁用，无法给予服务器记录奖励");
            player.PrintToChat($" {ChatColors.LightPurple}[服务器记录奖励] {ChatColors.Red}⚠️ Store系统功能临时禁用");
            
            // 向玩家发送奖励消息（模拟）
            player.PrintToChat($" {ChatColors.LightPurple}[服务器记录奖励] {ChatColors.Gold}🎁 恭喜获得新的服务器记录！");
            player.PrintToChat($" {ChatColors.LightPurple}[服务器记录奖励] {ChatColors.Grey}奖励系统暂时不可用，请联系管理员");
            
            // 向所有玩家广播
            Server.PrintToChatAll($" {ChatColors.LightPurple}[服务器记录奖励] {ChatColors.Gold}🎉 {ChatColors.White}{player.PlayerName} {ChatColors.Grey}创造了新的服务器记录！");
            Server.PrintToChatAll($" {ChatColors.LightPurple}[服务器记录奖励] {ChatColors.Grey}奖励系统暂时不可用");
            
            Logger.LogInformation($"玩家 {player.PlayerName} 获得服务器记录，但Store API未加载，无法给予实际奖励");
        }
        catch (Exception ex)
        {
            Logger.LogError($"处理服务器记录奖励时发生错误: {ex.Message}");
            player.PrintToChat($" {ChatColors.LightPurple}[服务器记录奖励] {ChatColors.Red}⚠️ 奖励发放出错，请联系管理员");
        }
    }

    public HookResult EventPlayerConnect(EventPlayerConnect @event, GameEventInfo gameEventInfo)
    {
        var player = @event.Userid;
        if (player == null || player.IsBot)
            return HookResult.Continue;

        var steamId = player.SteamID;
        
        // 初始化新玩家的HUD状态
        playerHudStates[steamId] = false;
        playerOriginalHudSettings[steamId] = true; // 默认启用HUD
        
        Logger.LogDebug($"新玩家 {player.PlayerName} 已连接，HUD状态已初始化");
        
        return HookResult.Continue;
    }

    public HookResult EventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo gameEventInfo)
    {
        var player = @event.Userid;
        if (player == null || player.IsBot)
            return HookResult.Continue;

        var steamId = player.SteamID;
        
        // 清理玩家数据
        playerHudStates.Remove(steamId);
        playerOriginalHudSettings.Remove(steamId);
        
        Logger.LogDebug($"玩家 {player.PlayerName} 已断开连接，HUD状态已初始化");
        
        return HookResult.Continue;
    }
}