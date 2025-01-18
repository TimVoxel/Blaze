namespace Blaze.Symbols.BuiltIn
{
    internal sealed class EntitiesNamespace : BuiltInNamespace
    {
        public NamedTypeSymbol Entity;

        public EntitiesNamespace(MinecraftNamespace parent) : base("entities", parent)
        {
            Entity = AbstractClass("Entity");
            var airField = AddField(Entity, "Air", TypeSymbol.Int);
            var customNameField = AddField(Entity, "CustomName", TypeSymbol.Int);
            var customNameVisibleField = AddField(Entity, "CustomNameVisible", TypeSymbol.Bool);
            var fallDistance = AddField(Entity, "FallDistance", TypeSymbol.Float);
            var fire = AddField(Entity, "Fire", TypeSymbol.Int);
            var glowing = AddField(Entity, "Glowing", TypeSymbol.Bool);
            var hasVisualFire = AddField(Entity, "HasVisualFire", TypeSymbol.Bool);
            var invulnerable = AddField(Entity, "Invulnerable", TypeSymbol.Bool);
            var motion = AddField(Entity, "Motion", Minecraft.General.Vector3);
            var noGravity = AddField(Entity, "NoGravity", TypeSymbol.Bool);
            var onGround = AddField(Entity, "OnGround", TypeSymbol.Bool);
            //var passengers = Field(Entity)
            var portalCooldown = AddField(Entity, "PortalCooldown", TypeSymbol.Int);
            var pos = AddField(Entity, "Pos", Minecraft.General.Vector3);
            var rotation = AddField(Entity, "Rotation", Minecraft.General.Vector2f);
            var silent = AddField(Entity, "Silent", TypeSymbol.Bool);
            //var tags = Field(Entity, "Tags");
            var ticksFrozen = AddField(Entity, "TicksFrozen", TypeSymbol.Int);
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
