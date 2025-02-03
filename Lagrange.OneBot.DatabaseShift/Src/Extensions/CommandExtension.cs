using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Lagrange.OneBot.DatabaseShift.Extensions;

public static class CommandExtension {
    public static T Option<T>(this T command, Option option) where T : Command {
        command.AddOption(option);
        return command;
    }

    public static T Handler<T>(this T command, Action<InvocationContext> action) where T : Command {
        command.SetHandler(action);
        return command;
    }
}