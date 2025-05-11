using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

namespace DrawingPlugin.PluginCommands
{
    public class AreaCalculationCommand
    {
        [CommandMethod("CALCULATEAREA")]
        public void CalculateArea()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Prompt the user to select entities
                PromptSelectionOptions selOpts = new PromptSelectionOptions();
                selOpts.MessageForAdding = "\nSelect entities to calculate area: ";
                selOpts.AllowDuplicates = false;
                selOpts.AllowSubSelections = false;

                PromptSelectionResult selRes = ed.GetSelection(selOpts);
                if (selRes.Status != PromptStatus.OK)
                    return;

                // Get the selected entities
                SelectionSet selSet = selRes.Value;
                if (selSet.Count == 0)
                {
                    ed.WriteMessage("\nNo entities selected.");
                    return;
                }

                // Process the selection and calculate area
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Create a list to store regions and curves
                    List<Region> regions = new List<Region>();
                    List<Curve> openCurves = new List<Curve>();
                    string entityType = "Unknown";

                    // Process each selected entity
                    foreach (SelectedObject selObj in selSet)
                    {
                        Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        // Determine entity type for display
                        entityType = GetEntityTypeName(ent);

                        // Handle different entity types
                        if (ent is Curve)
                        {
                            Curve curve = ent as Curve;
                            
                            // Check if the curve is closed
                            if (curve.Closed || ent is Circle || ent is Ellipse)
                            {
                                // Try to convert closed curve to region
                                Region region = ConvertToRegion(curve, tr);
                                if (region != null)
                                {
                                    regions.Add(region);
                                }
                            }
                            else
                            {
                                // Store open curves for later processing
                                openCurves.Add(curve);
                            }
                        }
                        else if (ent is BlockReference)
                        {
                            // Handle block references
                            Region region = ConvertBlockToRegion(ent as BlockReference, tr);
                            if (region != null)
                            {
                                regions.Add(region);
                            }
                        }
                    }

                    // If we have open curves, try to join them into a closed shape
                    if (openCurves.Count > 0)
                    {
                        ed.WriteMessage($"\nFound {openCurves.Count} open curves. Attempting to join them...");
                        
                        // Try to create a closed polyline from open curves
                        Polyline closedPoly = TryCreateClosedPolyline(openCurves, tr);
                        if (closedPoly != null)
                        {
                            // Convert the closed polyline to a region
                            Region region = ConvertToRegion(closedPoly, tr);
                            if (region != null)
                            {
                                regions.Add(region);
                                entityType = "Joined Curves";
                            }
                            closedPoly.Dispose();
                        }
                        else
                        {
                            // If we can't create a closed polyline, try to calculate area directly
                            double directCalculatedArea = CalculateAreaFromOpenCurves(openCurves);
                            if (directCalculatedArea > 0)
                            {
                                ShowAreaResult(directCalculatedArea, "Open Curves");
                                tr.Commit();
                                return;
                            }
                        }
                    }

                    // If no regions were created, exit
                    if (regions.Count == 0)
                    {
                        ed.WriteMessage("\nNo valid regions could be created from the selection.");
                        tr.Commit();
                        return;
                    }

                    // Create a union of all regions to get the outer boundary
                    Region unionRegion = regions[0];
                    for (int i = 1; i < regions.Count; i++)
                    {
                        // Create a clone of the current region to avoid modifying the original
                        Region tempRegion = regions[i].Clone() as Region;
                        unionRegion.BooleanOperation(BooleanOperationType.BoolUnite, tempRegion);
                    }

                    // Calculate the area of the union region
                    double resultArea = unionRegion.Area;

                    // Display the result
                    ShowAreaResult(resultArea, entityType);

