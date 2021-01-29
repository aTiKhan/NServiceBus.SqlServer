using System;

namespace NServiceBus.Transport.SqlServer
{
    using System.Data;
#if SYSTEMDATASQLCLIENT
    using System.Data.SqlClient;
#else
    using Microsoft.Data.SqlClient;
#endif
    using System.Threading.Tasks;

    class SubscriptionTableCreator
    {
        QualifiedSubscriptionTableName tableName;
        SqlConnectionFactory connectionFactory;

        public SubscriptionTableCreator(QualifiedSubscriptionTableName tableName, SqlConnectionFactory connectionFactory)
        {
            this.tableName = tableName;
            this.connectionFactory = connectionFactory;
        }
        public async Task CreateIfNecessary()
        {
            using (var connection = await connectionFactory.OpenNewConnection().ConfigureAwait(false))
            {
                try
                {
                    using (var transaction = connection.BeginTransaction())
                    {
#pragma warning disable 618
                        var sql = string.Format(SqlConstants.CreateSubscriptionTableText, tableName.QuotedQualifiedName,
                            tableName.QuotedCatalog);
#pragma warning restore 618
                        using (var command = new SqlCommand(sql, connection, transaction)
                        {
                            CommandType = CommandType.Text
                        })
                        {
                            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }

                        transaction.Commit();
                    }
                }
                catch (SqlException e) when (e.Number == 2714) //Object already exists
                {
                    //Table creation scripts are based on sys.objects metadata views.
                    //It looks that these views are not fully transactional and might
                    //not return information on already created table under heavy load.
                }
            }
        }
    }
}