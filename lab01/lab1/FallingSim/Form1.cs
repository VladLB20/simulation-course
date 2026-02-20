using System;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;

namespace FallingSim
{

    public partial class Form1 : Form
    {
        private Button btnClick;
        private Label lblHeight;
        private NumericUpDown NumHeight;
        private Label lblAngle;
        private NumericUpDown NumAngle;
        private Label lblSpeed;
        private NumericUpDown NumSpeed;
        private Label lbl_dt;
        private NumericUpDown Num_dt;
        private Label lbl_m;
        private NumericUpDown Num_S;
        private Label lbl_S;
        private NumericUpDown Num_m;
        private Chart ChrtTrackGraph;
        private DataGridView result_table;
        private System.Windows.Forms.Timer timer;

        private int colorIndex = 0;
        private readonly Color[] colors = new Color[]
        {
            Color.Red, Color.Green, Color.Blue, Color.Orange,
            Color.Purple,
        };


        private List<SimulationState> activeSimulations = new List<SimulationState>();

        private class SimulationState
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Prev_x { get; set; }
            public double Prev_y { get; set; }
            public double Vx { get; set; }
            public double Vy { get; set; }
            public double Prev_Vx { get; set; }
            public double Prev_Vy { get; set; }
            public double Time { get; set; }
            public double PrevTime{ get; set; }
            public double MaxY { get; set; }
            public double Dt { get; set; }          
            public Series Series { get; set; }      
            public Color Color { get; set; }
            public bool IsFinished { get; set; }
        }
        public Form1()
        {
            InitializeComponent();
          
            CreateControls();

                    
            

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1; 
            timer.Tick += timer_Tick;
        }

        private void CreateControls()
        {

            btnClick = new Button
            {
                Text = "Launch",
                Location = new Point(300, 20),
                Size = new Size(100, 30)
            };
            btnClick.Click += BtnClick_Click;

            lblHeight = new Label
            {
                Text = "Height:",
                Location = new Point(0, 0),
                AutoSize = true
            };

            NumHeight = new NumericUpDown
            {
                Value = 1,
                Location = new Point(60, 0),
                Size = new Size(50, 2)
            };

            lblAngle = new Label
            {
                Text = "Angle:",
                Location = new Point(0, 30),
                AutoSize = true
            };

            NumAngle = new NumericUpDown
            {
                Value = 45,
                Location = new Point(60, 30),
                Size = new Size(50, 2)
            };

            lblSpeed = new Label
            {
                Text = "Speed:",
                Location = new Point(0, 60),
                AutoSize = true
            };

            NumSpeed = new NumericUpDown
            {
                Value = 10,
                Location = new Point(60, 60),
                Size = new Size(50, 2)
            };

            lbl_dt = new Label
            {
                Text = "dt:",
                Location = new Point(130, 0),
                AutoSize = true
            };

            Num_dt = new NumericUpDown
            {
                Value = 1,
                Location = new Point(160, 0),
                Size = new Size(100, 2),
                DecimalPlaces = 4
            };

            lbl_m = new Label
            {
                Text = "m:",
                Location = new Point(130, 30),
                AutoSize = true
            };

            Num_m = new NumericUpDown
            {
                Value = 1,
                Location = new Point(160, 30),
                Size = new Size(100, 2),
                DecimalPlaces =2
            };

            lbl_S = new Label
            {
                Text = "S:",
                Location = new Point(130, 60),
                AutoSize = true
            };

            Num_S = new NumericUpDown
            {
                Value = 0.1M,
                Location = new Point(160, 60),
                Size = new Size(100, 2),
                DecimalPlaces = 2
            };

            ChrtTrackGraph = new Chart
            {
                Titles ={ new Title("track") },
                Location = new Point(0, 200),
                Size = new Size(900,500),
                BackColor = Color.White
            };

            
            ChartArea graphArea = new ChartArea("graphArea");
            graphArea.AxisX.Title = "Length, m";
            graphArea.AxisY.Title = "Height, m";
            graphArea.AxisY.Minimum = 0D;
            graphArea.AxisX.Minimum =0D;
            ChrtTrackGraph.ChartAreas.Add(graphArea);
            
 
            Legend legend = new Legend("Legend");
            ChrtTrackGraph.Legends.Add(legend);

            result_table = new DataGridView
            {
                Location = new Point(550,10),
                Size = new Size(350, 200),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnCount = 6
            };
            
            result_table.Columns[0].Name = "dt, с";
            result_table.Columns[1].Name = "Length, м";
            result_table.Columns[2].Name = "t, с";
            result_table.Columns[3].Name = "height, м";
            result_table.Columns[4].Name = "track";
            result_table.Columns[5].Name = "Speed";

            this.Controls.Add(btnClick);

            this.Controls.Add(lblHeight);
            this.Controls.Add(NumHeight);
            this.Controls.Add(lblAngle);
            this.Controls.Add(NumAngle);
            this.Controls.Add(lblSpeed);
            this.Controls.Add(NumSpeed);
            this.Controls.Add(lbl_dt);
            this.Controls.Add(Num_dt);
            this.Controls.Add(lbl_m);
            this.Controls.Add(Num_m);
            this.Controls.Add(lbl_S);
            this.Controls.Add(Num_S);

            this.Controls.Add(ChrtTrackGraph);

            this.Controls.Add(result_table);
        }

        
        
