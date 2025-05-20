using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using DrawingPlugin.Main;

namespace DrawingPlugin.PluginCommands
{
    public class PluginRegularFigures
    {
        public static void Square(){
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;

            PromptPointOptions pointQuestion = new PromptPointOptions("Select a point to build a square");
            PromptPointResult resPoint = editor.GetPoint(pointQuestion);

            double X = resPoint.Value.X;
            double Y = resPoint.Value.Y;

            PromptDoubleOptions lengthQuestion = new PromptDoubleOptions("Enter the length of the side of the square");
            PromptDoubleResult reLength = editor.GetDouble(lengthQuestion);

            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Autodesk.AutoCAD.DatabaseServices.Polyline square = new Autodesk.AutoCAD.DatabaseServices.Polyline();



                square.AddVertexAt(0, new Point2d(X, Y), 0, 0, 0);
                square.AddVertexAt(1, new Point2d(X, Y + reLength.Value), 0, 0, 0);
                square.AddVertexAt(2, new Point2d(X + reLength.Value, Y + reLength.Value), 0, 0, 0);
                square.AddVertexAt(3, new Point2d(X + reLength.Value, Y), 0, 0, 0);

                square.ColorIndex = PluginCommandsMenus.color;
                square.Closed = true;

                btr.AppendEntity(square);
                tr.AddNewlyCreatedDBObject(square, true);

                tr.Commit();
            }
        }
        
        public static void Triangle()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;

            PromptPointOptions pointQuestion = new PromptPointOptions("Select a point to build a triangle");
            PromptPointResult resPoint = editor.GetPoint(pointQuestion);

            double X = resPoint.Value.X;
            double Y = resPoint.Value.Y;

            PromptDoubleOptions lengthQuestion = new PromptDoubleOptions("Enter the length of the side of the triangle");
            PromptDoubleResult reLength = editor.GetDouble(lengthQuestion);

            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Autodesk.AutoCAD.DatabaseServices.Polyline square = new Autodesk.AutoCAD.DatabaseServices.Polyline();



                square.AddVertexAt(0, new Point2d(X, Y), 0, 0, 0);
                square.AddVertexAt(1, new Point2d(X + reLength.Value/2, Y + reLength.Value * Math.Sqrt(3)/2), 0, 0, 0);
                square.AddVertexAt(2, new Point2d(X + reLength.Value, Y), 0, 0, 0);
                

                square.ColorIndex = PluginCommandsMenus.color;
                square.Closed = true;

                btr.AppendEntity(square);
                tr.AddNewlyCreatedDBObject(square, true);

