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

            MessageBox.Show($"Была зарегистрирована по маршруту: {dbpath}");
        }

        else
        {
            MessageBox.Show("Введите путь!");
        }
    }
}