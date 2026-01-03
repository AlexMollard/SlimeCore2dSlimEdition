using EngineManaged.Numeric;
using Microsoft.Data.Sqlite;
using SlimeCore.Source.Common;
using SlimeCore.Source.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SlimeCore.GameModes.Factory.Systems;

public class FactorySaveGame : ISaveGame
{
    public static string SaveRoot => Path
       .Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
           "SlimeCore", "PissAnt", "SaveData");

    public static void InitSchema(SqliteConnection db)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS World (
          Id TEXT PRIMARY KEY,
          Data BLOB NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Actors (
          Id TEXT PRIMARY KEY,
          X INTEGER,
          Y INTEGER,
          State BLOB NOT NULL
        );
        """;
        cmd.ExecuteNonQuery();
    }

    public static void PreSave<TGameMode>(TGameMode game, string slot)
    where TGameMode : IGameMode
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

    public static void SaveWorld<TGameMode>(TGameMode game, string slot, Guid worldId, byte[] data)
        where TGameMode : IGameMode
    {
        Directory.CreateDirectory(SaveRoot);
        string path = Path.Combine(SaveRoot, $"{slot}.save");

        using var db = new SqliteConnection($"Data Source={path}");
        db.Open();
        using var transaction = db.BeginTransaction();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
                          INSERT INTO World (Id, Data)
                          VALUES ($id, $data)
                          ON CONFLICT(Id) DO UPDATE SET Data = excluded.Data;
                          """;
        cmd.Parameters.AddWithValue("$id", worldId.ToString());
        cmd.Parameters.AddWithValue("$data", data);
        cmd.ExecuteNonQuery();

        transaction.Commit();
        db.Close();
    }

    public static void SaveActors<TGameMode>(TGameMode game, string slot, params (Guid id, Vec2i position, byte[] actor)[] actors)
    where TGameMode : IGameMode
    {
        Directory.CreateDirectory(SaveRoot);
        string path = Path.Combine(SaveRoot, $"{slot}.save");

        using var db = new SqliteConnection($"Data Source={path}");
        db.Open();
        using var transaction = db.BeginTransaction();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
                          INSERT INTO Actors (Id, X, Y, State)
                          VALUES ($id, $x, $y, $state)
                          ON CONFLICT(Id) DO UPDATE SET
                              X = excluded.X,
                              Y = excluded.Y,
                              State = excluded.State;
                          """;

        foreach (var a in actors)
        {
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("$id", a.id);
            cmd.Parameters.AddWithValue("$x", a.position.X);
            cmd.Parameters.AddWithValue("$y", a.position.Y);
            cmd.Parameters.AddWithValue("$state", a.actor);
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
        db.Close();
    }
}

