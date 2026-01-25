using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Research.Components;

[RegisterComponent]
public sealed partial class TechnologyDiskComponent : Component
{
    [DataField("disciplines", customTypeSerializer: typeof(PrototypeIdListSerializer<TechDisciplinePrototype>))]
    public List<string> Disciplines = new();

    [DataField("unlocked", customTypeSerializer: typeof(PrototypeIdListSerializer<TechnologyPrototype>))]
    public List<string> UnlockedTechnologies = new();
}
