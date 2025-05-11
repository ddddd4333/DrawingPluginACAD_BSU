using System;
using System.Drawing;
using System.Windows.Forms;
using DrawingPlugin.Main;
using DrawingPlugin.PluginCommands;

namespace DrawingPlugin
{
    public partial class ChangeColor : Form
    {
       
        public ChangeColor()
        {
            InitializeComponent();
            textBox1.BackColor = MathematicalOperations.startColor(PluginCommandsMenus.color);
        }

        private void Red_Change(object sender, EventArgs e)
        {
            PluginCommandsMenus.color = 1;
            textBox1.BackColor = Color.Red; 
        }
        private void Yellow_Change(object sender, EventArgs e)
        {
            PluginCommandsMenus.color = 2;
            textBox1.BackColor = Color.Yellow;
        }
        private void Green_Change(object sender, EventArgs e)
        {
            PluginCommandsMenus.color = 3;
            textBox1.BackColor = Color.Green; 
        }
        private void Cyan_Change(object sender, EventArgs e)
        {
            PluginCommandsMenus.color = 4;
            textBox1.BackColor = Color.Cyan;
        }
        private void Blue_Change(object sender, EventArgs e)
        {
            PluginCommandsMenus.color = 5;
            textBox1.BackColor = Color.Blue;
        }
        private void Magenta_Change(object sender, EventArgs e)
        {
            PluginCommandsMenus.color = 6;
            textBox1.BackColor = Color.Magenta;
            
        }

        private void White_Change(object sender, EventArgs e)
        {
            PluginCommandsMenus.color = 7;
            textBox1.BackColor = Color.White;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }
    }
}