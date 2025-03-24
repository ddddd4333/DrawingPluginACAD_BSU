using System;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;

namespace DrawingPlugin
{
    public partial class ChangeColor : Form
    {
        public ChangeColor()
        {
            InitializeComponent();
        }

        private void Red_Change(object sender, EventArgs e)
        {
            plugin.color = 1;
        }
        private void Yellow_Change(object sender, EventArgs e)
        {
            plugin.color = 2;
        }
        private void Green_Change(object sender, EventArgs e)
        {
            plugin.color = 3;
        }
        private void Cyan_Change(object sender, EventArgs e)
        {
            plugin.color = 4;
        }
        private void Blue_Change(object sender, EventArgs e)
        {
            plugin.color = 5;
        }
        private void Magenta_Change(object sender, EventArgs e)
        {
            plugin.color = 6;
            
        }

        private void White_Change(object sender, EventArgs e)
        {
            plugin.color = 7;
        }
        
    }
}