using System;
using System.Windows.Forms;

namespace DrawingPlugin.Forms;

public partial class DBpathRequest : Form
{
    static public string dbpath = "";
    
    public DBpathRequest()
    {
        InitializeComponent();
    }

    private void Button1_Click(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(textBox2.Text))
        {
            dbpath = textBox2.Text;

            MessageBox.Show($"DataBase have registered on the path: {dbpath}");
        }

        else
        {
            MessageBox.Show("Enter the path!");
        }
    }

    private void textBox1_TextChanged(object sender, EventArgs e)
    {
    }
}