using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Vakuu.Engine
{
    internal static class StatusRepository
    {
        static readonly Dictionary<Type, IStatus> StatusesByType;
        static readonly IStatus[] applicationOrder;

        static StatusRepository()
        {
            var statusType = typeof(IStatus);
            StatusesByType = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type => type != statusType && statusType.IsAssignableFrom(type))
                .ToDictionary(type => type, type => (IStatus)Activator.CreateInstance(type)!);

            // TODO: 
            applicationOrder = StatusesByType.Values.ToArray();
        }

        public static IStatus Get<TStatus>()
            where TStatus : IStatus
            => StatusesByType[typeof(TStatus)];

        public static void Apply(Action<IStatus> applicator)
        {
            foreach (var status in applicationOrder)
                applicator(status);
        }
    }
}
