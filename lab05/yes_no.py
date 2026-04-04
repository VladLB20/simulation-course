import tkinter as tk
from tkinter import messagebox

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


class YesNoApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Генератор ответов ДА / НЕТ")
        self.root.geometry("500x300")
        self.root.resizable(False, False)

        self.rng = MRNG(seed=42)  

        self.label_question = tk.Label(root, text="Твой вопрос (односложный):", font=("Arial", 12))
        self.label_question.pack(pady=10)

        self.question_entry = tk.Entry(root, width=50, font=("Arial", 11))
        self.question_entry.pack(pady=5)
        self.question_entry.bind("<Return>", lambda event: self.get_answer())

        self.ask_button = tk.Button(root, text="Получить ответ", command=self.get_answer, bg="lightblue", font=("Arial", 11))
        self.ask_button.pack(pady=10)

        self.result_label = tk.Label(root, text="", font=("Arial", 16, "bold"), fg="darkgreen")
        self.result_label.pack(pady=20)

    def get_answer(self):
        question = self.question_entry.get().strip()
        if not question:
            messagebox.showwarning("Пустой вопрос", "Надо что-то ввести.")
            return

        r = self.rng.next_double()
        answer = "НЕТ" if r < 0.5 else "ДА"

        self.result_label.config(text=f"Ответ: {answer}", fg="darkblue" if answer == "ДА" else "darkred")

        self.question_entry.delete(0, tk.END)


if __name__ == "__main__":
    root = tk.Tk()
    app = YesNoApp(root)
    root.mainloop()