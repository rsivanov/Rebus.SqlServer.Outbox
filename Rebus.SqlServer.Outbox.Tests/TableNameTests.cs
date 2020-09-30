using Rebus.SqlServer.Outbox.Internal;
using Xunit;

namespace Rebus.SqlServer.Outbox.Tests
{
    /// <summary>
    /// Tests are copied from https://github.com/rebus-org/Rebus.SqlServer
    /// </summary>
	public class TableNameTests
	{
        [Theory]
        [InlineData("[bimse]", "[bimse]")]
        [InlineData("[bimse]", "[BIMSE]")]
        public void CheckEquality(string name1, string name2)
        {
            var tableName1 = TableName.Parse(name1);
            var tableName2 = TableName.Parse(name2);

            Assert.Equal(tableName1, tableName2);
        }

        [Theory]
        [InlineData("table", "dbo", "table")]
        [InlineData("[table]", "dbo", "table")]
        [InlineData("dbo.table", "dbo", "table")]
        [InlineData("schema.table", "schema", "table")]
        [InlineData("[schema].[table]", "schema", "table")]
        [InlineData("[Table name with spaces in it]", "dbo", "Table name with spaces in it")]
        [InlineData("[Table name with . in it]", "dbo", "Table name with . in it")]
        [InlineData("[schema-qualified table name with dots in it].[Table name with . in it]", "schema-qualified table name with dots in it", "Table name with . in it")]
        [InlineData("[Schema name with . in it].[Table name with . in it]", "Schema name with . in it", "Table name with . in it")]
        [InlineData("[Schema name with . in it] .[Table name with . in it]", "Schema name with . in it", "Table name with . in it")]
        [InlineData("[Schema name with . in it] . [Table name with . in it]", "Schema name with . in it", "Table name with . in it")]
        [InlineData("[Schema name with . in it]. [Table name with . in it]", "Schema name with . in it", "Table name with . in it")]
        public void MoreExamples(string input, string expectedSchema, string expectedTable)
        {
            var tableName = TableName.Parse(input);

            Assert.Equal(expectedSchema, tableName.Schema);
            Assert.Equal(expectedTable, tableName.Name);
            Assert.Equal($"[{expectedSchema}].[{expectedTable}]", tableName.QualifiedName);
        }

        [Fact]
        public void ParsesNameWithoutSchemaAssumingDboAsDefault()
        {
            var table = TableName.Parse("TableName");

            Assert.Equal("TableName", table.Name);
            Assert.Equal("dbo", table.Schema);
            Assert.Equal("[dbo].[TableName]", table.QualifiedName);
        }

        [Fact]
        public void ParsesBracketsNameWithoutSchemaAssumingDboAsDefault()
        {
            var table = TableName.Parse("[TableName]");

            Assert.Equal("TableName", table.Name);
            Assert.Equal("dbo", table.Schema);
            Assert.Equal("[dbo].[TableName]", table.QualifiedName);
        }

        [Fact]
        public void ParsesNameWithSchema()
        {
            var table = TableName.Parse("schema.TableName");

            Assert.Equal("TableName", table.Name);
            Assert.Equal("schema", table.Schema);
            Assert.Equal("[schema].[TableName]", table.QualifiedName);
        }

        [Fact]
        public void ParsesBracketsNameWithSchema()
        {
            var table = TableName.Parse("[schema].[TableName]");

            Assert.Equal("TableName", table.Name);
            Assert.Equal("schema", table.Schema);
            Assert.Equal("[schema].[TableName]", table.QualifiedName);
        }
    }
}