using System;
using System.Collections.Generic;
using Godot;

namespace Terrarium_Virtuel.scripts.database;

/// <summary>
/// Base interface for all query result types
/// </summary>
public interface DataQuery
{
}

/// <summary>
/// Represents the count of actions performed
/// </summary>
public class ActionCountRow : DataQuery
{
    public string LogAction { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Represents simulation metadata
/// </summary>
public class SimulationRow : DataQuery
{
    public int SimId { get; set; }
    public int SimSeed { get; set; }
    public int SimStartTime { get; set; }
}

/// <summary>
/// Represents population data at a specific tick
/// </summary>
public class PopulationAtTickRow : DataQuery
{
    public int SpeciesId { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Represents population statistics including births and deaths
/// </summary>
public class PopulationRow : DataQuery
{
    public int Tick { get; set; }
    public int SpeciesId { get; set; }
    public int Count { get; set; }
    public int Births { get; set; }
    public int Deaths { get; set; }
}

/// <summary>
/// Contains all database query definitions for creation, insertion, and updates
/// </summary>
public class CreationQueries
{
    /// <summary>
    /// SQL CREATE TABLE statements
    /// </summary>
    public static readonly Dictionary<string, string> CreateTables = new()
    {
        {
            "Simulation",
            @"CREATE TABLE IF NOT EXISTS Simulation (
                SIM_ID INTEGER PRIMARY KEY UNIQUE,
                SIM_SEED INTEGER,
                SIM_START_TIME INTEGER,
                SIM_CONFIG char(255)
            );"
        },
        {
            "Species",
            @"CREATE TABLE IF NOT EXISTS Species (
                SPE_ID INTEGER PRIMARY KEY UNIQUE,
                SPE_PARENT_ID INTEGER,
                SPE_SIM_ID INTEGER,
                SPE_BIRTH_TICK INTEGER,
                FOREIGN KEY (SPE_SIM_ID) REFERENCES Simulation(SIM_ID)
            );"
        },
        {
            "Creatures",
            @"CREATE TABLE IF NOT EXISTS Creatures (
                CRE_ID INTEGER PRIMARY KEY UNIQUE,
                CRE_SIM_ID INTEGER,
                CRE_PARENT_ID INTEGER,
                CRE_SPE_ID INTEGER,
                CRE_BIRTH_TICK INTEGER,
                CRE_DEATH_TICK INTEGER,
                CRE_SPE_FOUNDER INTEGER DEFAULT 0,
                FOREIGN KEY (CRE_SPE_ID) REFERENCES Species(SPE_ID),
                FOREIGN KEY (CRE_SIM_ID) REFERENCES Simulation(SIM_ID)
            );"
        },
        {
            "Logs",
            @"CREATE TABLE IF NOT EXISTS Logs (
                LOG_ID INTEGER PRIMARY KEY UNIQUE,
                LOG_TICK INTEGER,
                LOG_ACTION char(255),
                LOG_DETAILS char(255),
                LOG_ACTOR_ID INTEGER,
                LOG_SIM_ID INTEGER,
                FOREIGN KEY (LOG_ACTOR_ID) REFERENCES Creatures(CRE_ID),
                FOREIGN KEY (LOG_SIM_ID) REFERENCES Simulation(SIM_ID)
            );"
        }
    };

    /// <summary>
    /// SQL INSERT statements mapped by data type
    /// </summary>
    public static readonly Dictionary<Type, string> InsertQueries = new()
    {
        {
            typeof(SimulationEntry),
            "INSERT INTO Simulation (SIM_SEED, SIM_START_TIME, SIM_CONFIG) " +
            "VALUES (@SimSeed, @SimStartTime, @SimConfig) " +
            "RETURNING SIM_ID"
        },
        {
            typeof(SpeciesEntry),
            "INSERT INTO Species (SPE_PARENT_ID, SPE_BIRTH_TICK, SPE_SIM_ID) " +
            "VALUES (@SpeParentId, @SpeBirthTick, @SpeSimId) " +
            "RETURNING SPE_ID"
        },
        {
            typeof(CreatureEntry),
            "INSERT INTO Creatures (CRE_PARENT_ID, CRE_SPE_ID, CRE_BIRTH_TICK, CRE_SIM_ID, CRE_SPE_FOUNDER) " +
            "VALUES (@CreParentId, @CreSpeId, @CreBirthTick, @CreSimId, @CreSpeFounder) " +
            "RETURNING CRE_ID"
        },
        {
            typeof(LogEntry),
            "INSERT INTO Logs (LOG_TICK, LOG_ACTION, LOG_DETAILS, LOG_ACTOR_ID, LOG_SIM_ID) " +
            "VALUES (@LogTick, @LogAction, @LogDetails, @LogActorId, @LogSimId)"
        }
    };

    /// <summary>
    /// SQL UPDATE statements mapped by data type
    /// </summary>
    public static readonly Dictionary<Type, string> UpdateQueries = new()
    {
        {
            typeof(CreatureEntry),
            "UPDATE Creatures SET CRE_DEATH_TICK = @CreDeathTick WHERE CRE_ID = @CreId"
        }
    };

    /// <summary>
    /// Base interface for all data objects persisted to the database
    /// </summary>
    public interface IDataObject { }

    /// <summary>
    /// Represents a simulation entry in the database
    /// </summary>
    public class SimulationEntry : IDataObject
    {
        public int SimId { get; set; }
        public int SimSeed { get; set; }
        public int SimStartTime { get; set; }
        public string SimConfig { get; set; }
    }

    /// <summary>
    /// Represents a species entry in the database
    /// </summary>
    public class SpeciesEntry : IDataObject
    {
        public int SpeId { get; set; }
        public int SpeParentId { get; set; }
        public int SpeBirthTick { get; set; }
        public int SpeSimId { get; set; }
    }

    /// <summary>
    /// Represents a creature entry in the database
    /// </summary>
    public class CreatureEntry : IDataObject
    {
        public int CreId { get; set; }
        public int CreParentId { get; set; }
        public int CreSpeId { get; set; }
        public int CreBirthTick { get; set; }
        public int CreDeathTick { get; set; }
        public bool CreSpeFounder { get; set; }
        public int CreSimId { get; set; }
    }

    /// <summary>
    /// Represents a log entry in the database
    /// </summary>
    public class LogEntry : IDataObject
    {
        public long LogId { get; set; }
        public long LogTick { get; set; }
        public string LogAction { get; set; }
        public string LogDetails { get; set; }
        public int LogActorId { get; set; }
        public int LogSimId { get; set; }
    }

    /// <summary>
    /// Represents a database save or update request
    /// </summary>
    public class SaveRequest
    {
        public IDataObject Data { get; set; }
        public Callable OnSaved { get; set; }
        public bool IsUpdate { get; set; } = false;
        public string UpdateSql { get; set; }
    }
    
    public class SpeciesRow : DataQuery
    {
        public int SpeId { get; set; }
    }
}