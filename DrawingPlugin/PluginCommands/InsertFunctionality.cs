using System.Data.SQLite;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Newtonsoft.Json;

using Exception = Autodesk.AutoCAD.Runtime.Exception;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace DrawingPlugin.PluginCommands
{
    public class InsertFunctionality()
    {

        public void ExportGeometry()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    InitializeDatabase();
                    // Выбор объекта
                    PromptEntityOptions opt = new PromptEntityOptions("\nВыберите объект: ");
                    PromptEntityResult res = ed.GetEntity(opt);

                    if (res.Status != PromptStatus.OK) return;

                    Entity ent = tr.GetObject(res.ObjectId, OpenMode.ForRead) as Entity;
                    string entityData = "";

                    // Обработка разных типов объектов
                    switch (ent)
                    {
                        case Line line:
                            entityData = ProcessLine(line);
                            break;
                        case Polyline pline:
                            entityData = ProcessPolyline(pline);
                            break;
                        case Arc arc:
                            entityData = ProcessArc(arc);
                            break;
                        default:
                            ed.WriteMessage("\nНеподдерживаемый тип объекта");
                            return;
                    }

                    // Сохранение в SQLite
                    SaveToDatabase(ent, entityData);
                    ed.WriteMessage("\nДанные сохранены успешно!");

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\nОшибка: {ex.Message}");
                    tr.Abort();
                }
            }
        }

        private void InitializeDatabase()
        {
            string dbPath =
                @"C:\Users\stas5\OneDrive\Документы\GitHub\DrawingPluginACAD_BSU\DrawingPlugin\acad_geometry.db";
            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS entities (
                        id INTEGER PRIMARY KEY,
                        type TEXT NOT NULL,
                        layer TEXT,
                        color INTEGER,
                        data TEXT
                    )";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private string ProcessLine(Line line)
        {
            return $"{{\"start\": [{line.StartPoint.X}, {line.StartPoint.Y}], " +
                   $"\"end\": [{line.EndPoint.X}, {line.EndPoint.Y}]}}";
        }

        private string ProcessPolyline(Polyline pline)
        {
            var vertices = new List<double[]>();
            for (int i = 0; i < pline.NumberOfVertices; i++)
            {
                Point3d pt = pline.GetPoint3dAt(i);
                vertices.Add(new[] { pt.X, pt.Y });
            }

            return
                $"{{\"closed\": {pline.Closed}, \"vertices\": {JsonConvert.SerializeObject(vertices)}}}";
        }

        private string ProcessArc(Arc arc)
        {
            return $"{{\"center\": [{arc.Center.X}, {arc.Center.Y}], " +
                   $"\"radius\": {arc.Radius}, " +
                   $"\"startAngle\": {arc.StartAngle}, " +
                   $"\"endAngle\": {arc.EndAngle}}}";
        }

        private void SaveToDatabase(Entity ent, string geometryData)
        {
            InitializeDatabase();
            string dbPath =
                @"C:\Users\stas5\OneDrive\Документы\GitHub\DrawingPluginACAD_BSU\DrawingPlugin\acad_geometry.db";
            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText =
                        @"INSERT INTO entities (type, layer, color, data) VALUES (@type, @layer, @color, @data)";

                    cmd.Parameters.AddWithValue("@type", ent.GetType().Name);
                    cmd.Parameters.AddWithValue("@layer", ent.Layer);
                    cmd.Parameters.AddWithValue("@color", ent.ColorIndex);
                    cmd.Parameters.AddWithValue("@data", geometryData);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}