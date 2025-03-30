using System.Collections.Generic;

namespace SqliteManager
{
    public interface ISqliteManager
    {
        void AddUser(string name, int age);
        void UpdateUser(int id, string name, int age);
        void DeleteUser(int id);
        List<User> QueryUsers();
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
