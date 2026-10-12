using System;
using System.IO;
using System.Collections.Generic;

// Аналог @dataclass для модели процессора и операции
public record ProcessorModel(int BaseCost, int CacheMissPenalty, int BranchMispredictionPenalty);
public record Operation(string Name, int MemoryAccesses, int Branches);

class Program
{
    // Отдельные обязательные функции по заданию
    static int BestCase(Operation op, ProcessorModel model) => model.BaseCost;

    static int WorstCase(Operation op, ProcessorModel model)
    {
        int memoryPenalty = op.MemoryAccesses * model.CacheMissPenalty;
        int branchPenalty = op.Branches * model.BranchMispredictionPenalty;
        return model.BaseCost + memoryPenalty + branchPenalty;
    }

    static int Bcet(List<Operation> fragment, ProcessorModel model)
    {
        int total = 0;
        foreach (var op in fragment) total += BestCase(op, model);
        return total;
    }

    static int Wcet(List<Operation> fragment, ProcessorModel model)
    {
        int total = 0;
        foreach (var op in fragment) total += WorstCase(op, model);
        return total;
    }

    static double NondeterminismRatio(List<Operation> fragment, ProcessorModel model)
    {
        int b = Bcet(fragment, model);
        return b == 0 ? 0 : (double)Wcet(fragment, model) / b;
    }

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
        string filepath = Path.Combine(AppContext.BaseDirectory, "data", "input.txt");

        if (!File.Exists(filepath))
        {
            Console.WriteLine($"Ошибка: Файл данных не найден по пути: {filepath}");
            return;
        }

        ProcessorModel model = null;
        int deadline = 0;
        var fragment = new List<Operation>();

        // Парсинг кастомного текстового конфигурационного файла без магических чисел
        foreach (var line in File.ReadAllLines(filepath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

            var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (model == null)
            {
                model = new ProcessorModel(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
                deadline = int.Parse(parts[3]);
            }
            else
            {
                fragment.Add(new Operation(parts[0], int.Parse(parts[1]), int.Parse(parts[2])));
            }
        }

        // Вывод таблицы операций
        Console.WriteLine(new string('=', 85));
        Console.WriteLine($"{"Операция",-28} | {"Память",-8} | {"Ветвления",-9} | {"BCET (такт)",-13} | {"WCET (такт)",-13}");
        Console.WriteLine(new string('-', 85));

        foreach (var op in fragment)
        {
            Console.WriteLine($"{op.Name,-28} | {op.MemoryAccesses,-8} | {op.Branches,-9} | {BestCase(op, model),-13} | {WorstCase(op, model),-13}");
        }
        Console.WriteLine(new string('=', 85));

        // Вычисление итоговых метрик
        int totalBcet = Bcet(fragment, model);
        int totalWcet = Wcet(fragment, model);
        double ratio = NondeterminismRatio(fragment, model);
        var breakdown = SourceBreakdown(fragment, model);

        // Вывод результатов анализа
        Console.WriteLine("\n[РЕЗУЛЬТАТЫ АНАЛИЗА]");
        Console.WriteLine($"Суммарное лучшее время (BCET):           {totalBcet} тактов");
        Console.WriteLine($"Суммарное наихудшее время (WCET):        {totalWcet} тактов");
        Console.WriteLine($"Коэффициент недетерминизма (WCET/BCET):  {ratio:F2}");
        Console.WriteLine($"Вклад штрафов памяти:                    {breakdown.Memory} тактов");
        Console.WriteLine($"Вклад штрафов ветвлений:                 {breakdown.Branches} тактов");
        Console.WriteLine($"Заданный дедлайн:                        {deadline} тактов");
        Console.WriteLine(new string('-', 50));

        if (totalWcet <= deadline)
        {
            Console.WriteLine("ЗАКЛЮЧЕНИЕ: Фрагмент гарантированно УБИРАЕТСЯ в дедлайн в худшем случае.");
        }
        else
        {
            Console.WriteLine("ВНИМАНИЕ! Фрагмент НЕ УКЛАДЫВАЕТСЯ в дедлайн в худшем случае.");
        }
        Console.WriteLine(new string('=', 85));
    }
}