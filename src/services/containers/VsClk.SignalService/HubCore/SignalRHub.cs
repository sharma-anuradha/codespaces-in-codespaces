// <copyright file="SignalRHub.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsCloudKernel.SignalService.Common;
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

        public SignalRHub(
            IServiceScopeFactory serviceScopeFactory,
            IEnumerable<HubDispatcher> hubDispatchers)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.hubDispatchers = hubDispatchers.ToDictionary(d => d.HubName, d => d);
        }

        public override async Task OnConnectedAsync()
        {
            foreach (var hubDispatcher in hubDispatchers.Values)
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
                for (int index = 0; index < arguments.Length; ++index)
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
                        if (task.GetType().IsGenericType && methodInfo.ReturnType != typeof(Task))
                        {
                            result = task.GetType().GetProperty("Result").GetValue(task);
                        }
                        else
                        {
                            // void result to ensure serializer will do the right thing
                            result = null;
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

        private static ValueTask DisposeAsync(IDisposable disposable)
        {
            if (disposable is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }

            disposable.Dispose();
            return default;
        }

        private static object ToArgumentType(object value, Type argumentType)
        {
            if (argumentType.IsEnum)
            {
                return Enum.ToObject(argumentType, value);
            }
            else if (value is JToken jToken)
            {
                return NewtonsoftHelpers.ToObject(jToken, argumentType);
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
                return JsonHelpers.ConvertTo(jsonElement, argumentType);
            }
            else if (argumentType != typeof(object) && value is Dictionary<object, object> objectProperties)
            {
                if (typeof(IDictionary<string, object>).IsAssignableFrom(argumentType))
                {
                    value = objectProperties.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
                }
                else
                {
                    value = CreateArgument(objectProperties, argumentType);
                }
            }
            else if (argumentType.IsArray && value is object[] objectPropertiesArray)
            {
                var itemType = argumentType.GetElementType();
                var array = Array.CreateInstance(itemType, objectPropertiesArray.Length);
                int index = 0;
                foreach (var item in objectPropertiesArray)
                {
                    if (item == null)
                    {
                        array.SetValue(null, index);
                    }
                    else if (itemType.IsAssignableFrom(item.GetType()))
                    {
                        array.SetValue(item, index);
                    }
                    else if (item is Dictionary<object, object> itemObjectProperties)
                    {
                        object itemValue;
                        if (typeof(IDictionary<string, object>).IsAssignableFrom(itemType))
                        {
                            itemValue = itemObjectProperties.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
                        }
                        else
                        {
                            itemValue = CreateArgument(itemObjectProperties, itemType);
                        }

                        array.SetValue(itemValue, index);
                    }
                    else
                    {
                        throw new ArgumentException($"expecting Dictionary<object, object> on index:{index}");
                    }

                    ++index;
                }

                value = array;
            }

            return value;
        }

        /// <summary>
        /// Create an argument instance based on Dictionary deserialized by the message pack.
        /// </summary>
        /// <param name="objectProperties">The deserialized dictionary.</param>
        /// <param name="argumentType">Expected struct/class type.</param>
        /// <returns>The object instance.</returns>
        private static object CreateArgument(Dictionary<object, object> objectProperties, Type argumentType)
        {
            var argumentValue = Activator.CreateInstance(argumentType);
            foreach (var kvp in objectProperties)
            {
                GetProperty(kvp.Key.ToString(), argumentType).SetValue(argumentValue, kvp.Value);
            }

            return argumentValue;
        }

        private static System.Reflection.PropertyInfo GetProperty(string propertyName, Type argumentType)
        {
            var propertyInfo = argumentType.GetProperty(propertyName) ?? argumentType.GetProperty(propertyName.ToPascalCase());
            if (propertyInfo == null)
            {
                throw new ArgumentException($"Property:{propertyName} not found on target type:{argumentType.Name}");
            }

            return propertyInfo;
        }

        private async Task HubCallbackAsync(HubDispatcher hubDispatcher, Func<Hub, Task> hubCallback)
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
    }
}
