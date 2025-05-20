using System.Collections.Generic;
using System.Drawing;

namespace DrawingPlugin.PluginCommands
{
    public class DatabaseBlock
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<EntityData> EntitiesData { get; set; }
        public string CreatedAt { get; set; }
        public Bitmap Thumbnail { get; set; }
    }
    
    public class EntityData
    {
        public string Type { get; set; }
        public List<double[]> Points { get; set; }
        public List<double> Bulges { get; set; }
        public bool IsClosed { get; set; }
        public double[] Center { get; set; }
        public double Radius { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }
        public double[] MajorAxis { get; set; }
        public double RadiusRatio { get; set; }
        public double StartParam { get; set; }
        public double EndParam { get; set; }
    }
}