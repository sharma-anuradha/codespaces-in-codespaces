using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Define a universal hub that will dispatch method invocation into other hub types
    /// </summary>
    public class SignalRHub : Hub
    {
        private readonly Dictionary<string, HubDispatcher> hubDispatchers;
        private readonly IServiceScopeFactory serviceScopeFactory;

        private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public SignalRHub(
            IServiceScopeFactory serviceScopeFactory, 
            IEnumerable<HubDispatcher> hubDispatchers)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.hubDispatchers = hubDispatchers.ToDictionary(d => d.HubName, d => d);
        }

        public override async Task OnConnectedAsync()
        {
            foreach(var hubDispatcher in hubDispatchers.Values)
            {
                await HubCallbackAsync(hubDispatcher, async (hub) =>
                {
                    await hub.OnConnectedAsync().ConfigureAwait(false);
                });
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            foreach (var hubDispatcher in hubDispatchers.Values)
            {
                await HubCallbackAsync(hubDispatcher, async (hub) =>
                {
                    await hub.OnDisconnectedAsync(exception).ConfigureAwait(false);
                });
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<object> InvokeHubMethodAsync(string hubAndMethod, object[] arguments)
        {
            var s = hubAndMethod.Split('.');
            if (s.Length != 2)
            {
                throw new HubException($"Invalid value:{hubAndMethod} for paremeter:{nameof(hubAndMethod)}");
            }

            var hubName = s[0];
            var hubMethodName = s[1];

            HubDispatcher hubDispatcher;
            if (!this.hubDispatchers.TryGetValue(hubName, out hubDispatcher))
            {
                throw new HubException($"Hub:{hubName} not registered when invoking method:{hubMethodName}");
            }

            if (hubDispatcher.TryGetMethod(hubMethodName, out var methodInfo))
            {
                if (arguments.Length != methodInfo.GetParameters().Length)
                {
                    throw new HubException($"Wrong number of arguments, expected:{methodInfo.GetParameters().Length} actual:{arguments.Length} hubAndMethod:{hubAndMethod}");
                }

                // convert argument types
                for (int index = 0; index < arguments.Length; ++index )
                {
                    arguments[index] = ToArgumentType(arguments[index], methodInfo.GetParameters()[index].ParameterType);
                }

                object result = null;

                await HubCallbackAsync(hubDispatcher, async (hub) =>
                {
                    result = methodInfo.Invoke(hub, arguments);
                    if (result is Task task)
                    {
                        await task.ConfigureAwait(false);
                        if (task.GetType().IsGenericType)
                        {
                            result = task.GetType().GetProperty("Result").GetValue(task);
                        }
                    }
                });

                return result;
            }
            else
            {
                throw new ArgumentException($"Method:{hubMethodName} not found");
            }
        }

        private static object ToArgumentType(object value, Type argumentType)
        {
            if (argumentType.IsEnum)
            {
                return Enum.ToObject(argumentType, value);
            }
            else if (value is JToken jToken)
            {
                return jToken.ToObject(argumentType);
            }
            else if (value is JArray jArray)
            {
                if (!argumentType.IsArray)
                {
                    throw new ArgumentException();
                }

                var array = Array.CreateInstance(argumentType.GetElementType(), jArray.Count);
                int index = 0;
                foreach (var item in jArray)
                {
                    array.SetValue(ToArgumentType(item, argumentType.GetElementType()), index++);
                }

                return array;
            }
            else if (argumentType == typeof(byte[]) && value is string)
            {
                return Convert.FromBase64String((string)value);
            }
            else if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    var array = Array.CreateInstance(argumentType.GetElementType(), jsonElement.GetArrayLength());
                    int index = 0;
                    foreach (var item in jsonElement.EnumerateArray())
                    {
                        array.SetValue(ToObject(item, argumentType.GetElementType()), index++);
                    }

                    return array;
                }
                else if (argumentType == typeof(Dictionary<string, object>))
                {
                    var obj = new Dictionary<string, object>();
                    foreach (var property in jsonElement.EnumerateObject())
                    {
                        obj[property.Name] = ToObject(property.Value, ToType(property.Value.ValueKind));
                    }

                    return obj;
                }

                return ToObject(jsonElement, argumentType);
            }

            return value;
        }

        private static object ToObject(JsonElement jsonElement, Type argumentType)
        {
            if (jsonElement.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (argumentType == typeof(bool))
            {
                return jsonElement.GetBoolean();
            }
            else if (argumentType == typeof(sbyte))
            {
                return jsonElement.GetSByte();
            }
            else if (argumentType == typeof(byte))
            {
                return jsonElement.GetByte();
            }
            else if (argumentType == typeof(ushort))
            {
                return jsonElement.GetUInt16();
            }
            else if (argumentType == typeof(short))
            {
                return jsonElement.GetInt16();
            }
            if (argumentType == typeof(uint))
            {
                return jsonElement.GetUInt32();
            }
            else if (argumentType == typeof(int))
            {
                return jsonElement.GetInt32();
            }
            else if (argumentType == typeof(double))
            {
                return jsonElement.GetDouble();
            }
            else if (argumentType == typeof(string))
            {
                return jsonElement.GetString();
            }
            else if (argumentType == typeof(object))
            {
                var typeObj = ToType(jsonElement.ValueKind);
                if (typeObj != typeof(object))
                {
                    return ToObject(jsonElement, typeObj);
                }

                return jsonElement;
            }

            return JsonSerializer.Deserialize(jsonElement.ToString(), argumentType, JsonSerializerOptions);
        }

        private static Type ToType(JsonValueKind jsonValueKind)
        {
            switch(jsonValueKind)
            {
                case JsonValueKind.Array:
                    return typeof(object[]);
                case JsonValueKind.String:
                    return typeof(string);
                case JsonValueKind.Number:
                    return typeof(double);
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return typeof(bool);
                default:
                    return typeof(object);
            }
        }

        private async Task HubCallbackAsync(HubDispatcher hubDispatcher, Func<Hub, Task> hubCallback )
        {
            IServiceScope scope = null;
            try
            {
                scope = this.serviceScopeFactory.CreateScope();

                var hub = (Hub)scope.ServiceProvider.GetService(hubDispatcher.HubType);
                if (hub == null)
                {
                    hub = (Hub)ActivatorUtilities.CreateFactory(hubDispatcher.HubType, Type.EmptyTypes)(scope.ServiceProvider, Array.Empty<object>());
                }

                try
                {
                    InitializeHub(hub);

                    await hubCallback(hub).ConfigureAwait(false);
                }
                finally
                {
                    hub.Dispose();
                }
            }
            finally
            {
                await DisposeAsync(scope);
            }
        }

        private void InitializeHub(Hub hub)
        {
            hub.Clients = this.Clients;
            hub.Context = this.Context;
            hub.Groups = this.Groups;
        }

        private static ValueTask DisposeAsync(IDisposable disposable)
        {
#if _NETCORE3
            if (disposable is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }
#endif
            disposable.Dispose();
            return default;
        }
    }
}
