using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Text.Json;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

namespace DrawingPlugin.PluginCommands
{
    public class InsertCommands
    {
        [CommandMethod("InsertFromLIST")]
        public void PreviewDatabaseElements()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter full path to SQLite database file (e.g., C:\\Temp\\blocks.db): ");
                pStrOpts.AllowSpaces = true;
                PromptResult pStrRes = ed.GetString(pStrOpts);

                if (pStrRes.Status != PromptStatus.OK)
                    return;

                string dbPath = pStrRes.StringResult;
                
                if (string.IsNullOrWhiteSpace(dbPath))
                {
                    ed.WriteMessage("\nInvalid database path.");
                    return;
                }
                
                bool dbExists = File.Exists(dbPath);

                if (!dbExists)
                {
                    PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("\nDatabase file not found. Do you want to create a new one?");
                    pKeyOpts.Keywords.Add("Yes");
                    pKeyOpts.Keywords.Add("No");
                    pKeyOpts.Keywords.Default = "No";
                    pKeyOpts.AllowNone = false;

                    PromptResult pKeyRes = ed.GetKeywords(pKeyOpts);

                    if (pKeyRes.Status != PromptStatus.OK || pKeyRes.StringResult == "No")
                    {
                        ed.WriteMessage("\nOperation canceled.");
                        return;
                    }
                    
                    try
                    {
                        string directory = Path.GetDirectoryName(dbPath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        SQLiteConnection.CreateFile(dbPath);
                        
                        using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                        {
                            connection.Open();

                            using (SQLiteCommand command = new SQLiteCommand(
                                "CREATE TABLE IF NOT EXISTS Entities (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Data TEXT, CreatedAt TEXT)",
                                connection))
                            {
                                command.ExecuteNonQuery();
                            }

                            connection.Close();
                        }

                        ed.WriteMessage($"\nNew database created at: {dbPath}");
                        ed.WriteMessage("\nUse the BLOCKCOPY command to add entities to the database first.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        ed.WriteMessage($"\nError creating database: {ex.Message}");
                        return;
                    }
                }
                
                if (!ValidateDatabaseStructure(dbPath))
                {
                    ed.WriteMessage("\nThe database file is not valid or does not contain the required tables.");
                    return;
                }
                
                List<DatabaseBlock> blocks = LoadBlocksFromDatabase(dbPath);

                if (blocks.Count == 0)
                {
                    ed.WriteMessage("\nNo blocks found in the database. Use the BLOCKCOPY command to add entities first.");
                    return;
                }
                
                GenerateThumbnails(blocks);
                
                using (PreviewForm previewForm = new PreviewForm(blocks, dbPath))
                {
                    Application.ShowModalDialog(previewForm);
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    ed.WriteMessage($"\nInner Exception: {ex.InnerException.Message}");
                }

                ed.WriteMessage($"\nStack Trace: {ex.StackTrace}");
            }
        }

        private bool ValidateDatabaseStructure(string dbPath)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();
                    
