namespace AVG
{
    public enum GameState
    {
        Init,       // 初始化
        Normal,     // 正常（等待输入）
        Typing,     // 正在打字
        Interacting,// 等待交互（选项/弹窗确认）
        History,    // 浏览历史记录中
        Paused,     // 暂停/菜单
    }
}
