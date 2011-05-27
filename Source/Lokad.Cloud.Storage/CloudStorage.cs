﻿#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.ComponentModel;
using System.Net;
using Lokad.Cloud.Storage.Shared;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Storage
{
    /// <summary>Helper class to get access to cloud storage providers.</summary>
    public static class CloudStorage
    {
        /// <remarks></remarks>
        public static CloudStorageBuilder ForAzureAccount(CloudStorageAccount storageAccount)
        {
            return new AzureCloudStorageBuilder(storageAccount);
        }

        /// <remarks></remarks>
        public static CloudStorageBuilder ForAzureConnectionString(string connectionString)
        {
            CloudStorageAccount storageAccount;
            if (!CloudStorageAccount.TryParse(connectionString, out storageAccount))
            {
                throw new InvalidOperationException("Failed to get valid connection string");
            }

            return new AzureCloudStorageBuilder(storageAccount);
        }

        /// <remarks></remarks>
        public static CloudStorageBuilder ForAzureAccountAndKey(string accountName, string key, bool useHttps = true)
        {
            return new AzureCloudStorageBuilder(new CloudStorageAccount(new StorageCredentialsAccountAndKey(accountName, key), useHttps));
        }

        /// <remarks></remarks>
        public static CloudStorageBuilder ForDevelopmentStorage()
        {
            return new AzureCloudStorageBuilder(CloudStorageAccount.DevelopmentStorageAccount);
        }

        /// <remarks></remarks>
        public static CloudStorageBuilder ForInMemoryStorage()
        {
            return new InMemoryStorageBuilder();
        }

        /// <remarks></remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract class CloudStorageBuilder
        {
            /// <remarks>Can not be null</remarks>
            protected IDataSerializer DataSerializer { get; private set; }

            /// <remarks>Can be null if not needed</remarks>
            protected Shared.Logging.ILog Log { get; private set; }

            /// <remarks>Can be null if not needed</remarks>
            protected IRuntimeFinalizer RuntimeFinalizer { get; private set; }

            /// <remarks></remarks>
            protected CloudStorageBuilder()
            {
                // defaults
                DataSerializer = new CloudFormatter();
            }

            /// <summary>
            /// Replace the default data serializer with a custom implementation
            /// </summary>
            public CloudStorageBuilder WithDataSerializer(IDataSerializer dataSerializer)
            {
                DataSerializer = dataSerializer;
                return this;
            }

            /// <summary>
            /// Optionally provide a log provider.
            /// </summary>
            public CloudStorageBuilder WithLog(Shared.Logging.ILog log)
            {
                Log = log;
                return this;
            }

            /// <summary>
            /// Optionally provide a runtime finalizer.
            /// </summary>
            public CloudStorageBuilder WithRuntimeFinalizer(IRuntimeFinalizer runtimeFinalizer)
            {
                RuntimeFinalizer = runtimeFinalizer;
                return this;
            }

            /// <remarks></remarks>
            public abstract IBlobStorageProvider BuildBlobStorage();

            /// <remarks></remarks>
            public abstract ITableStorageProvider BuildTableStorage();

            /// <remarks></remarks>
            public abstract IQueueStorageProvider BuildQueueStorage();

            /// <remarks></remarks>
            public CloudStorageProviders BuildStorageProviders()
            {
                return new CloudStorageProviders(
                    BuildBlobStorage(),
                    BuildQueueStorage(),
                    BuildTableStorage(),
                    RuntimeFinalizer,
                    Log);
            }
        }
    }

    internal sealed class InMemoryStorageBuilder : CloudStorage.CloudStorageBuilder
    {
        public override IBlobStorageProvider BuildBlobStorage()
        {
            return new InMemory.MemoryBlobStorageProvider
            {
                DataSerializer = DataSerializer
            };
        }

        public override ITableStorageProvider BuildTableStorage()
        {
            return new InMemory.MemoryTableStorageProvider
            {
                DataSerializer = DataSerializer
            };
        }

        public override IQueueStorageProvider BuildQueueStorage()
        {
            return new InMemory.MemoryQueueStorageProvider
            {
                DataSerializer = DataSerializer
            };
        }
    }

    internal sealed class AzureCloudStorageBuilder : CloudStorage.CloudStorageBuilder
    {
        private readonly CloudStorageAccount _storageAccount;

        internal AzureCloudStorageBuilder(CloudStorageAccount storageAccount)
        {
            _storageAccount = storageAccount;

            // http://blogs.msdn.com/b/windowsazurestorage/archive/2010/06/25/nagle-s-algorithm-is-not-friendly-towards-small-requests.aspx
            ServicePointManager.FindServicePoint(storageAccount.TableEndpoint).UseNagleAlgorithm = false;
            ServicePointManager.FindServicePoint(storageAccount.QueueEndpoint).UseNagleAlgorithm = false;
        }

        public override IBlobStorageProvider BuildBlobStorage()
        {
            return new Azure.BlobStorageProvider(
                BlobClient(),
                DataSerializer,
                Log);
        }

        public override ITableStorageProvider BuildTableStorage()
        {
            return new Azure.TableStorageProvider(
                TableClient(),
                DataSerializer);
        }

        public override IQueueStorageProvider BuildQueueStorage()
        {
            return new Azure.QueueStorageProvider(
                QueueClient(),
                BuildBlobStorage(),
                DataSerializer,
                RuntimeFinalizer,
                Log);
        }

        CloudBlobClient BlobClient()
        {
            var blobClient = _storageAccount.CreateCloudBlobClient();
            blobClient.RetryPolicy = BuildDefaultRetry();
            return blobClient;
        }

        CloudTableClient TableClient()
        {
            var tableClient = _storageAccount.CreateCloudTableClient();
            tableClient.RetryPolicy = BuildDefaultRetry();
            return tableClient;
        }

        CloudQueueClient QueueClient()
        {
            var queueClient = _storageAccount.CreateCloudQueueClient();
            queueClient.RetryPolicy = BuildDefaultRetry();
            return queueClient;
        }

        static RetryPolicy BuildDefaultRetry()
        {
            // [abdullin]: in short this gives us MinBackOff + 2^(10)*Rand.(~0.5.Seconds())
            // at the last retry. Reflect the method for more details
            var deltaBackoff = TimeSpan.FromSeconds(0.5);
            return RetryPolicies.RetryExponential(10, deltaBackoff);
        }
    }
}
