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
using System.Windows.Forms;

namespace DrawingPlugin.Forms
{
    public class PreviewCommand
    {
        [CommandMethod("PREVIEWDBELEMENTS")]
        public void PreviewDatabaseElements()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // Prompt for database path with better guidance
                PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter full path to SQLite database file (e.g., C:\\Temp\\blocks.db): ");
                pStrOpts.AllowSpaces = true;
                PromptResult pStrRes = ed.GetString(pStrOpts);

                if (pStrRes.Status != PromptStatus.OK)
                    return;

                string dbPath = pStrRes.StringResult;

                // Validate the path
                if (string.IsNullOrWhiteSpace(dbPath))
                {
                    ed.WriteMessage("\nInvalid database path.");
                    return;
                }

                // Check if the file exists
                bool dbExists = File.Exists(dbPath);

                if (!dbExists)
                {
                    // Ask if the user wants to create a new database
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

                    // Create a new database
                    try
                    {
                        // Ensure directory exists
                        string directory = Path.GetDirectoryName(dbPath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // Create the database file
                        SQLiteConnection.CreateFile(dbPath);

                        // Create the table structure
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
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        ed.WriteMessage($"\nError creating database: {ex.Message}");
                        return;
                    }
                }

                // Validate the database structure
                if (!ValidateDatabaseStructure(dbPath))
                {
                    ed.WriteMessage("\nThe database file is not valid or does not contain the required tables.");
                    return;
                }

                // Load entities from database
                List<DatabaseBlock> blocks = LoadBlocksFromDatabase(dbPath);

                if (blocks.Count == 0)
                {
                    ed.WriteMessage("\nNo blocks found in the database. Use the BLOCKCOPY command to add entities first.");
                    return;
                }

                // Generate thumbnails
                GenerateThumbnails(blocks);

                // Create and show the preview form
                using (PreviewForm previewForm = new PreviewForm(blocks, dbPath))
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(previewForm);
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");

                // Additional debugging information
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

                    // Check if the Entities table exists
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
                            catch (Autodesk.AutoCAD.Runtime.Exception ex)
                            {
                                // Log the error but continue with other records
                                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
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
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            foreach (DatabaseBlock block in blocks)
            {
                try
                {
                    ed.WriteMessage($"\nГенерация миниатюры для блока: {block.Name}");

                    // Создаем новую временную БД для каждого блока
                    using (Database tempDb = new Database(true, true))
                    {
                        // Добавляем сущности во временную БД
                        using (Transaction tr = tempDb.TransactionManager.StartTransaction())
                        {
                            BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(
                                SymbolUtilityServices.GetBlockModelSpaceId(tempDb),
                                OpenMode.ForWrite
                            );

                            foreach (EntityData entityData in block.EntitiesData)
                            {
                                // Создаем сущность ВНЕ транзакции временной БД
                                Entity entity = CreateEntity(entityData);
                                if (entity != null && !entity.IsErased)
                                {
                                    modelSpace.AppendEntity(entity);
                                    tr.AddNewlyCreatedDBObject(entity, true);
                                }
                            }
                            tr.Commit();
                        }

                        // Рендеринг миниатюры...
                        Extents3d? extents = GetDatabaseExtents(tempDb);
                        block.Thumbnail = extents.HasValue
                            ? RenderThumbnail(tempDb, extents.Value, 150, 150)
                            : CreateDefaultThumbnail(150, 150, block.Name);
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    block.Thumbnail = CreateErrorThumbnail(150, 150, ex.Message);
                    ed.WriteMessage($"\nОшибка: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private Entity CreateEntity(EntityData data)
        {
            return data.Type switch
            {
                "Polyline" => CreatePolyline(data),
                "Line" => CreateLine(data),
                "Arc" => CreateArc(data),
                "Ellipse" => CreateEllipse(data),
                _ => null
            };
        }
        private Polyline CreatePolyline(EntityData data)
        {
            if (data.Points.Count < 2) return null;

            Polyline pline = new Polyline();
            for (int i = 0; i < data.Points.Count; i++)
            {
                pline.AddVertexAt(i, new Point2d(data.Points[i][0], data.Points[i][1]), 0, 0, 0);
            }
            pline.Closed = data.IsClosed;
            return pline;
        }

        private Line CreateLine(EntityData data)
        {
            return data.Points.Count >= 2
                ? new Line(
                    new Point3d(data.Points[0][0], data.Points[0][1], 0),
                    new Point3d(data.Points[1][0], data.Points[1][1], 0))
                : null;
        }

        private Arc CreateArc(EntityData data)
        {
            // Проверка обязательных параметров
            if (data.Center == null || data.Center.Length < 2 ||
                data.Radius <= 0 || data.StartAngle == data.EndAngle)
            {
                return null;
            }

            try
            {
                // Создание дуги
                Arc arc = new Arc(
                    new Point3d(data.Center[0], data.Center[1], 0), // Центр
                    data.Radius,                                    // Радиус
                    NormalizeAngle(data.StartAngle),                // Начальный угол
                    NormalizeAngle(data.EndAngle)                   // Конечный угол
                );

                // Дополнительные настройки
                arc.Layer = data.Layer ?? "0";
                return arc;
            }
            catch
            {
                return null;
            }
        }

        private Ellipse CreateEllipse(EntityData data)
        {
            // Проверка обязательных параметров
            if (data.Center == null || data.Center.Length < 2 ||
                data.MajorAxis == null || data.MajorAxis.Length < 2 ||
                data.RadiusRatio <= 0 || data.RadiusRatio > 1)
            {
                return null;
            }

            try
            {
                // Создание вектора большой оси
                Vector3d majorAxis = new Vector3d(
                    data.MajorAxis[0],
                    data.MajorAxis[1],
                    0);

                // Создание эллипса
                Ellipse ellipse = new Ellipse(
                    new Point3d(data.Center[0], data.Center[1], 0), // Центр
                    Vector3d.ZAxis,                                  // Нормаль
                    majorAxis,                                       // Большая ось
                    data.RadiusRatio,                                // Соотношение осей
                    NormalizeParameter(data.StartParam),             // Стартовый параметр
                    NormalizeParameter(data.EndParam)                // Конечный параметр
                );

                // Дополнительные настройки
                ellipse.Layer = data.Layer ?? "0";
                return ellipse;
            }
            catch
            {
                return null;
            }
        }

        // Вспомогательные методы
        private double NormalizeAngle(double angle)
        {
            // Приведение угла к диапазону [0, 2π)
            angle %= 2 * Math.PI;
            return angle < 0 ? angle + 2 * Math.PI : angle;
        }

        private double NormalizeParameter(double parameter)
        {
            // Приведение параметра к допустимому диапазону
            return Math.Max(0, Math.Min(parameter, 2 * Math.PI));
        }

        private Bitmap CreateDefaultThumbnail(int width, int height, string blockName)
        {
            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.DrawRectangle(Pens.LightGray, 0, 0, width - 1, height - 1);

                // Draw a placeholder icon
                g.DrawRectangle(Pens.Gray, width / 4, height / 4, width / 2, height / 2);
                g.DrawLine(Pens.Gray, width / 4, height / 4, width * 3 / 4, height * 3 / 4);
                g.DrawLine(Pens.Gray, width / 4, height * 3 / 4, width * 3 / 4, height / 4);

                // Draw the block name
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

                // Draw an error icon
                g.DrawLine(new Pen(Color.Red, 2), width / 4, height / 4, width * 3 / 4, height * 3 / 4);
                g.DrawLine(new Pen(Color.Red, 2), width / 4, height * 3 / 4, width * 3 / 4, height / 4);

                // Draw the error message
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
                    // Set common properties
                    entity.Layer = "0"; // Default layer for preview
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                // Log the error
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                doc.Editor.WriteMessage($"\nError creating entity of type {entityData.Type}: {ex.Message}");
            }

            return entity;
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
                            // Manually combine extents
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
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Create a bitmap for the thumbnail
            Bitmap bitmap = new Bitmap(width, height);

            try
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.White);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // Calculate the scale to fit the extents in the bitmap
                    double dbWidth = extents.MaxPoint.X - extents.MinPoint.X;
                    double dbHeight = extents.MaxPoint.Y - extents.MinPoint.Y;

                    // Handle case where extents might be zero
                    if (dbWidth <= 0) dbWidth = 1;
                    if (dbHeight <= 0) dbHeight = 1;

                    // Add padding
                    double padding = Math.Max(dbWidth, dbHeight) * 0.1;
                    dbWidth += padding * 2;
                    dbHeight += padding * 2;

                    double scaleX = width / dbWidth;
                    double scaleY = height / dbHeight;
                    double scale = Math.Min(scaleX, scaleY);

                    // Calculate the offset to center the drawing
                    double offsetX = (width - (dbWidth * scale)) / 2;
                    double offsetY = (height - (dbHeight * scale)) / 2;

                    ed.WriteMessage($"\nRender params: Width={dbWidth}, Height={dbHeight}, Scale={scale}, OffsetX={offsetX}, OffsetY={offsetY}");

                    // Draw the entities
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
                                    // Draw the entity based on its type
                                    if (entity is Polyline)
                                    {
                                        Polyline pline = entity as Polyline;
                                        DrawPolyline(g, pline, extents, scale, offsetX, offsetY, height);
                                        drawnEntities++;
                                    }
                                    else if (entity is Line)
                                    {
                                        Line line = entity as Line;
                                        DrawLine(g, line, extents, scale, offsetX, offsetY, height);
                                        drawnEntities++;
                                    }
                                    else if (entity is Arc)
                                    {
                                        Arc arc = entity as Arc;
                                        DrawArc(g, arc, extents, scale, offsetX, offsetY, height);
                                        drawnEntities++;
                                    }
                                    else if (entity is Ellipse)
                                    {
                                        Ellipse ellipse = entity as Ellipse;
                                        DrawEllipse(g, ellipse, extents, scale, offsetX, offsetY, height);
                                        drawnEntities++;
                                    }

                                }
                            }
                            catch (Autodesk.AutoCAD.Runtime.Exception ex)
                            {
                                ed.WriteMessage($"\nError drawing entity: {ex.Message}");
                            }
                        }

                        ed.WriteMessage($"\nSuccessfully drew {drawnEntities} entities");
                        tr.Commit();
                    }
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage($"\nError during rendering: {ex.Message}");
                ed.WriteMessage($"\nStack Trace: {ex.StackTrace}");

                // Create a simple error thumbnail instead
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

        private void DrawPolyline(Graphics g, Polyline pline, Extents3d extents, double scale, double offsetX, double offsetY, int height)
        {
            if (pline.NumberOfVertices < 2) return;

            // Create points array
            System.Drawing.Point[] points = new System.Drawing.Point[pline.NumberOfVertices];

            for (int i = 0; i < pline.NumberOfVertices; i++)
            {
                Point2d pt = pline.GetPoint2dAt(i);

                // Transform to screen coordinates
                float x = (float)((pt.X - extents.MinPoint.X) * scale + offsetX);
                float y = (float)(height - ((pt.Y - extents.MinPoint.Y) * scale + offsetY)); // Flip Y

                points[i] = new System.Drawing.Point((int)x, (int)y);
            }

            // Draw the polyline
            using (Pen pen = new Pen(Color.Black, 1))
            {
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

        private void DrawLine(Graphics g, Line line, Extents3d extents, double scale, double offsetX, double offsetY, int height)
        {
            // Transform to screen coordinates
            float x1 = (float)((line.StartPoint.X - extents.MinPoint.X) * scale + offsetX);
            float y1 = (float)(height - ((line.StartPoint.Y - extents.MinPoint.Y) * scale + offsetY)); // Flip Y
            float x2 = (float)((line.EndPoint.X - extents.MinPoint.X) * scale + offsetX);
            float y2 = (float)(height - ((line.EndPoint.Y - extents.MinPoint.Y) * scale + offsetY)); // Flip Y

            // Draw the line
            using (Pen pen = new Pen(Color.Black, 1))
            {
                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }

        private void DrawArc(Graphics g, Arc arc, Extents3d extents, double scale, double offsetX, double offsetY, int height)
        {
            // Transform to screen coordinates
            float centerX = (float)((arc.Center.X - extents.MinPoint.X) * scale + offsetX);
            float centerY = (float)(height - ((arc.Center.Y - extents.MinPoint.Y) * scale + offsetY)); // Flip Y
            float radius = (float)(arc.Radius * scale);

            // Calculate the rectangle that bounds the circle
            RectangleF rect = new RectangleF(centerX - radius, centerY - radius, radius * 2, radius * 2);

            // Convert angles from radians to degrees and adjust for GDI+ (which uses a different angle system)
            float startAngle = (float)(arc.StartAngle * 180 / Math.PI);
            float endAngle = (float)(arc.EndAngle * 180 / Math.PI);

            // Adjust angles for GDI+ (0 degrees is at 3 o'clock, positive is clockwise)
            startAngle = 90 - startAngle;
            endAngle = 90 - endAngle;

            // Calculate sweep angle
            float sweepAngle = endAngle - startAngle;
            if (sweepAngle < 0) sweepAngle += 360;

            // Draw the arc
            using (Pen pen = new Pen(Color.Black, 1))
            {
                g.DrawArc(pen, rect, startAngle, sweepAngle);
            }
        }

        private void DrawEllipse(Graphics g, Ellipse ellipse, Extents3d extents, double scale, double offsetX, double offsetY, int height)
        {
            // Transform to screen coordinates
            float centerX = (float)((ellipse.Center.X - extents.MinPoint.X) * scale + offsetX);
            float centerY = (float)(height - ((ellipse.Center.Y - extents.MinPoint.Y) * scale + offsetY)); // Flip Y

            // Calculate major and minor axes
            float majorRadius = (float)(ellipse.MajorAxis.Length * scale);
            float minorRadius = (float)(majorRadius * ellipse.RadiusRatio);

            // Calculate the angle of the major axis
            double angle = Math.Atan2(ellipse.MajorAxis.Y, ellipse.MajorAxis.X);

            // Calculate the rectangle that bounds the ellipse
            RectangleF rect = new RectangleF(centerX - majorRadius, centerY - minorRadius, majorRadius * 2, minorRadius * 2);

            // Save the current state of the graphics object
            System.Drawing.Drawing2D.Matrix originalTransform = g.Transform.Clone();

            // Rotate around the center of the ellipse
            g.TranslateTransform(centerX, centerY);
            g.RotateTransform((float)(angle * 180 / Math.PI));
            g.TranslateTransform(-centerX, -centerY);

            // Draw the ellipse
            using (Pen pen = new Pen(Color.Black, 1))
            {
                if (ellipse.StartParam == 0 && ellipse.EndParam == Math.PI * 2)
                {
                    // Full ellipse
                    g.DrawEllipse(pen, rect);
                }
                else
                {
                    // Elliptical arc - this is a simplification
                    // For a proper elliptical arc, we would need more complex calculations
                    g.DrawEllipse(pen, rect);
                }
            }

            // Restore the original transform
            g.Transform = originalTransform;
        }
    }

    public class DatabaseBlock
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<EntityData> EntitiesData { get; set; }
        public string CreatedAt { get; set; }
        public Bitmap Thumbnail { get; set; }
    }

    public class PreviewForm : Form
    {
        private List<DatabaseBlock> _blocks;
        private FlowLayoutPanel _blocksPanel;
        private Button _closeButton;
        private Button _insertButton;
        private string _dbPath;

        public PreviewForm(List<DatabaseBlock> blocks, string dbPath)
        {
            _blocks = blocks;
            _dbPath = dbPath;
            InitializeComponent();
            PopulateBlocksList();
        }

        private void InitializeComponent()
        {
            this.Text = "Database Blocks Preview";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create main layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 90F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));

            this.Controls.Add(mainLayout);

            // Create blocks panel
            _blocksPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(10)
            };

            mainLayout.Controls.Add(_blocksPanel, 0, 0);

            // Create buttons panel
            Panel buttonsPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            mainLayout.Controls.Add(buttonsPanel, 0, 1);

            // Add database path label
            Label dbPathLabel = new Label
            {
                Text = $"Database: {_dbPath}",
                AutoSize = true,
                Location = new Point(10, 15),
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };

            buttonsPanel.Controls.Add(dbPathLabel);

            // Add blocks count label
            Label blocksCountLabel = new Label
            {
                Text = $"Blocks: {_blocks.Count}",
                AutoSize = true,
                Location = new Point(10, 35),
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };

            buttonsPanel.Controls.Add(blocksCountLabel);

            // Add buttons
            _closeButton = new Button
            {
                Text = "Close",
                Size = new Size(80, 30),
                Location = new Point(buttonsPanel.Width - 90, 10),
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };

            _closeButton.Click += (s, e) => this.Close();
            buttonsPanel.Controls.Add(_closeButton);

            // Add a button for future "Insert" functionality
            _insertButton = new Button
            {
                Text = "Insert (Future)",
                Size = new Size(120, 30),
                Location = new Point(buttonsPanel.Width - 220, 10),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Enabled = false // Disabled for now as per requirements
            };

            buttonsPanel.Controls.Add(_insertButton);
        }

