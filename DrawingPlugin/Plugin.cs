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
                polyline.ColorIndex = 5;
                polyline.Closed = true;



                btr.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline, true);

                tr.Commit();
            }
        }


        [CommandMethod("Regular")]
        public void DrawRegularFigure()
        {

            Form1 Form = new Form1();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(Form);
            //Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            //Editor editor = doc.Editor;

            //PromptIntegerOptions question = new PromptIntegerOptions("Введите количество сторон n-угольника");
            //PromptIntegerResult resQuestion = editor.GetInteger(question);

            //PromptPointOptions point = new PromptPointOptions("Введите точку левого нижнего угла прямоугольника:\n");
            //PromptPointResult resPoint = editor.GetPoint(point);
            //Point3d point3D = resPoint.Value;
            //Point2d point2D = new Point2d(point3D.X, point3D.Y);

            //PromptDoubleOptions len = new PromptDoubleOptions("Введите длину стороны прямоугольника:\n");
            //PromptDoubleResult resLength = editor.GetDouble(len);

            //Database db = doc.Database;
            //    using (Transaction tr = db.TransactionManager.StartTransaction())
            //    {
            //        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            //        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            //        Autodesk.AutoCAD.DatabaseServices.Polyline polyline = new Autodesk.AutoCAD.DatabaseServices.Polyline();
            //        int quantityOfVertexes = resQuestion.Value;

            //        double radius = resLength.Value / (2 * (Math.Sin(Math.PI / quantityOfVertexes)));

            //        double centerX = point2D.X + radius * Math.Cos((90 - 360 / (2*quantityOfVertexes)) * (Math.PI / 180));
            //        double centerY = point2D.Y + radius * Math.Sin((90 - 360 / (2*quantityOfVertexes)) * (Math.PI / 180)); ;



            //        for (int i = 0; i < quantityOfVertexes; i++)
            //        {
            //            double angle = (i) * ((360 / quantityOfVertexes)) * (Math.PI / 180);
            //            double x = centerX + radius * Math.Cos(angle - Math.PI / quantityOfVertexes);
            //            double y = centerY + radius * Math.Sin(angle - Math.PI / quantityOfVertexes);

            //            polyline.AddVertexAt(i, new Point2d(x, y), 0, 0, 0);
            //        }

            //        polyline.ColorIndex = 3;
            //        polyline.Closed = true;


            //        btr.AppendEntity(polyline);
            //        tr.AddNewlyCreatedDBObject(polyline, true);

            //        tr.Commit();
            //    }

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
                polyline.ColorIndex = 9;
                polyline.Closed=true;

                btr.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline,true);

                tr.Commit();
            }

        }

        [CommandMethod("CopyToDB")]
        public void ExportLinesPolylinesSplinesArcsToDWG()
        {
            // Получение текущего документа, базы данных и редактора
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Настройка фильтра выбора для линий, полилиний, сплайнов и дуг
            TypedValue[] filter = new TypedValue[]
            {
            new TypedValue(-4, "<OR"),
            new TypedValue((int)DxfCode.Start, "LINE"),       // Линия
            new TypedValue((int)DxfCode.Start, "LWPOLYLINE"), // Полилиния
            new TypedValue((int)DxfCode.Start, "SPLINE"),     // Сплайн
            new TypedValue((int)DxfCode.Start, "ARC"),        // Дуга
            new TypedValue(-4, "OR>")
            };
            SelectionFilter selectionFilter = new SelectionFilter(filter);

            // Запрос выбора объектов у пользователя
            PromptSelectionResult selectionResult = ed.GetSelection(selectionFilter);

            // Проверка, были ли выбраны объекты
            if (selectionResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nНет выбранных объектов указанных типов.");
                return;
            }

            // Указание пути для сохранения нового DWG-файла
            string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ExportedObjects.dwg");

            // Создание новой базы данных для нового чертежа
            using (Database newDb = new Database(true, true))
            {
                using (Transaction tr = newDb.TransactionManager.StartTransaction())
                {
                    // Получение таблицы блоков и пространства модели нового чертежа
                    BlockTable newBlockTable = (BlockTable)tr.GetObject(newDb.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord newModelSpace = (BlockTableRecord)tr.GetObject(newBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // Коллекция для хранения ObjectId выбранных объектов
                    ObjectIdCollection objIds = new ObjectIdCollection();

                    // Транзакция для работы с текущей базой данных
                    using (Transaction oldTr = db.TransactionManager.StartTransaction())
                    {
                        foreach (SelectedObject selObj in selectionResult.Value)
                        {
                            if (selObj != null)
                            {
                                // Получение объекта как Entity
                                Entity entity = oldTr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                                // Проверка типа объекта
                                if (entity is Line || entity is Polyline || entity is Spline || entity is Arc)
                                {
                                    objIds.Add(entity.ObjectId);
                                }
                            }
                        }
                        oldTr.Commit();
                    }

                    // Клонирование объектов в новое пространство модели
                    IdMapping idMapping = new IdMapping();
                    db.WblockCloneObjects(objIds, newModelSpace.ObjectId, idMapping, DuplicateRecordCloning.Ignore, false);

                    // Завершение транзакции
                    tr.Commit();
                }

                // Сохранение нового DWG-файла (версия AutoCAD 2021)
                newDb.SaveAs(savePath, DwgVersion.AC1027);
            }

            // Обновление экрана и вывод сообщения об успехе
            doc.TransactionManager.QueueForGraphicsFlush();
            ed.WriteMessage("\nОбъекты (линии, полилинии, сплайны, дуги) успешно экспортированы в " + savePath);
        }



        [CommandMethod("InsertToLIST")]
        public void InsertPolylinesFromDWG()
        {
            // Получаем текущий документ и базу данных
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database destDb = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Запрашиваем путь к исходному DWG файлу
                PromptOpenFileOptions pfo = new PromptOpenFileOptions("Выберите DWG файл с полилиниями")
                {
                    Filter = "DWG файлы (*.dwg)|*.dwg"
                };

                PromptFileNameResult pfr = ed.GetFileNameForOpen(pfo);
                if (pfr.Status != PromptStatus.OK) return;

                string sourceDwgPath = pfr.StringResult;

                // Открываем исходную базу данных DWG
                using (Database sourceDb = new Database(false, true))
                {
                    sourceDb.ReadDwgFile(sourceDwgPath, FileOpenMode.OpenForReadAndAllShare, true, "");

                    using (Transaction tr = destDb.TransactionManager.StartTransaction())
                    {
                        // Получаем пространство модели целевого чертежа
                        BlockTable bt = tr.GetObject(destDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        // Создаем коллекцию для импорта объектов
                        ObjectIdCollection ids = new ObjectIdCollection();

                        // Открываем пространство модели исходного чертежа
                        using (Transaction sourceTr = sourceDb.TransactionManager.StartTransaction())
                        {
                            BlockTable sourceBt = sourceTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord sourceBtr = sourceTr.GetObject(sourceBt[BlockTableRecord.ModelSpace],
                                OpenMode.ForRead) as BlockTableRecord;

                            // Собираем все полилинии из исходного чертежа
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

                        // Копируем полилинии в целевой чертеж
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
            [CommandMethod("GetIndex")]
        public void GetIndex() {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;


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

                square.ColorIndex = 3;
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
                

                square.ColorIndex = 3;
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


                square.ColorIndex = 3;
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


                square.ColorIndex = 3;
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
