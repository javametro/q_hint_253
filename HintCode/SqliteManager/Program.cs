using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteManager
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SqliteConnectionString"].ConnectionString;
            ISqliteManager sqliteManager = new SqliteManager(connectionString);

            // Add users
            sqliteManager.AddUser("Alice", 30);
            sqliteManager.AddUser("Bob", 25);

            // Update a user
            sqliteManager.UpdateUser(1, "Alice", 31);

            // Delete a user
            sqliteManager.DeleteUser(2);

            // Query users
            var users = sqliteManager.QueryUsers();
            foreach (var user in users)
            {
                Console.WriteLine($"ID: {user.Id}, Name: {user.Name}, Age: {user.Age}");
            }
        }
    }
}
