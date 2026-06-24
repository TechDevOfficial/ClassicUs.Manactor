using System;
using System.Collections.Generic;
using System.Reflection;
using Hazel;

namespace ClassicUs.Manactor
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ManactorRpcAttribute : Attribute
    {
        public byte? CallId { get; }
        public string Key { get; }

        public ManactorRpcAttribute(byte callId)
        {
            CallId = callId;
        }

        public ManactorRpcAttribute(string key)
        {
            Key = key;
            RpcIdAllocator.Reserve(key);
        }
    }

    public static class ManactorRpc
    {
        private static readonly List<Action> _pendingRegistrations = new();
        private static bool _flushed;

        public static void RegisterMethods(object target)
        {
            if (target == null) return;
            var type = target.GetType();

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = method.GetCustomAttribute<ManactorRpcAttribute>();
                if (attr == null) continue;

                var parameters = method.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(byte))
                {
                    ManactorPlugin.Log.LogError($"[ManactorRpc] {type.Name}.{method.Name} needs a leading byte senderId parameter, skipping.");
                    continue;
                }

                var instance = method.IsStatic ? null : target;
                var capturedMethod = method;
                var capturedParameters = parameters;
                var capturedAttr = attr;

                void Register()
                {
                    byte callId = capturedAttr.CallId ?? RpcIdAllocator.GetId(capturedAttr.Key);
                    NetworkManager.RegisterHandler(callId, (senderId, reader) =>
                    {
                        var args = new object[capturedParameters.Length];
                        args[0] = senderId;
                        for (int i = 1; i < capturedParameters.Length; i++)
                            args[i] = ReadValue(reader, capturedParameters[i].ParameterType);

                        capturedMethod.Invoke(instance, args);
                    });
                }

                if (capturedAttr.CallId.HasValue) Register();
                else _pendingRegistrations.Add(Register);
            }
        }

        public static void EnsureFlushed()
        {
            if (_flushed) return;
            _flushed = true;

            foreach (var register in _pendingRegistrations) register();
            _pendingRegistrations.Clear();
        }

        public static void Send(byte callId, params object[] args)
        {
            NetworkManager.SendRpc(callId, w =>
            {
                foreach (var arg in args)
                    WriteValue(w, arg);
            });
        }

        public static void Send(string key, params object[] args)
        {
            EnsureFlushed();
            Send(RpcIdAllocator.GetId(key), args);
        }

        private static object ReadValue(MessageReader reader, Type type)
        {
            if (type == typeof(bool)) return reader.ReadBoolean();
            if (type == typeof(byte)) return reader.ReadByte();
            if (type == typeof(int)) return reader.ReadInt32();
            if (type == typeof(float)) return reader.ReadSingle();
            if (type == typeof(string)) return reader.ReadString();
            throw new NotSupportedException($"[ManactorRpc] Unsupported parameter type: {type}");
        }

        private static void WriteValue(MessageWriter writer, object value)
        {
            switch (value)
            {
                case bool b: writer.Write(b); break;
                case byte b: writer.Write(b); break;
                case int i: writer.Write(i); break;
                case float f: writer.Write(f); break;
                case string s: writer.Write(s); break;
                default: throw new NotSupportedException($"[ManactorRpc] Unsupported argument type: {value?.GetType()}");
            }
        }
    }
}
