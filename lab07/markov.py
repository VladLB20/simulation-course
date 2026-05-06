import tkinter as tk
from tkinter import ttk, messagebox
import numpy as np
import math
import matplotlib
matplotlib.use('TkAgg')
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
from matplotlib.figure import Figure
import csv

try:
    from scipy.linalg import expm
    SCIPY_AVAILABLE = True
except ImportError:
    SCIPY_AVAILABLE = False

def matrix_exponential(A):
    if SCIPY_AVAILABLE:
        return expm(A)
    eigvals, eigvecs = np.linalg.eig(A)
    if np.linalg.matrix_rank(eigvecs) < A.shape[0]:
        raise ValueError("Матрица не диагонализуема, expm не может быть вычислена без SciPy.")
    V = eigvecs
    V_inv = np.linalg.inv(V)
    D = np.diag(np.exp(eigvals))
    return V @ D @ V_inv

WEATHER_NAMES = {
    1: "Ясно ☀️",
    2: "Облачно ⛅",
    3: "Пасмурно ☁️",
    4: "Дождь 🌧️"
}
EMOJI = {1: "☀️", 2: "⛅", 3: "☁️", 4: "🌧️"}

INIT_Q = np.array([
    [-0.4,  0.2,  0.1,  0.1],
    [ 0.1, -0.4,  0.2,  0.1],
    [ 0.05, 0.15, -0.3, 0.1],
    [ 0.1,  0.1,  0.1, -0.3]
])

class MarkovWeatherApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Марковская модель погоды ")
        self.root.geometry("1300x900")
        self.root.configure(bg='black')
        self.root.protocol("WM_DELETE_WINDOW", self.on_closing)

        style = ttk.Style()
        style.theme_use('clam')
        style.configure('Black.TFrame', background='black')
        style.configure('Black.TLabel', background='black', foreground='white', font=("Arial", 10))
        style.configure('Black.TButton', background='gray20', foreground='white')
        style.configure('Black.TLabelframe', background='black', foreground='white', fieldbackground='black')
        style.configure('Black.TLabelframe.Label', background='black', foreground='white')

        self.num_states = 4
        self.Q = INIT_Q.copy()
        self.P = matrix_exponential(self.Q)
        self.pi_theoretical = self._calc_stationary(self.Q)

        self.current_state = 1
        self.day = 0
        self.history = [self.current_state]
        self.trans_count = np.zeros((self.num_states, self.num_states), dtype=int)

        self.running = False
        self.job_id = None
        self.speed_ms = 200

        self.q_vars = {}
        self.q_labels = {}

        self.create_widgets()
        self.update_info()
        self.graph_canvas.bind('<Configure>', lambda e: self.update_graph())
        self.update_comparison_plot()

    def _calc_stationary(self, Q):
        A = np.vstack([Q.T, np.ones(Q.shape[0])])
        b = np.zeros(Q.shape[0] + 1)
        b[-1] = 1.0
        pi, _, _, _ = np.linalg.lstsq(A, b, rcond=None)
        return pi

    def create_widgets(self):
        control_frame = ttk.Frame(self.root, style='Black.TFrame', padding=5)
        control_frame.pack(fill=tk.X)

        ttk.Button(control_frame, text="Старт", command=self.start, style='Black.TButton').pack(side=tk.LEFT, padx=5)
        ttk.Button(control_frame, text="Пауза", command=self.pause, style='Black.TButton').pack(side=tk.LEFT, padx=5)
        ttk.Button(control_frame, text="Сброс", command=self.reset, style='Black.TButton').pack(side=tk.LEFT, padx=5)

        ttk.Label(control_frame, text="Скорость (мс/день):", style='Black.TLabel').pack(side=tk.LEFT, padx=5)
        self.speed_slider = tk.Scale(control_frame, from_=10, to=1000, orient=tk.HORIZONTAL,
                                     length=150, command=self.set_speed,
                                     bg='gray20', fg='white', troughcolor='gray30', highlightbackground='black')
        self.speed_slider.set(self.speed_ms)
        self.speed_slider.pack(side=tk.LEFT, padx=5)

        ttk.Button(control_frame, text="Экспорт CSV", command=self.export_csv, style='Black.TButton').pack(side=tk.RIGHT, padx=5)
        ttk.Button(control_frame, text="Статистика", command=self.show_statistics, style='Black.TButton').pack(side=tk.RIGHT, padx=5)

        q_frame = ttk.LabelFrame(self.root, text="Матрица интенсивностей Q ",
                                 style='Black.TLabelframe', padding=10)
        q_frame.pack(fill=tk.X, padx=5, pady=5)

        ttk.Label(q_frame, text="Строка = текущее , столбец = следующее", style='Black.TLabel').pack()

        grid_frame = ttk.Frame(q_frame, style='Black.TFrame')
        grid_frame.pack(pady=5)

        ttk.Label(grid_frame, text="", style='Black.TLabel').grid(row=0, column=0, padx=10, pady=2)
        for j in range(self.num_states):
            ttk.Label(grid_frame, text=WEATHER_NAMES[j+1].split()[0], style='Black.TLabel').grid(row=0, column=j+1, padx=10, pady=2)

        for i in range(self.num_states):
            ttk.Label(grid_frame, text=WEATHER_NAMES[i+1].split()[0], style='Black.TLabel').grid(row=i+1, column=0, padx=10, pady=2)
            for j in range(self.num_states):
                if i == j:
                    lbl = tk.Label(grid_frame, text="0.0", relief="sunken", width=8,
                                   bg="gray30", fg="white", font=("Arial", 10))
                    lbl.grid(row=i+1, column=j+1, padx=5, pady=2)
                    self.q_labels[(i, j)] = lbl
                else:
                    var = tk.StringVar()
                    var.set(str(self.Q[i, j]))
                    entry = tk.Entry(grid_frame, textvariable=var, width=8, justify="center",
                                     bg="gray20", fg="white", insertbackground="white")
                    entry.grid(row=i+1, column=j+1, padx=5, pady=2)
                    var.trace_add("write", lambda *args, row=i: self._update_diagonals(row))
                    self.q_vars[(i, j)] = var

        ttk.Button(q_frame, text="Применить новую Q", command=self.apply_q, style='Black.TButton').pack(pady=5)

        info_frame = ttk.Frame(self.root, style='Black.TFrame', padding=5)
        info_frame.pack(fill=tk.X)

        self.day_label = ttk.Label(info_frame, text="День: 0", font=("Arial", 14), style='Black.TLabel')
        self.day_label.pack(side=tk.LEFT, padx=20)

        self.weather_label = ttk.Label(info_frame, text="Погода: Ясно ☀️", font=("Arial", 14, "bold"), style='Black.TLabel')
        self.weather_label.pack(side=tk.LEFT, padx=20)

        viz_frame = ttk.Frame(self.root, style='Black.TFrame')
        viz_frame.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        icon_frame = ttk.LabelFrame(viz_frame, text="Текущая погода", style='Black.TLabelframe', padding=10)
        icon_frame.pack(side=tk.LEFT, fill=tk.Y, padx=5)

        self.canvas_icon = tk.Canvas(icon_frame, width=150, height=150, bg='black', highlightthickness=0)
        self.canvas_icon.pack()

        graph_frame = ttk.LabelFrame(viz_frame, text="Граф переходов ", style='Black.TLabelframe', padding=10)
        graph_frame.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=5)

        self.graph_canvas = tk.Canvas(graph_frame, bg='black', highlightthickness=0)
        self.graph_canvas.pack(fill=tk.BOTH, expand=True)

        plot_frame = ttk.LabelFrame(viz_frame, text="Сравнение распределений", style='Black.TLabelframe', padding=10)
        plot_frame.pack(side=tk.RIGHT, fill=tk.BOTH, expand=True, padx=5)

        self.fig = Figure(figsize=(5, 4), dpi=100, facecolor='black')
        self.ax = self.fig.add_subplot(1, 1, 1)
        self.ax.set_facecolor('#2E2E2E')
        self.ax.tick_params(colors='white')
        self.ax.xaxis.label.set_color('white')
        self.ax.yaxis.label.set_color('white')
        self.ax.title.set_color('white')
        for spine in self.ax.spines.values():
            spine.set_color('white')
        self.ax.grid(True, alpha=0.3, color='white')

        self.canvas_plot = FigureCanvasTkAgg(self.fig, master=plot_frame)
        self.canvas_plot.get_tk_widget().pack(fill=tk.BOTH, expand=True)

        for i in range(self.num_states):
            self._update_diagonals(i)

    def _update_diagonals(self, row):
        try:
            total = 0.0
            for j in range(self.num_states):
                if j != row:
                    val_str = self.q_vars[(row, j)].get()
                    val = float(val_str)
                    if val < 0:
                        return
                    total += val
            diag = -total
            self.q_labels[(row, row)].config(text=f"{diag:.4f}")
        except (ValueError, tk.TclError):
            pass

    def apply_q(self):
        new_Q = np.zeros((self.num_states, self.num_states))
        try:
            for i in range(self.num_states):
                row_sum = 0.0
                for j in range(self.num_states):
                    if i != j:
                        val_str = self.q_vars[(i, j)].get()
                        val = float(val_str)
                        if val < 0:
                            raise ValueError(f"Отрицательная интенсивность в ячейке ({i+1},{j+1})")
                        new_Q[i, j] = val
                        row_sum += val
                new_Q[i, i] = -row_sum
            P_new = matrix_exponential(new_Q)
            pi_new = self._calc_stationary(new_Q)
            self.Q = new_Q
            self.P = P_new
            self.pi_theoretical = pi_new
            self.reset()
            messagebox.showinfo("Матрица Q обновлена","...........")
        except Exception as e:
            messagebox.showerror("Ошибка ввода", f"Некорректные данные: {str(e)}")

    def draw_icon(self):
        self.canvas_icon.delete("all")
        emoji = EMOJI.get(self.current_state, "?")
        self.canvas_icon.create_text(75, 75, text=emoji, font=("Arial", 60), anchor="center", fill="white")

    def update_graph(self):
        self.graph_canvas.delete("all")
        w = self.graph_canvas.winfo_width()
        h = self.graph_canvas.winfo_height()
        if w < 50 or h < 50:
            return

        node_centers = {
            1: (0.5 * w, 0.15 * h),
            2: (0.15 * w, 0.5 * h),
            3: (0.85 * w, 0.5 * h),
            4: (0.5 * w, 0.85 * h)
        }

        counts = [self.history.count(s) for s in range(1, self.num_states+1)]
        total = len(self.history)

        font_size = max(20, int(min(w, h) * 0.08))
        effective_radius = font_size * 0.5

        for i in range(1, self.num_states+1):
            for j in range(1, self.num_states+1):
                if i == j or self.trans_count[i-1, j-1] == 0:
                    continue
                xi, yi = node_centers[i]
                xj, yj = node_centers[j]
                dx = xj - xi
                dy = yj - yi
                dist = math.hypot(dx, dy)
                if dist == 0:
                    continue
                offset_i = effective_radius / dist
                offset_j = effective_radius / dist
                x1 = xi + dx * offset_i
                y1 = yi + dy * offset_i
                x2 = xj - dx * offset_j
                y2 = yj - dy * offset_j
                cnt = self.trans_count[i-1, j-1]
                width = 1 + math.log(cnt + 1)
                self.graph_canvas.create_line(x1, y1, x2, y2,
                                              arrow=tk.LAST, fill='skyblue', width=width)
                mx = (x1 + x2) / 2
                my = (y1 + y2) / 2
                perp_dx = -dy / dist
                perp_dy = dx / dist
                shift = 15
                tx = mx + perp_dx * shift
                ty = my + perp_dy * shift
                self.graph_canvas.create_text(tx, ty, text=str(cnt),
                                              fill='white', font=('Arial', 10, 'bold'))

        for state in range(1, self.num_states+1):
            cx, cy = node_centers[state]
            self.graph_canvas.create_text(cx, cy, text=EMOJI[state],
                                          font=('Arial', font_size), anchor='center',
                                          fill='white')
            label = f"{WEATHER_NAMES[state].split()[0]}: {counts[state-1]} дн."
            self.graph_canvas.create_text(cx, cy + font_size*0.7, text=label,
                                          fill='white', font=('Arial', 9))

    def update_comparison_plot(self):
        self.ax.clear()
        self.ax.set_facecolor('#2E2E2E')
        if self.history:
            states, cnts = np.unique(self.history, return_counts=True)
            emp_freq = np.zeros(self.num_states)
            for st, cnt in zip(states, cnts):
                emp_freq[int(st)-1] = cnt
            emp_prob = emp_freq / len(self.history)

            x = np.arange(1, self.num_states+1)
            width = 0.35
            self.ax.bar(x - width/2, emp_prob, width, label='Эмпирическое', color='skyblue')
            self.ax.bar(x + width/2, self.pi_theoretical, width, label='Теоретическое', color='salmon')
            self.ax.set_xticks(x)
            self.ax.set_xticklabels([WEATHER_NAMES[i].split()[0] for i in range(1, self.num_states+1)], color='white')
            self.ax.set_ylabel("Вероятность", color='white')
            self.ax.set_title("Сравнение распределений", color='white')
            self.ax.legend(facecolor='gray', edgecolor='white', labelcolor='white')
            self.ax.grid(True, alpha=0.3, color='white')
            self.ax.tick_params(colors='white')
            for spine in self.ax.spines.values():
                spine.set_color('white')
        else:
            self.ax.set_title("Нет данных", color='white')
            self.ax.tick_params(colors='white')
            for spine in self.ax.spines.values():
                spine.set_color('white')
        self.fig.tight_layout()
        self.canvas_plot.draw()

    def update_info(self):
        self.day_label.config(text=f"День: {self.day}")
        self.weather_label.config(text=f"Погода: {WEATHER_NAMES[self.current_state]}")
        self.draw_icon()
        self.update_graph()
        self.update_comparison_plot()

    def set_speed(self, val):
        self.speed_ms = int(val)

    def start(self):
        if not self.running:
            self.running = True
            self.run_step()

    def pause(self):
        if self.running:
            self.running = False
            if self.job_id:
                self.root.after_cancel(self.job_id)
                self.job_id = None

    def reset(self):
        self.pause()
        self.current_state = 1
        self.day = 0
        self.history = [self.current_state]
        self.trans_count = np.zeros((self.num_states, self.num_states), dtype=int)
        self.update_info()

    def run_step(self):
        if not self.running:
            return
        prev_state = self.current_state
        self.day += 1
        probs = self.P[self.current_state - 1, :]
        self.current_state = np.random.choice(np.arange(1, self.num_states+1), p=probs)
        self.trans_count[prev_state-1, self.current_state-1] += 1
        self.history.append(self.current_state)
        self.update_info()
        self.job_id = self.root.after(self.speed_ms, self.run_step)

    def export_csv(self):
        if not self.history:
            messagebox.showwarning("Экспорт", "Нет данных для сохранения.")
            return

        with open('weather_log.csv', 'w', newline='', encoding='utf-8') as f:
            writer = csv.writer(f)
            writer.writerow(['Day', 'State', 'Weather'])
            for day, state in enumerate(self.history):
                writer.writerow([day, state, WEATHER_NAMES[state].split()[0]])

        with open('statistics.csv', 'w', newline='', encoding='utf-8') as f:
            writer = csv.writer(f)
            writer.writerow(['Metric', 'State', 'Value'])
            counts = [self.history.count(s) for s in range(1, self.num_states+1)]
            total_days = len(self.history)

            for i in range(1, self.num_states+1):
                writer.writerow(['Empirical_Count', WEATHER_NAMES[i].split()[0], counts[i-1]])
                writer.writerow(['Empirical_Probability', WEATHER_NAMES[i].split()[0], counts[i-1]/total_days])
                writer.writerow(['Theoretical_Stationary_Probability', WEATHER_NAMES[i].split()[0], self.pi_theoretical[i-1]])

            writer.writerow([])
            writer.writerow(['Empirical Transition Matrix', '', ''])
            for i in range(self.num_states):
                for j in range(self.num_states):
                    writer.writerow(['Transition_Count', f'{i+1}->{j+1}', self.trans_count[i, j]])
            row_sums = self.trans_count.sum(axis=1, keepdims=True)
            with np.errstate(divide='ignore', invalid='ignore'):
                trans_prob = np.where(row_sums > 0, self.trans_count / row_sums, 0.0)
            writer.writerow([])
            writer.writerow(['Empirical Transition Probabilities', '', ''])
            for i in range(self.num_states):
                for j in range(self.num_states):
                    writer.writerow(['Transition_Probability', f'{i+1}->{j+1}', trans_prob[i, j]])

            writer.writerow([])
            writer.writerow(['Theoretical P(1) = expm(Q)', '', ''])
            for i in range(self.num_states):
                for j in range(self.num_states):
                    writer.writerow(['Theoretical_Transition', f'{i+1}->{j+1}', self.P[i, j]])

        messagebox.showinfo("Экспорт", "Данные сохранены в weather_log.csv и statistics.csv")

    def show_statistics(self):
        if not self.history:
            messagebox.showinfo("Статистика", "Нет данных для анализа.")
            return

        total = len(self.history)
        counts = [self.history.count(s) for s in range(1, self.num_states+1)]

        text = f"Статистика после {total} дней\n"
        text += "="*40 + "\n"
        text += f"{'Погода':<12} {'Дней':<6} {'Частота':<10} {'Теор.стац.':<12}\n"
        for i in range(self.num_states):
            wname = WEATHER_NAMES[i+1].split()[0]
            text += f"{wname:<12} {counts[i]:<6} {counts[i]/total:<10.4f} {self.pi_theoretical[i]:<12.4f}\n"

        text += "\nСредняя продолжительность серии:\n"
        for state in range(1, self.num_states+1):
            seq = []
            cnt = 0
            for s in self.history:
                if s == state:
                    cnt += 1
                elif cnt > 0:
                    seq.append(cnt)
                    cnt = 0
            if cnt > 0:
                seq.append(cnt)
            emp_mean = np.mean(seq) if seq else 0
            theo_mean = 1 / (1 - self.P[state-1, state-1]) if self.P[state-1, state-1] < 1 else float('inf')
            text += f"{WEATHER_NAMES[state].split()[0]:<12} эмп.={emp_mean:.2f}   теор.={theo_mean:.2f}\n"

        stat_win = tk.Toplevel(self.root)
        stat_win.title("Статистика моделирования")
        stat_win.configure(bg='black')
        text_widget = tk.Text(stat_win, wrap=tk.WORD, font=("Consolas", 11),
                              bg='gray20', fg='white', insertbackground='white')
        text_widget.insert("1.0", text)
        text_widget.config(state=tk.DISABLED)
        text_widget.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

    def on_closing(self):
        self.pause()
        self.root.destroy()


if __name__ == "__main__":
    root = tk.Tk()
    app = MarkovWeatherApp(root)
    root.mainloop()