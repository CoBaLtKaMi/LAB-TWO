using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json; // Подключаем библиотеку для работы с JSON
using System.Text.Json.Serialization;

// Модели данных (названия свойств должны совпадать с ключами в JSON-файле)
public record ProcessorModel(
    [property: JsonPropertyName("base_cost")] int BaseCost,
    [property: JsonPropertyName("cache_miss_penalty")] int CacheMissPenalty,
    [property: JsonPropertyName("branch_misprediction_penalty")] int BranchMispredictionPenalty
);

public record Operation(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("memory_accesses")] int MemoryAccesses,
    [property: JsonPropertyName("branches")] int Branches
);

// Вспомогательный класс-обертка для десериализации корневого JSON
public class RootConfig
{
    [JsonPropertyName("processor_model")]
    public ProcessorModel ProcessorModel { get; set; }

    [JsonPropertyName("deadline")]
    public int Deadline { get; set; }

    [JsonPropertyName("fragment")]
    public List<Operation> Fragment { get; set; }
}

class Program
{
    // Время выполнения операции в лучшем случае (без задержек)
    static int BestCase(Operation op, ProcessorModel model) => model.BaseCost;

    // Время выполнения операции в худшем случае (с учетом штрафов)
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

    // Расчет коэффициента недетерминизма
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

        // Поиск файла конфигурации JSON в папке приложения
        string filepath = Path.Combine(AppContext.BaseDirectory, "data", "input.json");

        if (!File.Exists(filepath))
        {
            Console.WriteLine($"Ошибка: Файл данных не найден по пути: {filepath}");
            return;
        }

        // Чтение всего содержимого файла JSON в виде одной текстовой строки
        string jsonString = File.ReadAllText(filepath);

        // Автоматическое превращение строки JSON в структурированные объекты C#
        RootConfig config = JsonSerializer.Deserialize<RootConfig>(jsonString);

        if (config == null || config.ProcessorModel == null || config.Fragment == null)
        {
            Console.WriteLine("Ошибка: Не удалось корректно распознать структуру JSON файла.");
            return;
        }

        ProcessorModel model = config.ProcessorModel;
        int deadline = config.Deadline;
        List<Operation> fragment = config.Fragment;

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
            Console.WriteLine("ВЫВОД: Фрагмент укладывается в дедлайн в худшем случае.");
        }
        else
        {
            Console.WriteLine("ВЫВОД: Фрагмент не укладывается в дедлайн в худшем случае.");
        }
        Console.WriteLine(new string('=', 95));
    }
}
