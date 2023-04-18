using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ToDo_List
{
    public partial class Form1 : Form
    {
        private bool Drag;
        private int MouseX;
        private int MouseY;

        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 0x1;
        private const int HTCAPTION = 0x2;

        private bool m_aeroEnabled;

        private const int CS_DROPSHADOW = 0x00020000;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_ACTIVATEAPP = 0x001C;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]

        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
            );

        public struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }
        protected override CreateParams CreateParams
        {
            get
            {
                m_aeroEnabled = CheckAeroEnabled();
                CreateParams cp = base.CreateParams;
                if (!m_aeroEnabled)
                    cp.ClassStyle |= CS_DROPSHADOW; return cp;
            }
        }
        private bool CheckAeroEnabled()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                int enabled = 0; DwmIsCompositionEnabled(ref enabled);
                return (enabled == 1) ? true : false;
            }
            return false;
        }
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCPAINT:
                    if (m_aeroEnabled)
                    {
                        var v = 2;
                        DwmSetWindowAttribute(this.Handle, 2, ref v, 4);
                        MARGINS margins = new MARGINS()
                        {
                            bottomHeight = 1,
                            leftWidth = 0,
                            rightWidth = 0,
                            topHeight = 0
                        }; DwmExtendFrameIntoClientArea(this.Handle, ref margins);
                    }
                    break;
                default: break;
            }
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT) m.Result = (IntPtr)HTCAPTION;
        }
        private void PanelMove_MouseDown(object sender, MouseEventArgs e)
        {
            Drag = true;
            MouseX = Cursor.Position.X - this.Left;
            MouseY = Cursor.Position.Y - this.Top;
        }
        private void PanelMove_MouseMove(object sender, MouseEventArgs e)
        {
            if (Drag)
            {
                this.Top = Cursor.Position.Y - MouseY;
                this.Left = Cursor.Position.X - MouseX;
            }
        }
        private void PanelMove_MouseUp(object sender, MouseEventArgs e) { Drag = false; }
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var form = new Form())
            {
                form.StartPosition = FormStartPosition.CenterScreen;
                form.Size = new Size(250, 200);

                var textBox = new TextBox();
                textBox.Location = new Point(10, 10);
                textBox.Size = new Size(200, 20);
                form.Controls.Add(textBox);

                var datePicker = new DateTimePicker();
                datePicker.Format = DateTimePickerFormat.Short;
                datePicker.Location = new Point(10, 40);
                form.Controls.Add(datePicker);

                var timePicker = new DateTimePicker();
                timePicker.Format = DateTimePickerFormat.Custom;
                timePicker.CustomFormat = "hh:mm"; // установка формата времени
                timePicker.ShowUpDown = true;
                timePicker.Location = new Point(10, 70);
                form.Controls.Add(timePicker);

                var buttonOk = new Button();
                buttonOk.DialogResult = DialogResult.OK;
                buttonOk.Text = "OK";
                buttonOk.Location = new Point(10, 100);
                form.Controls.Add(buttonOk);

                var buttonCancel = new Button();
                buttonCancel.DialogResult = DialogResult.Cancel;
                buttonCancel.Text = "Cancel";
                buttonCancel.Location = new Point(90, 100);
                form.Controls.Add(buttonCancel);

                form.AcceptButton = buttonOk;
                form.CancelButton = buttonCancel;

                if (form.ShowDialog() == DialogResult.OK)
                {
                    string[] row = { textBox.Text, datePicker.Value.ToShortDateString(), timePicker.Value.ToShortTimeString() };
                    var item = new ListViewItem(row);
                    item.SubItems[0].Text = textBox.Text;
                    listView1.Items.Add(item);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                // Получаем выбранный элемент ListView
                ListViewItem selectedItem = listView1.SelectedItems[0];

                // Удаляем выбранный элемент
                listView1.Items.Remove(selectedItem);

                MessageBox.Show("Задача «" + selectedItem.SubItems[0].Text + "» успешно удалена!");
            }
            else
            {
                MessageBox.Show("Не выбрана ни одна задача для удаления.");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!File.Exists("tasks.txt"))
            {
                File.Create("tasks.txt").Close();
            }
            using (StreamReader reader = new StreamReader("tasks.txt"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] items = line.Split(',');
                    var item = new ListViewItem(items);
                    listView1.Items.Add(item);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (StreamWriter writer = new StreamWriter("tasks.txt"))
            {
                foreach (ListViewItem item in listView1.Items)
                {
                    writer.WriteLine($"{item.SubItems[0].Text},{item.SubItems[1].Text},{item.SubItems[2].Text}");
                }
                writer.Flush();
            }
            this.Close();
        }

        async void button1_MouseEnter(object sender, EventArgs e)
        {
            button2.BackColor = Color.FromArgb(31, 32, 35);
            button3.BackColor = Color.FromArgb(31, 32, 35);
            for (byte r = 31, g = 32, b = 35; r <= 173 & b <= 216 & g <= 230; r += 7, g += 9, b += 10, await Task.Delay(30))
            {
                button1.BackColor = Color.FromArgb(r, g, b);
            }
        }

        async void button2_MouseEnter(object sender, EventArgs e)
        {
            button1.BackColor = Color.FromArgb(31, 32, 35);
            button3.BackColor = Color.FromArgb(31, 32, 35);
            for (byte r = 31, g = 32, b = 35; r <= 173 & b <= 216 & g <= 230; r += 7, g += 9, b += 10, await Task.Delay(30))
            {
                button2.BackColor = Color.FromArgb(r, g, b);
            }
        }

        async void button3_MouseEnter(object sender, EventArgs e)
        {
            button1.BackColor = Color.FromArgb(31, 32, 35);
            button2.BackColor = Color.FromArgb(31, 32, 35);
            for (byte r = 31, g = 32, b = 35; r <= 173 & b <= 216 & g <= 230; r += 7, g += 9, b += 10, await Task.Delay(30))
            {
                button3.BackColor = Color.FromArgb(r, g, b);
            }
        }

        void button1_MouseLeave(object sender, EventArgs e)
        {
            button1.BackColor = Color.FromArgb(31, 32, 35);
        }

        void button2_MouseLeave(object sender, EventArgs e)
        {
            button2.BackColor = Color.FromArgb(31, 32, 35);
        }

        void button3_MouseLeave(object sender, EventArgs e)
        {
            button3.BackColor = Color.FromArgb(31, 32, 35);
        }
    }
}
