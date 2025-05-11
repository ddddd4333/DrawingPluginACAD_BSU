using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using DrawingPlugin.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;


namespace DrawingPlugin
{
    public class InsertFunctionality
    {
        [CommandMethod("BLOCKCOPY")]
        public void CopyBlocksToDatabase()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = "Выбирите объекты для копирования (линии, дуги, полилинии): ";
                pso.AllowDuplicates = false;
                pso.AllowSubSelections = false;

                PromptSelectionResult psr = ed.GetSelection(pso);
                if (psr.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    SelectionSet ss = psr.Value;
                    List<EntityData> entitiesData = new List<EntityData>();

                    foreach (SelectedObject selObj in ss)
                    {
                        Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        EntityData entityData = null;

                        if (ent is Polyline)
                        {
                            entityData = ProcessPolyline(ent as Polyline);
                        }
                        else if (ent is Line)
                        {
                            entityData = ProcessLine(ent as Line);
                        }
                        else if (ent is Arc)
                        {
                            entityData = ProcessArc(ent as Arc);
                        }
                        else if (ent is Ellipse)
                        {
                            entityData = ProcessEllipse(ent as Ellipse);
                        }

                        if (entityData != null)
                        {
                            entitiesData.Add(entityData);
                        }
                    }

                    if (entitiesData.Count > 0)
                    {
                        if (DBpathRequest.dbpath != "")
                        {
                            string dbPath = DBpathRequest.dbpath;


                            SaveToDatabase(dbPath, entitiesData);

                            ed.WriteMessage($"\n{entitiesData.Count} entities saved to database successfully.");
                        }
                        else
                        {
                            MessageBox.Show("Register a Data Base!");
                            return;
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\nNo valid entities selected.");
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
        }

        private EntityData ProcessPolyline(Polyline pline)
        {
            List<Point2d> vertices = new List<Point2d>();
            List<double> bulges = new List<double>();
            
            for (int i = 0; i < pline.NumberOfVertices; i++)
            {
                Point2d pt = pline.GetPoint2dAt(i);
                double bulge = pline.GetBulgeAt(i);
                
                vertices.Add(pt);
                bulges.Add(bulge);
            }

            return new EntityData
            {
                Type = "Polyline",
                IsClosed = pline.Closed,
                Points = vertices.Select(p => new double[] { p.X, p.Y }).ToList(),
                Bulges = bulges,
                Layer = pline.Layer,
                Color = pline.Color.ColorValue.ToString(),
                Linetype = pline.Linetype,
                LinetypeScale = pline.LinetypeScale
            
            };
        }

        private EntityData ProcessLine(Line line)
        {
            return new EntityData
            {
                Type = "Line",
                Points = new List<double[]> 
                { 
                    new double[] { line.StartPoint.X, line.StartPoint.Y },
                    new double[] { line.EndPoint.X, line.EndPoint.Y }
                },
                Layer = line.Layer,
                Color = line.Color.ColorValue.ToString(),
                Linetype = line.Linetype,
                LinetypeScale = line.LinetypeScale
         
            };
        }

        private EntityData ProcessArc(Arc arc)
        {
            Point3d center = arc.Center;
            double radius = arc.Radius;
            double startAngle = arc.StartAngle;
            double endAngle = arc.EndAngle;

            return new EntityData
            {
                Type = "Arc",
                Center = new double[] { center.X, center.Y },
                Radius = radius,
                StartAngle = startAngle,
                EndAngle = endAngle,
                Layer = arc.Layer,
                Color = arc.Color.ColorValue.ToString(),
                Linetype = arc.Linetype,
                LinetypeScale = arc.LinetypeScale
         
            };
        }

        private EntityData ProcessEllipse(Ellipse ellipse)
        {
            Point3d center = ellipse.Center;
            Vector3d majorAxis = ellipse.MajorAxis;
            double radiusRatio = ellipse.RadiusRatio;
            double startParam = ellipse.StartParam;
            double endParam = ellipse.EndParam;

            return new EntityData
            {
                Type = "Ellipse",
                Center = new double[] { center.X, center.Y },
                MajorAxis = new double[] { majorAxis.X, majorAxis.Y },
                RadiusRatio = radiusRatio,
                StartParam = startParam,
                EndParam = endParam,
                Layer = ellipse.Layer,
                Color = ellipse.Color.ColorValue.ToString(),
                Linetype = ellipse.Linetype,
                LinetypeScale = ellipse.LinetypeScale
            };
        }

        private void SaveToDatabase(string dbPath, List<EntityData> entitiesData)
        {
            bool dbExists = File.Exists(dbPath);
            
            // Create connection string
            string connectionString = $"Data Source={dbPath};Version=3;";
            
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                // Create table if it doesn't exist
                if (!dbExists)
                {
                    using (SQLiteCommand command = new SQLiteCommand(
                        "CREATE TABLE IF NOT EXISTS Entities (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Data TEXT, CreatedAt TEXT)", 
                        connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                
                // Generate a default name based on timestamp
                string defaultName = $"Block_{DateTime.Now:yyyyMMdd_HHmmss}";
                
                // Serialize the entities data to JSON
                string jsonData = JsonSerializer.Serialize(entitiesData);
                
                // Insert the data
                using (SQLiteCommand command = new SQLiteCommand(
                    "INSERT INTO Entities (Name, Data, CreatedAt) VALUES (@Name, @Data, @CreatedAt)", 
                    connection))
                {
                    command.Parameters.AddWithValue("@Name", defaultName);
                    command.Parameters.AddWithValue("@Data", jsonData);
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    // Class to store entity data for serialization
    public class EntityData
    {
        public string Type { get; set; }
        public bool IsClosed { get; set; }
        public List<double[]> Points { get; set; }
        public List<double> Bulges { get; set; }
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