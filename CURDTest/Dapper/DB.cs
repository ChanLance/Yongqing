using DataModels;

namespace Dapper
{
    public class Db : Database<Db>
    {
        public Table<Manager> Manager { get; set; }
    }
}
