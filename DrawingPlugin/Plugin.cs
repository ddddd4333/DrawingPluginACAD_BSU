using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DrawingPlugin{

    public class plugin : IExtensionApplication
    {
        public static int color = 7;
        [CommandMethod("Color")]
        public void Color()
        {
            ChangeColor changeColor = new ChangeColor();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(changeColor);
            
        }
        
        [CommandMethod("Circle")]
        public void DrawCircle() {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;

            PromptPointResult resPoint = editor.GetPoint("Выберите точку: ");

            Point3d center3D = resPoint.Value;
            Point2d center2D = new Point2d(center3D.X, center3D.Y);

            PromptDoubleOptions len = new PromptDoubleOptions("Введите радиус:\n");
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
                polyline.ColorIndex = color;
                polyline.Closed = true;



                btr.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline, true);

                tr.Commit();
            }
        }


        [CommandMethod("Regular")]
        public void DrawRegularFigure()
        {
            RegFiguresMenu Form = new RegFiguresMenu();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(Form);
        }

        [CommandMethod("Line")]
        public void DrawLines() 
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;
            MathematicalOperations tranc = new MathematicalOperations();
            PromptPointOptions fp = new PromptPointOptions("Введите первую точку");
            PromptPointResult rfp = editor.GetPoint(fp);
            PromptPointOptions sp = new PromptPointOptions("Введите вторую точку");
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
                polyline.ColorIndex = color;
                polyline.Closed=true;

                btr.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline,true);

                tr.Commit();
            }

        }

        [CommandMethod("CopyToDB")]
        public void ExportLinesPolylinesSplinesArcsToDWG()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            TypedValue[] filter = new TypedValue[]
            {
            new TypedValue(-4, "<OR"),
            new TypedValue((int)DxfCode.Start, "LINE"),      
            new TypedValue((int)DxfCode.Start, "LWPOLYLINE"), 
            new TypedValue((int)DxfCode.Start, "SPLINE"),     
            new TypedValue((int)DxfCode.Start, "ARC"),        
            new TypedValue(-4, "OR>")
            };
            SelectionFilter selectionFilter = new SelectionFilter(filter);

          
            PromptSelectionResult selectionResult = ed.GetSelection(selectionFilter);

            
            if (selectionResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nНет выбранных объектов указанных типов.");
                return;
            }

            
            string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ExportedObjects.dwg");

            
            using (Database newDb = new Database(true, true))
            {
                using (Transaction tr = newDb.TransactionManager.StartTransaction())
                {
                   
                    BlockTable newBlockTable = (BlockTable)tr.GetObject(newDb.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord newModelSpace = (BlockTableRecord)tr.GetObject(newBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                  
                    ObjectIdCollection objIds = new ObjectIdCollection();

                
                    using (Transaction oldTr = db.TransactionManager.StartTransaction())
                    {
                        foreach (SelectedObject selObj in selectionResult.Value)
                        {
                            if (selObj != null)
                            {
                             
                                Entity entity = oldTr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                                
                                if (entity is Line || entity is Polyline || entity is Spline || entity is Arc)
                                {
                                    objIds.Add(entity.ObjectId);
                                }
                            }
                        }
                        oldTr.Commit();
                    }

                    
                    IdMapping idMapping = new IdMapping();
                    db.WblockCloneObjects(objIds, newModelSpace.ObjectId, idMapping, DuplicateRecordCloning.Ignore, false);

                   
                    tr.Commit();
                }

                newDb.SaveAs(savePath, DwgVersion.AC1027);
            }

   
            doc.TransactionManager.QueueForGraphicsFlush();
            ed.WriteMessage("\nОбъекты (линии, полилинии, сплайны, дуги) успешно экспортированы в " + savePath);
        }

        [CommandMethod("InsertToLIST")]
        public void InsertPolylinesFromDWG()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database destDb = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                
                PromptOpenFileOptions pfo = new PromptOpenFileOptions("Выберите DWG файл с полилиниями")
                {
                    Filter = "DWG файлы (*.dwg)|*.dwg"
                };

                PromptFileNameResult pfr = ed.GetFileNameForOpen(pfo);
                if (pfr.Status != PromptStatus.OK) return;

                string sourceDwgPath = pfr.StringResult;

                using (Database sourceDb = new Database(false, true))
                {
                    sourceDb.ReadDwgFile(sourceDwgPath, FileOpenMode.OpenForReadAndAllShare, true, "");

                    using (Transaction tr = destDb.TransactionManager.StartTransaction())
                    {
                      
                        BlockTable bt = tr.GetObject(destDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        ObjectIdCollection ids = new ObjectIdCollection();

                        using (Transaction sourceTr = sourceDb.TransactionManager.StartTransaction())
                        {
                            BlockTable sourceBt = sourceTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord sourceBtr = sourceTr.GetObject(sourceBt[BlockTableRecord.ModelSpace],
                                OpenMode.ForRead) as BlockTableRecord;

                            foreach (ObjectId objId in sourceBtr)
                            {
                                Entity ent = sourceTr.GetObject(objId, OpenMode.ForRead) as Entity;
                                if (ent is Polyline || ent is Polyline2d || ent is Polyline3d)
                                {
                                    ids.Add(objId);
                                }
                            }
                            sourceTr.Commit();
                        }

                        IdMapping mapping = new IdMapping();
                        sourceDb.WblockCloneObjects(ids, btr.ObjectId, mapping, DuplicateRecordCloning.Ignore, false);

                        tr.Commit();
                    }
                }

                ed.WriteMessage("\nПолилинии успешно импортированы!");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nОшибка: {ex.Message}");
            }
        }

        [CommandMethod("Square")]
        public void Square(){
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;

            PromptPointOptions pointQuestion = new PromptPointOptions("Выберите точку для построения квадрата");
            PromptPointResult resPoint = editor.GetPoint(pointQuestion);

            double X = resPoint.Value.X;
            double Y = resPoint.Value.Y;

            PromptDoubleOptions lengthQuestion = new PromptDoubleOptions("Введите длину стороны квадрата");
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

                square.ColorIndex = color;
                square.Closed = true;

                btr.AppendEntity(square);
                tr.AddNewlyCreatedDBObject(square, true);

                tr.Commit();
            }
        }

        [CommandMethod("Triangle")]
        public void Triangle()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;

            PromptPointOptions pointQuestion = new PromptPointOptions("Выберите точку для построения квадрата");
            PromptPointResult resPoint = editor.GetPoint(pointQuestion);

            double X = resPoint.Value.X;
            double Y = resPoint.Value.Y;

            PromptDoubleOptions lengthQuestion = new PromptDoubleOptions("Введите длину стороны квадрата");
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
                

                square.ColorIndex = color;
                square.Closed = true;

                btr.AppendEntity(square);
                tr.AddNewlyCreatedDBObject(square, true);

                tr.Commit();
            }
        }

        [CommandMethod("Pentagon")]
        public void Pentagon()
        {

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;

            PromptPointOptions pointQuestion = new PromptPointOptions("Выберите точку для построения квадрата");
            PromptPointResult resPoint = editor.GetPoint(pointQuestion);

            double X = resPoint.Value.X;
            double Y = resPoint.Value.Y;

            PromptDoubleOptions lengthQuestion = new PromptDoubleOptions("Введите длину стороны квадрата");
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


                square.ColorIndex = color;
                square.Closed = true;

                btr.AppendEntity(square);
                tr.AddNewlyCreatedDBObject(square, true);

                tr.Commit();
            }
        }

        [CommandMethod("Hexagon")]
        public void Hexagon()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;

            PromptPointOptions pointQuestion = new PromptPointOptions("Выберите точку для построения квадрата");
            PromptPointResult resPoint = editor.GetPoint(pointQuestion);

            double X = resPoint.Value.X;
            double Y = resPoint.Value.Y;

            PromptDoubleOptions lengthQuestion = new PromptDoubleOptions("Введите длину стороны квадрата");
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


                square.ColorIndex = color;
                square.Closed = true;

                btr.AppendEntity(square);
                tr.AddNewlyCreatedDBObject(square, true);

                tr.Commit();
            }
        }

        public void Initialize() {
            var editor = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            editor.WriteMessage("GOOD!!!!\n");

            MainMenu Form = new MainMenu();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(Form);


        }
        public void Terminate() { }
    }
}
