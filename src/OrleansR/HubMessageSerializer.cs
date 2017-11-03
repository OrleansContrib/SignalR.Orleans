using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using OrleansR.Clients;
using OrleansR.Groups;
using Orleans.Runtime;
using Orleans.Serialization;

namespace OrleansR
{
    public class HubMessageSerializer : IExternalSerializer
    {
        private readonly IReadOnlyList<Type> _supportedType = new List<Type> { typeof(GroupMessage), typeof(AllMessage), typeof(ClientMessage) };
        private readonly ILBasedSerializer _serializer;

        public HubMessageSerializer(IServiceProvider serviceProvider)
        {
            this._serializer = ActivatorUtilities.CreateInstance<ILBasedSerializer>(serviceProvider);
        }

        public object DeepCopy(object source, ICopyContext context) => this._serializer.DeepCopy(source, context);

        public object Deserialize(Type expectedType, IDeserializationContext context) => this._serializer.Deserialize(expectedType, context);

        public void Initialize(Logger logger) => this._serializer.Initialize(logger);

        public bool IsSupportedType(Type itemType) => this._supportedType.Contains(itemType);

        public void Serialize(object item, ISerializationContext context, Type expectedType) => this._serializer.Serialize(item, context, expectedType);
    }
}