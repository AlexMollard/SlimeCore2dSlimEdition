using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Source.Core;

public interface ISaveGame
{
    /// <summary>
    /// Location of the save game files.
    /// </summary>
    static abstract string SaveRoot { get; }

    /// <summary>
    /// Schema for the save game database.
    /// </summary>
    static abstract void InitSchema(SqliteConnection db);
    /// <summary>
    /// init the schema and set up any other on disk requirements for saving/loading
    /// </summary>
    public abstract static void PreSave<TGameMode>(TGameMode game, string slot)
        where TGameMode : IGameMode;
}

