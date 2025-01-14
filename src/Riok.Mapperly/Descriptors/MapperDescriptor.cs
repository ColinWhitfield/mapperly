using Microsoft.CodeAnalysis;
using Riok.Mapperly.Descriptors.Mappings;

namespace Riok.Mapperly.Descriptors;

public class MapperDescriptor
{
    private const string FileNameSuffix = ".g.cs";

    private readonly List<TypeMapping> _mappings = new();

    public MapperDescriptor(string name)
    {
        Name = name;
    }

    public string? Namespace { get; set; }

    public string Name { get; }

    public string FileName => Name + FileNameSuffix;

    public Accessibility Accessibility { get; set; } = Accessibility.Public;

    public IEnumerable<MethodMapping> MethodTypeMappings
        => _mappings.OfType<MethodMapping>();

    public void AddTypeMapping(TypeMapping mapping)
        => _mappings.Add(mapping);
}
