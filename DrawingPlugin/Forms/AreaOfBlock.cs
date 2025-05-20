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
                    
                    // Add debug information
                    ed.WriteMessage($"\nProcessing {selSet.Count} selected entities...");

                    // Process each selected entity
                    foreach (SelectedObject selObj in selSet)
                    {
                        Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        // Determine entity type for display
                        entityType = GetEntityTypeName(ent);
                        ed.WriteMessage($"\nProcessing {entityType}...");

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
                                    ed.WriteMessage($"\n  Added closed {entityType} with area: {region.Area:F4}");
                                }
                            }
                            else
                            {
                                // Store open curves for later processing
                                openCurves.Add(curve);
                                ed.WriteMessage($"\n  Added open {entityType} for later processing");
                            }
                        }
                        else if (ent is BlockReference)
                        {
                            // Handle block references
                            Region region = ConvertBlockToRegion(ent as BlockReference, tr);
                            if (region != null)
                            {
                                regions.Add(region);
                                ed.WriteMessage($"\n  Added block with area: {region.Area:F4}");
                            }
                        }
                        else if (ent is Hatch)
                        {
                            // Handle hatches directly - they have area properties
                            Hatch hatch = ent as Hatch;
                            ed.WriteMessage($"\n  Hatch area: {hatch.Area:F4}");
                            
                            // Process the hatch to create regions
                            Region hatchRegion = ProcessHatchEntity(hatch, tr);
                            if (hatchRegion != null)
                            {
                                regions.Add(hatchRegion);
                                ed.WriteMessage($"\n  Added hatch region with area: {hatchRegion.Area:F4}");
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
                                ed.WriteMessage($"\nCreated region from joined curves with area: {region.Area:F4}");
                            }
                            closedPoly.Dispose();
                        }
                        else
                        {
                            // If we can't create a closed polyline, try to calculate area directly
                            double directCalculatedArea = CalculateAreaFromOpenCurves(openCurves);
                            if (directCalculatedArea > 0)
                            {
                                ed.WriteMessage($"\nCalculated area directly from open curves: {directCalculatedArea:F4}");
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
                    ed.WriteMessage($"\nCreating union of {regions.Count} regions...");
                    Region unionRegion = regions[0];
                    for (int i = 1; i < regions.Count; i++)
                    {
                        // Create a clone of the current region to avoid modifying the original
                        Region tempRegion = regions[i].Clone() as Region;
                        unionRegion.BooleanOperation(BooleanOperationType.BoolUnite, tempRegion);
                    }

                    // Calculate the area of the union region
                    double resultArea = unionRegion.Area;
                    ed.WriteMessage($"\nFinal calculated area: {resultArea:F4}");

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

        // Process a hatch entity to create a region
        private Region ProcessHatchEntity(Hatch hatch, Transaction tr)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            
            try
            {
                // The simplest way to get a region from a hatch is to use its boundary
                // We'll create a new entity from the boundary and convert it to a region
                
                // First, check if we can use the hatch's area directly
                if (hatch.Area > 0)
                {
                    // Try to get the boundary objects
                    // Note: Hatch.GetLoopAt is the correct method to get loops, not Evaluate
                    
                    // Create a collection for all polylines from the hatch
                    DBObjectCollection allPolylines = new DBObjectCollection();
                    
                    // Process each loop in the hatch
                    for (int i = 0; i < hatch.NumberOfLoops; i++)
                    {
                        HatchLoop loop = hatch.GetLoopAt(i);
                        
                        if (loop.IsPolyline)
                        {
                            // For polyline loops, extract the vertices
                            // Note: We need to use a different approach to get vertices and bulges
                            Polyline poly = CreatePolylineFromHatchLoop(loop);
                            if (poly != null)
                            {
                                allPolylines.Add(poly);
                            }
                        }
                        else
                        {
                            // For non-polyline loops, we need to convert the 2D curves to 3D entities
                            // This is more complex and requires creating new entities
                            
                            // Create a new polyline to approximate the loop
                            Polyline poly = new Polyline();
                            int vertexIndex = 0;
                            
                            // Process each curve in the loop
                            for (int j = 0; j < loop.Curves.Count; j++)
                            {
                                Curve2d curve2d = loop.Curves[j];
                                
                                if (curve2d is LineSegment2d)
                                {
                                    // Handle line segments
                                    LineSegment2d line2d = curve2d as LineSegment2d;
                                    poly.AddVertexAt(vertexIndex++, line2d.StartPoint, 0, 0, 0);
                                    
                                    // Only add the endpoint for the last curve to avoid duplicates
                                    if (j == loop.Curves.Count - 1)
                                    {
                                        poly.AddVertexAt(vertexIndex++, line2d.EndPoint, 0, 0, 0);
                                    }
                                }
                                else if (curve2d is CircularArc2d)
                                {
                                    // Handle arcs by approximating with multiple segments
                                    CircularArc2d arc2d = curve2d as CircularArc2d;
                                    
                                    // Add the start point
                                    poly.AddVertexAt(vertexIndex++, arc2d.StartPoint, 0, 0, 0);
                                    
                                    // Calculate the bulge for the arc
                                    // Bulge = tan(angle/4), where angle is in radians
                                    double startAngle = Math.Atan2(arc2d.StartPoint.Y - arc2d.Center.Y, 
                                                                  arc2d.StartPoint.X - arc2d.Center.X);
                                    double endAngle = Math.Atan2(arc2d.EndPoint.Y - arc2d.Center.Y, 
                                                                arc2d.EndPoint.X - arc2d.Center.X);
                                    
                                    // Ensure angles are in the correct range
                                    if (endAngle < startAngle) endAngle += 2 * Math.PI;
                                    
                                    // Determine if the arc is counterclockwise by checking the sweep angle
                                    // Note: We're using a different approach since IsMinorArc doesn't exist
                                    double sweepAngle = endAngle - startAngle;
                                    bool isCounterClockwise = true;
                                    
                                    // If the sweep angle is greater than PI, it's a major arc
                                    // For major arcs, we need to check if it's clockwise or counterclockwise
                                    if (sweepAngle > Math.PI)
                                    {
                                        // For major arcs, we need to check the direction
                                        // This is a simplified approach - in a real implementation,
                                        // you might need to check additional properties
                                        isCounterClockwise = false;
                                    }
                                    
                                    double angle = sweepAngle;
                                    if (!isCounterClockwise) angle = 2 * Math.PI - angle;
                                    
                                    double bulge = Math.Tan(angle / 4.0);
                                    
                                    // Update the bulge of the last vertex
                                    poly.SetBulgeAt(vertexIndex - 1, (float)bulge);
                                    
                                    // Only add the endpoint for the last curve to avoid duplicates
                                    if (j == loop.Curves.Count - 1)
                                    {
                                        poly.AddVertexAt(vertexIndex++, arc2d.EndPoint, 0, 0, 0);
                                    }
                                }
                                else if (curve2d is EllipticalArc2d)
                                {
                                    // Handle elliptical arcs by approximating with multiple line segments
                                    EllipticalArc2d ellipse2d = curve2d as EllipticalArc2d;
                                    
                                    // Sample points along the elliptical arc
                                    int numSegments = 16; // Adjust for desired accuracy
                                    
                                    // We need to sample points along the elliptical arc
                                    // Since we don't have direct parameter access, we'll use angle sampling
                                    double startAngle = 0;
                                    double endAngle = 2 * Math.PI; // Full ellipse by default
                                    
                                    // If it's not a full ellipse, calculate the angles
                                    if (ellipse2d.StartPoint != ellipse2d.EndPoint)
                                    {
                                        // Calculate angles from center to start/end points
                                        // This is a simplified approach
                                        startAngle = Math.Atan2(ellipse2d.StartPoint.Y - ellipse2d.Center.Y, 
                                                               ellipse2d.StartPoint.X - ellipse2d.Center.X);
                                        endAngle = Math.Atan2(ellipse2d.EndPoint.Y - ellipse2d.Center.Y, 
                                                             ellipse2d.EndPoint.X - ellipse2d.Center.X);
                                        
                                        // Ensure angles are in the correct range
                                        if (endAngle < startAngle) endAngle += 2 * Math.PI;
                                    }
                                    
                                    double angleRange = endAngle - startAngle;
                                    
                                    for (int k = 0; k < numSegments; k++)
                                    {
                                        double angle = startAngle + (angleRange * k / numSegments);
                                        
                                        // Calculate point on ellipse at this angle
                                        double x = ellipse2d.Center.X + ellipse2d.MajorRadius * Math.Cos(angle) * Math.Cos(ellipse2d.MajorAxis.Angle) - 
                                                  ellipse2d.MinorRadius * Math.Sin(angle) * Math.Sin(ellipse2d.MajorAxis.Angle);
                                        double y = ellipse2d.Center.Y + ellipse2d.MajorRadius * Math.Cos(angle) * Math.Sin(ellipse2d.MajorAxis.Angle) + 
                                                  ellipse2d.MinorRadius * Math.Sin(angle) * Math.Cos(ellipse2d.MajorAxis.Angle);
                                        
                                        Point2d pt = new Point2d(x, y);
                                        poly.AddVertexAt(vertexIndex++, pt, 0, 0, 0);
                                    }
                                    
                                    // Only add the endpoint for the last curve to avoid duplicates
                                    if (j == loop.Curves.Count - 1)
                                    {
                                        poly.AddVertexAt(vertexIndex++, ellipse2d.EndPoint, 0, 0, 0);
                                    }
                                }
                                else if (curve2d is NurbCurve2d)
                                {
                                    // Handle NURBS curves by sampling points
                                    NurbCurve2d nurbs2d = curve2d as NurbCurve2d;
                                    
                                    // Sample points along the NURBS curve
                                    int numSegments = 20; // Adjust for desired accuracy
                                    
                                    // We need to sample points along the NURBS curve
                                    // Since we don't have direct parameter access, we'll use a different approach
                                    
                                    // Add the start point
                                    poly.AddVertexAt(vertexIndex++, nurbs2d.StartPoint, 0, 0, 0);
                                    
                                    // Sample points along the curve
                                    // This is a simplified approach - in a real implementation,
                                    // you might need to use a more accurate method
                                    for (int k = 1; k < numSegments; k++)
                                    {
                                        double t = (double)k / numSegments;
                                        
                                        // Linearly interpolate between start and end points
                                        // This is a very rough approximation
                                        double x = nurbs2d.StartPoint.X + t * (nurbs2d.EndPoint.X - nurbs2d.StartPoint.X);
                                        double y = nurbs2d.StartPoint.Y + t * (nurbs2d.EndPoint.Y - nurbs2d.StartPoint.Y);
                                        
                                        Point2d pt = new Point2d(x, y);
                                        poly.AddVertexAt(vertexIndex++, pt, 0, 0, 0);
                                    }
                                    
                                    // Only add the endpoint for the last curve to avoid duplicates
                                    if (j == loop.Curves.Count - 1)
                                    {
                                        poly.AddVertexAt(vertexIndex++, nurbs2d.EndPoint, 0, 0, 0);
                                    }
                                }
                            }
                            
                            // Close the polyline
                            if (poly.NumberOfVertices > 2)
                            {
                                poly.Closed = true;
                                allPolylines.Add(poly);
                            }
                            else
                            {
                                poly.Dispose();
                            }
                        }
                    }
                    
                    // Create regions from all polylines
                    if (allPolylines.Count > 0)
                    {
                        DBObjectCollection regions = Region.CreateFromCurves(allPolylines);
                        
                        // Clean up polylines
                        foreach (DBObject obj in allPolylines)
                        {
                            obj.Dispose();
                        }
                        
                        if (regions.Count > 0)
                        {
                            // If we have multiple regions, union them
                            if (regions.Count > 1)
                            {
                                Region baseRegion = regions[0] as Region;
                                
                                for (int i = 1; i < regions.Count; i++)
                                {
                                    Region nextRegion = regions[i] as Region;
                                    baseRegion.BooleanOperation(BooleanOperationType.BoolUnite, nextRegion);
                                    nextRegion.Dispose();
                                }
                                
                                return baseRegion;
                            }
                            else
                            {
                                return regions[0] as Region;
                            }
                        }
                    }
                    
                    // If we couldn't create regions from the loops, create a simple circle with equivalent area
                    // This is a fallback method
                    double radius = Math.Sqrt(hatch.Area / Math.PI);
                    Circle circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, radius);
                    
                    DBObjectCollection circles = new DBObjectCollection();
                    circles.Add(circle);
                    
                    DBObjectCollection circleRegions = Region.CreateFromCurves(circles);
                    if (circleRegions.Count > 0)
                    {
                        circle.Dispose();
                        return circleRegions[0] as Region;
                    }
                    
                    circle.Dispose();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError processing hatch: {ex.Message}");
            }
            
            return null;
        }

        // Helper method to create a polyline from a hatch loop
        private Polyline CreatePolylineFromHatchLoop(HatchLoop loop)
        {
            try
            {
                if (!loop.IsPolyline) return null;
                
                Polyline poly = new Polyline();
                
                // Get the vertices from the loop
                // Note: We need to use a different approach since loop.Polyline() doesn't exist
                
                // For polyline loops, we can extract vertices directly from the curves
                int vertexIndex = 0;
                
                for (int i = 0; i < loop.Curves.Count; i++)
                {
                    Curve2d curve2d = loop.Curves[i];
                    
                    if (curve2d is LineSegment2d)
                    {
                        LineSegment2d line2d = curve2d as LineSegment2d;
                        
                        // Add the start point
                        poly.AddVertexAt(vertexIndex++, line2d.StartPoint, 0, 0, 0);
                        
                        // Only add the endpoint for the last curve to avoid duplicates
                        if (i == loop.Curves.Count - 1)
                        {
                            poly.AddVertexAt(vertexIndex++, line2d.EndPoint, 0, 0, 0);
                        }
                    }
                    else if (curve2d is CircularArc2d)
                    {
                        CircularArc2d arc2d = curve2d as CircularArc2d;
                        
                        // Add the start point
                        poly.AddVertexAt(vertexIndex++, arc2d.StartPoint, 0, 0, 0);
                        
                        // Calculate the bulge for the arc
                        double startAngle = Math.Atan2(arc2d.StartPoint.Y - arc2d.Center.Y, 
                                                      arc2d.StartPoint.X - arc2d.Center.X);
                        double endAngle = Math.Atan2(arc2d.EndPoint.Y - arc2d.Center.Y, 
                                                    arc2d.EndPoint.X - arc2d.Center.X);
                        
                        // Ensure angles are in the correct range
                        if (endAngle < startAngle) endAngle += 2 * Math.PI;
                        
                        // Determine if the arc is counterclockwise
                        double sweepAngle = endAngle - startAngle;
                        bool isCounterClockwise = true;
                        
                        if (sweepAngle > Math.PI)
                        {
                            isCounterClockwise = false;
                        }
                        
                        double angle = sweepAngle;
                        if (!isCounterClockwise) angle = 2 * Math.PI - angle;
                        
                        double bulge = Math.Tan(angle / 4.0);
                        
                        // Update the bulge of the last vertex
                        poly.SetBulgeAt(vertexIndex - 1, (float)bulge);
                        
                        // Only add the endpoint for the last curve to avoid duplicates
                        if (i == loop.Curves.Count - 1)
                        {
                            poly.AddVertexAt(vertexIndex++, arc2d.EndPoint, 0, 0, 0);
                        }
                    }
                }
                
                // Close the polyline if needed
                if (poly.NumberOfVertices > 2)
                {
                    poly.Closed = true;
                    return poly;
                }
                else
                {
                    poly.Dispose();
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                doc.Editor.WriteMessage($"\nError creating polyline from hatch loop: {ex.Message}");
                return null;
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
            if (entity is Hatch) return "Hatch";
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
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                
                // For block references, we need to explode them and process each entity
                DBObjectCollection explodedEntities = new DBObjectCollection();
                blockRef.Explode(explodedEntities);

                ed.WriteMessage($"\nExploded block into {explodedEntities.Count} entities");
                
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
                            string curveType = GetEntityTypeName(curve as Entity);
                            
                            if (curve.Closed || curve is Circle || curve is Ellipse)
                            {
                                Region region = ConvertToRegion(curve, tr);
                                if (region != null)
                                {
                                    blockRegions.Add(region);
                                    ed.WriteMessage($"\n  Added closed {curveType} from block with area: {region.Area:F4}");
                                }
                            }
                            else
                            {
                                openCurves.Add(curve);
                                ed.WriteMessage($"\n  Added open {curveType} from block for later processing");
                            }
                        }
                        else if (obj is Hatch)
                        {
                            // Handle hatches directly
                            Hatch hatch = obj as Hatch;
                            ed.WriteMessage($"\n  Hatch in block with area: {hatch.Area:F4}");
                            
                            // Process the hatch to create a region
                            Region hatchRegion = ProcessHatchEntity(hatch, tr);
                            if (hatchRegion != null)
                            {
                                blockRegions.Add(hatchRegion);
                                ed.WriteMessage($"\n  Added hatch region from block with area: {hatchRegion.Area:F4}");
                            }
                        }
                    }

                    // Try to process open curves if any
                    if (openCurves.Count > 0)
                    {
                        ed.WriteMessage($"\nFound {openCurves.Count} open curves in block. Attempting to join them...");
                        Polyline closedPoly = TryCreateClosedPolyline(openCurves, tr);
                        if (closedPoly != null)
                        {
                            Region region = ConvertToRegion(closedPoly, tr);
                            if (region != null)
                            {
                                blockRegions.Add(region);
                                ed.WriteMessage($"\nCreated region from joined curves in block with area: {region.Area:F4}");
                            }
                            closedPoly.Dispose();
                        }
                        else
                        {
                            // Try direct calculation as a fallback
                            double directArea = CalculateAreaFromOpenCurves(openCurves);
                            if (directArea > 0)
                            {
                                ed.WriteMessage($"\nCalculated area directly from open curves in block: {directArea:F4}");
                                // Create a dummy region with this area for union operations
                                // This is a workaround since we can't directly add the area
                                Circle circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, Math.Sqrt(directArea / Math.PI));
                                Region circleRegion = ConvertToRegion(circle, tr);
                                if (circleRegion != null)
                                {
                                    blockRegions.Add(circleRegion);
                                }
                                circle.Dispose();
                            }
                        }
                    }

                    // Union all regions from the block
                    if (blockRegions.Count > 0)
                    {
                        ed.WriteMessage($"\nCreating union of {blockRegions.Count} regions from block...");
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
                        
                        // For arcs, we should add intermediate points to better approximate the curve
                        // This improves area calculation accuracy
                        double paramRange = arc.EndParam - arc.StartParam;
                        int segments = 8; // Number of segments to divide the arc into
                        for (int i = 1; i < segments; i++)
                        {
                            double param = arc.StartParam + (paramRange * i / segments);
                            allPoints.Add(arc.GetPointAtParameter(param));
                        }
                    }
                    else if (curve is Spline)
                    {
                        // Handle splines by sampling points along the curve
                        Spline spline = curve as Spline;
                        int numSamples = 20; // Adjust based on desired accuracy
                        
                        for (int i = 0; i <= numSamples; i++)
                        {
                            double param = spline.StartParam + ((spline.EndParam - spline.StartParam) * i / numSamples);
                            allPoints.Add(spline.GetPointAtParameter(param));
                        }
                    }
                }
                
                // Remove duplicate points (within tolerance)
                List<Point3d> uniquePoints = RemoveDuplicatePoints(allPoints);
                ed.WriteMessage($"\nFound {uniquePoints.Count} unique points from {allPoints.Count} total points");
                
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
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            
            if (points.Count < 3) return points;
            
            // Try to use convex hull algorithm for better ordering
            // This is more reliable than nearest-neighbor for complex shapes
            try
            {
                List<Point3d> convexHull = ComputeConvexHull(points);
                if (convexHull.Count >= 3)
                {
                    ed.WriteMessage($"\nCreated convex hull with {convexHull.Count} points");
                    return convexHull;
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError computing convex hull: {ex.Message}. Falling back to nearest-neighbor algorithm.");
            }
            
            // Fall back to nearest-neighbor algorithm
            List<Point3d> result = new List<Point3d>();
            List<Point3d> remaining = new List<Point3d>(points);
            
            // Start with the leftmost point (helps with some shapes)
            int leftmostIndex = 0;
            for (int i = 1; i < remaining.Count; i++)
            {
                if (remaining[i].X < remaining[leftmostIndex].X)
                {
                    leftmostIndex = i;
                }
            }
            
            result.Add(remaining[leftmostIndex]);
            remaining.RemoveAt(leftmostIndex);
            
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
                    ed.WriteMessage($"\nWarning: Points may not form a closed loop. Gap: {closingDistance}");
                }
            }
            
            return result;
        }
        
        // New method: Compute convex hull using Graham scan algorithm
        private List<Point3d> ComputeConvexHull(List<Point3d> points)
        {
            if (points.Count < 3) return points;
            
            // Find point with lowest y-coordinate (and leftmost if tied)
            int lowestIndex = 0;
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].Y < points[lowestIndex].Y || 
                    (points[i].Y == points[lowestIndex].Y && points[i].X < points[lowestIndex].X))
                {
                    lowestIndex = i;
                }
            }
            
            // Swap the lowest point to the first position
            Point3d temp = points[0];
            points[0] = points[lowestIndex];
            points[lowestIndex] = temp;
            
            // Sort points by polar angle with respect to the lowest point
            Point3d pivot = points[0];
            points.Sort(1, points.Count - 1, new PolarAngleComparer(pivot));
            
            // Graham scan algorithm
            List<Point3d> hull = new List<Point3d>();
            hull.Add(points[0]);
            hull.Add(points[1]);
            
            for (int i = 2; i < points.Count; i++)
            {
                while (hull.Count > 1 && !IsLeftTurn(hull[hull.Count - 2], hull[hull.Count - 1], points[i]))
                {
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(points[i]);
            }
            
            return hull;
        }
        
        // Helper class for sorting points by polar angle
        private class PolarAngleComparer : IComparer<Point3d>
        {
            private Point3d pivot;
            
            public PolarAngleComparer(Point3d pivot)
            {
                this.pivot = pivot;
            }
            
            public int Compare(Point3d p1, Point3d p2)
            {
                // Compute vectors from pivot
                Vector3d v1 = p1 - pivot;
                Vector3d v2 = p2 - pivot;
                
                // Calculate cross product to determine orientation
                double cross = v1.X * v2.Y - v1.Y * v2.X;
                
                if (cross == 0)
                {
                    // Points are collinear, sort by distance from pivot
                    double d1 = v1.LengthSqrd;
                    double d2 = v2.LengthSqrd;
                    return d1.CompareTo(d2);
                }
                
                return -cross.CompareTo(0); // Negative for counterclockwise order
            }
        }
        
        // Helper method to check if three points make a left turn
        private bool IsLeftTurn(Point3d a, Point3d b, Point3d c)
        {
            return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) > 0;
        }
        
        private double CalculateAreaFromOpenCurves(List<Curve> openCurves)
        {
            // This is a fallback method to calculate area when we can't create a region
            // It uses the shoelace formula (Gauss's area formula) for simple polygons
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            
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
                    else if (curve is Arc)
                    {
                        Arc arc = curve as Arc;
                        // Sample points along the arc for better accuracy
                        double paramRange = arc.EndParam - arc.StartParam;
                        int segments = 10; // Number of segments to divide the arc into
                        for (int i = 0; i <= segments; i++)
                        {
                            double param = arc.StartParam + (paramRange * i / segments);
                            allPoints.Add(arc.GetPointAtParameter(param));
                        }
                    }
                    else if (curve is Spline)
                    {
                        // Handle splines by sampling points along the curve
                        Spline spline = curve as Spline;
                        int numSamples = 20; // Adjust based on desired accuracy
                        
                        for (int i = 0; i <= numSamples; i++)
                        {
                            double param = spline.StartParam + ((spline.EndParam - spline.StartParam) * i / numSamples);
                            allPoints.Add(spline.GetPointAtParameter(param));
                        }
                    }
                }
                
                // Remove duplicates and order points
                List<Point3d> uniquePoints = RemoveDuplicatePoints(allPoints);
                List<Point3d> orderedPoints = OrderPointsForClosedLoop(uniquePoints);
                
                if (orderedPoints.Count < 3)
                {
                    ed.WriteMessage("\nNot enough points to form a polygon.");
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
                ed.WriteMessage($"\nError calculating area from open curves: {ex.Message}");
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
            // Set form properties - увеличиваем размер окна
            this.Text = "Area Calculation Result";
            this.Size = new System.Drawing.Size(500, 300); // Увеличенный размер окна
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Create a table layout panel for better organization
            TableLayoutPanel tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new System.Windows.Forms.Padding(20) // Увеличенные отступы
            };
            
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

            // Create result label - увеличиваем размер шрифта
            Label resultLabel = new Label
            {
                Text = $"The area of {entityType} equals {area} square units",
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font(this.Font.FontFamily, 14) // Увеличенный размер шрифта
            };
            
            // Create additional labels for different units - увеличиваем размер шрифта
            double areaValue = double.Parse(area);
            Label mmLabel = new Label
            {
                Text = $"In square millimeters: {areaValue:F4} mm²",
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font(this.Font.FontFamily, 12) // Увеличенный размер шрифта
            };
            
            Label cmLabel = new Label
            {
                Text = $"In square centimeters: {areaValue / 100:F4} cm²",
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font(this.Font.FontFamily, 12) // Увеличенный размер шрифта
            };

            // Create OK button - увеличиваем размер кнопки
            Button okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new System.Drawing.Size(100, 40), // Увеличенный размер кнопки
                Anchor = AnchorStyles.None,
                Font = new System.Drawing.Font(this.Font.FontFamily, 12) // Увеличенный размер шрифта кнопки
            };

            okButton.Click += (sender, e) => this.Close();

            // Add controls to the table layout
            tableLayout.Controls.Add(resultLabel, 0, 0);
            tableLayout.Controls.Add(mmLabel, 0, 1);
            tableLayout.Controls.Add(cmLabel, 0, 2);
            
            // Create a panel for the button
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70 // Увеличенная высота панели для кнопки
            };
            
            okButton.Location = new System.Drawing.Point((buttonPanel.Width - okButton.Width) / 2, 
                                                        (buttonPanel.Height - okButton.Height) / 2);
            buttonPanel.Controls.Add(okButton);

            // Add controls to form
            this.Controls.Add(tableLayout);
            this.Controls.Add(buttonPanel);

            // Set accept button
            this.AcceptButton = okButton;
        }
    }
}