using System;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;

namespace GridSim
{
    public partial class Form1 : Form
    {
        private Button btnRun;
        private NumericUpDown numL;
        private NumericUpDown numTleft;
        private NumericUpDown numTright;
        private NumericUpDown numTinit;
        private NumericUpDown numDt;
        private NumericUpDown numDx;
        private NumericUpDown numTmax;
        private ComboBox cmbMaterial;
        private Chart chart;
        private DataGridView resultTable;

        
        private Dictionary<string, (double rho, double c, double lambda)> materials = new Dictionary<string, (double, double, double)>
            {
                { "Сталь", (7800, 460, 46) },
                { "Алюминий", (2700, 900, 200) },
                { "Дерево", (600, 2500, 0.15) },
                { "Медь", (8900, 385, 400) }
            };

        public Form1()
        {
            InitializeComponent();
            CreateControls();
            cmbMaterial.SelectedIndex = 0;
        }

        private void CreateControls()
        {
            int startX = 10;
            int startY = 20;
            int rowHeight = 25;
            int colWidth = 70;
            int labelOffset = 15;

            Label lblL = new Label 
            {   
                Text = "толщина L (м):",
                Location = new Point(startX, startY),
                AutoSize = true 
            };

            numL = new NumericUpDown
            {
                Location = new Point(startX + 90, startY - 2),
                Width = colWidth,
                Minimum = 0.01m,
                Maximum = 10m,
                Value = 0.1m,
                DecimalPlaces = 3
            };
            startX += 170;

            Label lblTleft = new Label 
            { 
                Text = "T слева (°C):",
                Location = new Point(startX, startY), 
                AutoSize = true 
            };

            numTleft = new NumericUpDown
            {
                Location = new Point(startX + 80, startY - 2),
                Width = colWidth,
                Minimum = -100m,
                Maximum = 1000m,
                Value = 100m,
                DecimalPlaces = 1
            };
            startX += 150;

            Label lblTright = new Label 
            {
                Text = "T справа (°C):", 
                Location = new Point(startX, startY),
                AutoSize = true 
            };

            numTright = new NumericUpDown
            {
                Location = new Point(startX + 80, startY - 2),
                Width = colWidth,
                Minimum = -100m,
                Maximum = 1000m,
                Value = 0m,
                DecimalPlaces = 1
            };
            startX = 10;
            startY += rowHeight;

            Label lblTinit = new Label 
            { 
                Text = "T0 (°C):",
                Location = new Point(startX, startY),
                AutoSize = true 
            };

            numTinit = new NumericUpDown
            {
                Location = new Point(startX + 70, startY - 2),
                Width = colWidth,
                Minimum = -100m,
                Maximum = 1000m,
                Value = 20m,
                DecimalPlaces = 1
            };
            startX += 150;

            Label lblMaterial = new Label
            { 
                Text = "Материал:",
                Location = new Point(startX, startY), AutoSize = true
            };

            cmbMaterial = new ComboBox
            {
                Location = new Point(startX + 70, startY - 2),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbMaterial.Items.AddRange(new object[] { "Сталь", "Алюминий", "Дерево", "Медь" });
            startX = 10;
            startY += rowHeight;

            Label lblDt = new Label
            { 
                Text = "dt (с):",
                Location = new Point(startX, startY),
                AutoSize = true 
            };

            numDt = new NumericUpDown
            {
                Location = new Point(startX + 50, startY - 2),
                Width = colWidth,
                Minimum = 0.0001m,
                Maximum = 1m,
                Value = 0.01m,
                DecimalPlaces = 4,
                Increment = 0.001m
            };
            startX += 130;

            Label lblDx = new Label 
            { 
                Text = "dx (м):",
                Location = new Point(startX, startY),
                AutoSize = true 
            };

            numDx = new NumericUpDown
            {
                Location = new Point(startX + 50, startY - 2),
                Width = colWidth,
                Minimum = 0.0001m,
                Maximum = 0.1m,
                Value = 0.01m,
                DecimalPlaces = 4,
                Increment = 0.001m
            };
            startX += 130;

            Label lblTmax = new Label 
            { 
                Text = "время моделирования (с):",
                Location = new Point(startX, startY),
                AutoSize = true 
            };

            numTmax = new NumericUpDown
            {
                Location = new Point(startX + 160, startY - 2),
                Width = colWidth,
                Minimum = 0.1m,
                Maximum = 1000m,
                Value = 2m,
                DecimalPlaces = 1
            };

            
            btnRun = new Button
            {
                Text = "Старт",
                Location = new Point(700, 30),
                Size = new Size(100, 30)
            };

            btnRun.Click += BtnRun_Click;

            
            chart = new Chart
            {
                Location = new Point(10, 150),
                Size = new Size(700, 400),
                BackColor = Color.White
            };

            ChartArea area = new ChartArea("area");
            area.AxisX.Title = "x, м";
            area.AxisY.Title = "T, °C";
            chart.ChartAreas.Add(area);
            Series series = new Series
            {
                Name = "T(x)",
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                Color = Color.Red
            };
            chart.Series.Add(series);
            chart.Titles.Add("Распределение температуры");

            resultTable = new DataGridView
            {
                Location = new Point(720, 150),
                Size = new Size(450, 400),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = true,  
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
            };
            
            resultTable.Columns.Add("L", "L, м");
            resultTable.Columns.Add("Tl", "Tl, °C");
            resultTable.Columns.Add("Tr", "Tr, °C");
            resultTable.Columns.Add("T0", "T0, °C");
            resultTable.Columns.Add("Material", "Материал");
            resultTable.Columns.Add("rho", "ρ, кг/м³");
            resultTable.Columns.Add("c", "c, Дж/(кг·°С)");
            resultTable.Columns.Add("lambda", "λ, Вт/(м·°С)");
            resultTable.Columns.Add("dt", "dt, с");
            resultTable.Columns.Add("dx", "dx, м");
            resultTable.Columns.Add("tmax", "t , с");
            resultTable.Columns.Add("Tcenter", "T центр, °C");
            resultTable.Columns.Add("Time_ms", "Время, мс");

            this.Controls.Add(lblL);
            this.Controls.Add(numL);
            this.Controls.Add(lblTleft);
            this.Controls.Add(numTleft); 
            this.Controls.Add(lblTright); 
            this.Controls.Add(numTright); 
            this.Controls.Add(lblTinit);
            this.Controls.Add(numTinit);  
            this.Controls.Add(lblMaterial);
            this.Controls.Add(cmbMaterial); 
            this.Controls.Add(lblDt);
            this.Controls.Add(numDt);  
            this.Controls.Add(lblDx);
            this.Controls.Add(numDx); 
            this.Controls.Add(lblTmax);
            this.Controls.Add(numTmax); 
            this.Controls.Add(btnRun);
            this.Controls.Add(chart); 
            this.Controls.Add(resultTable);
            
        }

        private (double rho, double c, double lambda) GetCurrentMaterialParams()
        {
            string material = cmbMaterial.SelectedItem?.ToString() ?? "Сталь";
            if (materials.ContainsKey(material))
                return materials[material];
            else
                return materials["Сталь"]; 
        }


        private double[] Solve(double L, double Tleft, double Tright, double Tinit, double rho, double c, double lambda, double dt, double dx, double tmax)
        {
            int N = (int)(L / dx) + 1;
            int timeSteps = (int)(tmax / dt);

            double[] T = new double[N];
            double[] Tnew = new double[N];

            for (int i = 0; i < N; i++)
                T[i] = Tinit;

            double coeff = lambda / (dx * dx);
            double coeffTime = rho * c / dt;
            double A = coeff;
            double C = coeff;
            double B = 2 * coeff + coeffTime;

            double[] alpha = new double[N];
            double[] beta = new double[N];

            for (int step = 0; step < timeSteps; step++)
            {
                alpha[0] = 0;
                beta[0] = Tleft;

                for (int i = 1; i < N - 1; i++)
                {
                    double denominator = B - C * alpha[i - 1];
                    alpha[i] = A / denominator;
                    double Fi = -coeffTime * T[i];
                    beta[i] = (C * beta[i - 1] - Fi) / denominator;
                }

                Tnew[N - 1] = Tright;

                for (int i = N - 2; i >= 0; i--)
                {
                    Tnew[i] = alpha[i] * Tnew[i + 1] + beta[i];
                }

                Array.Copy(Tnew, T, N);
            }

            return T;
        }

        private void BtnRun_Click(object sender, EventArgs e)
        {
            
            double L = (double)numL.Value;
            double Tleft = (double)numTleft.Value;
            double Tright = (double)numTright.Value;
            double Tinit = (double)numTinit.Value;                
            double dt = (double)numDt.Value;
            double dx = (double)numDx.Value;
            double tmax = (double)numTmax.Value;

            var (rho, c, lambda) = GetCurrentMaterialParams();

            int N = (int)(L / dx) + 1;
            int timeSteps = (int)(tmax / dt);

            var sw = Stopwatch.StartNew();
            double[] T = Solve(L, Tleft, Tright, Tinit, rho, c, lambda, dt, dx, tmax);
            sw.Stop();

            chart.Series["T(x)"].Points.Clear();
            for (int i = 0; i < N; i++)
            {
                double x = i * dx;
                chart.Series["T(x)"].Points.AddXY(x, T[i]);
            }

            int centerIndex = (N - 1) / 2;
            double tempCenter = T[centerIndex];

            resultTable.Rows.Add(
                L.ToString("F3"),
                Tleft.ToString("F1"),
                Tright.ToString("F1"),
                Tinit.ToString("F1"),
                cmbMaterial.SelectedItem.ToString(),
                rho.ToString("F0"),
                c.ToString("F0"),
                lambda.ToString("F2"),
                dt.ToString("F4"),
                dx.ToString("F4"),
                tmax.ToString("F1"),
                tempCenter.ToString("F2"),
                sw.ElapsedMilliseconds.ToString()
            );

            if (resultTable.Rows.Count > 0)
            {
                resultTable.FirstDisplayedScrollingRowIndex = resultTable.Rows.Count - 1;
            }
        

        }
    }
}