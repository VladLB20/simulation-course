import random
import numpy as np  
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


class MidSquare:
    def __init__(self, seed=1994, digits=4):
        if digits % 2 != 0:
            raise ValueError("Количество цифр должно быть чётным.")
        self.digits = digits
        self.modulus = 10 ** digits
        self.current = seed % self.modulus
        if self.current == 0:
            self.current = 1994  

    def _next_int(self):
        square = self.current ** 2
        num_digits_sq = len(str(square))
        shift = (num_digits_sq - self.digits) // 2
        next_val = (square // (10 ** shift)) % self.modulus
        self.current = next_val
        return next_val

    def next_double(self):
        return self._next_int() / self.modulus


class FibonacciRNG:
    def __init__(self, seed=42):
        self.mod = 2**31
        self.k = 5
        self.l = 3
        mrng = MRNG(seed)
        self.state = [mrng._next() % self.mod for _ in range(self.k)]
        self.index = 0

    def _next(self):
        new_val = (self.state[(self.index - self.k) % self.k] +self.state[(self.index - self.l) % self.k]) % self.mod
        self.state[self.index] = new_val
        self.index = (self.index + 1) % self.k
        return new_val

    def next_double(self):
        return self._next() / self.mod


def compute_mean_variance(samples):
    n = len(samples)
    mean = sum(samples) / n
    variance = sum((x - mean) ** 2 for x in samples) / (n - 1)
    return mean, variance


def main():
    n = 100000
    seed = 42

    mrng = MRNG(seed)
    mrng_samples = [mrng.next_double() for _ in range(n)]
    mean_mrng, var_mrng = compute_mean_variance(mrng_samples)

    ms = MidSquare(seed=1994, digits=4)
    ms_samples = [ms.next_double() for _ in range(n)]
    mean_ms, var_ms = compute_mean_variance(ms_samples)

    fib = FibonacciRNG(seed)
    fib_samples = [fib.next_double() for _ in range(n)]
    mean_fib, var_fib = compute_mean_variance(fib_samples)

    random.seed(seed)
    builtin_samples = [random.random() for _ in range(n)]
    mean_builtin, var_builtin = compute_mean_variance(builtin_samples)

    theo_mean = 0.5
    theo_var = 1.0 / 12.0

    print("Мультипликативный конгруэнтный генератор ")
    print(f"Среднее: {mean_mrng:.8f}  Дисперсия: {var_mrng:.8f}")
    print(f"Отклонение от теории: |ср-0.5| = {abs(mean_mrng - theo_mean):.8f}, |дисп-1/12| = {abs(var_mrng - theo_var):.8f}\n")

    print("Метод середины квадрата (4 знака)")
    print(f"Среднее: {mean_ms:.8f}  Дисперсия: {var_ms:.8f}")
    print(f"Отклонение от теории: |ср-0.5| = {abs(mean_ms - theo_mean):.8f}, |дисп-1/12| = {abs(var_ms - theo_var):.8f}\n")

    print("Лагающий генератор Фибоначчи ")
    print(f"Среднее: {mean_fib:.8f}  Дисперсия: {var_fib:.8f}")
    print(f"Отклонение от теории: |ср-0.5| = {abs(mean_fib - theo_mean):.8f}, |дисп-1/12| = {abs(var_fib - theo_var):.8f}\n")

    print("Встроенный генератор random.random()")
    print(f"Среднее: {mean_builtin:.8f}  Дисперсия: {var_builtin:.8f}")
    print(f"Отклонение от теории: |ср-0.5| = {abs(mean_builtin - theo_mean):.8f}, |дисп-1/12| = {abs(var_builtin - theo_var):.8f}")



if __name__ == "__main__":
    main()