using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Zadanie2GK
{
    public partial class Kontroler : Form
    {
        Zadanie2 drawerWindow;
        static int numXSegments = 30;
        static int numYSegments = 30;
        static int m = 30;
        static double kd = 0.2;
        static Color lightColor = Color.White;
        static Color objectColor = Color.Red;
        static double lightR = 1;
        static double lightH = 2;

        bool isStopped = false;

        public Kontroler()
        {
            InitializeComponent();
            drawerWindow = new Zadanie2(numXSegments, numYSegments, m, kd, lightColor, objectColor);
            drawerWindow.Show();

            SetColorDataSource(lightColorBox);
            SetColorDataSource(objectColorBox);
            SetDisplayStyle(lightColorBox);
            SetDisplayStyle(objectColorBox);

            InitDefaultFields();
        }

        private void InitDefaultFields()
        {
            xSegNum.Value = numXSegments;
            ySegNum.Value = numYSegments;
            mValueNum.Value = m;
            kdValueNum.Value = (decimal)kd;
            lightColorBox.SelectedItem = lightColor;
            objectColorBox.SelectedItem = objectColor;
            lightHNum.Value = (decimal)lightH;
            lightRNum.Value = (decimal)lightR;
        }
        void SetColorDataSource(System.Windows.Forms.ComboBox button)
        {
            button.DataSource = typeof(Color).GetProperties()
            .Where(x => x.PropertyType == typeof(Color))
            .Select(x => x.GetValue(null)).Where(x => (Color)x != Color.Transparent).ToList();
        }
        void SetDisplayStyle(System.Windows.Forms.ComboBox button)
        {
            button.MaxDropDownItems = 10;
            button.IntegralHeight = false;
            button.DrawMode = DrawMode.OwnerDrawFixed;
            button.DropDownStyle = ComboBoxStyle.DropDownList;
            button.DrawItem += lightColorBox_DrawItem;
        }
        //https://stackoverflow.com/questions/59007745/show-list-of-colors-in-combobox-color-picker
        private void lightColorBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index >= 0)
            {
                var txt = lightColorBox.GetItemText(lightColorBox.Items[e.Index]);
                var color = (Color)lightColorBox.Items[e.Index];
                var r1 = new Rectangle(e.Bounds.Left + 1, e.Bounds.Top + 1,
                    2 * (e.Bounds.Height - 2), e.Bounds.Height - 2);
                var r2 = Rectangle.FromLTRB(r1.Right + 2, e.Bounds.Top,
                    e.Bounds.Right, e.Bounds.Bottom);
                using (var b = new SolidBrush(color))
                    e.Graphics.FillRectangle(b, r1);
                e.Graphics.DrawRectangle(Pens.Black, r1);
                TextRenderer.DrawText(e.Graphics, txt, lightColorBox.Font, r2,
                    lightColorBox.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }
        private void resetButton_Click(object sender, EventArgs e)
        {
            drawerWindow.Close();
            drawerWindow.Dispose();
            drawerWindow = new Zadanie2(numXSegments, numYSegments, m, kd, lightColor, objectColor);
            drawerWindow.Show();
            InitDefaultFields();
            isStopped = false;
        }

        private void lightColorBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            drawerWindow.lightColor = (Color)lightColorBox.SelectedItem;
        }

        private void objectColorBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            drawerWindow.objectColor = (Color)objectColorBox.SelectedItem;
        }

        private void mValueNum_ValueChanged(object sender, EventArgs e)
        {
            drawerWindow.m = (int)mValueNum.Value;
        }

        private void kdValueNum_ValueChanged(object sender, EventArgs e)
        {
            drawerWindow.kd = (double)kdValueNum.Value;
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            if(isStopped)
                drawerWindow.StartDrawing();
            else
                drawerWindow.StopDrawing();
            isStopped = !isStopped;
        }

        private void xSegNum_ValueChanged(object sender, EventArgs e)
        {
            drawerWindow.numXSegments = (int)xSegNum.Value;
        }

        private void ySegNum_ValueChanged(object sender, EventArgs e)
        {
            drawerWindow.numYSegments = (int)ySegNum.Value;
        }

        private void lightHNum_ValueChanged(object sender, EventArgs e)
        {
            drawerWindow.lightH = (double)lightHNum.Value;
        }

        private void lightRNum_ValueChanged(object sender, EventArgs e)
        {
            drawerWindow.lightR = (double)lightRNum.Value;
        }

        private void loadButton_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = ".//";
            openFileDialog.RestoreDirectory = false;
            openFileDialog.Filter = "Image Files |*.jpg;*.jpeg;*.png;";
            openFileDialog.DefaultExt = "xml";
            //openFileDialog.FilterIndex = 2;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                drawerWindow.normalMap = openFileDialog.FileName;

            }
            openFileDialog.Dispose();
        }
    }
}
