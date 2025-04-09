using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.IO;
using DrawingPlugin.PluginCommands;


namespace DrawingPlugin.Main{

    public class plugin : IExtensionApplication
    {
        [CommandMethod("Color")]
        public void Color() {
            PluginCommandsMenus.Color();
        }

        [CommandMethod("Circle")]
        public void Circle()
        {
            PluginRegularFigures.Circle();
        }

        [CommandMethod("Regular")]
        public void Regular()
        {
            PluginCommandsMenus.DrawRegularFigure();
        }

        [CommandMethod("Line")]
        public void Lines()
        {
            PluginRegularFigures.Lines();
        }

        [CommandMethod("CopyToDB")]
        public void CopyToDB()
        {
            InsertFunctionality copy = new InsertFunctionality();
            copy.ExportGeometry();
        }

        [CommandMethod("InsertFromLIST")]
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
        public void Square()
        {
            PluginRegularFigures.Square();
        }

        [CommandMethod("Triangle")]
        public void Triangle()
        {
            PluginRegularFigures.Triangle();
        }

        [CommandMethod("Pentagon")]
        public void Pentagon()
        {
            PluginRegularFigures.Pentagon();
        }

        [CommandMethod("Hexagon")]
        public void Hexagon()
        {
            PluginRegularFigures.Hexagon();
        }

        public void Initialize()
        {
            PluginCommands.PluginCommandsMenus.Initialize();
        }
        public void Terminate() { }
    }
}
