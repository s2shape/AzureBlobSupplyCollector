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

            Assert.Equal(3, tables.Count);

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
    }
}
