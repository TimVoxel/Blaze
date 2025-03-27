namespace Blaze.Emit.Nodes
{
    public class WeatherCommand : CommandNode
    {
        public string WeatherType { get; }
        public string? Duration { get; }
        public string? TimeUnits { get; }

        public override string Keyword => "weather";
        public override EmittionNodeKind Kind => EmittionNodeKind.WeatherCommand;

        public override string Text
        {
            get
            {
                if (Duration != null)
                {
                    if (TimeUnits != null)
                        return $"{Keyword} {WeatherType} {Duration}{TimeUnits}";
                    else
                        return $"{Keyword} {WeatherType} {Duration}";
                }
                else
                    return $"{Keyword} {WeatherType}";
            }
        }

        public WeatherCommand(string weatherType, string? duration = null, string? timeUnits = null)
        {
            WeatherType = weatherType;
            Duration = duration;
            TimeUnits = timeUnits;
        }
    }
}
