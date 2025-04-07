using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawingPlugin
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

        public Point2d p3Dto2D(Point3d point)
        {
            Point2d res = new Point2d(point.X, point.Y);
            return res;
        }
    }
}