                    // Clean up
                    foreach (Region region in regions)
                    {
                        region.Dispose();
                    }
                    unionRegion.Dispose();

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
                if (ex.InnerException != null)
                {
                    ed.WriteMessage($"\nInner Exception: {ex.InnerException.Message}");
                }
                ed.WriteMessage($"\nStack Trace: {ex.StackTrace}");
            }
        }

        private string GetEntityTypeName(Entity entity)
        {
            if (entity is Polyline) return "Polyline";
            if (entity is Line) return "Line";
            if (entity is Arc) return "Arc";
            if (entity is Circle) return "Circle";
            if (entity is Ellipse) return "Ellipse";
            if (entity is Spline) return "Spline";
            if (entity is BlockReference) return "Block";
            return entity.GetType().Name;
        }

        private Region ConvertToRegion(Curve curve, Transaction tr)
        {
            try
            {
                // Create a DBObjectCollection with the curve
                DBObjectCollection curves = new DBObjectCollection();
                curves.Add(curve);

                // Create regions from the curves
                DBObjectCollection regions = Region.CreateFromCurves(curves);
                
                if (regions.Count > 0)
                {
                    return regions[0] as Region;
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue processing
                Document doc = Application.DocumentManager.MdiActiveDocument;
                doc.Editor.WriteMessage($"\nError converting entity to region: {ex.Message}");
            }

            return null;
        }

        private Region ConvertBlockToRegion(BlockReference blockRef, Transaction tr)
        {
            try
            {
                // For block references, we need to explode them and process each entity
                DBObjectCollection explodedEntities = new DBObjectCollection();
                blockRef.Explode(explodedEntities);

                if (explodedEntities.Count > 0)
                {
                    // Convert each exploded entity to a region and union them
                    List<Region> blockRegions = new List<Region>();
                    List<Curve> openCurves = new List<Curve>();
                    
                    foreach (DBObject obj in explodedEntities)
                    {
                        if (obj is Curve)
                        {
                            Curve curve = obj as Curve;
                            if (curve.Closed || curve is Circle || curve is Ellipse)
                            {
                                Region region = ConvertToRegion(curve, tr);
                                if (region != null)
                                {
                                    blockRegions.Add(region);
                                }
                            }
                            else
                            {
                                openCurves.Add(curve);
                            }
                        }
                    }

                    // Try to process open curves if any
                    if (openCurves.Count > 0)
                    {
                        Polyline closedPoly = TryCreateClosedPolyline(openCurves, tr);
                        if (closedPoly != null)
                        {
                            Region region = ConvertToRegion(closedPoly, tr);
                            if (region != null)
                            {
                                blockRegions.Add(region);
                            }
                            closedPoly.Dispose();
                        }
                    }

                    // Union all regions from the block
                    if (blockRegions.Count > 0)
                    {
                        Region unionRegion = blockRegions[0];
                        for (int i = 1; i < blockRegions.Count; i++)
                        {
                            unionRegion.BooleanOperation(BooleanOperationType.BoolUnite, blockRegions[i]);
                        }
                        return unionRegion;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue processing
                Document doc = Application.DocumentManager.MdiActiveDocument;
                doc.Editor.WriteMessage($"\nError processing block: {ex.Message}");
            }

            return null;
        }

        private Polyline TryCreateClosedPolyline(List<Curve> openCurves, Transaction tr)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            
            try
            {
                // First, try to join the curves using AutoCAD's DBCurve.JoinEntity method
                // This works best when the curves are already end-to-end
                
                // Start with the first curve
                if (openCurves.Count == 0) return null;
                
                // Create a new polyline
                Polyline newPoly = new Polyline();
                
                // Get all vertices from all curves
                List<Point3d> allPoints = new List<Point3d>();
                
                foreach (Curve curve in openCurves)
                {
                    if (curve is Line)
                    {
                        Line line = curve as Line;
                        allPoints.Add(line.StartPoint);
                        allPoints.Add(line.EndPoint);
                    }
                    else if (curve is Polyline)
                    {
                        Polyline pline = curve as Polyline;
                        for (int i = 0; i < pline.NumberOfVertices; i++)
                        {
                            allPoints.Add(pline.GetPoint3dAt(i));
                        }
                    }
                    else if (curve is Arc)
                    {
                        Arc arc = curve as Arc;
                        // Get start and end points of the arc
                        Point3d startPoint = arc.GetPointAtParameter(arc.StartParam);
                        Point3d endPoint = arc.GetPointAtParameter(arc.EndParam);
                        allPoints.Add(startPoint);
                        allPoints.Add(endPoint);
                    }
                }
                
                // Remove duplicate points (within tolerance)
                List<Point3d> uniquePoints = RemoveDuplicatePoints(allPoints);
                
                // Try to order the points to form a closed loop
                List<Point3d> orderedPoints = OrderPointsForClosedLoop(uniquePoints);
                
                if (orderedPoints.Count >= 3)
                {
                    // Create a new polyline from the ordered points
                    for (int i = 0; i < orderedPoints.Count; i++)
                    {
                        newPoly.AddVertexAt(i, new Point2d(orderedPoints[i].X, orderedPoints[i].Y), 0, 0, 0);
                    }
                    
                    // Close the polyline
                    newPoly.Closed = true;
                    
                    ed.WriteMessage($"\nCreated closed polyline with {newPoly.NumberOfVertices} vertices.");
                    return newPoly;
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError creating closed polyline: {ex.Message}");
            }
            
            return null;
        }
        
        private List<Point3d> RemoveDuplicatePoints(List<Point3d> points)
        {
            List<Point3d> result = new List<Point3d>();
            double tolerance = 0.001; // Adjust tolerance as needed
            
            foreach (Point3d point in points)
            {
                bool isDuplicate = false;
                foreach (Point3d existingPoint in result)
                {
                    if (point.DistanceTo(existingPoint) < tolerance)
                    {
                        isDuplicate = true;
                        break;
                    }
                }
                
                if (!isDuplicate)
                {
                    result.Add(point);
                }
            }
            
            return result;
        }
        
        private List<Point3d> OrderPointsForClosedLoop(List<Point3d> points)
        {
            if (points.Count < 3) return points;
            
            List<Point3d> result = new List<Point3d>();
            List<Point3d> remaining = new List<Point3d>(points);
            
            // Start with the first point
            result.Add(remaining[0]);
            remaining.RemoveAt(0);
            
            double tolerance = 0.001;
            
            // Find the closest point repeatedly
            while (remaining.Count > 0)
            {
                Point3d lastPoint = result[result.Count - 1];
                int closestIndex = -1;
                double minDistance = double.MaxValue;
                
                for (int i = 0; i < remaining.Count; i++)
                {
                    double distance = lastPoint.DistanceTo(remaining[i]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestIndex = i;
                    }
                }
                
                if (closestIndex >= 0)
                {
                    result.Add(remaining[closestIndex]);
                    remaining.RemoveAt(closestIndex);
                }
                else
                {
                    break;
                }
            }
            
            // Check if the last point is close to the first point
            if (result.Count > 2)
            {
                double closingDistance = result[0].DistanceTo(result[result.Count - 1]);
                if (closingDistance > tolerance)
                {
                    // If not close, try to find a better ordering
                    // This is a simple approach - more sophisticated algorithms could be used
                    Document doc = Application.DocumentManager.MdiActiveDocument;
                    doc.Editor.WriteMessage($"\nWarning: Points may not form a closed loop. Gap: {closingDistance}");
                }
            }
            
            return result;
        }
        
        private double CalculateAreaFromOpenCurves(List<Curve> openCurves)
        {
            // This is a fallback method to calculate area when we can't create a region
            // It uses the shoelace formula (Gauss's area formula) for simple polygons
            
            try
            {
                // First try to create a closed polyline
                Polyline closedPoly = TryCreateClosedPolyline(openCurves, null);
                if (closedPoly != null)
                {
                    double polyArea = Math.Abs(closedPoly.Area);
                    closedPoly.Dispose();
                    return polyArea;
                }
                
                // If that fails, try to calculate area directly from points
                List<Point3d> allPoints = new List<Point3d>();
                
                foreach (Curve curve in openCurves)
                {
                    if (curve is Line)
                    {
                        Line line = curve as Line;
                        allPoints.Add(line.StartPoint);
                        allPoints.Add(line.EndPoint);
                    }
                    else if (curve is Polyline)
                    {
                        Polyline pline = curve as Polyline;
                        for (int i = 0; i < pline.NumberOfVertices; i++)
                        {
                            allPoints.Add(pline.GetPoint3dAt(i));
                        }
                    }
                }
                
                // Remove duplicates and order points
                List<Point3d> uniquePoints = RemoveDuplicatePoints(allPoints);
                List<Point3d> orderedPoints = OrderPointsForClosedLoop(uniquePoints);
                
                if (orderedPoints.Count < 3)
                {
                    return 0;
                }
                
                // Calculate area using shoelace formula
                double calculatedArea = 0;
                int j = orderedPoints.Count - 1;
                
                for (int i = 0; i < orderedPoints.Count; i++)
                {
                    calculatedArea += (orderedPoints[j].X + orderedPoints[i].X) * (orderedPoints[j].Y - orderedPoints[i].Y);
                    j = i;
                }
                
                return Math.Abs(calculatedArea / 2.0);
            }
            catch (Exception ex)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                doc.Editor.WriteMessage($"\nError calculating area from open curves: {ex.Message}");
                return 0;
            }
        }

        private void ShowAreaResult(double area, string entityType)
        {
            // Format the area with appropriate precision
            string formattedArea = area.ToString("F4");
            
            // Create and show a simple form with the result
            using (AreaResultForm form = new AreaResultForm(formattedArea, entityType))
            {
                Application.ShowModalDialog(form);
            }
        }
    }

    public class AreaResultForm : Form
    {
        public AreaResultForm(string area, string entityType)
        {
            InitializeComponents(area, entityType);
        }

        private void InitializeComponents(string area, string entityType)
        {
            // Set form properties
            this.Text = "Area Calculation Result";
            this.Size = new System.Drawing.Size(350, 150);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Create result label
            Label resultLabel = new Label
            {
                Text = $"The square of {entityType} equals {area}",
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font(this.Font.FontFamily, 12)
            };

            // Create OK button
            Button okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new System.Drawing.Size(80, 30),
                Location = new System.Drawing.Point((this.ClientSize.Width - 80) / 2, this.ClientSize.Height - 45),
                Anchor = AnchorStyles.Bottom
            };

            okButton.Click += (sender, e) => this.Close();

            // Add controls to form
            this.Controls.Add(resultLabel);
            this.Controls.Add(okButton);

            // Set accept button
            this.AcceptButton = okButton;
        }
    }
}
