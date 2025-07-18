﻿﻿using Content.Shared.Examine;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype; //Corvax-Frontier

namespace Content.Shared.Construction.Steps
{
    public abstract partial class ArbitraryInsertConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        [DataField] public LocId Name { get; private set; } = string.Empty;

        [DataField] public SpriteSpecifier? Icon { get; private set; }

        [DataField("tag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))] //Corvax-Frontier
        public string? Tag { get; private set; } //Corvax-Frontier

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            if (string.IsNullOrEmpty(Name))
                return;

            var stepName = Loc.GetString(Name);
            examinedEvent.PushMarkup(Loc.GetString("construction-insert-arbitrary-entity", ("stepName", stepName)));
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            var stepName = Loc.GetString(Name);
            return new ConstructionGuideEntry
            {
                Localization = "construction-presenter-arbitrary-step",
                Arguments = new (string, object)[]{("name", stepName)},
                Icon = Icon,
            };
        }
    }
}