                    using (SQLiteCommand command = new SQLiteCommand(
                        "SELECT name FROM sqlite_master WHERE type='table' AND name='Entities'",
                        connection))
                    {
                        object result = command.ExecuteScalar();
                        return result != null && result.ToString() == "Entities";
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private List<DatabaseBlock> LoadBlocksFromDatabase(string dbPath)
        {
            List<DatabaseBlock> blocks = new List<DatabaseBlock>();
            string connectionString = $"Data Source={dbPath};Version=3;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(
                    "SELECT Id, Name, Data, CreatedAt FROM Entities ORDER BY CreatedAt DESC",
                    connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                int id = reader.GetInt32(0);
                                string name = reader.GetString(1);
                                string jsonData = reader.GetString(2);
                                string createdAt = reader.GetString(3);

                                List<EntityData> entitiesData = JsonSerializer.Deserialize<List<EntityData>>(jsonData);

                                blocks.Add(new DatabaseBlock
                                {
                                    Id = id,
                                    Name = name,
                                    EntitiesData = entitiesData,
                                    CreatedAt = createdAt
                                });
                            }
                            catch (Exception ex)
                            {
                                Document doc = Application.DocumentManager.MdiActiveDocument;
                                doc.Editor.WriteMessage($"\nError loading block: {ex.Message}");
                            }
                        }
                    }
                }
            }

            return blocks;
        }

        private void GenerateThumbnails(List<DatabaseBlock> blocks)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            foreach (DatabaseBlock block in blocks)
            {
                try
                {
                    ed.WriteMessage($"\nГенерация миниатюры для блока: {block.Name}");

                    using (Database tempDb = new Database(true, true))
                    {
                        InitTempDatabase(tempDb);

                        using (Transaction tr = tempDb.TransactionManager.StartTransaction())
                        {
                            BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(
                                SymbolUtilityServices.GetBlockModelSpaceId(tempDb),
                                OpenMode.ForWrite
                            );

                            foreach (EntityData entityData in block.EntitiesData)
                            {
                                using (Entity entity = CreateEntity(tempDb, entityData))
                                {
                                    if (entity != null)
                                    {
                                        modelSpace.AppendEntity(entity);
                                        tr.AddNewlyCreatedDBObject(entity, true);
                                    }
                                }
                            }
                            tr.Commit();
                        }

                        Extents3d? extents = GetDatabaseExtents(tempDb);
                        block.Thumbnail = RenderThumbnail(tempDb, extents ?? new Extents3d(), 150, 150);
                    }
                }
                catch (Exception ex)
                {
                    block.Thumbnail = CreateErrorThumbnail(150, 150, ex.Message);
                    ed.WriteMessage($"\nОшибка: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private void InitTempDatabase(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
                LinetypeTable ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForWrite);

                if (!lt.Has("0"))
                {
                    using (LayerTableRecord ltr = new LayerTableRecord())
                    {
                        ltr.Name = "0";
                        lt.Add(ltr);
                        tr.AddNewlyCreatedDBObject(ltr, true);
                    }
                }
                tr.Commit();
            }
        }
        
        private Entity CreateEntity(Database targetDb, EntityData data)
        {
            try
            {
                Entity entity = data.Type switch
                {
                    "Polyline" => CreatePolyline(targetDb, data),
                    "Line" => CreateLine(targetDb, data),
                    "Arc" => CreateArc(targetDb, data),
                    "Ellipse" => CreateEllipse(targetDb, data),
                    _ => null
                };

                if (entity != null)
                {
                    entity.Layer = "0";
                    entity.ColorIndex = PluginCommandsMenus.color;
                    entity.Linetype = "ByLayer";
                    entity.SetDatabaseDefaults(targetDb);
                }
                return entity;
            }
            catch
            {
                return null;
            }
        }

        private Polyline CreatePolyline(Database db, EntityData data)
        {
            if (data.Points?.Count < 2) return null;

            Polyline pline = new Polyline();
            pline.SetDatabaseDefaults(db);
    
            for (int i = 0; i < data.Points.Count; i++)
            {
                pline.AddVertexAt(i, 
                    new Point2d(data.Points[i][0], data.Points[i][1]), 
                    data.Bulges?.Count > i ? data.Bulges[i] : 0, 
                    0, 0);
            }
            pline.Closed = data.IsClosed;
            return pline;
        }

        private Line CreateLine(Database targetDb, EntityData data)
        {
            try
            {
                if (data.Points?.Count < 2) 
                {
                    return null;
                }

                Point3d start = SafePointConversion(data.Points[0]);
                Point3d end = SafePointConversion(data.Points[1]);

                Line line = new Line(start, end);
                ConfigureEntity(targetDb, line);
                
                return line;
            }
            catch
            {
                return null;
            }
        }

        private Arc CreateArc(Database targetDb, EntityData data)
        {
            try
            {
                if (data.Center?.Length < 2 || 
                    data.Radius <= 0 || 
                    Math.Abs(data.StartAngle - data.EndAngle) < 0.001)
                {
                    return null;
                }

                Point3d center = SafePointConversion(data.Center);
        
                double startAngle = NormalizeAngle(data.StartAngle);
                double endAngle = NormalizeAngle(data.EndAngle);

                if (startAngle > endAngle)
                    endAngle += 2 * Math.PI;

                Arc arc = new Arc(center, data.Radius, startAngle, endAngle);
        
                ConfigureEntity(targetDb, arc);
                return arc;
            }
            catch (Exception ex)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                doc.Editor.WriteMessage($"\nОшибка создания дуги: {ex.Message}");
                return null;
            }
        }

        private Ellipse CreateEllipse(Database targetDb, EntityData data)
        {
            try
            {
                if (data.Center?.Length < 2 || 
                    data.MajorAxis?.Length < 2 || 
                    data.RadiusRatio <= 0 || 
                    data.RadiusRatio > 1)
                {
                    return null;
                }

                Vector3d majorAxis = new Vector3d(
                    data.MajorAxis[0],
                    data.MajorAxis[1],
                    0);

                Ellipse ellipse = new Ellipse(
                    SafePointConversion(data.Center),
                    Vector3d.ZAxis,
                    majorAxis,
                    data.RadiusRatio,
                    NormalizeParameter(data.StartParam),
                    NormalizeParameter(data.EndParam)
                );

                ConfigureEntity(targetDb, ellipse);
                return ellipse;
            }
            catch
            {
                return null;
            }
        }

        private void ConfigureEntity(Database targetDb, Entity entity)
        {
            if (entity == null) return;

            entity.SetDatabaseDefaults(targetDb);
            
            entity.Layer = "0";
            entity.ColorIndex = PluginCommandsMenus.color;
            entity.Linetype = "ByLayer";
            entity.LinetypeScale = 1.0;
        }

        private Point3d SafePointConversion(double[] coordinates)
        {
            return new Point3d(
                coordinates?.Length > 0 ? coordinates[0] : 0.0,
                coordinates?.Length > 1 ? coordinates[1] : 0.0,
                coordinates?.Length > 2 ? coordinates[2] : 0.0
            );
        }

        private double NormalizeAngle(double angle)
        {
            angle %= 2 * Math.PI;
            return angle < 0 ? angle + 2 * Math.PI : angle;
        }

        private double NormalizeParameter(double parameter)
        {
            return Math.Max(0, Math.Min(parameter, 2 * Math.PI));
        }

        private Bitmap CreateDefaultThumbnail(int width, int height, string blockName)
        {
            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.DrawRectangle(Pens.LightGray, 0, 0, width - 1, height - 1);

                g.DrawRectangle(Pens.Gray, width / 4, height / 4, width / 2, height / 2);
                g.DrawLine(Pens.Gray, width / 4, height / 4, width * 3 / 4, height * 3 / 4);
                g.DrawLine(Pens.Gray, width / 4, height * 3 / 4, width * 3 / 4, height / 4);

                using (System.Drawing.Font font = new System.Drawing.Font("Arial", 8))
                {
                    string text = "No preview";
                    SizeF textSize = g.MeasureString(text, font);
                    g.DrawString(text, font, Brushes.Gray,
                        (width - textSize.Width) / 2,
                        height - textSize.Height - 5);
                }
            }

            return bitmap;
        }

        private Bitmap CreateErrorThumbnail(int width, int height, string errorMessage)
        {
            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.DrawRectangle(new Pen(Color.Red), 0, 0, width - 1, height - 1);

                g.DrawLine(new Pen(Color.Red, 2), width / 4, height / 4, width * 3 / 4, height * 3 / 4);
                g.DrawLine(new Pen(Color.Red, 2), width / 4, height * 3 / 4, width * 3 / 4, height / 4);

                using (System.Drawing.Font font = new System.Drawing.Font("Arial", 7))
                {
                    string text = "Nothing";
                    SizeF textSize = g.MeasureString(text, font);
                    g.DrawString(text, font, Brushes.Red,
                        (width - textSize.Width) / 2,
                        height - textSize.Height - 5);
                }
            }

            return bitmap;
        }

        private Extents3d? GetDatabaseExtents(Database db)
        {
            Extents3d? result = null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in modelSpace)
                {
                    Entity entity = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                    if (entity != null)
                    {
                        Extents3d entityExtents = entity.GeometricExtents;
                        if (result.HasValue)
                        {
                            Point3d min = result.Value.MinPoint;
                            Point3d max = result.Value.MaxPoint;

                            min = new Point3d(
                                Math.Min(min.X, entityExtents.MinPoint.X),
                                Math.Min(min.Y, entityExtents.MinPoint.Y),
                                Math.Min(min.Z, entityExtents.MinPoint.Z));

                            max = new Point3d(
                                Math.Max(max.X, entityExtents.MaxPoint.X),
                                Math.Max(max.Y, entityExtents.MaxPoint.Y),
                                Math.Max(max.Z, entityExtents.MaxPoint.Z));

                            result = new Extents3d(min, max);
                        }
                        else
                            result = entityExtents;
                    }
                }

                tr.Commit();
            }

            return result;
        }

        private Bitmap RenderThumbnail(Database db, Extents3d extents, int width, int height)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            Bitmap bitmap = new Bitmap(width, height);

            try
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.White);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    double dbWidth = extents.MaxPoint.X - extents.MinPoint.X;
                    double dbHeight = extents.MaxPoint.Y - extents.MinPoint.Y;

                    if (dbWidth < 0.001) dbWidth = 1;
                    if (dbHeight < 0.001) dbHeight = 1;

                    double padding = Math.Max(dbWidth, dbHeight) * 0.1;
                    
                    Point3d minWithPadding = new Point3d(
                        extents.MinPoint.X - padding,
                        extents.MinPoint.Y - padding,
                        extents.MinPoint.Z);
                    
                    Point3d maxWithPadding = new Point3d(
                        extents.MaxPoint.X + padding,
                        extents.MaxPoint.Y + padding,
                        extents.MaxPoint.Z);
                    
                    dbWidth = maxWithPadding.X - minWithPadding.X;
                    dbHeight = maxWithPadding.Y - minWithPadding.Y;

                    double scaleX = width / dbWidth;
                    double scaleY = height / dbHeight;
                    
                    double scale = Math.Min(scaleX, scaleY);

                    double offsetX = (width - (dbWidth * scale)) / 2;
                    double offsetY = (height - (dbHeight * scale)) / 2;

                    ed.WriteMessage($"\nRender params: Width={dbWidth}, Height={dbHeight}, Scale={scale}, OffsetX={offsetX}, OffsetY={offsetY}");

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                        int drawnEntities = 0;

                        foreach (ObjectId objId in modelSpace)
                        {
                            try
                            {
                                Entity entity = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                                if (entity != null)
                                {
                                    if (entity is Polyline)
                                    {
                                        Polyline pline = entity as Polyline;
                                        DrawPolyline(g, pline, minWithPadding, scale, offsetX, offsetY, height);
                                        drawnEntities++;
                                    }
                                    else if (entity is Line)
                                    {
                                        Line line = entity as Line;
                                        DrawLine(g, line, minWithPadding, scale, offsetX, offsetY, height);
                                        drawnEntities++;
                                    }
                                    else if (entity is Arc)
                                    {
                                        Arc arc = entity as Arc;
                                        DrawArc(g, arc, minWithPadding, scale, offsetX, offsetY, height);
                                        drawnEntities++;
                                    }
                                    else if (entity is Ellipse)
                                    {
                                        Ellipse ellipse = entity as Ellipse;
                                        DrawEllipse(g, ellipse, minWithPadding, scale, offsetX, offsetY, height);
                                        drawnEntities++;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                ed.WriteMessage($"\nError drawing entity: {ex.Message}");
                            }
                        }

                        ed.WriteMessage($"\nSuccessfully drew {drawnEntities} entities");
                        tr.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError during rendering: {ex.Message}");
                ed.WriteMessage($"\nStack Trace: {ex.StackTrace}");

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.White);
                    using (Pen pen = new Pen(Color.Red, 1))
                    {
                        g.DrawRectangle(pen, 0, 0, width - 1, height - 1);
                        g.DrawString("Render Error", new System.Drawing.Font("Arial", 8), Brushes.Red, width / 2 - 30, height / 2 - 5);
                    }
                }
            }

            return bitmap;
        }

        private void DrawPolyline(Graphics g, Polyline pline, Point3d minPoint, double scale, double offsetX, double offsetY, int height)
        {
            if (pline.NumberOfVertices < 2) return;
            
            using (Pen pen = new Pen(Color.Black, 1))
            {
                bool hasBulges = false;
                for (int i = 0; i < pline.NumberOfVertices; i++)
                {
                    if (Math.Abs(pline.GetBulgeAt(i)) > 0.0001)
                    {
                        hasBulges = true;
                        break;
                    }
                }

                if (hasBulges)
                {
                    List<PointF> curvePoints = new List<PointF>();
            
                    for (int i = 0; i < pline.NumberOfVertices; i++)
                    {
                        Point2d startPt = pline.GetPoint2dAt(i);
                        double bulge = pline.GetBulgeAt(i);
                
                        float x1 = (float)((startPt.X - minPoint.X) * scale + offsetX);
                        float y1 = (float)(height - ((startPt.Y - minPoint.Y) * scale + offsetY));
                        curvePoints.Add(new PointF(x1, y1));
                
                        if (Math.Abs(bulge) > 0.0001 && (i < pline.NumberOfVertices - 1 || pline.Closed))
                        {
                            int nextIdx = (i + 1) % pline.NumberOfVertices;
                            Point2d endPt = pline.GetPoint2dAt(nextIdx);
                    
                            double chordLength = startPt.GetDistanceTo(endPt);
                            double sagHeight = Math.Abs(bulge) * chordLength / 2.0;
                            double apothem = (chordLength / 2.0) / Math.Abs(bulge);
                            double radius = Math.Sqrt(Math.Pow(apothem, 2) + Math.Pow(chordLength / 2.0, 2));
                    
                            Vector2d midPoint = (startPt.GetAsVector() + endPt.GetAsVector()) / 2.0;
                            Vector2d perpVector = new Vector2d(-(endPt.Y - startPt.Y), endPt.X - startPt.X).GetNormal();
                            if (bulge < 0) perpVector = -perpVector;
                            Point2d center = new Point2d(
                                midPoint.X + perpVector.X * apothem,
                                midPoint.Y + perpVector.Y * apothem);
                    
                            double startAngle = Math.Atan2(startPt.Y - center.Y, startPt.X - center.X);
                            double endAngle = Math.Atan2(endPt.Y - center.Y, endPt.X - center.X);
                    
                            if (bulge > 0 && startAngle > endAngle) endAngle += 2 * Math.PI;
                            if (bulge < 0 && startAngle < endAngle) startAngle += 2 * Math.PI;
                    
                            int segments = Math.Max(5, (int)(Math.Abs(endAngle - startAngle) * radius / 5));
                            double angleStep = (endAngle - startAngle) / segments;
                    
                            for (int j = 1; j < segments; j++)
                            {
                                double angle = startAngle + j * angleStep;
                                double arcX = center.X + radius * Math.Cos(angle);
                                double arcY = center.Y + radius * Math.Sin(angle);
                        
                                float px = (float)((arcX - minPoint.X) * scale + offsetX);
                                float py = (float)(height - ((arcY - minPoint.Y) * scale + offsetY));
                                curvePoints.Add(new PointF(px, py));
                            }
                        }
                    }
            
                    if (curvePoints.Count > 1)
                    {
                        if (pline.Closed && curvePoints.Count > 2)
                        {
                            g.DrawPolygon(pen, curvePoints.ToArray());
                        }
                        else
                        {
                            g.DrawLines(pen, curvePoints.ToArray());
                        }
                    }
                }
                else
                {
                    System.Drawing.Point[] points = new System.Drawing.Point[pline.NumberOfVertices];
            
                      points = new System.Drawing.Point[pline.NumberOfVertices];
            
                    for (int i = 0; i < pline.NumberOfVertices; i++)
                    {
                        Point2d pt = pline.GetPoint2dAt(i);
                
                        float x = (float)((pt.X - minPoint.X) * scale + offsetX);
                        float y = (float)(height - ((pt.Y - minPoint.Y) * scale + offsetY));
                
                        points[i] = new System.Drawing.Point((int)x, (int)y);
                    }
            
                    if (pline.Closed && points.Length > 2)
                    {
                        g.DrawPolygon(pen, points);
                    }
                    else
                    {
                        g.DrawLines(pen, points);
                    }
                }
            }
        }

        private void DrawLine(Graphics g, Line line, Point3d minPoint, double scale, double offsetX, double offsetY, int height)
        {
            float x1 = (float)((line.StartPoint.X - minPoint.X) * scale + offsetX);
            float y1 = (float)(height - ((line.StartPoint.Y - minPoint.Y) * scale + offsetY)); 
            float x2 = (float)((line.EndPoint.X - minPoint.X) * scale + offsetX);
            float y2 = (float)(height - ((line.EndPoint.Y - minPoint.Y) * scale + offsetY)); 

            using (Pen pen = new Pen(Color.Black, 1))
            {
                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }

        private void DrawArc(Graphics g, Arc arc, Point3d minPoint, double scale, double offsetX, double offsetY, int height)
        {
            try
            {
                if (arc.Radius <= 0)
                    return;

                float centerX = (float)((arc.Center.X - minPoint.X) * scale + offsetX);
                float centerY = (float)(height - ((arc.Center.Y - minPoint.Y) * scale + offsetY));

                float radius = (float)(arc.Radius * scale);

                RectangleF rect = new RectangleF(centerX - radius, centerY - radius, radius * 2, radius * 2);
                
                float startAngleDegrees = (float)(360 - (arc.StartAngle * 180 / Math.PI));
                float endAngleDegrees = (float)(360 - (arc.EndAngle * 180 / Math.PI));
        
                float sweepAngle = startAngleDegrees - endAngleDegrees;
        
                if (sweepAngle < 0) 
                    sweepAngle += 360;
                else if (Math.Abs(sweepAngle) < 0.01)
                    sweepAngle = 360;
            
                using (Pen pen = new Pen(Color.Black, 1))
                {
                    g.DrawArc(pen, rect, startAngleDegrees, -sweepAngle);
                }
            }
            catch (Exception ex)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                doc.Editor.WriteMessage($"\nОшибка отрисовки дуги: {ex.Message}");
            }
        }

        private void DrawEllipse(Graphics g, Ellipse ellipse, Point3d minPoint, double scale, double offsetX, double offsetY, int height)
        {
            try
            {
                float centerX = (float)((ellipse.Center.X - minPoint.X) * scale + offsetX);
                float centerY = (float)(height - ((ellipse.Center.Y - minPoint.Y) * scale + offsetY)); 

                float majorRadius = (float)(ellipse.MajorAxis.Length * scale);
                float minorRadius = (float)(majorRadius * ellipse.RadiusRatio);

                float angleDegrees = (float)(Math.Atan2(ellipse.MajorAxis.Y, ellipse.MajorAxis.X) * 180 / Math.PI);

                RectangleF rect = new RectangleF(centerX - majorRadius, centerY - minorRadius, majorRadius * 2, minorRadius * 2);

                System.Drawing.Drawing2D.Matrix originalTransform = g.Transform.Clone();
                
                g.TranslateTransform(centerX, centerY);
                g.RotateTransform(angleDegrees);
                g.TranslateTransform(-centerX, -centerY);

                using (Pen pen = new Pen(Color.Black, 1))
                {
                    if (Math.Abs(ellipse.StartParam) < 0.001 && Math.Abs(ellipse.EndParam - Math.PI * 2) < 0.001)
                    {
                        g.DrawEllipse(pen, rect);
                    }
                    else
                    {
                        g.DrawEllipse(pen, rect);
                    }
                }

                g.Transform = originalTransform;
            }
            catch (Exception ex)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                doc.Editor.WriteMessage($"\nОшибка отрисовки эллипса: {ex.Message}");
            }
        }

        public bool InsertBlockAtPoint(DatabaseBlock block, Point3d insertionPoint)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
    
            try
            {
                using (Database tempDb = new Database(true, true))
                {
                    InitTempDatabase(tempDb);
                    
                    using (Transaction tr = tempDb.TransactionManager.StartTransaction())
                    {
                        BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(
                            SymbolUtilityServices.GetBlockModelSpaceId(tempDb),
                            OpenMode.ForWrite
                        );
                        
                        foreach (EntityData entityData in block.EntitiesData)
                        {
                            using (Entity entity = CreateEntity(tempDb, entityData))
                            {
                                if (entity != null)
                                {
                                    modelSpace.AppendEntity(entity);
                                    tr.AddNewlyCreatedDBObject(entity, true);
                                }
                            }
                        }
                        
                        tr.Commit();
                    }
                    
                    Extents3d? extents = GetDatabaseExtents(tempDb);
                    
                    if (!extents.HasValue)
                    {
                        ed.WriteMessage("\nCould not determine block extents.");
                        return false;
                    }
                    
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTableRecord currentSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                        
                        Vector3d displacement = insertionPoint - extents.Value.MinPoint;
                        
                        ed.WriteMessage($"\nInsertion point: {insertionPoint.X}, {insertionPoint.Y}, {insertionPoint.Z}");
                        ed.WriteMessage($"\nBlock min point: {extents.Value.MinPoint.X}, {extents.Value.MinPoint.Y}, {extents.Value.MinPoint.Z}");
                        ed.WriteMessage($"\nDisplacement vector: {displacement.X}, {displacement.Y}, {displacement.Z}");
                        
                        foreach (EntityData entityData in block.EntitiesData)
                        {
                            Entity entity = CreateEntityFromData(entityData);
                            
                            if (entity != null)
                            {
                                entity.TransformBy(Matrix3d.Displacement(displacement));
                                
                                currentSpace.AppendEntity(entity);
                                tr.AddNewlyCreatedDBObject(entity, true);
                            }
                        }
                        
                        tr.Commit();
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError inserting block: {ex.Message}");
                if (ex.InnerException != null)
                {
                    ed.WriteMessage($"\nInner exception: {ex.InnerException.Message}");
                }
                ed.WriteMessage($"\nStack trace: {ex.StackTrace}");
                return false;
            }
        }

        private Entity CreateEntityFromData(EntityData entityData)
        {
            Entity entity = null;
            
            try
            {
                switch (entityData.Type)
                {
                    case "Polyline":
                        Polyline pline = new Polyline();
                        if (entityData.Points != null)
                        {
                            for (int i = 0; i < entityData.Points.Count; i++)
                            {
                                double[] point = entityData.Points[i];
                                double bulge = 0;
                                if (entityData.Bulges != null && i < entityData.Bulges.Count)
                                {
                                    bulge = entityData.Bulges[i];
                                }
                                pline.AddVertexAt(i, new Point2d(point[0], point[1]), bulge, 0, 0);
                            }
                            if (entityData.IsClosed)
                                pline.Closed = true;
                            entity = pline;
                        }
                        break;
                        
                    case "Line":
                        if (entityData.Points != null && entityData.Points.Count >= 2)
                        {
                            Line line = new Line(
                                new Point3d(entityData.Points[0][0], entityData.Points[0][1], 0),
                                new Point3d(entityData.Points[1][0], entityData.Points[1][1], 0));
                            entity = line;
                        }
                        break;
                        
                    case "Arc":
                        if (entityData.Center != null && entityData.Center.Length >= 2)
                        {
                            Arc arc = new Arc(
                                new Point3d(entityData.Center[0], entityData.Center[1], 0),
                                entityData.Radius,
                                entityData.StartAngle,
                                entityData.EndAngle);
                            entity = arc;
                        }
                        break;
                        
                    case "Ellipse":
                        if (entityData.Center != null && entityData.Center.Length >= 2 &&
                            entityData.MajorAxis != null && entityData.MajorAxis.Length >= 2)
                        {
                            Point3d center = new Point3d(entityData.Center[0], entityData.Center[1], 0);
                            Vector3d majorAxis = new Vector3d(entityData.MajorAxis[0], entityData.MajorAxis[1], 0);
                            Ellipse ellipse = new Ellipse(center, Vector3d.ZAxis, majorAxis, entityData.RadiusRatio,
                                entityData.StartParam, entityData.EndParam);
                            entity = ellipse;
                        }
                        break;
                }
                
                if (entity != null)
                {
                    entity.Layer = "0";
                    entity.ColorIndex = PluginCommandsMenus.color;
                    entity.Linetype = "ByLayer";
                }
            }
            catch (Exception ex)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                doc.Editor.WriteMessage($"\nError creating entity of type {entityData.Type}: {ex.Message}");
            }
            
            return entity;
        }
    }
}
