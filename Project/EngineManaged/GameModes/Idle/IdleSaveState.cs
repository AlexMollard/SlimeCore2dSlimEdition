using GameModes.Idle;
using MessagePack;
using Microsoft.Data.Sqlite;
using SlimeCore.Source.Common;
using SlimeCore.Source.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SlimeCore.GameModes.Idle;

public static class IdleSaveState
{
    [MessagePackObject]
    public class SaveState
    {
        [Key(0)]
        public int Number { get; set; }
        [Key(1)]
        public string? Message { get; set; }
        [Key(2)]
        public SaveSubState[] IndentedData { get; set; } = [];
    }

    [MessagePackObject]
    public class SaveSubState
    {
        [Key(0)]
        public int Number { get; set; }
        [Key(1)]
        public string? Message { get; set; }
    }

    public class IdleSaveGame : ISaveGame
    {
        public static string SaveRoot => Path
            .Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SlimeCore", "SmellWhatTheRockIsCooking", "SaveData");

        public static void InitSchema(SqliteConnection db)
        {
            using var cmd = db.CreateCommand();
            cmd.CommandText =
                """
                CREATE TABLE IF NOT EXISTS Dwayne (
                  Id TEXT PRIMARY KEY,
                  Data BLOB NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        }

        public static void PreSave<TGameMode>(TGameMode game, string slot) where TGameMode : IGameMode
        {
            Directory.CreateDirectory(SaveRoot);
            string path = Path.Combine(SaveRoot, $"{slot}.save");

            Logger.Info($"SQLite connection string = 'Data Source={path}'");
            using var db = new SqliteConnection($"Data Source={path}");
            db.Open();
            using var transaction = db.BeginTransaction();
            InitSchema(db);
            transaction.Commit();
            db.Close();
        }

        public static void Save(IdleGame game, string slot)
        {
            //Save your game state here
            var content = new SaveState();

            byte[] blob = MessagePackSerializer.Serialize(content);
            string path = Path.Combine(SaveRoot, $"{slot}.save");
            using var db = new SqliteConnection($"Data Source={path}");
            db.Open();
            using var transaction = db.BeginTransaction();
            using var cmd = db.CreateCommand();
            cmd.CommandText = """
                              INSERT INTO Dwayne (Id, Data)
                              VALUES ($id, $data)
                              ON CONFLICT(Id) DO UPDATE SET Data = excluded.Data;
                              """;
            cmd.Parameters.AddWithValue("$id", slot);
            cmd.Parameters.AddWithValue("$data", blob);

            cmd.ExecuteNonQuery();

            transaction.Commit();
            db.Close();
        }

        public static SaveState Load(string slot)
        {
            string path = Path.Combine(SaveRoot, $"{slot}.save");
            using var db = new SqliteConnection($"Data Source={path}");
            db.Open();

            using var cmd = db.CreateCommand();
            cmd.CommandText = """
                              SELECT Data FROM Dwayne
                              WHERE Id = $slot
                              """;
            cmd.Parameters.AddWithValue("$slot", slot);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                throw new InvalidOperationException("Save slot not found, sir.");

            byte[] blob = (byte[])reader["Data"];
            db.Close();

            return MessagePackSerializer.Deserialize<SaveState>(blob);
        }
    }
}




