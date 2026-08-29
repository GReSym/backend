using GReSym.Parser.Interfaces;
using GReSym.Parser.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GReSym.Parser.Services;

public class FileSystemParserStateManager : IParserStateManager
{
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileSystemParserStateManager(string? storageDirectory = null)
    {
        _storageDirectory = storageDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ParserStates"
        );
        
        // Настройка сериализации JSON
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        
        // Создание директории, если её нет
        EnsureStorageDirectoryExists();
    }

    public async Task<ParserState?> LoadStateAsync(string checkpointName)
    {
        if (string.IsNullOrEmpty(checkpointName))
            throw new ArgumentException("Checkpoint name cannot be null or empty", nameof(checkpointName));

        string filePath = GetFilePath(checkpointName);
        
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<ParserState>(json, _jsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Логирование ошибки (добавьте здесь вашу логику логирования)
            Console.WriteLine($"Error loading state from {filePath}: {ex.Message}");
            return null;
        }
    }

    public async Task SaveStateAsync(ParserState state, string checkpointName)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        if (string.IsNullOrEmpty(checkpointName))
            throw new ArgumentException("Checkpoint name cannot be null or empty", nameof(checkpointName));

        string filePath = GetFilePath(checkpointName);
        string tempFilePath = GetTempFilePath(checkpointName);

        try
        {
            // Создаём JSON
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            
            // Сначала сохраняем во временный файл (атомарность операции)
            await File.WriteAllTextAsync(tempFilePath, json);
            
            // Затем перемещаем в основной файл
            File.Move(tempFilePath, filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Логирование ошибки
            Console.WriteLine($"Error saving state to {filePath}: {ex.Message}");
            throw;
        }
        finally
        {
            // Очистка временного файла, если он остался
            if (File.Exists(tempFilePath))
            {
                try { File.Delete(tempFilePath); } catch { /* Игнорируем ошибки удаления временного файла */ }
            }
        }
    }

    public Task ClearStateAsync(string checkpointName)
    {
        if (string.IsNullOrEmpty(checkpointName))
            throw new ArgumentException("Checkpoint name cannot be null or empty", nameof(checkpointName));

        string filePath = GetFilePath(checkpointName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        
        return Task.CompletedTask;
    }

    public IEnumerable<string> GetAvailableCheckpoints()
    {
        if (!Directory.Exists(_storageDirectory))
            return Enumerable.Empty<string>();

        try
        {
            return Directory.GetFiles(_storageDirectory, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => name != null)
                .Select(name => name!)
                .ToList();
        }
        catch (IOException)
        {
            // Логирование ошибки доступа к директории
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Полная очистка всех сохранённых состояний
    /// </summary>
    public Task ClearAllStatesAsync()
    {
        if (Directory.Exists(_storageDirectory))
        {
            try
            {
                Directory.Delete(_storageDirectory, recursive: true);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error clearing all states: {ex.Message}");
                throw;
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Получает информацию о размере хранилища
    /// </summary>
    public (long FileCount, long TotalSize) GetStorageInfo()
    {
        if (!Directory.Exists(_storageDirectory))
            return (0, 0);

        var files = Directory.GetFiles(_storageDirectory, "*.json");
        long totalSize = 0;
        
        foreach (var file in files)
        {
            try
            {
                totalSize += new FileInfo(file).Length;
            }
            catch { /* Игнорируем недоступные файлы */ }
        }
        
        return (files.Length, totalSize);
    }

    private string GetFilePath(string checkpointName)
    {
        // Заменяем недопустимые символы в имени файла
        string safeFileName = MakeFileNameSafe(checkpointName);
        return Path.Combine(_storageDirectory, $"{safeFileName}.json");
    }

    private string GetTempFilePath(string checkpointName)
    {
        string safeFileName = MakeFileNameSafe(checkpointName);
        return Path.Combine(_storageDirectory, $"{safeFileName}.tmp");
    }

    private void EnsureStorageDirectoryExists()
    {
        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    private string MakeFileNameSafe(string fileName)
    {
        // Убираем недопустимые символы в именах файлов
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.');
    }
}