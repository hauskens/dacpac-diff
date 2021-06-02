﻿using DacpacDiff.Core.Utility;
using System;

namespace DacpacDiff.Core.Model
{
    public class IndexModuleModel : ModuleModel
    {
        public bool IsClustered { get; set; }
        public bool IsUnique { get; set; }

        public string IndexedObject { get; init; } = string.Empty;

        public string[] IndexedColumns { get; set; } = Array.Empty<string>();
        public string[] IncludedColumns { get; set; } = Array.Empty<string>();
        public string? Condition { get; set; }

        public IndexModuleModel(SchemaModel schema, string name)
            : base(schema, name, ModuleType.INDEX)
        {
        }

        public override bool IsSimilarDefinition(ModuleModel other)
        {
            if (other is not IndexModuleModel idx)
            {
                return false;
            }

            return this.IsEqual(idx,
                m => m.IsClustered,
                m => m.IsUnique,
                m => m.IndexedObject,
                m => m.IndexedColumns,
                m => m.IncludedColumns,
                m => m.Condition?.ScrubSQL());
        }
    }
}
