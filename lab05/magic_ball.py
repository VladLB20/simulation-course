import tkinter as tk
from tkinter import messagebox, ttk, filedialog
from PIL import Image, ImageTk
import os

class MRNG:
    def __init__(self, seed=1):
        self.M = 2**63
        self.beta = 2**32 + 3
        self.seed = seed % self.M
        if self.seed == 0:
            self.seed = 1   

    def _next(self):
        self.seed = (self.beta * self.seed) % self.M
        return self.seed

    def next_double(self):
        return self._next() / self.M


MAGIC_ANSWERS = ["Да", "Абсолютно точно", "Не могу сказать", "Нет", "Безусловно", "Вряд ли", "Похоже, что да", "Без сомнений", "Должно быть так",  "Мало шансов"]


class MagicBallApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Magic 8-Ball с картинками ")
        self.root.geometry("900x700")
        
        self.rng = MRNG(seed=42)
        
        self.prob_vars = []
        self.image_paths = [None] * len(MAGIC_ANSWERS)
        self.image_labels = []   
        self.photo_images = []  
        
        question_frame = ttk.LabelFrame(root, text="Вопрос", padding=10)
        question_frame.pack(fill="x", padx=10, pady=5)
        
        self.question_entry = tk.Entry(question_frame, width=70, font=("Arial", 11))
        self.question_entry.pack(fill="x", padx=5, pady=5)
        self.question_entry.bind("<Return>", lambda e: self.get_answer())
        
        prob_frame = ttk.LabelFrame(root, text="Настройка вероятностей и изображений (сумма вероятностей = 1)", padding=10)
        prob_frame.pack(fill="both", expand=True, padx=10, pady=5)
        
        canvas = tk.Canvas(prob_frame)
        scrollbar = ttk.Scrollbar(prob_frame, orient="vertical", command=canvas.yview)
        scrollable_frame = ttk.Frame(canvas)
        
        scrollable_frame.bind(
            "<Configure>",
            lambda e: canvas.configure(scrollregion=canvas.bbox("all"))
        )
        canvas.create_window((0, 0), window=scrollable_frame, anchor="nw")
        canvas.configure(yscrollcommand=scrollbar.set)
        
        canvas.pack(side="left", fill="both", expand=True)
        scrollbar.pack(side="right", fill="y")
        
        header_frame = ttk.Frame(scrollable_frame)
        header_frame.pack(fill="x", pady=5)
        ttk.Label(header_frame, text="Ответ", width=30, anchor="w").pack(side="left", padx=5)
        ttk.Label(header_frame, text="Вероятность (0..1)", width=15, anchor="center").pack(side="left", padx=5)
        ttk.Label(header_frame, text="Изображение", width=15, anchor="center").pack(side="left", padx=5)
        ttk.Label(header_frame, text="Действие", width=12, anchor="center").pack(side="left", padx=5)
        
        for i, answer in enumerate(MAGIC_ANSWERS):
            row_frame = ttk.Frame(scrollable_frame)
            row_frame.pack(fill="x", pady=3)
            
            lbl = ttk.Label(row_frame, text=answer, width=30, anchor="w")
            lbl.pack(side="left", padx=5)
            
            var = tk.StringVar(value="0.1")  
            entry = ttk.Entry(row_frame, textvariable=var, width=8)
            entry.pack(side="left", padx=5)
            self.prob_vars.append(var)
            
            img_label = ttk.Label(row_frame, text="нет картинки", width=15, anchor="center", relief="sunken")
            img_label.pack(side="left", padx=5)
            self.image_labels.append(img_label)
            
            btn_load = ttk.Button(row_frame, text="Загрузить", command=lambda idx=i: self.load_image(idx))
            btn_load.pack(side="left", padx=5)
            
            btn_clear = ttk.Button(row_frame, text="X", width=2, command=lambda idx=i: self.clear_image(idx))
            btn_clear.pack(side="left", padx=2)
        
        btn_frame = ttk.Frame(root)
        btn_frame.pack(fill="x", padx=10, pady=5)
        
        ttk.Button(btn_frame, text="Проверить сумму ", command=self.check_sum).pack(side="left", padx=5)
        ttk.Button(btn_frame, text="Сбросить вероятности (0.1)", command=self.reset_probs).pack(side="left", padx=5)
        ttk.Button(btn_frame, text="Сбросить все картинки", command=self.clear_all_images).pack(side="left", padx=5)
        ttk.Button(btn_frame, text="Получить ответ", command=self.get_answer, width=15).pack(side="right", padx=5)
        
        result_frame = ttk.LabelFrame(root, text="Ответ магического шара", padding=10)
        result_frame.pack(fill="both", expand=True, padx=10, pady=10)
        
        self.result_label = ttk.Label(result_frame, text="", font=("Arial", 14, "bold"), foreground="darkblue")
        self.result_label.pack(pady=10)
        
        self.result_image_label = ttk.Label(result_frame)
        self.result_image_label.pack(pady=10)
        
        self.status_label = ttk.Label(root, text="Сумма вероятностей: 1.00", foreground="green")
        self.status_label.pack(side="bottom", fill="x", padx=10, pady=5)
        
        self.check_sum(show_message=False)
    
    def load_image(self, idx):

        file_path = filedialog.askopenfilename(
            title=f"Изображение для ответа: {MAGIC_ANSWERS[idx]}",
            filetypes=[("Image files", "*.png *.jpg *.jpeg *.gif *.bmp")]
        )
        if file_path:
            self.image_paths[idx] = file_path
            self.update_thumbnail(idx)
    
    def update_thumbnail(self, idx):
        path = self.image_paths[idx]
        if path and os.path.exists(path):
            try:
                img = Image.open(path)
                img.thumbnail((60, 60))  
                photo = ImageTk.PhotoImage(img)
                self.image_labels[idx].config(image=photo, text="")
                self.image_labels[idx].image = photo  
            except Exception as e:
                self.image_labels[idx].config(text="ошибка", image="")
                self.image_labels[idx].image = None
        else:
            self.image_labels[idx].config(text="нет картинки", image="")
            self.image_labels[idx].image = None
    
    def clear_image(self, idx):
        self.image_paths[idx] = None
        self.update_thumbnail(idx)
    
    def clear_all_images(self):
        for i in range(len(MAGIC_ANSWERS)):
            self.image_paths[i] = None
            self.update_thumbnail(i)
        messagebox.showinfo("Сброс", "Всё удалено.")
    
    def get_current_probs(self):
        probs = []
        for var in self.prob_vars:
            try:
                val = float(var.get().replace(',', '.'))
                if val < 0 or val > 1:
                    raise ValueError
                probs.append(val)
            except:
                messagebox.showerror("Ошибка", f"Некорректная вероятность: {var.get()}\nДопустимы числа от 0 до 1.")
                return None
        return probs
    
    def check_sum(self, show_message=True):
        probs = self.get_current_probs()
        if probs is None:
            return False
        total = sum(probs)
        if abs(total - 1.0) > 1e-6:
            self.status_label.config(text=f"Сумма вероятностей = {total:.6f} (должна быть 1)", foreground="red")
            if show_message:
                messagebox.showwarning("Сумма не равна 1", f"Текущая сумма = {total:.6f}\nНадо нормально ввести.")
            return False
        else:
            self.status_label.config(text=f"Сумма вероятностей = {total:.6f} (OK)", foreground="green")
            return True
    
    def reset_probs(self):
        equal = 1.0 / len(MAGIC_ANSWERS)
        for var in self.prob_vars:
            var.set(f"{equal:.6f}")
        self.check_sum(show_message=False)
        messagebox.showinfo("Сброс вероятностей", f"Вероятности установлены равными: {equal:.6f} каждая.")
    
    def get_answer(self):
        question = self.question_entry.get().strip()
        if not question:
            messagebox.showwarning("Нет вопроса", "Надо ввести вопрос.")
            return
        
        if not self.check_sum(show_message=True):
            return
        
        probs = self.get_current_probs()
        if probs is None:
            return
        
        total = sum(probs)
        if abs(total - 1.0) > 1e-9:
            probs = [p / total for p in probs]
        
        r = self.rng.next_double()
        cum = 0.0
        chosen_idx = 0
        for i, p in enumerate(probs):
            cum += p
            if r < cum:
                chosen_idx = i
                break
        else:
            chosen_idx = len(probs) - 1
        
        answer_text = MAGIC_ANSWERS[chosen_idx]
        self.result_label.config(text=f"{answer_text}")
        
        img_path = self.image_paths[chosen_idx]
        if img_path and os.path.exists(img_path):
            try:
                img = Image.open(img_path)
                img.thumbnail((300, 300))
                photo = ImageTk.PhotoImage(img)
                self.result_image_label.config(image=photo)
                self.result_image_label.image = photo 
            except Exception as e:
                self.result_image_label.config(image="")
                self.result_image_label.image = None
                messagebox.showwarning("Ошибка", f"Не удалось загрузить :\n{img_path}")
        else:
            self.result_image_label.config(image="")
            self.result_image_label.image = None


def main():
    root = tk.Tk()
    app = MagicBallApp(root)
    root.mainloop()

if __name__ == "__main__":
    main()