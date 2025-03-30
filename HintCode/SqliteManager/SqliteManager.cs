using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SqliteManager
{
    public class SqliteManager : ISqliteManager
    {
        private readonly string _connectionString;

        public SqliteManager(string connectionString)
        {
            _connectionString = connectionString;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string createTableQuery = "CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public void AddUser(string name, int age)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string insertDataQuery = "INSERT INTO users (name, age) VALUES (@name, @age)";
                using (var command = new SQLiteCommand(insertDataQuery, connection))
                {
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@age", age);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateUser(int id, string name, int age)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string updateDataQuery = "UPDATE users SET name = @name, age = @age WHERE id = @id";
                using (var command = new SQLiteCommand(updateDataQuery, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@age", age);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteUser(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string deleteDataQuery = "DELETE FROM users WHERE id = @id";
                using (var command = new SQLiteCommand(deleteDataQuery, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<User> QueryUsers()
        {
            var users = new List<User>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT * FROM users";
                using (var command = new SQLiteCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Name = reader["name"].ToString(),
                            Age = Convert.ToInt32(reader["age"])
                        });
                    }
                }
            }
            return users;
        }
    }
}