        private void PopulateBlocksList()
        {
            foreach (DatabaseBlock block in _blocks)
            {
                // Create a panel for each block
                Panel blockPanel = new Panel
                {
                    Width = 180,
                    Height = 200,
                    Margin = new Padding(5),
                    BorderStyle = BorderStyle.FixedSingle
                };

                // Add thumbnail
                PictureBox thumbnailBox = new PictureBox
                {
                    Width = 150,
                    Height = 150,
                    Location = new Point(15, 10),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = block.Thumbnail ?? new Bitmap(150, 150)
                };

                blockPanel.Controls.Add(thumbnailBox);

                // Add name label
                Label nameLabel = new Label
                {
                    Text = block.Name,
                    Location = new Point(5, 165),
                    Width = 170,
                    Font = new System.Drawing.Font(this.Font, FontStyle.Bold),
                    AutoEllipsis = true
                };

                blockPanel.Controls.Add(nameLabel);

                // Add date label
                Label dateLabel = new Label
                {
                    Text = block.CreatedAt,
                    Location = new Point(5, 180),
                    Width = 170,
                    Font = new System.Drawing.Font(this.Font.FontFamily, 8),
                    ForeColor = Color.Gray
                };

                blockPanel.Controls.Add(dateLabel);

                // Add click handler for future selection functionality
                blockPanel.Click += (s, e) => SelectBlock(block);
                thumbnailBox.Click += (s, e) => SelectBlock(block);

                // Add to flow layout
                _blocksPanel.Controls.Add(blockPanel);
            }
        }

        private void SelectBlock(DatabaseBlock block)
        {
            // This method is a placeholder for future functionality
            // It would be used to select a block for insertion
            MessageBox.Show($"Selected block: {block.Name}\nThis functionality will be implemented in the future.",
                "Block Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public class EntityData
    {
        public string Type { get; set; }
        public List<double[]> Points { get; set; } = new List<double[]>();
        public List<double> Bulges { get; set; } = new List<double>();
        public bool IsClosed { get; set; }
        public double[] Center { get; set; }
        public double Radius { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }
        public double[] MajorAxis { get; set; }
        public double RadiusRatio { get; set; }
        public double StartParam { get; set; }
        public double EndParam { get; set; }
        public string Layer { get; set; }
        public string Color { get; set; }
        public string Linetype { get; set; }
        public double LinetypeScale { get; set; }
    }
}