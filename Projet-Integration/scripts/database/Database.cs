using System;
using System.Collections.Generic;
using Godot;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using FileAccess = Godot.FileAccess;
using Dapper;
using Terrarium_Virtuel.signals.database;

namespace Terrarium_Virtuel.scripts.database;

/// <summary>
/// The class that does the reads and writes only.
/// </summary>
public class Database
{
	private BlockingCollection<CreationQueries.SaveRequest> _queue = new();
	
	public string GlobalDatabasePath { get; set; }
	

	public Database()
	{
		
	}
	
	public void InitializeDatabase()
	{
		if (GlobalDatabasePath == null)
		{
			GlobalDatabasePath = ProjectSettings.GlobalizePath("res://_persistent/db.sqlite");
		}
		
		var dataDir = Path.GetDirectoryName(GlobalDatabasePath);
		
		if (!Directory.Exists(dataDir))
		{
			Directory.CreateDirectory(dataDir);
		}
		
		using var connection = new SqliteConnection($"Data Source={GlobalDatabasePath}");
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = "PRAGMA journal_mode=WAL";
		command.ExecuteNonQuery();

		foreach (var table in CreationQueries.CreateTables.Keys)
		{
			command.CommandText = CreationQueries.CreateTables[table];
			command.ExecuteNonQuery();
		}
	}


	/// <summary>
	/// The method that will be run asynchronously in a different thread.
	/// </summary>
	public void LoopThroughQueue(CancellationToken ct)
	{
		using var connection = new SqliteConnection($"Data Source={GlobalDatabasePath}");
		connection.Open();

		using (var command = connection.CreateCommand())
		{
			command.CommandText = "PRAGMA journal_mode=WAL";
			command.ExecuteNonQuery();
		}

		while (!ct.IsCancellationRequested)
		{
			try
			{
				if (_queue.TryTake(out CreationQueries.SaveRequest request, 100))
				{
					var batch = new List<CreationQueries.SaveRequest> { request };
					while (_queue.TryTake(out CreationQueries.SaveRequest next))
						batch.Add(next);
					
					ExecuteBatch(connection, batch);
				}
			}
			catch (ThreadInterruptedException)
			{
				break;
			}
		}
		using (var command = connection.CreateCommand())
		{
			command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
			command.ExecuteNonQuery();
		}
		
	}

	private void ExecuteBatch(SqliteConnection connection, List<CreationQueries.SaveRequest> batch)
	{
		// begins the transaction
		using var transaction = connection.BeginTransaction();
		try
		{
			foreach (var request in batch)
			{
				if (request.IsUpdate)
				{
					if (CreationQueries.UpdateQueries.TryGetValue(request.Data.GetType(), out var updateSql))
						connection.Execute(updateSql, request.Data, transaction);
					else
						GD.PushError("[Database - ExecuteBatch] No update query for " + request.Data.GetType().Name);
					continue;
				}
				
				var type = request.Data.GetType();
				if (CreationQueries.InsertQueries.TryGetValue(type, out var query))
				{
					var data = request.Data;
					// GD.Print($"[Database --- ExecuteBatch] Data Type: {data.GetType().Name}");

					if (request.Data is CreationQueries.LogEntry)
					{
						connection.Execute(query, request.Data, transaction);
					}
					else
					{
						int generatedId = connection.QuerySingle<int>(query, request.Data, transaction);
					
						if (data is CreationQueries.SimulationEntry sim) sim.SimId = generatedId;
						else if (data is CreationQueries.SpeciesEntry spe) spe.SpeId = generatedId;
						else if (data is CreationQueries.CreatureEntry cre) cre.CreId = generatedId;

						if (!String.IsNullOrEmpty(request.OnSaved.Method))
							request.OnSaved.CallDeferred(generatedId);
					}
				}
				else
				{
					GD.PushError("[Database - ExecuteBatch] Query for " + type.Name + " not implemented.");
				}
			}

			// commits the changes and ends the transaction
			transaction.Commit();
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex);
			transaction.Rollback();
		}
	}
	
	public void Save(CreationQueries.IDataObject data, Callable? onSaved = null)
	{
		_queue.Add(new CreationQueries.SaveRequest
		{
			Data = data,
			OnSaved = onSaved?? default
		});
		Controller.Instance.Emit(new UpdateGraphSignal());
	}

	public void Update(CreationQueries.IDataObject data)
	{
		_queue.Add(new CreationQueries.SaveRequest
		{
			Data = data,
			IsUpdate = true
		});
		Controller.Instance.Emit(new UpdateGraphSignal());
	}
	
	public IEnumerable<T> Query<T>(string sql, object parameters = null)
	{
		using var connection = new SqliteConnection($"Data Source={GlobalDatabasePath}");
		connection.Open();
		return connection.Query<T>(sql, parameters).ToList();
	}
}
