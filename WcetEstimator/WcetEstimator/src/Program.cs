using System;
using System.IO;
using System.Collections.Generic;

// Структуры данных для параметров процессора и операции
public record ProcessorModel(int BaseCost, int CacheMissPenalty, int BranchMispredictionPenalty);
public record Operation(string Name, int MemoryAccesses, int Branches);

class Program
{
    // Время выполнения операции в лучшем случае (без задержек)
    static int BestCase(Operation op, ProcessorModel model) => model.BaseCost;

    // Время выполнения операции в худшем случае (с учетом максимальных штрафов)
    static int WorstCase(Operation op, ProcessorModel model)
    {
        int memoryPenalty = op.MemoryAccesses * model.CacheMissPenalty;
        int branchPenalty = op.Branches * model.BranchMispredictionPenalty;
        return model.BaseCost + memoryPenalty + branchPenalty;
    }

    // Суммарное лучшее время фрагмента (BCET)
    static int Bcet(List<Operation> fragment, ProcessorModel model)
    {
        int total = 0;
        foreach (var op in fragment) total += BestCase(op, model);
        return total;
    }

    // Суммарное наихудшее время фрагмента (WCET)
    static int Wcet(List<Operation> fragment, ProcessorModel model)
    {
        int total = 0;
        foreach (var op in fragment) total += WorstCase(op, model);
        return total;
    }

    // Расчет коэффициента недетерминизма (непредсказуемости)
    static double NondeterminismRatio(List<Operation> fragment, ProcessorModel model)
    {
        int b = Bcet(fragment, model);
        return b == 0 ? 0 : (double)Wcet(fragment, model) / b;
    }

    // Разделение вклада источников задержек (память и ветвления)
    static (int Memory, int Branches) SourceBreakdown(List<Operation> fragment, ProcessorModel model)
    {
        int totalMemory = 0;
        int totalBranch = 0;
        foreach (var op in fragment)
        {
            totalMemory += op.MemoryAccesses * model.CacheMissPenalty;
            totalBranch += op.Branches * model.BranchMispredictionPenalty;
        }
        return (totalMemory, totalBranch);
    }

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Поиск файла конфигурации в папке приложения
        string filepath = Path.Combine(AppContext.BaseDirectory, "data", "input.txt");

        if (!File.Exists(filepath))
        {
            Console.WriteLine($"Ошибка: Файл данных не найден по пути: {filepath}");
            return;
        }

        ProcessorModel model = null;
        int deadline = 0;
        var fragment = new List<Operation>();

        // Чтение данных из текстового файла
        foreach (var line in File.ReadAllLines(filepath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

            var parts = trimmed.Split(';');
            for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();

            if (model == null)
            {
                // Первая строка: параметры процессора и дедлайн
                model = new ProcessorModel(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
                deadline = int.Parse(parts[3]);
            }
            else
            {
                // Последующие строки: операции
                fragment.Add(new Operation(parts[0], int.Parse(parts[1]), int.Parse(parts[2])));
            }
        }

        // Вывод таблицы операций
        Console.WriteLine(new string('=', 95));
        Console.WriteLine($"{"Операция",-32} | {"Память",-8} | {"Ветвления",-9} | {"BCET (такт)",-13} | {"WCET (такт)",-13}");
        Console.WriteLine(new string('-', 95));

        foreach (var op in fragment)
        {
            Console.WriteLine($"{op.Name,-32} | {op.MemoryAccesses,-8} | {op.Branches,-9} | {BestCase(op, model),-13} | {WorstCase(op, model),-13}");
        }
        Console.WriteLine(new string('=', 95));

        // Вычисление глобальных метрик
        int totalBcet = Bcet(fragment, model);
        int totalWcet = Wcet(fragment, model);
        double ratio = NondeterminismRatio(fragment, model);
        var breakdown = SourceBreakdown(fragment, model);

        // Вывод итоговых результатов
        Console.WriteLine("\n[РЕЗУЛЬТАТЫ АНАЛИЗА]");
        Console.WriteLine($"Суммарное лучшее время (BCET):           {totalBcet} тактов");
        Console.WriteLine($"Суммарное наихудшее время (WCET):        {totalWcet} тактов");
        Console.WriteLine($"Коэффициент недетерминизма (WCET/BCET):  {ratio:F2}");
        Console.WriteLine($"Вклад штрафов памяти:                    {breakdown.Memory} тактов");
        Console.WriteLine($"Вклад штрафов ветвлений:                 {breakdown.Branches} тактов");
        Console.WriteLine($"Заданный дедлайн:                        {deadline} тактов");
        Console.WriteLine(new string('-', 55));

        // Проверка соблюдения условий реального времени
        if (totalWcet <= deadline)
        {
            Console.WriteLine("ЗАКЛЮЧЕНИЕ: Фрагмент гарантированно УБИРАЕТСЯ в дедлайн в худшем случае.");
        }
        else
        {
            Console.WriteLine("ЗАКЛЮЧЕНИЕ: ВНИМАНИЕ! Фрагмент НЕ УКЛАДЫВАЕТСЯ в дедлайн в худшем случае.");
        }
        Console.WriteLine(new string('=', 95));
    }
}
