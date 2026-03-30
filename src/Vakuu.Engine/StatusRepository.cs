using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Vakuu.Engine.Statuses;

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
            applicationOrder = new IStatus[]
            {
                StatusesByType[typeof(Strength)],
                StatusesByType[typeof(Vulnerable)],
                StatusesByType[typeof(Block)],
            };
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
