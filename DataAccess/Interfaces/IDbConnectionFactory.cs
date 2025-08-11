using System.Data;

namespace DataAccess.Interfaces;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
    IDbConnection CreateConnection();
}