using System.Data.SQLite;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Newtonsoft.Json;
using Exception = Autodesk.AutoCAD.Runtime.Exception;
using DrawingPlugin.Forms;


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
        private static Entity _previewEntity;
        private static TransientManager _tm;
        private static IntegerCollection _transientIds;
        public void InsertFromDatabase()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // 1. Загрузка данных из SQLite
                List<EntityData> entities = LoadEntitiesFromDatabase();
                if (entities.Count == 0)
                {
                    ed.WriteMessage("\nБаза данных пуста!");
                    return;
                }

                // 2. Показ формы выбора
                using (var form = new PreviewForm(entities))
                {
                    form.EntitySelected += (selectedEntity) => { ShowPreview(selectedEntity, doc); };

                    form.InsertRequested += (selectedEntity) => { InsertEntity(selectedEntity, doc); };

                    Application.ShowModalDialog(form);
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nОшибка: {ex.Message}");
            }
            finally
            {
                ClearPreview();
            }
        }

        private List<EntityData> LoadEntitiesFromDatabase()
        {
            var entities = new List<EntityData>();
            string dbPath = "acad_geometry.db";

            using (var conn = new SQLiteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM entities", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entities.Add(new EntityData
                        {
                            Id = reader.GetInt32(0),
                            Type = reader.GetString(1),
                            Layer = reader.GetString(2),
                            Color = reader.GetInt32(3),
                            GeometryData = reader.GetString(4)
                        });
                    }
                }
            }

            return entities;
        }

        private void ShowPreview(EntityData data, Document doc)
        {
            ClearPreview();

            _previewEntity = CreateEntityFromData(data);
            if (_previewEntity == null) return;

            _tm = TransientManager.CurrentTransientManager;
            _transientIds = new IntegerCollection();

            doc.Editor.PointMonitor += OnPointMonitor;
            _tm.AddTransient(
                _previewEntity,
                TransientDrawingMode.DirectShortTerm,
                128,
                _transientIds
            );
        }

        private void OnPointMonitor(object sender, PointMonitorEventArgs e)
        {
            if (_previewEntity == null) return;

            Matrix3d transform = Matrix3d.Displacement(
                _previewEntity.GeometricExtents.MinPoint.GetVectorTo(e.Context.RawPoint)
            );

            _previewEntity.TransformBy(transform);
            e.Context.DrawContext.Geometry.Draw(_previewEntity);
            e.Context.UpdateDetectionGraphics();
        }

        private void InsertEntity(EntityData data, Document doc)
        {
            using (var tr = doc.TransactionManager.StartTransaction())
            {
                var db = doc.Database;
                var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                var btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                var entity = CreateEntityFromData(data);
                entity.ColorIndex = data.Color;
                entity.Layer = data.Layer;

                btr.AppendEntity(entity);
                tr.AddNewlyCreatedDBObject(entity, true);
                tr.Commit();
            }
        }

        private Entity CreateEntityFromData(EntityData data)
        {
            dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(data.GeometryData);

            switch (data.Type)
            {
                case "Line":
                    return new Line(
                        new Point3d((double)json.start[0], (double)json.start[1], 0),
                        new Point3d((double)json.end[0], (double)json.end[1], 0));

                case "Polyline":
                    var pline = new Polyline();
                    for (int i = 0; i < json.vertices.Count; i++)
                    {
                        pline.AddVertexAt(i,
                            new Point2d((double)json.vertices[i][0], (double)json.vertices[i][1]), 0, 0, 0);
                    }

                    pline.Closed = json.closed;
                    return pline;

                case "Arc":
                    return new Arc(
                        new Point3d((double)json.center[0], (double)json.center[1], 0),
                        (double)json.radius,
                        (double)json.startAngle,
                        (double)json.endAngle);

                default: return null;
            }
        }

        private void ClearPreview()
        {
            if (_previewEntity != null && _tm != null)
            {
                _tm.EraseTransient(_previewEntity, _transientIds);
                _previewEntity.Dispose();
                _previewEntity = null;
            }
        }
    }
}