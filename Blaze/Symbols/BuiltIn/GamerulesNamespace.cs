namespace Blaze.Symbols.BuiltIn
{
    internal sealed class GamerulesNamespace : BuiltInNamespace
    {
        //TODO: Add limitations to short values
        public FunctionSymbol SetGamerule { get; private set; }
        public FieldSymbol AnnounceAdvancements { get; }
        public FieldSymbol BlockExplosionDropDecay { get; }
        public FieldSymbol CommandBlockOutput { get; }
        public FieldSymbol CommandModificationBlockLimit { get; }
        public FieldSymbol DisableElytraMovementCheck { get; }
        public FieldSymbol DisableRaids { get; }
        public FieldSymbol DoDaylightCycle { get; }
        public FieldSymbol DoEntityDrops { get; }
        public FieldSymbol DoFireTick { get; }
        public FieldSymbol DoImmediateRespawn { get; }
        public FieldSymbol DoInsomnia { get; }
        public FieldSymbol DoLimitedCrafting { get; }
        public FieldSymbol DoMobLoot { get; }
        public FieldSymbol DoMobSpawning { get; }
        public FieldSymbol DoPatrolSpawning { get; }
        public FieldSymbol DoTileDrops { get; }
        public FieldSymbol DoTraderSpawning { get; }
        public FieldSymbol DoVinesSpread { get; }
        public FieldSymbol DoWardenSpawning { get; }
        public FieldSymbol DoWeatherCycle { get; }
        public FieldSymbol DrowningDamage { get; }
        public FieldSymbol EnderPearlsVanishOnDeath { get; }
        public FieldSymbol FallDamage { get; }
        public FieldSymbol FireDamage { get; }
        public FieldSymbol ForgiveDeadPlayers { get; }
        public FieldSymbol FreezeDamage { get; }
        public FieldSymbol GlobalSoundEffects { get; }
        public FieldSymbol KeepInventory { get; }
        public FieldSymbol LavaSourceConversion { get; }
        public FieldSymbol LogAdminCommands { get; }
        public FieldSymbol MaxCommandChainLength { get; }
        public FieldSymbol MaxCommandForkCount { get; }
        public FieldSymbol MaxEntityCramming { get; }
        public FieldSymbol MobExplosionDropDecay { get; }
        public FieldSymbol MobGriefing { get; }
        public FieldSymbol NaturalRegeneration { get; }
        public FieldSymbol PlayersNetherPortalCreativeDelay { get; }
        public FieldSymbol PlayersNetherPortalDefaultDelay { get; }
        public FieldSymbol PlayersSleepingPercentage { get; }
        public FieldSymbol ProjectilesCanBreakBlocks { get; }
        public FieldSymbol Pvp { get; }
        public FieldSymbol RandomTickSpeed { get; }
        public FieldSymbol ReducedDebugInfo { get; }
        public FieldSymbol SendCommandFeedback { get; }
        public FieldSymbol ShowDeathMessages { get; }
        public FieldSymbol SnowAccumulationHeight { get; }
        public FieldSymbol SpawnChunkRadius { get; }
        public FieldSymbol SpawnRadius { get; }
        public FieldSymbol SpectatorsGenerateChunks { get; }
        public FieldSymbol TntExplosionDropDecay { get; }
        public FieldSymbol UniversalAnger { get; }
        public FieldSymbol WaterSourceConversion { get; }

        private static List<FieldSymbol> _gamerules = new List<FieldSymbol>(74);

        public GamerulesNamespace(GeneralNamespace parent) : base("gamerules", parent)
        {
            SetGamerule = Function("set_gamerule", TypeSymbol.Void, new ParameterSymbol("rule", TypeSymbol.String), new ParameterSymbol("value", TypeSymbol.Int));

            AnnounceAdvancements = AddField(Symbol, "announceAdvancements", TypeSymbol.Bool);
            BlockExplosionDropDecay = AddField(Symbol, "blockExplosionDropDecay", TypeSymbol.Bool);
            CommandBlockOutput = AddField(Symbol, "commandBlockOutput", TypeSymbol.Bool);
            CommandModificationBlockLimit = AddField(Symbol, "commandModificationBlockLimit", TypeSymbol.Int);
            DisableElytraMovementCheck = AddField(Symbol, "disableElytraMovementCheck", TypeSymbol.Bool);
            DisableRaids = AddField(Symbol, "disableRaids", TypeSymbol.Bool);
            DoDaylightCycle = AddField(Symbol, "doDaylightCycle", TypeSymbol.Bool);
            DoEntityDrops = AddField(Symbol, "doEntityDrops", TypeSymbol.Bool);
            DoFireTick = AddField(Symbol, "doFireTick", TypeSymbol.Bool);
            DoImmediateRespawn = AddField(Symbol, "doImmediateRespawn", TypeSymbol.Bool);
            DoInsomnia = AddField(Symbol, "doInsomnia", TypeSymbol.Bool);
            DoLimitedCrafting = AddField(Symbol, "doLimitedCrafting", TypeSymbol.Bool);
            DoMobLoot = AddField(Symbol, "doMobLoot", TypeSymbol.Bool);
            DoMobSpawning = AddField(Symbol, "doMobSpawning", TypeSymbol.Bool);
            DoPatrolSpawning = AddField(Symbol, "doPatrolSpawning", TypeSymbol.Bool);
            DoTileDrops = AddField(Symbol, "doTileDrops", TypeSymbol.Bool);
            DoTraderSpawning = AddField(Symbol, "doTraderSpawning", TypeSymbol.Bool);
            DoVinesSpread = AddField(Symbol, "doVinesSpread", TypeSymbol.Bool);
            DoWardenSpawning = AddField(Symbol, "doWardenSpawning", TypeSymbol.Bool);
            DoWeatherCycle = AddField(Symbol, "doWeatherCycle", TypeSymbol.Bool);
            DrowningDamage = AddField(Symbol, "drowningDamage", TypeSymbol.Bool);
            EnderPearlsVanishOnDeath = AddField(Symbol, "enderPearlsVanishOnDeath", TypeSymbol.Bool);
            FallDamage = AddField(Symbol, "fallDamage", TypeSymbol.Bool);
            FireDamage = AddField(Symbol, "fireDamage", TypeSymbol.Bool);
            ForgiveDeadPlayers = AddField(Symbol, "forgiveDeadPlayers", TypeSymbol.Bool);
            FreezeDamage = AddField(Symbol, "freezeDamage", TypeSymbol.Bool);
            GlobalSoundEffects = AddField(Symbol, "globalSoundEffects", TypeSymbol.Bool);
            KeepInventory = AddField(Symbol, "keepInventory", TypeSymbol.Bool);
            LavaSourceConversion = AddField(Symbol, "lavaSourceConversion", TypeSymbol.Bool);
            LogAdminCommands = AddField(Symbol, "logAdminCommands", TypeSymbol.Bool);
            MaxCommandChainLength = AddField(Symbol, "maxCommandChainLength", TypeSymbol.Int);
            MaxCommandForkCount = AddField(Symbol, "maxCommandForkCount", TypeSymbol.Int);
            MaxEntityCramming = AddField(Symbol, "maxEntityCramming", TypeSymbol.Int);
            MobExplosionDropDecay = AddField(Symbol, "mobExplosionDropDecay", TypeSymbol.Bool);
            MobGriefing = AddField(Symbol, "mobGriefing", TypeSymbol.Bool);
            NaturalRegeneration = AddField(Symbol, "naturalRegeneration", TypeSymbol.Bool);
            PlayersNetherPortalCreativeDelay = AddField(Symbol, "playersNetherPortalCreativeDelay", TypeSymbol.Int);
            PlayersNetherPortalDefaultDelay = AddField(Symbol, "playersNetherPortalDefaultDelay", TypeSymbol.Int);
            PlayersSleepingPercentage = AddField(Symbol, "playersSleepingPercentage", TypeSymbol.Int);
            ProjectilesCanBreakBlocks = AddField(Symbol, "projectilesCanBreakBlocks", TypeSymbol.Bool);
            Pvp = AddField(Symbol, "pvp", TypeSymbol.Bool);
            RandomTickSpeed = AddField(Symbol, "randomTickSpeed", TypeSymbol.Int);
            ReducedDebugInfo = AddField(Symbol, "reducedDebugInfo", TypeSymbol.Bool);
            SendCommandFeedback = AddField(Symbol, "sendCommandFeedback", TypeSymbol.Bool);
            ShowDeathMessages = AddField(Symbol, "showDeathMessages", TypeSymbol.Bool);
            SnowAccumulationHeight = AddField(Symbol, "snowAccumulationHeight", TypeSymbol.Int);
            SpawnChunkRadius = AddField(Symbol, "spawnChunkRadius", TypeSymbol.Int);
            SpawnRadius = AddField(Symbol, "spawnRadius", TypeSymbol.Int);
            SpectatorsGenerateChunks = AddField(Symbol, "spectatorsGenerateChunks", TypeSymbol.Bool);
            TntExplosionDropDecay = AddField(Symbol, "tntExplosionDropDecay", TypeSymbol.Bool);
            UniversalAnger = AddField(Symbol, "universalAnger", TypeSymbol.Bool);
            WaterSourceConversion = AddField(Symbol, "waterSourceConversion", TypeSymbol.Bool);

            _gamerules.Clear();
            _gamerules.AddRange(new[]
            {
                    AnnounceAdvancements,
                    BlockExplosionDropDecay,
                    CommandBlockOutput,
                    CommandModificationBlockLimit,
                    DisableElytraMovementCheck,
                    DisableRaids,
                    DoDaylightCycle,
                    DoEntityDrops,
                    DoFireTick,
                    DoImmediateRespawn,
                    DoInsomnia,
                    DoLimitedCrafting,
                    DoMobLoot,
                    DoMobSpawning,
                    DoPatrolSpawning,
                    DoTileDrops,
                    DoTraderSpawning,
                    DoVinesSpread,
                    DoWardenSpawning,
                    DoWeatherCycle,
                    DrowningDamage,
                    EnderPearlsVanishOnDeath,
                    FallDamage,
                    FireDamage,
                    ForgiveDeadPlayers,
                    FreezeDamage,
                    GlobalSoundEffects,
                    KeepInventory,
                    LavaSourceConversion,
                    LogAdminCommands,
                    MaxCommandChainLength,
                    MaxCommandForkCount,
                    MaxEntityCramming,
                    MobExplosionDropDecay,
                    MobGriefing,
                    NaturalRegeneration,
                    PlayersNetherPortalCreativeDelay,
                    PlayersNetherPortalDefaultDelay,
                    PlayersSleepingPercentage,
                    ProjectilesCanBreakBlocks,
                    Pvp,
                    RandomTickSpeed,
                    ReducedDebugInfo,
                    SendCommandFeedback,
                    ShowDeathMessages,
                    SnowAccumulationHeight,
                    SpawnChunkRadius,
                    SpawnRadius,
                    SpectatorsGenerateChunks,
                    TntExplosionDropDecay,
                    UniversalAnger,
                    WaterSourceConversion
                });
        }

        public bool IsGamerule(FieldSymbol field) => _gamerules.Contains(field);
    }
}
