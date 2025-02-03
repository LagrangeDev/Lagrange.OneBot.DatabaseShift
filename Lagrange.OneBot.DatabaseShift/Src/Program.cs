using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Reflection;
using System.Text;
using Lagrange.OneBot.Database;
using Lagrange.OneBot.DatabaseShift.Exceptions;
using Lagrange.OneBot.DatabaseShift.Extensions;
using Lagrange.OneBot.Realms;
using Lagrange.OneBot.Utility;
using LiteDB;
using Realms;

namespace Lagrange.OneBot.DatabaseShift;

internal class Program {
    private static readonly Option<string> _IN = new("--in", "LiteDB database file path") {
        IsRequired = true
    };

    private static readonly Option<string> _OUT = new("--out", "Realm database file directory");

    private static readonly Option<string> _SUMMARY = new("--summary", "Summary file path");

    private static void Main(string[] args) => new RootCommand("Lagrange.OneBot database shift")
        .Option(_IN)
        .Option(_OUT)
        .Handler(CliMain)
        .Invoke(args);

    private static void CliMain(InvocationContext context) {
        string version = Assembly.GetAssembly(typeof(Program))?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? "Unknown";
        Console.WriteLine("Lagrange.OneBot.DatabaseShift Version: {0}", version);

        ParseResult result = context.ParseResult;
        if (result.Errors.Count > 0) {
            foreach (ParseError error in result.Errors) {
                Console.WriteLine(error.Message);
            }
        }

        string @in = Path.GetFullPath(result.GetValueForOption(_IN)
            ?? throw new Exception("Option '--in' is required."));
        string @out = result.GetValueForOption(_OUT) ??
            Path.Join(Path.GetDirectoryName(@in), Path.GetFileName(@in).Replace('.', '-'));
        string summary = Path.GetFullPath(result.GetValueForOption(_SUMMARY) ?? "./summary.txt");
        Console.WriteLine("IN: {0}", @in);
        Console.WriteLine("OUT: {0}", @out);
        Console.WriteLine("SUMMARY: {0}", summary);

        if (!File.Exists(@in)) throw new FileNotFoundException($"LiteDB database file({@in}) not found!");
        if (Directory.Exists(@out)) throw new DirectoryExistedException($"Realm database file directory existed!");
        if (File.Exists(summary)) throw new FileExistedException($"Summary file existed!");

        List<(int, Exception)> failed = [];
        DateTimeOffset start = DateTimeOffset.Now;
        (long total, long succeed, long failed, int percentage) statistical = (
            total: 0,
            succeed: 0,
            failed: 0,
            percentage: -1
        );
        using (LiteDatabase liteDB = InitializeLiteDB(@in)) {
            using Realm realm = InitializeRealm(@out);

            ILiteCollection<BsonDocument> collection = liteDB.GetCollection("MessageRecord");
            statistical.total = collection.LongCount();

            foreach (BsonDocument bson in collection.FindAll()) {
                try {
                    realm.Write(() => realm.Add<RealmMessageRecord>(BsonMapper.Global.Deserialize<MessageRecord>(bson)));
                    statistical.succeed++;
                } catch (Exception e) {
                    int id = bson["_id"].AsInt32;
                    failed.Add((id, e));
                    statistical.failed++;

                    Console.Write(new StringBuilder().Append('\r').Append(' ', Console.CursorLeft));
                    Console.WriteLine("\r_id: {0} | exception: {1} {2}", id, e.GetType(), e.Message);

                    continue;
                }

                int percentage = (int)((statistical.succeed + statistical.failed) * 10000 / statistical.total);
                if (percentage != statistical.percentage || statistical.failed != failed.Count) {
                    statistical.percentage = percentage;
                    statistical.failed = failed.Count;

                    Console.Write(new StringBuilder().Append('\r').Append(' ', Console.CursorLeft));
                    Console.Write(new StringBuilder().Append("\rfailed: ")
                        .Append(failed.Count)
                        .Append(" percentage: ")
                        .Append(statistical.percentage / 100)
                        .Append('.')
                        .AppendFormat("{0:D2}", statistical.percentage % 100)
                        .Append('%'));
                }
            }
        }

        Console.Write(new StringBuilder().Append('\r').Append(' ', Console.BufferWidth));
        Console.WriteLine("\rWrite Summary...");
        StringBuilder failedBuilder = new();
        foreach ((int id, Exception e) in failed) {
            failedBuilder.Append("id: ").Append(id).Append(" exception: ").Append(e).AppendLine();
        }
        StringBuilder summaryBuilder = new();
        summaryBuilder.Append("============ summary ============\n")
            .Append("Total: ")
            .Append(statistical.total)
            .Append(" Succeed: ")
            .Append(statistical.succeed)
            .Append(" Failed: ")
            .Append(statistical.failed)
            .AppendLine()
            .Append("Totle Time: ")
            .Append(DateTimeOffset.Now - start)
            .AppendLine();
        try {
            using StreamWriter writer = new(summary, false, Encoding.UTF8);
            writer.WriteLine(failedBuilder);
            writer.WriteLine(summaryBuilder);
        } catch (Exception e) {
            Console.WriteLine(e);
        }

        Console.WriteLine(summaryBuilder);
    }

    private static LiteDatabase InitializeLiteDB(string path) {
        BsonMapper.Global.TrimWhitespace = false;
        BsonMapper.Global.EmptyStringToNull = false;

        BsonMapper.Global.RegisterType(
            LiteDbUtility.IMessageEntitySerialize,
            LiteDbUtility.IMessageEntityDeserialize
        );

        var db = new LiteDatabase(path) {
            CheckpointSize = 50
        };

        return db;
    }

    private static Realm InitializeRealm(string path) {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return Realm.GetInstance(new RealmConfiguration($"{path}/.realm"));
    }
}