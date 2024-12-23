namespace Blaze.Symbols.BuiltIn
{
    internal sealed class EntitiesNamespace : BuiltInNamespace
    {
        public NamedTypeSymbol Entity;

        public EntitiesNamespace(MinecraftNamespace parent) : base("entities", parent)
        {
            Entity = AbstractClass("Entity");
            var airField = Field(Entity, "Air", TypeSymbol.Int);
            var customNameField = Field(Entity, "CustomName", TypeSymbol.Int);
            var customNameVisibleField = Field(Entity, "CustomNameVisible", TypeSymbol.Bool);
            var fallDistance = Field(Entity, "FallDistance", TypeSymbol.Float);
            var fire = Field(Entity, "Fire", TypeSymbol.Int);
            var glowing = Field(Entity, "Glowing", TypeSymbol.Bool);
            var hasVisualFire = Field(Entity, "HasVisualFire", TypeSymbol.Bool);
            var invulnerable = Field(Entity, "Invulnerable", TypeSymbol.Bool);
            var motion = Field(Entity, "Motion", Minecraft.General.Vector3);
            var noGravity = Field(Entity, "NoGravity", TypeSymbol.Bool);
            var onGround = Field(Entity, "OnGround", TypeSymbol.Bool);
            //var passengers = Field(Entity)
            var portalCooldown = Field(Entity, "PortalCooldown", TypeSymbol.Int);
            var pos = Field(Entity, "Pos", Minecraft.General.Vector3);
            var rotation = Field(Entity, "Rotation", Minecraft.General.Vector2f);
            var silent = Field(Entity, "Silent", TypeSymbol.Bool);
            //var tags = Field(Entity, "Tags");
            var ticksFrozen = Field(Entity, "TicksFrozen", TypeSymbol.Int);
            //var uuid =
        }
    }

    internal sealed class MinecraftNamespace : BuiltInNamespace
    {
        public ChatNamespace Chat { get; }
        public GeneralNamespace General { get; }

        public MinecraftNamespace() : base("minecraft")
        {
            Chat = new ChatNamespace(this);
            General = new GeneralNamespace(this);
        }
    }
}
