namespace DrawingPlugin.PluginCommands
{
    public class EntityData
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Layer { get; set; }
        public int Color { get; set; }
        public string GeometryData { get; set; }

        public string DisplayInfo =>
            $"{Type} (ID: {Id}, Слой: {Layer}, Цвет: {Color})";
    }
}