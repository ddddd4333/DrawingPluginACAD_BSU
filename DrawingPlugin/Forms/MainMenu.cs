using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DrawingPlugin.Forms;

namespace DrawingPlugin
{
    public partial class MainMenu : Form
    {
        public MainMenu()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            RunACADCommand("Circle");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            RunACADCommand("Regular");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            RunACADCommand("Line");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Autodesk.AutoCAD.ApplicationServices.Core.Application.Quit();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            RunACADCommand("InsertFromLIST");
        }

        private void copyButton_Click(object sender, EventArgs e)
        {
            RunACADCommand("CopyToDB");
        }

        private void RunACADCommand(string command)
        {
            try
            {
                Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.SendStringToExecute(command + " ", true, false, true);
            }
            catch (Exception ex) {
                MessageBox.Show("Command execution error: " + ex.Message);
            }
        }
        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void CallColorMenu(object sender, EventArgs e)
        {
            RunACADCommand("Color");
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            DBpathRequest dbpathRequest = new DBpathRequest();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(dbpathRequest);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            RunACADCommand("CALCULATEAREA");
        }
    }
}
