using System;
using System.Drawing;
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
            textBox1.BackColor = Color.White;
        }

        private void Red_Change(object sender, EventArgs e)
        {
            plugin.color = 1;
            textBox1.BackColor = Color.Red; 
        }
        private void Yellow_Change(object sender, EventArgs e)
        {
            plugin.color = 2;
            textBox1.BackColor = Color.Yellow;
        }
        private void Green_Change(object sender, EventArgs e)
        {
            plugin.color = 3;
            textBox1.BackColor = Color.Green; 
        }
        private void Cyan_Change(object sender, EventArgs e)
        {
            plugin.color = 4;
            textBox1.BackColor = Color.Cyan;
        }
        private void Blue_Change(object sender, EventArgs e)
        {
            plugin.color = 5;
            textBox1.BackColor = Color.Blue;
        }
        private void Magenta_Change(object sender, EventArgs e)
        {
            plugin.color = 6;
            textBox1.BackColor = Color.Magenta;
            
        }

        private void White_Change(object sender, EventArgs e)
        {
            plugin.color = 7;
            textBox1.BackColor = Color.White;
        }
        
    }
}