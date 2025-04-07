using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DrawingPlugin
{
    public partial class RegFiguresMenu : Form
    {

        private void RunACADCommand(string command)
        {
            try
            {
                Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.SendStringToExecute(command + " ", true, false, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка выполнения команды: " + ex.Message);
            }
        }
        public RegFiguresMenu()
        {
            InitializeComponent();
        }

        public void triangleButton_Click(object sender, EventArgs e) {
            RunACADCommand("Triangle");
        }

        public void squareButton_Click(object sender, EventArgs e)
        {
            RunACADCommand("Square");
        }

        public void pentagonButton_Click(object sender, EventArgs e)
        {
            RunACADCommand("Pentagon");
        }

        public void hexagonButton_Click(object sender, EventArgs e)
        {
            RunACADCommand("Hexagon");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void text1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
