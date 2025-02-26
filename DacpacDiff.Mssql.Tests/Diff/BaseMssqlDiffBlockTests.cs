﻿using DacpacDiff.Core.Diff;
using DacpacDiff.Core.Model;
using DacpacDiff.Core.Output;
using DacpacDiff.Core.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace DacpacDiff.Mssql.Diff.Tests;

[TestClass]
public class BaseMssqlDiffBlockTests
{
    [ExcludeFromCodeCoverage(Justification = "Not used")]
    public class TestDiff : IDifference
    {
        public IModel? Model { get; set; }

        public string? Title { get; set; }

        public string Name { get; set; } = "TestDiff";
    }
    public class TestMssqlDiffBlock(TestDiff diff)
        : BaseMssqlDiffBlock<TestDiff>(diff)
    {
        public bool Formatted { get; set; }

        protected override void GetFormat(ISqlFileBuilder sb)
        {
            Formatted = true;
            sb.AppendLine("TestMssqlDiffBlock");
        }
    }

    public class TestDataLossChange : TestDiff, IDataLossChange
    {
        public string DataLossTable { get; set; } = string.Empty;

        public bool GetDataLossTable(out string tableName)
        {
            tableName = DataLossTable;
            return tableName.Length > 0;
        }
    }
    public class TestMssqlDataLossBlock(TestDataLossChange diff)
        : BaseMssqlDiffBlock<TestDataLossChange>(diff)
    {
        protected override void GetFormat(ISqlFileBuilder sb)
        {
            sb.AppendLine("TestMssqlDiffBlock");
        }
    }

    [TestMethod]
    public void BaseMssqlDiffBlock__Must_provide_diff()
    {
        // Act
        var ex = Assert.ThrowsException<ArgumentNullException>(() =>
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = new TestMssqlDiffBlock(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        });

        // Assert
        Assert.AreEqual("diff", ex.ParamName);
    }