        const double g = 9.81;
        const double C = 0.15;
        const double rho = 1.29;



        private void BtnClick_Click(object sender, EventArgs e)
        {
            if (!timer.Enabled)
            {   
                double m = (double)Num_m.Value; 
                double S = (double)Num_S.Value; 
                double k = 0.5 * C * rho * S / m;
                double h0 = (double)NumHeight.Value;
                double angle = (double)NumAngle.Value;
                double v0 = (double)NumSpeed.Value;
                double dt = (double)Num_dt.Value; 


                double aRad = angle * Math.PI / 180.0;
                double vx0 = v0 * Math.Cos(aRad);
                double vy0 = v0 * Math.Sin(aRad);



                Color color = colors[colorIndex % colors.Length];
                colorIndex++;

                Series series = new Series
                {
                    Name = $"dt={dt}",
                    ChartType = SeriesChartType.Line,
                    Color = color,
                    BorderWidth = 2,
                    ChartArea = "graphArea",
                    Legend = "Legend"
                };

                ChrtTrackGraph.Series.Add(series);
                
                series.Points.AddXY(0, h0);

                SimulationState sim = new SimulationState
                {
                    X = 0,
                    Y = h0,
                    Prev_x = 0,
                    Prev_y = h0,
                    Vx = vx0,
                    Vy = vy0,
                    Prev_Vx = vx0,
                    Prev_Vy = vy0,
                    Time = 0,
                    PrevTime = 0,
                    MaxY = h0,
                    Dt = dt,
                    Series = series,
                    Color = color,
                    IsFinished = false
                };

                activeSimulations.Add(sim);

                timer.Start();
            }

        }


        double dt;
        private void timer_Tick(object sender, EventArgs e)
        {       
            for (int i = activeSimulations.Count - 1; i >= 0; i--)
            {
                SimulationState sim = activeSimulations[i];

                    sim.Prev_x = sim.X;
                    sim.Prev_y = sim.Y;
                    sim.Prev_Vx = sim.Vx;
                    sim.Prev_Vy = sim.Vy;
                    sim.Prev_Vx = sim.X;
                    sim.PrevTime =sim.Time;

                    double m = (double)Num_m.Value; 
                    double S = (double)Num_S.Value; 
                    double k = 0.5 * C * rho * S / m;
                    double v = Math.Sqrt(sim.Vx * sim.Vx + sim.Vy * sim.Vy);
                    double ax = -k * v * sim.Vx;
                    double ay = -g - k * v * sim.Vy;

                    sim.Vx += ax * sim.Dt;
                    sim.Vy += ay * sim.Dt;

                    sim.X += sim.Vx * sim.Dt;
                    sim.Y += sim.Vy * sim.Dt;
                    sim.Time += sim.Dt;

                    if (sim.Y > sim.MaxY) sim.MaxY = sim.Y;

                    
                    sim.Series.Points.AddXY(sim.X, sim.Y);

                    
                    if (sim.Y <=0)
                    {
                        sim.IsFinished = true;

                        double speed = Math.Sqrt(sim.Vx*sim.Vx+sim.Vy*sim.Vy);
 
                                    result_table.Rows.Add(
                                    sim.Dt.ToString("F3"),
                                    sim.X.ToString("F2"),      
                                    sim.Time.ToString("F2"),    
                                    sim.MaxY.ToString("F2"),    
                                    speed.ToString("F2"),

                                    sim.Color.Name
            );

                        activeSimulations.RemoveAt(i);
                    }
                }

                if (activeSimulations.Count == 0)
                {
                    timer.Stop();
                }

        }

    }
}
