# SharpTimer 商店兼容插件

## 功能概述

这是一个专门为解决SharpTimer和Store插件的HUD面板冲突而设计的中间插件。当Store插件的WASD菜单打开时，自动禁用SharpTimer的HUD面板，当菜单关闭时，自动重新启用HUD面板。

## 主要功能

### 1. HUD协调器
- **自动检测WASD菜单状态** - 实时监控玩家的菜单状态
- **智能HUD控制** - 菜单打开时自动禁用SharpTimer HUD，关闭时自动恢复
- **状态记忆** - 记住每个玩家的原始HUD设置，确保恢复时状态正确

### 2. 服务器记录奖励系统
- **自动奖励** - 当玩家创造新的服务器记录时，自动奖励5个Credits
- **实时通知** - 向所有玩家广播服务器记录和奖励信息
- **状态管理** - 提供命令来启用/禁用奖励功能

### 3. 事件监听
- **SharpTimer事件** - 监听玩家完成地图、开始计时等事件
- **玩家连接管理** - 自动管理玩家的HUD状态

## 安装要求

- CounterStrikeSharp
- SharpTimer插件
- CS2MenuManager (用于WASD菜单检测)
- Store插件 (用于Credits奖励系统)

## 配置

### 奖励设置
```csharp
// 服务器记录奖励Credits数量
private const int SR_REWARD_CREDITS = 5;

// 是否启用奖励功能
private bool srRewardEnabled = true;
```

## 可用命令

### 管理员命令
- `css_sr_reward` - 切换服务器记录奖励功能 (启用/禁用)
- `css_sr_reward_status` - 查看服务器记录奖励状态

### 状态信息
- 功能启用状态
- 奖励Credits数量
- Store API连接状态

## 工作原理

### HUD协调流程
1. 插件启动后，开始监控所有玩家的WASD菜单状态
2. 当检测到菜单打开时：
   - 保存当前HUD状态
   - 禁用SharpTimer HUD
   - 记录状态变化
3. 当检测到菜单关闭时：
   - 恢复原始HUD状态
   - 重新启用SharpTimer HUD

### 奖励系统流程
1. 监听SharpTimer的`FinishMapEvent`事件
2. 当`IsSr`为true时（新服务器记录）：
   - 检查奖励功能是否启用
   - 通过Store API给玩家添加Credits
   - 发送奖励通知给所有玩家
   - 记录日志信息

## 事件处理

### 支持的事件类型
- `FinishMapEvent` - 玩家完成地图
- `StartTimerEvent` - 玩家开始计时

### 事件数据
```csharp
public record FinishMapEvent(
    CCSPlayerController? Player,  // 完成地图的玩家
    bool IsSr,                    // 是否是新的服务器记录
    bool IsPb,                    // 是否是新的个人最佳记录
    int Tier                      // 地图难度等级
);
```

## 日志信息

插件会记录以下信息：
- API加载状态
- HUD状态变化
- 服务器记录奖励发放
- 错误和异常信息

## 故障排除

### 常见问题
1. **Store API未加载** - 确保Store插件已正确安装并运行
2. **HUD协调不工作** - 检查CS2MenuManager是否正确安装
3. **奖励发放失败** - 检查Store插件的数据库连接

### 调试命令
使用`css_sr_reward_status`命令查看系统状态，包括：
- 功能启用状态
- API连接状态
- 配置信息

## 版本信息

- **版本**: 1.0.0
- **作者**: 小彩旗
- **兼容性**: CounterStrikeSharp, .NET 8.0

## 更新日志

### v1.0.0
- 初始版本发布
- 实现HUD协调器功能
- 实现服务器记录奖励系统
- 支持SharpTimer事件监听
- 完整的玩家状态管理

## 技术支持

如果遇到问题，请检查：
1. 所有依赖插件是否正确安装
2. 日志文件中的错误信息
3. 插件加载顺序是否正确

## 许可证

本插件遵循相关开源许可证。
