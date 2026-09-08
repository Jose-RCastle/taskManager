using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManagerOS.Core.Models;

namespace TaskManagerOS.Core.Persistence;

public sealed class ProjectStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true, PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public void Save(string path, ProjectData project)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null) Directory.CreateDirectory(directory);
        File.WriteAllText(path, JsonSerializer.Serialize(project, Options));
    }

    public ProjectData Load(string path) => JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(path), Options)
        ?? throw new InvalidDataException("El archivo JSON no contiene un proyecto válido.");
}
