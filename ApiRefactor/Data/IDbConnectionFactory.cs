using System.Data;

namespace ApiRefactor.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