    [TestMethod]
    public void Format__Diff_block_has_header_and_footer()
    {
        // Arrange
        var diff = new TestDiff();

        var blk = new TestMssqlDiffBlock(diff);

        var optionsMock = new Mock<IOutputOptions>();
        optionsMock.SetupGet(m => m.PrettyPrint).Returns(true);

        var fb = new Mock<BaseSqlFileBuilder>
        {
            CallBase = true
        };
        fb.Object.Options = optionsMock.Object;
        var sql = (StringBuilder)(typeof(BaseSqlFileBuilder).GetField("_sql", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fb.Object)
            ?? throw new NullReferenceException());

        // Act
        blk.Format(fb.Object);
        var res = sql.ToString().StandardiseLineEndings("\n").Trim().Split(['\r', '\n']);

        // Assert
        Assert.IsTrue(blk.Formatted);
        CollectionAssert.AreEqual(new[]
        {
            "EXEC #usp_CheckState 1",
            "BEGIN TRAN",
            "IF (dbo.tmpIsActive() = 0) SET NOEXEC ON",
            "GO",
            "",
            "TestMssqlDiffBlock",
            "",
            "GO",
            "SET NOEXEC OFF",
            "EXEC #usp_CheckState 2",
            "IF (dbo.tmpIsActive() = 0) SET NOEXEC ON",
            "COMMIT",
            "GO",
        }, res, string.Join("\n", res));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Format__DataLoss_block_is_unaffected_if_no_dataloss(bool ddl)
    {
        // Arrange
        var diff = new TestDataLossChange();

        var blk = new TestMssqlDataLossBlock(diff);

        var optionsMock = new Mock<IOutputOptions>();
        optionsMock.SetupGet(m => m.DisableDatalossCheck).Returns(ddl);
        optionsMock.SetupGet(m => m.PrettyPrint).Returns(true);

        var fb = new Mock<BaseSqlFileBuilder>
        {
            CallBase = true
        };
        fb.Object.Options = optionsMock.Object;
        var sql = (StringBuilder)(typeof(BaseSqlFileBuilder).GetField("_sql", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fb.Object)
            ?? throw new NullReferenceException());

        // Act
        blk.Format(fb.Object);
        var res = fb.Object.ToString().StandardiseLineEndings("\n").Trim().Split(['\r', '\n']);

        // Assert
        CollectionAssert.AreEqual(new[]
        {
            "EXEC #usp_CheckState 1",
            "BEGIN TRAN",
            "IF (dbo.tmpIsActive() = 0) SET NOEXEC ON",
            "GO",
            "",
            "TestMssqlDiffBlock",
            "",
            "GO",
            "SET NOEXEC OFF",
            "EXEC #usp_CheckState 2",
            "IF (dbo.tmpIsActive() = 0) SET NOEXEC ON",
            "COMMIT",
            "GO",
        }, res, string.Join("\n", res));
    }

    [TestMethod]
    public void Format__DataLoss_block_includes_fail_block_if_dataloss()
    {
        // Arrange
        var diff = new TestDataLossChange
        {
            DataLossTable = "DataLossTable"
        };

        var blk = new TestMssqlDataLossBlock(diff);

        var optionsMock = new Mock<IOutputOptions>();
        optionsMock.SetupGet(m => m.PrettyPrint).Returns(true);

        var fb = new Mock<BaseSqlFileBuilder>
        {
            CallBase = true
        };
        fb.Object.Options = optionsMock.Object;
        var sql = (StringBuilder)(typeof(BaseSqlFileBuilder).GetField("_sql", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fb.Object)
            ?? throw new NullReferenceException());

        // Act
        blk.Format(fb.Object);
        var res = fb.Object.ToString().StandardiseLineEndings("\n").Trim().Split(['\r', '\n']);

        // Assert
        CollectionAssert.AreEqual(new[]
        {
            "EXEC #usp_CheckState 1",
            "BEGIN TRAN",
            "IF (dbo.tmpIsActive() = 0) SET NOEXEC ON",
            "GO",
            "",
            "IF EXISTS (SELECT TOP 1 1 FROM DataLossTable) BEGIN",
            "    EXEC #print 1, '[WARN] This change may cause dataloss to DataLossTable. Verify and remove this error block to continue.'",
            "    IF (@@TRANCOUNT > 0) ROLLBACK",
            "    SET NOEXEC ON",
            "END",
            "",
            "TestMssqlDiffBlock",
            "",
            "GO",
            "SET NOEXEC OFF",
            "EXEC #usp_CheckState 2",
            "IF (dbo.tmpIsActive() = 0) SET NOEXEC ON",
            "COMMIT",
            "GO",
        }, res, string.Join("\n", res));
    }

    [TestMethod]
    public void Format__DataLoss_block_can_be_disabled_with_option()
    {
        // Arrange
        var diff = new TestDataLossChange
        {
            DataLossTable = "DataLossTable"
        };

        var blk = new TestMssqlDataLossBlock(diff);

        var optionsMock = new Mock<IOutputOptions>();
        optionsMock.SetupGet(m => m.DisableDatalossCheck).Returns(true);
        optionsMock.SetupGet(m => m.PrettyPrint).Returns(true);

        var fb = new Mock<BaseSqlFileBuilder>
        {
            CallBase = true
        };
        fb.Object.Options = optionsMock.Object;
        var sql = (StringBuilder)(typeof(BaseSqlFileBuilder).GetField("_sql", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fb.Object)
            ?? throw new NullReferenceException());

        // Act
        blk.Format(fb.Object);
        var res = fb.Object.ToString().StandardiseLineEndings("\n").Trim().Split(['\r', '\n']);

        // Assert
        CollectionAssert.AreEqual(new[]
        {
            "EXEC #usp_CheckState 1",
            "BEGIN TRAN",
            "IF (dbo.tmpIsActive() = 0) SET NOEXEC ON",
            "GO",
            "",
            "TestMssqlDiffBlock",
            "",
            "GO",
            "SET NOEXEC OFF",
            "EXEC #usp_CheckState 2",
            "IF (dbo.tmpIsActive() = 0) SET NOEXEC ON",
            "COMMIT",
            "GO",
        }, res, string.Join("\n", res));
    }

    [TestMethod]
    public void Format__Without_PrettyPrint_shorter_block()
    {
        // Arrange
        var diff = new TestDiff();

        var blk = new TestMssqlDiffBlock(diff);

        var fb = new Mock<BaseSqlFileBuilder>
        {
            CallBase = true
        };
        var sql = (StringBuilder)(typeof(BaseSqlFileBuilder).GetField("_sql", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fb.Object)
            ?? throw new NullReferenceException());

        // Act
        blk.Format(fb.Object);
        var res = sql.ToString().StandardiseLineEndings("\n").Trim().Split(['\r', '\n']);

        // Assert
        Assert.IsTrue(blk.Formatted);
        CollectionAssert.AreEqual(new[]
        {
            "EXEC #usp_CheckState 1; BEGIN TRAN; IF (dbo.tmpIsActive() = 0) SET NOEXEC ON",
            "GO",
            "",
            "TestMssqlDiffBlock",
            "",
            "GO",
            "SET NOEXEC OFF; EXEC #usp_CheckState 2; IF (dbo.tmpIsActive() = 0) SET NOEXEC ON; COMMIT",
            "GO",
        }, res, string.Join("\n", res));
    }
}