using System;
using System.Linq.Expressions;
using System.Reflection;
using Lagrange.Core.Message;
using static Lagrange.Core.Message.MessageChain;

namespace Lagrange.OneBot.DatabaseShift.Utilities;

public delegate MessageChain OldMessageChainGroupCtor(uint groupUin, uint friendUin, uint sequence, ulong messageId);

public delegate MessageChain OldMessageChainFriendCtor(uint friendUin, string selfUid, string friendUid, uint targetUin, uint sequence, uint clientSequence, ulong? messageId, MessageType type);

public delegate void OldMessageChainTimeSetter(MessageChain @this, DateTime value);

public static class MessageChainUtility {
    public static readonly OldMessageChainGroupCtor MessageChainGroupCtor;

    public static readonly OldMessageChainFriendCtor MessageChainFriendCtor;

    public static readonly OldMessageChainTimeSetter MessageChainTimeSetter;

    static MessageChainUtility() {
        { // Create Group Message Chain
            ConstructorInfo ctor = typeof(MessageChain).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                [
                    typeof(uint),   // groupUin
                    typeof(uint),   // friendUin
                    typeof(uint),   // sequence
                    typeof(ulong)   // messageId
                ]
            ) ?? throw new Exception();

            ParameterExpression[] parameters = [
                Expression.Parameter(typeof(uint), "groupUin"),
                Expression.Parameter(typeof(uint), "friendUin"),
                Expression.Parameter(typeof(uint), "sequence"),
                Expression.Parameter(typeof(ulong), "messageId")
            ];

            MessageChainGroupCtor = Expression.Lambda<OldMessageChainGroupCtor>(
                Expression.New(ctor, parameters),
                parameters
            ).Compile();
        }

        { // Create Friend Message Chain
            ConstructorInfo ctor = typeof(MessageChain).GetConstructor(
                BindingFlags.NonPublic,
                [
                    typeof(uint),       // friendUin
                    typeof(string),     // selfUid
                    typeof(string),     // friendUid
                    typeof(uint),       // targetUin
                    typeof(uint),       // sequence
                    typeof(uint),       // clientSequence
                    typeof(ulong?),     // messageId
                    typeof(MessageChain) // type
                ]
            ) ?? throw new Exception();

            ParameterExpression[] parameters = [
                Expression.Parameter(typeof(uint), "friendUin"),
                Expression.Parameter(typeof(string), "selfUid"),
                Expression.Parameter(typeof(string), "friendUid"),
                Expression.Parameter(typeof(uint), "targetUin"),
                Expression.Parameter(typeof(uint), "sequence"),
                Expression.Parameter(typeof(uint), "clientSequence"),
                Expression.Parameter(typeof(ulong?), "messageId"),
                Expression.Parameter(typeof(MessageChain), "type")
            ];

            MessageChainFriendCtor = Expression.Lambda<OldMessageChainFriendCtor>(
                Expression.New(ctor, parameters),
                parameters
            ).Compile();
        }

        { // Time Setter
            MethodInfo? method = typeof(MessageChain).GetProperty("Time")?.GetSetMethod() ?? throw new Exception();
            MessageChainTimeSetter = (OldMessageChainTimeSetter)Delegate.CreateDelegate(
                typeof(OldMessageChainTimeSetter),
                method
            );
        }
    }
}
