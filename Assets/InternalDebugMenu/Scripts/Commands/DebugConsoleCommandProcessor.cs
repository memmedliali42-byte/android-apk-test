using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace InternalDebugMenu
{
    public readonly struct DebugCommandExecutionResult
    {
        public DebugCommandExecutionResult(DebugAuditSeverity severity, string message)
        {
            Severity = severity;
            Message = message ?? string.Empty;
        }

        public DebugAuditSeverity Severity { get; }
        public string Message { get; }
    }

    internal interface IDebugConsoleCommand
    {
        string Keyword { get; }
        string HelpText { get; }
        DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args);
    }

    public sealed class DebugConsoleCommandProcessor
    {
        private readonly DebugManager manager;
        private readonly Dictionary<string, IDebugConsoleCommand> commands = new Dictionary<string, IDebugConsoleCommand>(StringComparer.OrdinalIgnoreCase);

        public DebugConsoleCommandProcessor(DebugManager manager)
        {
            this.manager = manager;

            Register(new HelpCommand(() => commands.Values));
            Register(new GodCommand());
            Register(new SpeedCommand());
            Register(new HealCommand());
            Register(new SpawnCommand());
            Register(new AmmoCommand());
            Register(new ReloadCommand());
            Register(new AiCommand());
            Register(new DifficultyCommand());
            Register(new HitboxesCommand());
            Register(new RaycastsCommand());
            Register(new OutlineCommand());
            Register(new LatencyCommand());
            Register(new PacketLossCommand());
            Register(new SyncLogsCommand());
            Register(new SafeModeCommand());
        }

        public DebugCommandExecutionResult Execute(string rawCommand)
        {
            if (string.IsNullOrWhiteSpace(rawCommand))
            {
                return new DebugCommandExecutionResult(DebugAuditSeverity.Warning, "Command is empty.");
            }

            var tokens = Tokenize(rawCommand);
            if (tokens.Count == 0)
            {
                return new DebugCommandExecutionResult(DebugAuditSeverity.Warning, "Command is empty.");
            }

            if (!commands.TryGetValue(tokens[0], out var command))
            {
                return new DebugCommandExecutionResult(DebugAuditSeverity.Warning, $"Unknown command '{tokens[0]}'. Try 'help'.");
            }

            try
            {
                return command.Execute(manager, tokens);
            }
            catch (Exception exception)
            {
                return new DebugCommandExecutionResult(DebugAuditSeverity.Error, $"Command failed: {exception.Message}");
            }
        }

        private void Register(IDebugConsoleCommand command)
        {
            commands[command.Keyword] = command;
        }

        private static List<string> Tokenize(string value)
        {
            var tokens = new List<string>();
            var builder = new StringBuilder();
            var inQuotes = false;

            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];

                if (current == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(current) && !inQuotes)
                {
                    if (builder.Length > 0)
                    {
                        tokens.Add(builder.ToString());
                        builder.Length = 0;
                    }

                    continue;
                }

                builder.Append(current);
            }

            if (builder.Length > 0)
            {
                tokens.Add(builder.ToString());
            }

            return tokens;
        }
    }

    internal abstract class DebugConsoleCommandBase : IDebugConsoleCommand
    {
        public abstract string Keyword { get; }
        public abstract string HelpText { get; }
        public abstract DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args);

        protected static bool TryParseToggle(IReadOnlyList<string> args, int index, bool defaultValue, out bool value)
        {
            value = defaultValue;

            if (args.Count <= index)
            {
                return true;
            }

            switch (args[index].ToLowerInvariant())
            {
                case "1":
                case "on":
                case "true":
                case "enable":
                case "enabled":
                    value = true;
                    return true;
                case "0":
                case "off":
                case "false":
                case "disable":
                case "disabled":
                    value = false;
                    return true;
                default:
                    return false;
            }
        }

        protected static bool TryParseFloat(IReadOnlyList<string> args, int index, out float value)
        {
            value = 0.0f;
            return args.Count > index && float.TryParse(args[index], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        protected static bool TryParseInt(IReadOnlyList<string> args, int index, out int value)
        {
            value = 0;
            return args.Count > index && int.TryParse(args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        protected static DebugCommandExecutionResult Success(string message)
        {
            return new DebugCommandExecutionResult(DebugAuditSeverity.Info, message);
        }

        protected static DebugCommandExecutionResult Failure(string message)
        {
            return new DebugCommandExecutionResult(DebugAuditSeverity.Warning, message);
        }
    }

    internal sealed class HelpCommand : DebugConsoleCommandBase
    {
        private readonly Func<IEnumerable<IDebugConsoleCommand>> getCommands;

        public HelpCommand(Func<IEnumerable<IDebugConsoleCommand>> getCommands)
        {
            this.getCommands = getCommands;
        }

        public override string Keyword => "help";
        public override string HelpText => "help";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            var builder = new StringBuilder("Commands:");
            foreach (var command in getCommands())
            {
                builder.AppendLine();
                builder.Append(command.HelpText);
            }

            return Success(builder.ToString());
        }
    }

    internal sealed class GodCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "god";
        public override string HelpText => "god [on|off]";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (!TryParseToggle(args, 1, true, out var enabled))
            {
                return Failure("Usage: god [on|off]");
            }

            return manager.SetGodMode(enabled)
                ? Success($"God Mode {(enabled ? "enabled" : "disabled")}.")
                : Failure("God Mode request denied.");
        }
    }

    internal sealed class SpeedCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "speed";
        public override string HelpText => "speed <multiplier>";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (!TryParseFloat(args, 1, out var multiplier))
            {
                return Failure("Usage: speed <multiplier>");
            }

            return manager.SetMovementSpeedMultiplier(multiplier)
                ? Success($"Movement speed set to {manager.MovementSpeedMultiplier:0.00}.")
                : Failure("Speed change denied.");
        }
    }

    internal sealed class HealCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "heal";
        public override string HelpText => "heal";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            return manager.HealPlayer()
                ? Success("Player healed to full.")
                : Failure("Heal request denied.");
        }
    }

    internal sealed class SpawnCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "spawn";
        public override string HelpText => "spawn enemy <count> | spawn weapon <weaponId>";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (args.Count < 3)
            {
                return Failure("Usage: spawn enemy <count> | spawn weapon <weaponId>");
            }

            switch (args[1].ToLowerInvariant())
            {
                case "enemy":
                    if (!TryParseInt(args, 2, out var count))
                    {
                        return Failure("Usage: spawn enemy <count>");
                    }

                    return manager.TrySpawnEnemies(count, out var enemyMessage)
                        ? Success(enemyMessage)
                        : Failure(enemyMessage);

                case "weapon":
                    return manager.TrySpawnWeapon(args[2], out var weaponMessage)
                        ? Success(weaponMessage)
                        : Failure(weaponMessage);

                default:
                    return Failure("Usage: spawn enemy <count> | spawn weapon <weaponId>");
            }
        }
    }

    internal sealed class AmmoCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "ammo";
        public override string HelpText => "ammo [on|off]";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (!TryParseToggle(args, 1, true, out var enabled))
            {
                return Failure("Usage: ammo [on|off]");
            }

            return manager.SetInfiniteAmmo(enabled)
                ? Success($"Infinite ammo {(enabled ? "enabled" : "disabled")}.")
                : Failure("Infinite ammo request denied.");
        }
    }

    internal sealed class ReloadCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "reload";
        public override string HelpText => "reload [on|off]";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (!TryParseToggle(args, 1, true, out var enabled))
            {
                return Failure("Usage: reload [on|off]");
            }

            return manager.SetInstantReload(enabled)
                ? Success($"Instant reload {(enabled ? "enabled" : "disabled")}.")
                : Failure("Instant reload request denied.");
        }
    }

    internal sealed class AiCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "ai";
        public override string HelpText => "ai freeze [on|off]";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (args.Count < 2 || !string.Equals(args[1], "freeze", StringComparison.OrdinalIgnoreCase))
            {
                return Failure("Usage: ai freeze [on|off]");
            }

            if (!TryParseToggle(args, 2, true, out var enabled))
            {
                return Failure("Usage: ai freeze [on|off]");
            }

            return manager.SetAiFrozen(enabled)
                ? Success($"AI freeze {(enabled ? "enabled" : "disabled")}.")
                : Failure("AI freeze request denied.");
        }
    }

    internal sealed class DifficultyCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "difficulty";
        public override string HelpText => "difficulty <multiplier>";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (!TryParseFloat(args, 1, out var multiplier))
            {
                return Failure("Usage: difficulty <multiplier>");
            }

            return manager.SetDifficulty(multiplier)
                ? Success($"AI difficulty set to {manager.DifficultyMultiplier:0.00}.")
                : Failure("Difficulty request denied.");
        }
    }

    internal sealed class HitboxesCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "hitboxes";
        public override string HelpText => "hitboxes [on|off]";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (!TryParseToggle(args, 1, true, out var enabled))
            {
                return Failure("Usage: hitboxes [on|off]");
            }

            return manager.SetHitboxesVisible(enabled)
                ? Success($"Hitboxes {(enabled ? "visible" : "hidden")}.")
                : Failure("Hitbox toggle denied.");
        }
    }

    internal sealed class RaycastsCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "raycasts";
        public override string HelpText => "raycasts [on|off]";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (!TryParseToggle(args, 1, true, out var enabled))
            {
                return Failure("Usage: raycasts [on|off]");
            }

            return manager.SetRaycastsVisible(enabled)
                ? Success($"Raycasts {(enabled ? "visible" : "hidden")}.")
                : Failure("Raycast toggle denied.");
        }
    }

    internal sealed class OutlineCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "outline";
        public override string HelpText => "outline [on|off]";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (!TryParseToggle(args, 1, true, out var enabled))
            {
                return Failure("Usage: outline [on|off]");
            }

            return manager.SetEnemyOutlineVisible(enabled)
                ? Success($"Enemy outline {(enabled ? "enabled" : "disabled")}.")
                : Failure("Outline toggle denied.");
        }
    }

    internal sealed class LatencyCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "latency";
        public override string HelpText => "latency <milliseconds>";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (!TryParseInt(args, 1, out var latencyMs))
            {
                return Failure("Usage: latency <milliseconds>");
            }

            return manager.SetLatencyMs(latencyMs)
                ? Success($"Latency simulation set to {manager.LatencyMs} ms.")
                : Failure("Latency request denied.");
        }
    }

    internal sealed class PacketLossCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "packetloss";
        public override string HelpText => "packetloss <percent>";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (!TryParseInt(args, 1, out var packetLoss))
            {
                return Failure("Usage: packetloss <percent>");
            }

            return manager.SetPacketLossPercent(packetLoss)
                ? Success($"Packet loss simulation set to {manager.PacketLossPercent}%.")
                : Failure("Packet loss request denied.");
        }
    }

    internal sealed class SyncLogsCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "synclogs";
        public override string HelpText => "synclogs [on|off]";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (!TryParseToggle(args, 1, true, out var enabled))
            {
                return Failure("Usage: synclogs [on|off]");
            }

            return manager.SetSyncDebugLogs(enabled)
                ? Success($"Sync debug logs {(enabled ? "enabled" : "disabled")}.")
                : Failure("Sync logs request denied.");
        }
    }

    internal sealed class SafeModeCommand : DebugConsoleCommandBase
    {
        public override string Keyword => "safe";
        public override string HelpText => "safe [on|off]";

        public override DebugCommandExecutionResult Execute(DebugManager manager, IReadOnlyList<string> args)
        {
            if (!TryParseToggle(args, 1, true, out var enabled))
            {
                return Failure("Usage: safe [on|off]");
            }

            return manager.SetSafeMode(enabled)
                ? Success($"SAFE MODE {(enabled ? "enabled" : "disabled")}.")
                : Failure("SAFE MODE request denied.");
        }
    }
}
