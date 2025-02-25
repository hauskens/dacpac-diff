﻿using DacpacDiff.Core.Diff;
using DacpacDiff.Core.Model;
using DacpacDiff.Core.Output;

namespace DacpacDiff.Mssql.Diff;

public class MssqlDiffFieldCreate(DiffFieldCreate diff)
    : BaseMssqlDiffBlock<DiffFieldCreate>(diff)
{
    private static void appendFieldSql(FieldModel fld, ISqlFileBuilder sb)
    {
        sb.Append($"[{fld.Name}]");

        if ((fld.Computation?.Length ?? 0) > 0)
        {
            sb.Append($" AS {fld.Computation}");
            return;
        }

        sb.Append($" {fld.Type}")
            .AppendIf(() => " COLLATE " + fld.Collation, fld.Collation != null)
            .Append(!fld.Nullable && fld.HasDefault ? " NOT NULL" : " NULL")
            .AppendIf(() => $" DEFAULT ({fld.DefaultValue})", fld.IsDefaultSystemNamed)
            .AppendIf(() => $" REFERENCES {fld.Ref?.TargetField.Table.FullName} ([{fld.Ref?.TargetField.Name}])", fld.Ref?.IsSystemNamed == true);
    }

    protected override void GetFormat(ISqlFileBuilder sb)
    {
        // TODO: unique

        var fld = _diff.Field;
        sb.Append($"ALTER TABLE {fld.Table.FullName} ADD ");
        appendFieldSql(fld, sb);
        sb.AppendIf(() => " -- NOTE: Cannot create NOT NULL column", !fld.Nullable && !fld.HasDefault)
            .AppendLine();

        // TODO: Way to provide transformation method for adding NOT NULL column
    }
}
