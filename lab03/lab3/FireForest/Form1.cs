using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace FireForest
{
    public partial class Form1 : Form
    {
        // Размеры поля
        private const int Rows = 100;
        private const int Cols = 100;
        private const int CellSize = 6;

        // Вероятности типов растительности при инициализации
        private const double ProbEmpty = 0.4;
        private const double ProbGrass = 0.3;
        private const double ProbShrub = 0.2;
        private const double ProbTree = 0.1;

        // Состояния клеток
        private const int EMPTY = 0;
        private const int GRASS = 1;
        private const int SHRUB = 2;
        private const int TREE = 3;
        private const int FIRE_GRASS = 4;
        private const int FIRE_SHRUB = 5;
        private const int FIRE_TREE = 6;
        private const int WATER = 7;

        // Параметры горения
        private readonly double[] baseIgnitionProb = { 0, 1.0, 0.7, 0.4 };
        private readonly double[] sourceFactor = { 0, 1.0, 1.2, 1.8 };

        // Параметры регенерации
        private const double RegrowthBaseProb = 0.02;
        private readonly int[] regrowthWeight = { 0, 1, 2, 3 };

        private int[,] forest = new int[Rows, Cols];
        private Random rand = new Random();

        // UI элементы
        private PictureBox pictureBox;
        private Button btnStart, btnPause, btnReset;
        private System.Windows.Forms.Timer timer;
        private bool isRunning = false;

        // Элементы для воды
        private NumericUpDown nudWaterSize;
        private Button btnAddWater;
        private bool waterPlacementMode = false;
        private int waterSize = 5;

        // Элементы для облака (плавное движение)
        private NumericUpDown nudCloudSize;
        private Button btnStartCloud;
        private bool cloudActive = false;
        private double cloudRow = 0;       // дробные координаты левого верхнего угла облака
        private double cloudCol = 0;
        private int cloudSize = 5;
        private Random randCloud = new Random();
        private const double CloudStep = 0.3; // шаг перемещения за тик (меньше 1 для плавности)

        // Элемент для регенерации
        private NumericUpDown nudRegrowthProb;

        public Form1()
        {
            InitializeComponent();
            SetupUI();
            ResetForest();
        }

        private void SetupUI()
        {
            this.Text = "Лесной пожар (плавное облако)";
            this.Size = new Size(Cols * CellSize + 20, Rows * CellSize + 250);
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
            pictureBox.MouseClick += PictureBox_MouseClick;
            this.Controls.Add(pictureBox);

            // Кнопки управления
            btnStart = new Button { Text = "Старт", Location = new Point(10, pictureBox.Bottom + 10), Size = new Size(75, 30) };
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            btnPause = new Button { Text = "Пауза", Location = new Point(95, pictureBox.Bottom + 10), Size = new Size(75, 30) };
            btnPause.Click += BtnPause_Click;
            this.Controls.Add(btnPause);

            btnReset = new Button { Text = "Сброс", Location = new Point(180, pictureBox.Bottom + 10), Size = new Size(75, 30) };
            btnReset.Click += BtnReset_Click;
            this.Controls.Add(btnReset);

            // Панель для воды
            Label lblWaterSize = new Label { Text = "Размер водоёма:", Location = new Point(10, pictureBox.Bottom + 50), Size = new Size(100, 20) };
            this.Controls.Add(lblWaterSize);

            nudWaterSize = new NumericUpDown { Location = new Point(120, pictureBox.Bottom + 48), Size = new Size(60, 20), Minimum = 1, Maximum = 20, Value = 5 };
            this.Controls.Add(nudWaterSize);

            btnAddWater = new Button { Text = "Добавить водоём", Location = new Point(200, pictureBox.Bottom + 45), Size = new Size(120, 30) };
            btnAddWater.Click += BtnAddWater_Click;
            this.Controls.Add(btnAddWater);

            // Панель для облака
            Label lblCloudSize = new Label { Text = "Размер облака:", Location = new Point(10, pictureBox.Bottom + 90), Size = new Size(100, 20) };
            this.Controls.Add(lblCloudSize);

            nudCloudSize = new NumericUpDown { Location = new Point(120, pictureBox.Bottom + 88), Size = new Size(60, 20), Minimum = 1, Maximum = 20, Value = 5 };
            this.Controls.Add(nudCloudSize);

            btnStartCloud = new Button { Text = "Запустить облако", Location = new Point(200, pictureBox.Bottom + 85), Size = new Size(120, 30) };
            btnStartCloud.Click += BtnStartCloud_Click;
            this.Controls.Add(btnStartCloud);

            // Панель для регенерации
            Label lblRegrowth = new Label { Text = "Вероятность регенерации (%):", Location = new Point(10, pictureBox.Bottom + 130), Size = new Size(140, 20) };
            this.Controls.Add(lblRegrowth);

            nudRegrowthProb = new NumericUpDown { Location = new Point(160, pictureBox.Bottom + 128), Size = new Size(60, 20), Minimum = 0, Maximum = 100, Value = 2, DecimalPlaces = 1, Increment = 0.5m };
            this.Controls.Add(nudRegrowthProb);

            timer = new System.Windows.Forms.Timer  { Interval = 50 };
            timer.Tick += Timer_Tick;
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Рисуем клетки
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    Color color = forest[i, j] switch
                    {
                        EMPTY => Color.SaddleBrown,
                        GRASS => Color.LightGreen,
                        SHRUB => Color.Green,
                        TREE => Color.DarkGreen,
                        FIRE_GRASS => Color.Orange,
                        FIRE_SHRUB => Color.Red,
                        FIRE_TREE => Color.DarkRed,
                        WATER => Color.Blue,
                        _ => Color.Black
                    };
                    using (SolidBrush brush = new SolidBrush(color))
                    {
                        g.FillRectangle(brush, j * CellSize, i * CellSize, CellSize - 1, CellSize - 1);
                    }
                }
            }

            // Рисуем облако (полупрозрачный белый прямоугольник)
            if (cloudActive)
            {
                int x = (int)(cloudCol * CellSize);
                int y = (int)(cloudRow * CellSize);
                int w = cloudSize * CellSize;
                int h = cloudSize * CellSize;
                using (SolidBrush cloudBrush = new SolidBrush(Color.FromArgb(120, Color.White)))
                {
                    g.FillRectangle(cloudBrush, x, y, w, h);
                }
            }

            pictureBox.Cursor = waterPlacementMode ? Cursors.Cross : Cursors.Default;
        }

        private void PictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            int col = e.X / CellSize;
            int row = e.Y / CellSize;
            if (row < 0 || row >= Rows || col < 0 || col >= Cols) return;

            if (waterPlacementMode)
            {
                int size = waterSize;
                int half = size / 2;
                for (int i = row - half; i <= row - half + size - 1; i++)
                {
                    for (int j = col - half; j <= col - half + size - 1; j++)
                    {
                        if (i >= 0 && i < Rows && j >= 0 && j < Cols)
                            forest[i, j] = WATER;
                    }
                }
                waterPlacementMode = false;
                pictureBox.Invalidate();
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                int state = forest[row, col];
                if (state == GRASS || state == SHRUB || state == TREE)
                {
                    forest[row, col] = state + 3;
                    pictureBox.Invalidate();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                forest[row, col] = WATER;
                pictureBox.Invalidate();
            }
        }

        private void BtnAddWater_Click(object sender, EventArgs e)
        {
            waterSize = (int)nudWaterSize.Value;
            waterPlacementMode = true;
            pictureBox.Invalidate();
        }

        private void BtnStartCloud_Click(object sender, EventArgs e)
        {
            StartCloud();
        }

        private void StartCloud()
        {
            cloudSize = (int)nudCloudSize.Value;
            // Ограничиваем, чтобы облако полностью помещалось в поле
            double maxRow = Rows - cloudSize;
            double maxCol = Cols - cloudSize;
            if (maxRow >= 0 && maxCol >= 0)
            {
                cloudRow = randCloud.NextDouble() * maxRow;
                cloudCol = randCloud.NextDouble() * maxCol;
                cloudActive = true;
            }
            else
            {
                MessageBox.Show("Размер облака слишком большой для поля!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            pictureBox.Invalidate();
        }

        private void ResetForest()
        {
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                {
                    double r = rand.NextDouble();
                    if (r < ProbEmpty)
                        forest[i, j] = EMPTY;
                    else if (r < ProbEmpty + ProbGrass)
                        forest[i, j] = GRASS;
                    else if (r < ProbEmpty + ProbGrass + ProbShrub)
                        forest[i, j] = SHRUB;
                    else
                        forest[i, j] = TREE;
                }
            pictureBox.Invalidate();
        }

        // Применение эффекта облака: тушим все клетки, пересекающиеся с облаком
        private void ApplyCloudEffect()
        {
            if (!cloudActive) return;

            // Границы облака в пикселях
            double cloudLeft = cloudCol * CellSize;
            double cloudTop = cloudRow * CellSize;
            double cloudRight = (cloudCol + cloudSize) * CellSize;
            double cloudBottom = (cloudRow + cloudSize) * CellSize;

            // Проходим по всем клеткам, которые потенциально могут пересекаться
            int startRow = Math.Max(0, (int)cloudRow);
            int endRow = Math.Min(Rows - 1, (int)Math.Ceiling(cloudRow + cloudSize));
            int startCol = Math.Max(0, (int)cloudCol);
            int endCol = Math.Min(Cols - 1, (int)Math.Ceiling(cloudCol + cloudSize));

            for (int i = startRow; i <= endRow; i++)
            {
                for (int j = startCol; j <= endCol; j++)
                {
                    // Границы клетки в пикселях
                    double cellLeft = j * CellSize;
                    double cellTop = i * CellSize;
                    double cellRight = (j + 1) * CellSize;
                    double cellBottom = (i + 1) * CellSize;

                    // Проверка пересечения прямоугольников
                    if (cloudLeft < cellRight && cloudRight > cellLeft &&
                        cloudTop < cellBottom && cloudBottom > cellTop)
                    {
                        int state = forest[i, j];
                        if (state >= FIRE_GRASS && state <= FIRE_TREE)
                            forest[i, j] = EMPTY; // тушим
                    }
                }
            }
        }

        // Плавное случайное движение облака
        private void MoveCloudSmooth()
        {
            if (!cloudActive) return;

            double minRow = 0;
            double maxRow = Rows - cloudSize;
            double minCol = 0;
            double maxCol = Cols - cloudSize;

            // Пытаемся двигаться в одном из четырёх направлений
            var directions = new (double dr, double dc)[] { (-CloudStep, 0), (CloudStep, 0), (0, -CloudStep), (0, CloudStep) };
            directions = directions.OrderBy(x => randCloud.NextDouble()).ToArray();

            foreach (var dir in directions)
            {
                double newRow = cloudRow + dir.dr;
                double newCol = cloudCol + dir.dc;

                if (newRow >= minRow && newRow <= maxRow && newCol >= minCol && newCol <= maxCol)
                {
                    cloudRow = newRow;
                    cloudCol = newCol;
                    break;
                }
            }
        }

        // Распространение огня (без изменений)
        private void UpdateForest()
        {
            int[,] newForest = (int[,])forest.Clone();
            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    int state = forest[i, j];
                    if (state >= FIRE_GRASS && state <= FIRE_TREE)
                    {
                        newForest[i, j] = EMPTY;
                    }
                    else if (state >= GRASS && state <= TREE)
                    {
                        double maxFactor = 0;
                        for (int k = 0; k < 8; k++)
                        {
                            int ni = i + dx[k];
                            int nj = j + dy[k];
                            if (ni >= 0 && ni < Rows && nj >= 0 && nj < Cols)
                            {
                                int ns = forest[ni, nj];
                                if (ns >= FIRE_GRASS && ns <= FIRE_TREE)
                                {
                                    int srcType = ns - 3;
                                    if (sourceFactor[srcType] > maxFactor)
                                        maxFactor = sourceFactor[srcType];
                                }
                            }
                        }
                        if (maxFactor > 0)
                        {
                            double prob = baseIgnitionProb[state] * maxFactor;
                            if (prob > 1.0) prob = 1.0;
                            if (rand.NextDouble() < prob)
                                newForest[i, j] = state + 3;
                        }
                    }
                }
            }
            forest = newForest;
        }

        // Регенерация растительности
        private void RegrowForest()
        {
            double regrowthProb = (double)nudRegrowthProb.Value / 100.0;
            if (regrowthProb <= 0) return;

            int[,] newForest = (int[,])forest.Clone();
            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    if (forest[i, j] != EMPTY) continue;

                    int[] counts = new int[4];
                    for (int k = 0; k < 8; k++)
                    {
                        int ni = i + dx[k];
                        int nj = j + dy[k];
                        if (ni >= 0 && ni < Rows && nj >= 0 && nj < Cols)
                        {
                            int ns = forest[ni, nj];
                            if (ns >= GRASS && ns <= TREE)
                                counts[ns]++;
                        }
                    }

                    int totalWeight = counts[GRASS] * regrowthWeight[GRASS] +
                                     counts[SHRUB] * regrowthWeight[SHRUB] +
                                     counts[TREE] * regrowthWeight[TREE];

                    if (totalWeight > 0 && rand.NextDouble() < regrowthProb)
                    {
                        int r = rand.Next(totalWeight);
                        int cumulative = 0;
                        int newType = GRASS;
                        for (int type = GRASS; type <= TREE; type++)
                        {
                            cumulative += counts[type] * regrowthWeight[type];
                            if (r < cumulative)
                            {
                                newType = type;
                                break;
                            }
                        }
                        newForest[i, j] = newType;
                    }
                }
            }
            forest = newForest;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!isRunning) return;

            // 1. Облако тушит
            if (cloudActive)
                ApplyCloudEffect();

            // 2. Огонь распространяется
            UpdateForest();

            // 3. Регенерация
            RegrowForest();

            // 4. Облако движется плавно
            if (cloudActive)
                MoveCloudSmooth();

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
            cloudActive = false;
            ResetForest();
        }
    }
}