var builder = DistributedApplication.CreateBuilder(args);
var sql = builder.AddSqlServer("sql");
var sqldb = sql.AddDatabase("Samples");
 
builder.Build().Run();