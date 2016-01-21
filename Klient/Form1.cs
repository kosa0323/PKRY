using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Klient
{
    public partial class Form1 : Form
    {
        public static TextWriter _writer = null;
        public Form1()
        {
            InitializeComponent();
            _writer = new TextBoxStreamWriter(textBox1);
            Console.SetOut(_writer);
        }

        public void WpiszDoTextBoxa(string value)
        {
            MethodInvoker action = delegate
            { _writer.WriteLine(value); }; //textBox1.Text += value;
            textBox1.BeginInvoke(action);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Klient Klient = new Klient();
            Klient.StworzKlienta();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

       
    }
}
