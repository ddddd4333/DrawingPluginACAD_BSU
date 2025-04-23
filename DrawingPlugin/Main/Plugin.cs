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
            copy.CopyBlocksToDatabase();
        }

        [CommandMethod("InsertFromLIST")]
        public void InsertFromLIST()
        {
           InsertCommands insert = new InsertCommands();
           insert.PreviewDatabaseElements();
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