                tr.Commit();
            }
        }
        
        public static void Pentagon()
        {

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;

            PromptPointOptions pointQuestion = new PromptPointOptions("Select a point to build a pentagon");
            PromptPointResult resPoint = editor.GetPoint(pointQuestion);

            double X = resPoint.Value.X;
            double Y = resPoint.Value.Y;

            PromptDoubleOptions lengthQuestion = new PromptDoubleOptions("Enter the length of the side of the pentagon");
            PromptDoubleResult reLength = editor.GetDouble(lengthQuestion);

            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Autodesk.AutoCAD.DatabaseServices.Polyline square = new Autodesk.AutoCAD.DatabaseServices.Polyline();



                square.AddVertexAt(0, new Point2d(X, Y), 0, 0, 0);
                square.AddVertexAt(1, new Point2d(X + reLength.Value, Y), 0, 0, 0);
                square.AddVertexAt(2, new Point2d(X + reLength.Value + reLength.Value*Math.Cos(MathematicalOperations.DegToRad(72.0)), Y+reLength.Value*Math.Sin(MathematicalOperations.DegToRad(72.0))), 0, 0, 0);
                square.AddVertexAt(3, new Point2d(X + reLength.Value/2, Y + reLength.Value * Math.Sqrt(5+2*Math.Sqrt(5))/2), 0, 0, 0);
                square.AddVertexAt(4, new Point2d(X - reLength.Value * Math.Cos(MathematicalOperations.DegToRad(72.0)), Y + reLength.Value * Math.Sin(MathematicalOperations.DegToRad(72.0))), 0, 0, 0);


                square.ColorIndex = PluginCommandsMenus.color;
                square.Closed = true;

                btr.AppendEntity(square);
                tr.AddNewlyCreatedDBObject(square, true);

                tr.Commit();
            }
        }
        
         public static void Hexagon()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;

            PromptPointOptions pointQuestion = new PromptPointOptions("Select a point to build a hexagon");
            PromptPointResult resPoint = editor.GetPoint(pointQuestion);

            double X = resPoint.Value.X;
            double Y = resPoint.Value.Y;

            PromptDoubleOptions lengthQuestion = new PromptDoubleOptions("Enter the length of the side of the hexagon");
            PromptDoubleResult reLength = editor.GetDouble(lengthQuestion);

            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Autodesk.AutoCAD.DatabaseServices.Polyline square = new Autodesk.AutoCAD.DatabaseServices.Polyline();



                square.AddVertexAt(0, new Point2d(X, Y), 0, 0, 0);
                square.AddVertexAt(1, new Point2d(X + reLength.Value, Y), 0, 0, 0);
                square.AddVertexAt(2, new Point2d(X + reLength.Value + reLength.Value * Math.Cos(MathematicalOperations.DegToRad(60)), Y + reLength.Value * Math.Sin(MathematicalOperations.DegToRad(60))), 0, 0, 0);
                square.AddVertexAt(3, new Point2d(X+ reLength.Value, Y + 2*reLength.Value * Math.Sin(MathematicalOperations.DegToRad(60))), 0, 0, 0);
                square.AddVertexAt(4, new Point2d(X, Y + 2*reLength.Value*Math.Sin(MathematicalOperations.DegToRad(60))), 0, 0, 0);
                square.AddVertexAt(5, new Point2d(X - reLength.Value * Math.Cos(MathematicalOperations.DegToRad(60)), Y + reLength.Value * Math.Sin(MathematicalOperations.DegToRad(60))), 0, 0, 0);


                square.ColorIndex = PluginCommandsMenus.color;
                square.Closed = true;

                btr.AppendEntity(square);
                tr.AddNewlyCreatedDBObject(square, true);

                tr.Commit();
            }
        }
        public static void Lines() 
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;
            MathematicalOperations tranc = new MathematicalOperations();
            PromptPointOptions fp = new PromptPointOptions("Select the first point");
            PromptPointResult rfp = editor.GetPoint(fp);
            PromptPointOptions sp = new PromptPointOptions("Select the second point");
            PromptPointResult rsp = editor.GetPoint(sp);
            Point2d firp = tranc.p3Dto2D(rfp.Value);
            Point2d secp = tranc.p3Dto2D(rsp.Value);

            Database db=doc.Database;
            using(Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                Autodesk.AutoCAD.DatabaseServices.Polyline polyline = new Autodesk.AutoCAD.DatabaseServices.Polyline();
                polyline.AddVertexAt(0, firp, 0, 0, 0);
                polyline.AddVertexAt(1, secp, 0, 0, 0);
                polyline.ColorIndex = PluginCommandsMenus.color;
                polyline.Closed=true;

                btr.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline,true);

                tr.Commit();
            }
        }
        public static void Circle() {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;

            PromptPointResult resPoint = editor.GetPoint("Select a point: ");

            Point3d center3D = resPoint.Value;
            Point2d center2D = new Point2d(center3D.X, center3D.Y);

            PromptDoubleOptions len = new PromptDoubleOptions("Enter the radius:\n");
            PromptDoubleResult resLen = editor.GetDouble(len);

            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                uint segment = 144;
                Autodesk.AutoCAD.DatabaseServices.Polyline polyline = new Autodesk.AutoCAD.DatabaseServices.Polyline();

                for (int i = 0; i < segment; i++)
                {

                    double rotation = 2 * Math.PI * i / segment;
                    double x = center2D.X + resLen.Value * Math.Cos(rotation);
                    double y = center2D.Y + resLen.Value * Math.Sin(rotation);

                    polyline.AddVertexAt(i, new Point2d(x, y), 0, 0, 0);
                }
                polyline.ColorIndex = PluginCommandsMenus.color;
                polyline.Closed = true;



                btr.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline, true);

                tr.Commit();
            }
        }

    }
}