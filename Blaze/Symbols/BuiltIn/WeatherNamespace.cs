namespace Blaze.Symbols.BuiltIn
{
    internal sealed class WeatherNamespace : BuiltInNamespace
    {
        public EnumSymbol Weather { get; }
        
        public FunctionSymbol SetWeather { get; }
        public FunctionSymbol SetWeatherForTicks { get; }
        public FunctionSymbol SetWeatherForSeconds { get; }
        public FunctionSymbol SetWeatherForDays { get; }

        public WeatherNamespace(GeneralNamespace parent) : base("weather", parent)
        {
            Weather = Enum("Weather", true);
            DeclareEnumMember("Clear", Weather, 0);
            DeclareEnumMember("Rain", Weather, 1);
            DeclareEnumMember("Thunder", Weather, 2);

            SetWeather = Function("set_weather", TypeSymbol.Void, Parameter("kind", Weather));
            SetWeatherForTicks = Function("set_weather_for_ticks", TypeSymbol.Void, Parameter("kind", Weather), Parameter("duration", TypeSymbol.Int));
            SetWeatherForSeconds = Function("set_weather_for_seconds", TypeSymbol.Void, Parameter("kind", Weather), Parameter("duration", TypeSymbol.Int));
            SetWeatherForDays = Function("set_weather_for_days", TypeSymbol.Void, Parameter("kind", Weather), Parameter("duration", TypeSymbol.Int));
        }
    }
}
