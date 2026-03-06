using System;
using System.Drawing;
using System.Windows.Forms;

namespace FireForest
{
    public partial class Form1 : Form
    {
        
        private const int Rows = 100;
        private const int Cols = 100;
        private const int CellSize = 6; 

        
        private const double TreeDensity = 0.6;
        private const double InitialFireProb = 0.005;

        //  0 - пусто, 1 - дерево, 2 - огонь
        private int[,] forest = new int[Rows, Cols];

        
        private PictureBox pictureBox;
        private Button btnStart, btnPause, btnReset;
        private System.Windows.Forms.Timer timer;
        private bool isRunning = true;

        private Random rand = new Random();

        public Form1()
        {
            InitializeComponent(); 
            SetupUI();             
            ResetForest();         
        }

        
        private void SetupUI()
        {
            
            this.Text = "Лесной пожар (клеточный автомат)";
            this.Size = new Size(Cols * CellSize + 20, Rows * CellSize + 80);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            
            pictureBox = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(Cols * CellSize, Rows * CellSize),
                BackColor = Color.Black
            };
            pictureBox.Paint += PictureBox_Paint;
            this.Controls.Add(pictureBox);

            
            btnStart = new Button
            {
                Text = "Старт",
                Location = new Point(10, pictureBox.Bottom + 10),
                Size = new Size(75, 30)
            };
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            
            btnPause = new Button
            {
                Text = "Пауза",
                Location = new Point(95, pictureBox.Bottom + 10),
                Size = new Size(75, 30)
            };
            btnPause.Click += BtnPause_Click;
            this.Controls.Add(btnPause);

            
            btnReset = new Button
            {
                Text = "Сброс",
                Location = new Point(180, pictureBox.Bottom + 10),
                Size = new Size(75, 30)
            };
            btnReset.Click += BtnReset_Click;
            this.Controls.Add(btnReset);

            
            timer = new System.Windows.Forms.Timer { Interval = 50 };
            timer.Tick += Timer_Tick;
        }

        
        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    Color color;
                    switch (forest[i, j])
                    {
                        case 0: color = Color.SaddleBrown; break;
                        case 1: color = Color.ForestGreen; break;
                        case 2: color = Color.Firebrick; break;
                        default: color = Color.Black; break;
                    }

                    using (SolidBrush brush = new SolidBrush(color))
                    {
                        g.FillRectangle(brush, j * CellSize, i * CellSize, CellSize - 1, CellSize - 1);
                    }
                }
            }
        }

        
        private void ResetForest()
        {
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    forest[i, j] = rand.NextDouble() < TreeDensity ? 1 : 0;

            
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    if (forest[i, j] == 1 && rand.NextDouble() < InitialFireProb)
                        forest[i, j] = 2;

            pictureBox.Invalidate();
        }

        
        private void UpdateForest()
        {
            int[,] newForest = (int[,])forest.Clone();

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    if (forest[i, j] == 2)
                    {
                        newForest[i, j] = 0;
                    }
                    else if (forest[i, j] == 1)
                    {
                        
                        if (IsNeighborOnFire(i - 1, j) || IsNeighborOnFire(i + 1, j) ||
                            IsNeighborOnFire(i, j - 1) || IsNeighborOnFire(i, j + 1))
                        {
                            newForest[i, j] = 2;
                        }
                    }
                }
            }

            forest = newForest;
        }

        
        private bool IsNeighborOnFire(int i, int j)
        {
            if (i < 0 || i >= Rows || j < 0 || j >= Cols) return false;
            return forest[i, j] == 2;
        }

        
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!isRunning) return;
            UpdateForest();
            pictureBox.Invalidate();
        }

       
        private void BtnStart_Click(object sender, EventArgs e)
        {
            isRunning = true;
            timer.Start();
        }

        
        private void BtnPause_Click(object sender, EventArgs e)
        {
            isRunning = false;
        }

        
        private void BtnReset_Click(object sender, EventArgs e)
        {
            isRunning = false;
            timer.Stop();
            ResetForest();
        }
    }
}