using System;
using S2.BlackSwan.SupplyCollector.Models;
using Xunit;

namespace AzureBlobSupplyCollectorTests
{
    public class AzureBlobSupplyCollectorTests : IClassFixture<LaunchSettingsFixture>
    {
        private AzureBlobSupplyCollector.AzureBlobSupplyCollector _instance;
        private DataContainer _container;
        private LaunchSettingsFixture _fixture;

        public AzureBlobSupplyCollectorTests(LaunchSettingsFixture fixture) {
            _fixture = fixture;

            _instance = new AzureBlobSupplyCollector.AzureBlobSupplyCollector();
            _container = new DataContainer()
            {
                ConnectionString = _instance.BuildConnectionString(
                    Environment.GetEnvironmentVariable("AZUREBLOB_ACCOUNT_NAME"),
                    Environment.GetEnvironmentVariable("AZUREBLOB_ACCOUNT_KEY"),
                    Environment.GetEnvironmentVariable("AZUREBLOB_CONTAINER")
                    )
            };
        }

        [Fact]
        public void DataStoreTypesTest()
        {
            var result = _instance.DataStoreTypes();
            Assert.Contains("AzureBlob", result);
        }

        [Fact]
        public void TestConnectionTest()
        {
            var result = _instance.TestConnection(_container);
            Assert.True(result);
        }

        [Fact]
        public void TestSchemaTest()
        {
            var (tables, elements) = _instance.GetSchema(_container);

            Assert.Equal(2, tables.Count);

            Assert.NotNull(elements.Find(x => x.Name.Equals("FROM_NAME")));
        }

        [Fact]
        public void TestCollectSamplesTest()
        {
            var entity = new DataEntity("FROM_ADDR", DataType.String, "String", _container, new DataCollection(_container, "EMAILS-UTF8.CSV"));
            var samples = _instance.CollectSample(entity, 5);
            Assert.Equal(5, samples.Count);
            Assert.Contains("sally@example.com", samples);
        }

        [Fact]
        public void TestFilenamesInSchema() {
            var prefixCollector = new AzureBlobSupplyCollector.AzureBlobSupplyCollector("emails/2019/08", 0, true);
            var (tables, elements) = prefixCollector.GetSchema(_container);

            Assert.Equal(1, tables.Count);
            Assert.Equal(39, elements.Count);
            Assert.Equal("EMAILS-UTF8.CSV", tables[0].Name);

            var levelsCollector = new AzureBlobSupplyCollector.AzureBlobSupplyCollector(null, 1, false);
            (tables, elements) = levelsCollector.GetSchema(_container);

            Assert.Equal(1, tables.Count);
            Assert.Equal(39, elements.Count);
            Assert.Equal("emails", tables[0].Name);

            var noprefixCollector = new AzureBlobSupplyCollector.AzureBlobSupplyCollector(null, 1, true);
            (tables, elements) = noprefixCollector.GetSchema(_container);

            Assert.Equal(2, tables.Count);
            Assert.Equal(69, elements.Count);

            Assert.NotNull(tables.Find(x => x.Name.Equals("emails/EMAILS-UTF8.CSV")));
            Assert.NotNull(tables.Find(x => x.Name.Equals("emails/emails-utf8.parquet")));
        }
    }
}
