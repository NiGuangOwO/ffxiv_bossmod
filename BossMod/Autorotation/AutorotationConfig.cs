namespace BossMod
{
    [ConfigDisplay(Name = "自动循环设置 (实验性!)", Order = 5)]
    class AutorotationConfig : ConfigNode
    {
        [PropertyDisplay("禁用UseAction Hook(提高兼容性, 但将无法使用自动输出, 更改后重载)")]
        public bool Disable_Hook = true;

        [PropertyDisplay("启用自动循环")]
        public bool Enabled = false;

        [PropertyDisplay("记录信息")]
        public bool Logging = false;

        [PropertyDisplay("显示游戏内GUI")]
        public bool ShowUI = false;

        [PropertyDisplay("在世界内显示移动指示")]
        public bool ShowPositionals = false;

        [PropertyDisplay("启用能影响玩家位置的技能 (例如 猛攻, 蛮荒崩裂)")]
        public bool EnableMovement = true;

        [PropertyDisplay("Sticky auto actions")]
        public bool StickyAutoActions = false;
    }
}
