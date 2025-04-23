using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawingPlugin.Main
{
    public class MathematicalOperations
    {
        public static double DegToRad(double degree)
        {
            return degree * Math.PI / 180;
        }
        public static (double, double) RotrationPoint(double x, double y, double cx, double cy, double angle)
        {
            double angleInRad = DegToRad(angle);
            x -= cx;
            y -= cy;
            double newX = x * Math.Cos(angleInRad) - y * Math.Cos(angleInRad);
            double newY = x * Math.Sin(angleInRad) + y * Math.Cos(angleInRad);


            return (newX + cx, newY + cy);
        }

        public static Color startColor(int index)
        {
            switch (index)
            {
                case 1: return Color.Red;
                case 2: return Color.Yellow;
                case 3: return Color.Green;
                case 4: return Color.Cyan;
                case 5: return Color.Blue;
                case 6: return Color.Magenta;
                case 7: return Color.White;
                default: return Color.White;
            }
        }

        public Point2d p3Dto2D(Point3d point)
        {
            Point2d res = new Point2d(point.X, point.Y);
            return res;
        }
    }
}
