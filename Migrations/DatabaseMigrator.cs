using System.Reflection;
using DbUp;
using DbUp.Engine;

namespace InstallmentBusiness.Api.Migrations;

// Applies any embedded SQL migration scripts (Migrations/Scripts/*.sql) that
// haven't already run against the target database. DbUp tracks what's been
// applied in its own table (SchemaVersions by default) -- once a script is
// recorded there, DbUp never runs it again, regardless of the script's own
// content. The IF NOT EXISTS guards inside the scripts themselves are a
// second, independent safety net for the one-time situation of pointing
// this at a database that already received some of these scripts by hand,
// before DbUp existed -- see BaselineExistingDatabase.sql.
//
// Called once at startup, before the app accepts any requests. If a script
// fails, this throws and the app does not start, rather than run against a
// possibly half-migrated database.
public static class DatabaseMigrator
{
    public static void Migrate(string connectionString)
    {
        // Creates the target database itself if it doesn't exist yet (e.g.
        // a brand-new client site pointed at a connection string whose
        // database has never been created). Requires the connecting
        // credentials to have permission to create databases (e.g. the
        // dbcreator server role) -- a higher privilege than just being
        // db_owner on an already-existing database. No-ops instantly if the
        // database already exists.
        EnsureDatabase.For.SqlDatabase(connectionString);

        var assembly = Assembly.GetExecutingAssembly();
        const string marker = "Migrations.Scripts.";

        var scripts = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(marker) && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                var contents = reader.ReadToEnd();

                // The bare filename (e.g. "0001_....sql"), not the full dotted
                // resource path, becomes this script's identifying name --
                // this is exactly the string that gets written to
                // SchemaVersions.ScriptName, and what the baseline script
                // needs to match exactly if it's ever needed.
                var idx = name.IndexOf(marker, StringComparison.Ordinal);
                var shortName = idx >= 0 ? name[(idx + marker.Length)..] : name;

                return new SqlScript(shortName, contents);
            })
            .ToList();

        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScripts(scripts)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw new InvalidOperationException(
                $"Database migration failed on script '{result.ErrorScript?.Name}'. " +
                "The application will not start against a possibly incomplete schema. " +
                "See the console output above for the exact SQL error.",
                result.Error);
        }
    }
}
