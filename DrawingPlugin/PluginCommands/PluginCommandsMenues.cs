namespace DrawingPlugin.PluginCommands
{
    public class PluginCommandsMenus
    {
        public static int color = 7;
        public static void Color()
        {
            ChangeColor changeColor = new ChangeColor();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(changeColor);
            
        }
        
        public static void DrawRegularFigure()
        {
            RegFiguresMenu Form = new RegFiguresMenu();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(Form);
        }
        public static void Initialize() {
            var editor = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            editor.WriteMessage("Plugin started\n");

            MainMenu Form = new MainMenu();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(Form);
        }
    }
}